# Интеграция IsaacDungeonLayout в Godot 4 (C#)

Этот документ — практическое руководство по встраиванию библиотеки в проект **Godot 4.x с C#** (как в репозитории LudumDareJam: папка `IsaacDungeonLayout` в корне рядом с `project.godot`).

Общая архитектура и термины: [Documentation.md](Documentation.md). Быстрый старт с `dotnet`: [docs/GettingStarted.md](docs/GettingStarted.md).

---

## 1. Сборка и ссылка на проект

### Вариант A — папка уже в дереве репо (текущий случай)

Если `IsaacDungeonLayout` лежит рядом с основным `.csproj` Godot, в **корневом** `.csproj` игры добавьте:

```xml
<ItemGroup>
  <ProjectReference Include="IsaacDungeonLayout\IsaacDungeonLayout.csproj" />
</ItemGroup>
```

Путь поправьте под вашу структуру. После этого в коде игры доступен `using IsaacDungeonLayout;`.

### Вариант B — копия только библиотеки

Скопируйте каталог `IsaacDungeonLayout` **без** `bin/`, `obj/`, `.vs/` и укажите `ProjectReference` на скопированный `.csproj`.

### Про `OutputType` библиотеки

`IsaacDungeonLayout.csproj` сейчас с **`OutputType` Exe** из‑за консольного `Program.cs` (раннер тестов). Для Godot это обычно не мешает: движок линкует проект как зависимость. Если понадобится «чистая» DLL без точки входа — вынесите тесты в отдельный `.csproj` и смените `OutputType` на `Library` (решение команды).

---

## 2. Узлы-шаблоны комнат (`RoomScene`)

На корень сцены комнаты (например `Node3D`) вешается C#-скрипт с экспортами:

```csharp
using Godot;
using Godot.Collections;

public partial class RoomScene : Node3D
{
    [Export] public string RoomType = "base"; // "base" | "start" | "end" | "mob"

    /// <summary>Локальные направления выходов: (1,0), (-1,0), (0,1), (0,-1) — маппятся в Int2(X,Z).</summary>
    [Export] public Array<Vector2I> OutsDir = [];
}
```

Требования алгоритма к шаблонам совпадают с [`RoomTemplate.Validate`](Core/RoomTemplate.cs): кардинальные векторы, для Base 2–4 выхода, для Start/End/Mob ровно **один** выход.

---

## 3. Сборка `RoomTemplate` из сцен

Типичный фрагмент (как в прототипе `LevelGenerator`):

```csharp
var templates = new List<RoomTemplate>();
foreach (var scene in RoomScenes)
{
    var instance = scene.Instantiate<RoomScene>();
    var roomTypeVal = instance.RoomType switch
    {
        "start" => RoomType.Start,
        "end" => RoomType.End,
        "mob" => RoomType.Mob,
        _ => RoomType.Base,
    };
    var outs = instance.OutsDir.Select(v => new Int2(v.X, v.Y)).ToArray();
    templates.Add(new RoomTemplate
    {
        Id = instance.Name,
        Type = roomTypeVal,
        OutsNum = outs.Length,
        OutsDir = outs,
    });
    instance.QueueFree();
}
```

`RoomTemplate.Id` должен совпадать с тем, что вы используете как ключ кэша сцен (`instance.Name` / имя файла сцены).

---

## 4. Генерация с нуля и спавн

```csharp
var config = new DungeonGenerationConfig
{
    Templates = templates,
    BaseRoomCount = 15,
    MobRoomCount = 4,
    Seed = 42,
    MaxAttempts = 200,
    DiagnosticLog = GD.Print,
};

var generator = new DungeonGenerator();
var outcome = generator.Generate(config);
if (!outcome.Success)
{
    GD.PrintErr(outcome.Failure!.Value.Reason);
    return;
}

var layout = outcome.Result!;
// layout.Source == DungeonLayoutSource.Generated
```

**Позиция и поворот:**

