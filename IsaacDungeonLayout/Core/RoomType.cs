namespace IsaacDungeonLayout;

public enum RoomType
{
    Base,
    Mob,
    Start,
    End,
    /// <summary>Тупиковая заглушка (1 выход); может добавляться планировщиком вне колоды <see cref="DungeonGenerationConfig.TemplateUsageCapsById"/>.</summary>
    Plug,
}
