namespace IsaacDungeonLayout;

/// <summary>Единое сравнение для детерминированных tie-break в планировщике и реплее валидатора.</summary>
internal static class Int2Comparers
{
    internal static int Lexical(Int2 a, Int2 b)
    {
        int c = a.X.CompareTo(b.X);
        return c != 0 ? c : a.Z.CompareTo(b.Z);
    }
}
