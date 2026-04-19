namespace IsaacDungeonLayout;

/// <summary>Клетка сетки: X — горизонталь, Z — вертикаль (как в ТЗ).</summary>
public readonly record struct Int2(int X, int Z)
{
    public static Int2 Zero => new(0, 0);

    public static Int2 operator +(Int2 a, Int2 b) => new(a.X + b.X, a.Z + b.Z);

    public static Int2 operator -(Int2 a, Int2 b) => new(a.X - b.X, a.Z - b.Z);

    public override string ToString() => $"({X},{Z})";
}
