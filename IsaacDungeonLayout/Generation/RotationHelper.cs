namespace IsaacDungeonLayout;

public static class RotationHelper
{
    public static IReadOnlyList<Int2> RotateDirections(IReadOnlyList<Int2> dirs, int steps90)
    {
        int q = GridSteps.QuarterTurns;
        steps90 = ((steps90 % q) + q) % q;
        if (steps90 == 0)
            return dirs;
        var list = new Int2[dirs.Count];
        for (int i = 0; i < dirs.Count; i++)
            list[i] = DirectionExtensions.RotateVector(dirs[i], steps90);
        return list;
    }
}
