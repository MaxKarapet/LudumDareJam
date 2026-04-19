namespace IsaacDungeonLayout;

/// <summary>Кардинальные шаги сетки и константы алгоритма (без «магических чисел» в логике).</summary>
public static class GridSteps
{
    public const int CardinalCount = 4;
    public const int QuarterTurns = 4;
    /// <summary>Минимум базовых клеток в полимино (1 допустима: к ней цепляются Start/End/Mob на leaf-слотах).</summary>
    public const int MinBaseRoomCount = 1;
    /// <summary>Верхняя граница итераций роста полимино: n * множитель.</summary>
    public const int PolyominoGrowthGuardMultiplier = 400;

    public static readonly Int2[] Cardinal =
    [
        new Int2(1, 0),
        new Int2(-1, 0),
        new Int2(0, 1),
        new Int2(0, -1),
    ];
}
