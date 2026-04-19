namespace IsaacDungeonLayout;

/// <summary>Игровые метаданные по комнате после генерации (BFS по графу комнат).</summary>
public sealed class RoomGameplayMetadata
{
    /// <summary>Количество рёбер от старта по кратчайшему пути в графе комнат.</summary>
    public required int DistanceFromStartEdges { get; init; }

    /// <summary>Количество рёбер до финиша по кратчайшему пути.</summary>
    public required int DistanceToEndEdges { get; init; }

    /// <summary>Комната лежит хотя бы на одном кратчайшем пути Start→End.</summary>
    public required bool OnShortestPathStartToEnd { get; init; }

    /// <summary>Расстояние до ближайшей клетки типа Mob; -1 если мобов нет.</summary>
    public required int DistanceToNearestMobEdges { get; init; }

    /// <summary>Типы соседей по рёбрам графа (счётчики по <see cref="RoomType"/>).</summary>
    public required IReadOnlyDictionary<RoomType, int> NeighborCountByType { get; init; }
}
