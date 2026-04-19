namespace IsaacDungeonLayout;

public sealed class DungeonLayout
{
    public required IReadOnlyList<PlacedRoom> Rooms { get; init; }
    public required Int2 StartPosition { get; init; }
    public required Int2 EndPosition { get; init; }
    public required IReadOnlyList<Int2> MobPositions { get; init; }
    public required int StartEndGraphDistance { get; init; }
    /// <summary>Снимок топологии для проверки инвариантов (max leaf-пара, maximin mob).</summary>
    public required DungeonTopologyTrace Topology { get; init; }

    public override string ToString() =>
        $"Rooms={Rooms.Count}, SE_dist={StartEndGraphDistance}, Start={StartPosition}, End={EndPosition}, Mobs={MobPositions.Count}";
}

public readonly record struct DungeonGenerationFailure(string Reason);

public readonly record struct DungeonGenerationOutcome(
    bool Success,
    DungeonLayout? Result,
    DungeonGenerationFailure? Failure,
    int AttemptsUsed)
{
    public static DungeonGenerationOutcome Ok(DungeonLayout r, int attempts) =>
        new(true, r, null, attempts);

    public static DungeonGenerationOutcome Fail(string reason, int attempts) =>
        new(false, null, new DungeonGenerationFailure(reason), attempts);
}
