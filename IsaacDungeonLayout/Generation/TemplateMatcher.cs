namespace IsaacDungeonLayout;

public static class TemplateMatcher
{
    /// <summary>Подбор шаблона и поворота под требуемые мировые направления (множество рёбер к соседям).</summary>
    public static bool TryMatch(
        RoomType type,
        IReadOnlyList<RoomTemplate> templates,
        HashSet<Int2> requiredWorldDirs,
        out RoomTemplate? template,
        out int rotationSteps90)
    {
        template = null;
        rotationSteps90 = 0;
        foreach (var t in templates.Where(x => x.Type == type).OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            for (int r = 0; r < GridSteps.QuarterTurns; r++)
            {
                var rotated = RotationHelper.RotateDirections(t.OutsDir, r);
                if (SetEquals(rotated, requiredWorldDirs))
                {
                    template = t;
                    rotationSteps90 = r;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Все пары (шаблон, поворот), подходящие под множество мировых направлений к соседям.</summary>
    public static IEnumerable<(RoomTemplate Template, int RotationSteps90)> EnumerateMatchesForDirections(
        IReadOnlyList<RoomTemplate> templates,
        HashSet<Int2> requiredWorldDirs)
    {
        foreach (var t in templates.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            for (int r = 0; r < GridSteps.QuarterTurns; r++)
            {
                var rotated = RotationHelper.RotateDirections(t.OutsDir, r);
                if (SetEquals(rotated, requiredWorldDirs))
                    yield return (t, r);
            }
        }
    }

    public static HashSet<Int2> RequiredDirectionsFromNeighbors(Int2 pos, HashSet<Int2> allCells)
    {
        var set = new HashSet<Int2>();
        foreach (var d in GridSteps.Cardinal)
        {
            if (allCells.Contains(pos + d))
                set.Add(d);
        }

        return set;
    }

    private static bool SetEquals(IReadOnlyList<Int2> a, HashSet<Int2> b)
    {
        if (a.Count != b.Count)
            return false;
        foreach (var x in a)
        {
            if (!b.Contains(x))
                return false;
        }

        return true;
    }
}
