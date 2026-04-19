namespace IsaacDungeonLayout;

/// <summary>Кардинальные направления в плоскости XZ. Поворот — вокруг оси Y (вид сверху), шаг = 90° CCW.</summary>
public enum Direction
{
    East = 0,  // (+1, 0)
    North = 1, // (0, +1)
    West = 2,  // (-1, 0)
    South = 3, // (0, -1)
}

public static class DirectionExtensions
{
    private static readonly Int2[] Vectors =
    [
        new Int2(1, 0),
        new Int2(0, 1),
        new Int2(-1, 0),
        new Int2(0, -1),
    ];

    public static Int2 ToInt2(this Direction d) => Vectors[(int)d];

    public static Direction RotateSteps(this Direction d, int steps90)
    {
        int n = ((int)d + steps90) & 3;
        return (Direction)n;
    }

    /// <summary>Повернуть вектор на steps90 четвертей (90° CCW в XZ).</summary>
    public static Int2 RotateVector(Int2 v, int steps90)
    {
        steps90 &= 3;
        return steps90 switch
        {
            0 => v,
            1 => new Int2(-v.Z, v.X),
            2 => new Int2(-v.X, -v.Z),
            3 => new Int2(v.Z, -v.X),
            _ => v
        };
    }

    public static Direction FromInt2(Int2 v)
    {
        foreach (Direction d in Enum.GetValues<Direction>())
            if (d.ToInt2() == v)
                return d;
        throw new ArgumentOutOfRangeException(nameof(v), v, "Только кардинальные единичные векторы.");
    }
}
