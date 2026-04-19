namespace IsaacDungeonLayout;

/// <summary>Результат размещения одной комнаты (DTO для Godot и др.).</summary>
public sealed class PlacedRoom
{
    public required string TemplateId { get; init; }
    public required RoomType RoomType { get; init; }
    public required Int2 GridPosition { get; init; }
    /// <summary>0..3 четверти оборота CCW вокруг Y относительно локали шаблона.</summary>
    public required int RotationSteps90 { get; init; }
    public required IReadOnlyList<Int2> FinalOutsDir { get; init; }
    public required IReadOnlyList<Int2> ConnectedNeighborPositions { get; init; }

    /// <summary>Заполняется <see cref="DungeonLayoutEnricher"/> после успешной валидации.</summary>
    public RoomGameplayMetadata? GameplayMetadata { get; init; }

    public override string ToString() =>
        $"{RoomType} @ {GridPosition} tmpl={TemplateId} rot={RotationSteps90}";
}
