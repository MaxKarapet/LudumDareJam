namespace IsaacDungeonLayout;

/// <summary>Слот комнаты для режима shuffle: тип и опционально зафиксированный шаблон.</summary>
public sealed class RoomSlotDescriptor
{
    public required RoomType RoomType { get; init; }

    /// <summary>Если задано, на клетку можно назначить только этот шаблон (поворот подбирается).</summary>
    public string? RequiredTemplateId { get; init; }
}
