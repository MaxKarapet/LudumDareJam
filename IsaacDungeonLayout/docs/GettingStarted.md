# Быстрый старт и внедрение IsaacDungeonLayout

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) для сборки консольного проекта и тестов.
- Для игры на **Godot 4 + C#**: тот же SDK; в проекте Godot должен быть включён модуль C#.

## Клонирование / расположение в репозитории

Библиотека живёт в каталоге **`IsaacDungeonLayout/`** в корне репо (рядом с `project.godot`). Так Godot подхватывает `.cs` файлы автоматически, если папка не исключена из сборки.

**Не коммитьте** в git каталоги `bin/`, `obj/`, `.vs/` — добавьте их в `.gitignore` в корне вашего репозитория (в LudumDareJam это уже сделано для `IsaacDungeonLayout/bin` и т.д.).

## Сборка и тесты (консоль)

```bash
cd IsaacDungeonLayout
dotnet build
dotnet run -- test      # smoke + metadata + shuffle
dotnet run -- smoke
dotnet run -- stress    # 100 сидов
dotnet run -- helper
dotnet run -- metadata
dotnet run -- shuffle
```

Код возврата `0` — успех. Используйте в CI перед мержем.

## Минимальный пример: генерация с нуля

```csharp
using IsaacDungeonLayout;

var templates = DemoTemplates.BuildDefault(); // или свой список RoomTemplate
var cfg = new DungeonGenerationConfig
{
    Templates = templates,
    BaseRoomCount = 10,
    MobRoomCount = 2,
    Seed = 42,
    MaxAttempts = 400,
    // Опционально: фиксированная «колода» — не больше N раз каждый Id (см. GodotIntegration / LevelGenerator).
    // TemplateUsageCapsById = caps,
};

var gen = new DungeonGenerator();
var outcome = gen.Generate(cfg);
if (!outcome.Success)
{
    Console.WriteLine(outcome.Failure!.Value.Reason);
    return;
}

DungeonLayout layout = outcome.Result!; // уже с GameplayMetadata и Source == Generated
```

## Минимальный пример: shuffle

См. подробности в [ShuffleMode.md](ShuffleMode.md).

```csharp
var occ = new HashSet<Int2> { new(0, 0), new(1, 0), new(2, 0) };
var slots = new RoomSlotDescriptor[]
{
    new() { RoomType = RoomType.Start },
    new() { RoomType = RoomType.Base },
    new() { RoomType = RoomType.End },
};
var input = new ShuffleDungeonInput
{
    OccupiedCells = occ,
    StartPosition = new Int2(0, 0),
    Slots = slots,
    Templates = DemoTemplates.BuildDefault(),
    Seed = 7,
};

var outcome = new DungeonGenerator().Shuffle(input);
```

## Подключение к другому .NET-проекту (не Godot)

1. Скопируйте папку `IsaacDungeonLayout` **или** добавьте submodule / `ProjectReference` на `.csproj`.
2. В целевом `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\IsaacDungeonLayout\IsaacDungeonLayout.csproj" />
</ItemGroup>
```

3. `using IsaacDungeonLayout;` — типы в корневом namespace.

**Замечание:** текущий `IsaacDungeonLayout.csproj` имеет `OutputType` Exe из‑за `Program.cs` (тестовый раннер). Для чистой библиотеки можно вынести тесты в отдельный проект и поставить `OutputType` Library — по договорённости с командой.

## Дальнейшие шаги

| Шаг | Документ |
|-----|----------|
| Архитектура, инварианты, расширения | [Documentation.md](../Documentation.md) |
| Godot: сцены, шаблоны, метаданные на нодах | [GodotIntegration.md](../GodotIntegration.md) |
| Метаданные комнат (BFS, SetMeta) | [RoomGameplayMetadata.md](RoomGameplayMetadata.md) |
| Режим shuffle | [ShuffleMode.md](ShuffleMode.md) |
