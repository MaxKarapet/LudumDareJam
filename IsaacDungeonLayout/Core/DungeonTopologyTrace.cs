namespace IsaacDungeonLayout;

/// <summary>Данные для проверки доменных инвариантов топологии (max дистанция пары листьев, maximin mob).</summary>
public sealed record DungeonTopologyTrace(
    IReadOnlyList<Int2> BaseCellPositions,
    IReadOnlyList<Int2> LeafSlotIterationOrder,
    IReadOnlyList<Int2> MobPlacementOrder)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"TopologyTrace(bases={BaseCellPositions.Count}, leafSlots={LeafSlotIterationOrder.Count}, mobSteps={MobPlacementOrder.Count})";
}