- Мир: `Position = new Vector3(room.GridPosition.X * cellSize, 0, room.GridPosition.Z * cellSize)`.
- Поворот CCW вокруг Y в библиотеке: `Rotation = new Vector3(0, room.RotationSteps90 * (Mathf.Pi / 2f), 0)` — при несовпадении с моделью попробуйте инвертировать знак (см. раздел про оси ниже).

---

## 5. Метаданные на инстансе (`GameplayMetadata`)

После `Generate` / `Shuffle` у каждой `PlacedRoom` заполнено `GameplayMetadata`. Удобно пробросить в Godot через `SetMeta` (не требует GDScript):

```csharp
foreach (var room in layout.Rooms)
{
    var instance = scene.Instantiate<Node3D>();
    // ... position, rotation ...
    if (room.GameplayMetadata is { } meta)
    {
        instance.SetMeta("gameplay_dist_from_start", meta.DistanceFromStartEdges);
        instance.SetMeta("gameplay_dist_to_end", meta.DistanceToEndEdges);
        instance.SetMeta("gameplay_on_shortest_path", meta.OnShortestPathStartToEnd);
        instance.SetMeta("gameplay_dist_to_nearest_mob", meta.DistanceToNearestMobEdges);
        foreach (var kv in meta.NeighborCountByType)
            instance.SetMeta($"gameplay_neighbor_{kv.Key}", kv.Value);
    }
    AddChild(instance);
}
```

Список ключей и смысл полей: [docs/RoomGameplayMetadata.md](docs/RoomGameplayMetadata.md).

---

## 6. Режим Shuffle из Godot

1. Соберите **`HashSet<Int2>`** всех занятых клеток данжа (в той же сетке, что и при генерации).
2. Задайте **`StartPosition`** — клетка старта игрока.
3. Постройте **`List<RoomSlotDescriptor>`**: по одному слоту на клетку, ровно один `RoomType.Start`, один `End`, остальные `Base`/`Mob` так, чтобы **число слотов степени 1** (не-Base) совпало с числом клеток сетки со степенью 1 (см. [ShuffleMode.md](docs/ShuffleMode.md)).
4. Передайте тот же каталог **`RoomTemplate`**, что и для `Generate` (или его подмножество, если слоты жёстко задают `RequiredTemplateId`).
5. Задайте **`Seed`** для воспроизводимого порядка перебора при ничьях по BFS.

```csharp
var input = new ShuffleDungeonInput
{
    OccupiedCells = occupiedFromGame,
    StartPosition = playerStartCell,
    Slots = slotDescriptors,
    Templates = templates,
    Seed = GameSeed,
    DiagnosticLog = GD.Print,
};
var outcome = generator.Shuffle(input);
```

Проверка вручную: `DungeonValidator.ValidateShuffledOrThrow(layout, ShuffleTypeExpectation.FromSlots(slots), occupiedFromGame)`.

---

## 7. Оси и повороты (важно)

В библиотеке:

- Ось **X** — восток/запад, **Z** — север/юг на плоскости комнат.
- `RotationSteps90` — четверти оборота **CCW** вокруг Y в мировой системе шаблона.

В Godot 3D: привычная сцена может смотреть в **-Z** «вперёд». Сверьте один раз визуально стыковку коридоров; при необходимости инвертируйте `rotationY` или отразите экспорт `OutsDir` в сценах.

---

## 8. Чеклист перед релизом уровня

- [ ] В пуле шаблонов есть **все** нужные степени для баз (2, 3, 4 выходов).
- [ ] `MaxAttempts` и таймаут на UX при неудачной генерации.
- [ ] Для shuffle: граф **связен**, слоты согласованы с `GridBfs.CellDegree` (см. валидацию входа).
- [ ] Прогнать `dotnet run -- test` в каталоге `IsaacDungeonLayout` перед мержем.

---

## 9. Дальнейшее развитие (идеи)

- Общий базовый класс `RoomBase` / `RoomScene` в игре: метод `ToSlotDescriptor()` для shuffle.
- Вынести консольные тесты в отдельный `*.Tests.csproj` и оставить библиотеку как `Library`.
- Асинхронный / пошаговый shuffle с отменой, если граф большой.
