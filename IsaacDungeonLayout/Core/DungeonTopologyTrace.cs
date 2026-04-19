namespace IsaacDungeonLayout;

/// <summary>Данные для проверки доменных инвариантов топологии (max дистанция пары листьев, maximin mob).</summary>
public sealed record DungeonTopologyTrace
{
    public required IReadOnlyList<Int2> BaseCellPositions { get; init; }
    public required IReadOnlyList<Int2> LeafSlotIterationOrder { get; init; }
    public required IReadOnlyList<Int2> MobPlacementOrder { get; init; }
    /// <summary>Клетки типа <see cref="RoomType.Plug"/>, добавленные после базового плана (инварианты leaf/maximin к ним не применяются).</summary>
    public IReadOnlyList<Int2> PlugCellPositions { get; init; } = Array.Empty<Int2>();

    public override string ToString() =>
        $"TopologyTrace(bases={BaseCellPositions.Count}, leafSlots={LeafSlotIterationOrder.Count}, mobSteps={MobPlacementOrder.Count}, plugs={PlugCellPositions.Count})";
}
