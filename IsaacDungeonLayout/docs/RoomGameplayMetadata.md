# Метаданные комнат (`RoomGameplayMetadata`)

После успешной генерации и валидации подземелья `DungeonGenerator` вызывает `DungeonLayoutEnricher.Enrich`: для каждой `PlacedRoom` заполняется свойство `GameplayMetadata`.

Граф — это **комнаты как вершины**, рёбра — **соседство по сетке** (4-связность), расстояния считаются в **количестве рёбер** (BFS).

## Поля C# (`IsaacDungeonLayout`)

| Поле | Смысл |
|------|--------|
| `DistanceFromStartEdges` | Кратчайшее число рёбер от старта до этой комнаты. |
| `DistanceToEndEdges` | Кратчайшее число рёбер от этой комнаты до финиша. |
| `OnShortestPathStartToEnd` | `true`, если комната лежит на **хотя бы одном** кратчайшем пути Start→End (эквивалентно `d(S,p)+d(p,E)=d(S,E)`). |
| `DistanceToNearestMobEdges` | Расстояние до ближайшей комнаты типа `Mob`; для клетки-моба — `0`. Если мобов нет — `-1`. |
| `NeighborCountByType` | Сколько соседей по графу имеют тип `Base` / `Mob` / `Start` / `End`. |

## Godot (`LevelGenerator`)

На каждый инстанс комнаты навешиваются метаданные (если `GameplayMetadata` не `null`):

- `gameplay_dist_from_start` — `int`
- `gameplay_dist_to_end` — `int`
- `gameplay_on_shortest_path` — `bool`
- `gameplay_dist_to_nearest_mob` — `int`
- `gameplay_neighbor_Base`, `gameplay_neighbor_Mob`, `gameplay_neighbor_Start`, `gameplay_neighbor_End` — `int` (только для типов, у которых есть хотя бы один сосед)

В GDScript:

```gdscript
if node.has_meta("gameplay_dist_from_start"):
    var d0: int = node.get_meta("gameplay_dist_from_start")
```

## Запуск тестов консоли

Из каталога `IsaacDungeonLayout`:

```bash
dotnet run -- test      # smoke + проверки метаданных
dotnet run -- smoke
dotnet run -- metadata
dotnet run -- stress
dotnet run -- helper
```
