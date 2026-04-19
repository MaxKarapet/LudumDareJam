namespace IsaacDungeonLayout;

public static class DemoTemplates
{
    public static IReadOnlyList<RoomTemplate> BuildDefault() =>
    [
        new RoomTemplate
        {
            Id = "base_line",
            Type = RoomType.Base,
            OutsNum = 2,
            OutsDir = [new Int2(1, 0), new Int2(-1, 0)],
        },
        new RoomTemplate
        {
            Id = "base_corner",
            Type = RoomType.Base,
            OutsNum = 2,
            OutsDir = [new Int2(1, 0), new Int2(0, 1)],
        },
        new RoomTemplate
        {
            Id = "base_tee",
            Type = RoomType.Base,
            OutsNum = 3,
            OutsDir = [new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1)],
        },
        new RoomTemplate
        {
            Id = "base_fork",
            Type = RoomType.Base,
            OutsNum = 3,
            OutsDir = [new Int2(1, 0), new Int2(0, 1), new Int2(0, -1)],
        },
        new RoomTemplate
        {
            Id = "base_cross",
            Type = RoomType.Base,
            OutsNum = 4,
            OutsDir = [new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1), new Int2(0, -1)],
        },
        new RoomTemplate
        {
            Id = "special_exit_e",
            Type = RoomType.Start,
            OutsNum = 1,
            OutsDir = [new Int2(1, 0)],
        },
        new RoomTemplate
        {
            Id = "special_exit_e_mob",
            Type = RoomType.Mob,
            OutsNum = 1,
            OutsDir = [new Int2(1, 0)],
        },
        new RoomTemplate
        {
            Id = "special_exit_e_end",
            Type = RoomType.End,
            OutsNum = 1,
            OutsDir = [new Int2(1, 0)],
        },
    ];
}
