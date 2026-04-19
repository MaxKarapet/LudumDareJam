# Режим перестановки (`Shuffle`)

Общая документация: [Documentation.md](../Documentation.md) · быстрый старт: [GettingStarted.md](GettingStarted.md) · Godot: [GodotIntegration.md](../GodotIntegration.md).

Генерация «с нуля» ([`DungeonGenerator.Generate`](Generation/DungeonGenerator.cs)) строит топологию через [`TopologyPlanner`](Generation/TopologyPlanner.cs) и проверяет её через [`DungeonValidator.ValidateGenerated`](Validation/DungeonValidator.cs) (листья полимино, maximin mob и т.д.).

Режим **shuffle** другой:

- На входе уже известен **набор занятых клеток** и **фиксированная клетка старта**.
- Есть **пул слотов** [`RoomSlotDescriptor`](Core/RoomSlotDescriptor.cs) — по одному на каждую клетку; ровно один слот типа `Start`, один `End`, остальные `Base` / `Mob` в соответствии со **степенями сетки** клеток (степень 1 → не-Base; степень 2..4 → только Base).
- Алгоритм ([`DungeonShuffleSolver`](Generation/DungeonShuffleSolver.cs)) подбирает **биекцию** слот↔клетка и шаблоны/поворота через [`TemplateMatcher`](Generation/TemplateMatcher.cs), при этом **финиш** ставится на клетку с **максимальной BFS-дистанцией** от старта среди успешных решений (кандидаты — клетки степени 1, не старт).

Результат — тот же [`DungeonLayout`](Core/DungeonLayout.cs) с `Source = DungeonLayoutSource.Shuffled` и пустой [`DungeonTopologyTrace`](Core/DungeonTopologyTrace.cs) (инварианты планировщика к shuffle не применяются). Проверка: [`DungeonValidator.ValidateShuffled`](Validation/DungeonValidator.cs) с [`ShuffleTypeExpectation.FromSlots`](Core/ShuffleDungeonInput.cs) (учитывает слоты `RoomType.Plug` как клетки степени 1). Третий аргумент `expectedOccupiedCells` (опционально) сверяет множество позиций комнат с исходным графом.

**`ShuffleDungeonInput.Seed`** — детерминированный tie-break: порядок кандидатов в End при равной BFS-дистанции, порядок остальных клеток в backtracking и порядок перебора индексов слотов. Один и тот же вход с разным `Seed` может дать другое первое допустимое решение (например, другой лист при ничьей по дистанции).

Степень клетки в подграфе: [`GridBfs.CellDegree`](Generation/GridBfs.cs) — общая функция для валидации и shuffle.

## Пример вызова

```csharp
var input = new ShuffleDungeonInput
{
    OccupiedCells = new HashSet<Int2> { new(0, 0), new(1, 0), new(2, 0) },
    StartPosition = new Int2(0, 0),
    Slots =
    [
        new RoomSlotDescriptor { RoomType = RoomType.Start },
        new RoomSlotDescriptor { RoomType = RoomType.Base },
        new RoomSlotDescriptor { RoomType = RoomType.End },
    ],
    Templates = DemoTemplates.BuildDefault(),
    Seed = 123,
};

var outcome = new DungeonGenerator().Shuffle(input);
// после ручной сборки layout:
// DungeonValidator.ValidateShuffled(layout, ShuffleTypeExpectation.FromSlots(slots), occupiedCells);
```

## Godot

Сборка `List<RoomSlotDescriptor>` из инстансов комнат (базовый класс с типом и числом выходов) и вызов `Shuffle` — на стороне игры; библиотека остаётся без зависимости от Godot.

## Тесты

```bash
dotnet run -- shuffle
```

В полном прогоне `dotnet run -- test` тесты shuffle подключаются автоматически.
