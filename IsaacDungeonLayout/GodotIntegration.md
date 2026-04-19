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

В [`LevelGenerator`](LevelGenerator.cs) это сведено в `TryBuildDeckFromRoomScenes`: уникальный каталог по `instance.Name` + словарь `TemplateUsageCapsById` (сколько раз каждый `Id` встречается в `RoomScenes`). Ниже — тот же цикл «вручную» для кастомного кода:

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

Размер данжа и «сколько раз какая сцена» задаются **мультимножеством** `RoomScenes` в [`LevelGenerator`](LevelGenerator.cs): каждый элемент массива — один экземпляр в колоде (одинаковые сцены с тем же корневым `Name` дают несколько использований одного `RoomTemplate.Id`). `BaseRoomCount` / `MobRoomCount` в конфиг считаются из числа сцен с типами `base` / `mob`; в массиве должно быть **ровно одно** `start` и **ровно одно** `end`.

Чтобы генератор не подбирал шаблоны «сколько угодно раз» из каталога, передайте лимиты:

```csharp
var catalog = /* уникальные RoomTemplate по Id */;
var caps = new Dictionary<string, int>(StringComparer.Ordinal) { ["room_a"] = 2, ["start"] = 1, ... }; // сумма = число комнат
var config = new DungeonGenerationConfig
{
    Templates = catalog,
    BaseRoomCount = baseFromDeck,
    MobRoomCount = mobFromDeck,
    Seed = 42,
    MaxAttempts = 500,
    DiagnosticLog = GD.Print,
    TemplateUsageCapsById = caps,
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

### 4.1. Заглушки Plug и проверка колоды

- Отдельный [`Export`](LevelGenerator.cs) **`PlugScene`**: тупиковая комната (1 выход), тип в коде `RoomType.Plug`. Она **не** входит в `RoomScenes` и не расходует `TemplateUsageCapsById`. Если `PlugScene` задан, [`DungeonGenerator`](Generation/DungeonGenerator.cs) при провале подбора шаблонов может **добавлять клетки Plug** у границы полимино (до `MaxTopologyPlugExpansions`), пока не удастся назначить шаблоны.
- В `RoomScenes` строку `plug` для типа комнаты **не** используйте — только `PlugScene`.
- Перед ретраями вызывается [`DeckFeasibility`](Generation/DeckFeasibility.cs): заведомый провал (например `n=1` без base с 2 выходами) возвращается сразу; мягкие предупреждения пишутся в `DiagnosticLog`.

Поля конфига: `PlugTemplateId`, `AllowTopologyPlugExpansion`, `MaxTopologyPlugExpansions` (см. [`DungeonGenerationConfig`](Core/DungeonGenerationConfig.cs)).

**Теория и обобщение:** в текущей реализации Plug — это всегда клетка **степени 1** в графе (один выход в сторону «основного» данжа). В принципе тот же приём можно распространить на **заглушки под 2, 3 или 4** инцидентных ребра: отдельные маленькие комнаты/коридоры с шаблоном степени 2…4, которые «съедают» лишнюю степень соседа и оставляют граф валидным. Тогда задача смещается в **минимизацию числа таких вспомогательных клеток** (и шаблонов): чем меньше заглушек, тем ближе топология к «чистой» колоде из `RoomScenes`. Реализация в библиотеке пока заточена под тупик Plug×1; расширение до 2/3/4 — отдельный шаг планировщика и валидации.

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

### Что такое `OccupiedCells`

Это **множество вершин графа комнат** в дискретной сетке: каждая клетка `Int2(X, Z)`, где уже стоит ровно одна комната данжа — те же координаты, что потом попадут в `PlacedRoom.GridPosition`. Это не «занято ли место для коллизий» и не пиксели: шаг сетки вы задаёте сами (в прототипе совпадает с тем, как вы переводите `GridPosition` в `Position` при спавне).

Откуда брать значения:

- После **`Generate`**: `new HashSet<Int2>(layout.Rooms.Select(r => r.GridPosition))`.
- Из сцены Godot: те же индексы, что вы получаете из `room.Position` обратным преобразованием, например `Round(Position.X / cellSize)` и `Round(Position.Z / cellSize)` — так сделано в [`LevelGenerator.TryShuffleCurrentLayout`](LevelGenerator.cs).

Число элементов `OccupiedCells` должно совпадать с числом слотов; граф по 4-соседству должен быть связным (см. `ShuffleDungeonInput.Validate`).

### Ручная сборка входа

1. Соберите **`HashSet<Int2>`** всех занятых клеток данжа (в той же сетке, что и при генерации).
2. Задайте **`StartPosition`** — клетка старта игрока.
3. Постройте **`List<RoomSlotDescriptor>`**: по одному слоту на клетку, ровно один `RoomType.Start`, один `End`, остальные `Base`/`Mob`/`Plug` так, чтобы **число слотов степени 1** (не-Base) совпало с числом клеток сетки со степенью 1 (см. [ShuffleMode.md](docs/ShuffleMode.md)).
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

### Готовый вызов из `LevelGenerator`

Если комнаты уже размещены как потомки с корнем [`RoomScene`](LevelGenerator.cs) (как после `SpawnRooms` в том же примере), shuffle и возврат `outcome` уже обёрнуты:

```csharp
var outcome = levelGenerator.TryShuffleCurrentLayout(gameSeed: 12345);
if (!outcome.Success)
{
    GD.PrintErr(outcome.Failure!.Value.Reason);
    return;
}
// outcome.Result — новый DungeonLayout (Source == Shuffled)

// Обновить уже существующие ноды (позиции клеток те же; сцена меняется только при смене TemplateId):
// var outcome2 = levelGenerator.TryShuffleCurrentLayoutInPlace(gameSeed: 12345);
// Полное удаление и пересоздание (старое поведение):
// var outcome3 = levelGenerator.TryShuffleCurrentLayoutWithFullRespawn(gameSeed: 12345);
```

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
- [ ] Прогнать `dotnet run -- test` в каталоге `IsaacDungeonLayout` перед мержем (включает `deck`: DeckFeasibility + PlugExpander).

---

## 9. Дальнейшее развитие (идеи)

- Общий базовый класс `RoomBase` / `RoomScene` в игре: метод `ToSlotDescriptor()` для shuffle.
- Вынести консольные тесты в отдельный `*.Tests.csproj` и оставить библиотеку как `Library`.
- Асинхронный / пошаговый shuffle с отменой, если граф большой.
