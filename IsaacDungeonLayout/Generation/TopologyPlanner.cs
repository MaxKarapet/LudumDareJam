namespace IsaacDungeonLayout;

/// <summary>Фаза 1: занятость клеток и типы комнат без шаблонов.</summary>
internal sealed class TopologyPlan
{
    public required HashSet<Int2> AllCells { get; init; }
    public required Dictionary<Int2, RoomType> CellType { get; init; }
}

internal readonly record struct TopologyBuildResult(TopologyPlan Plan, DungeonTopologyTrace Trace);

internal static class TopologyPlanner
{
    internal static TopologyBuildResult? TryBuild(DungeonGenerationConfig cfg, Random rng)
    {
        if (!TryGrowPolyomino(cfg.BaseRoomCount, rng, out var bases))
            return null;

        var leafSlots = LeafSlotGeometry.EnumerateLeafSlots(bases);
        int specialsNeeded = cfg.MobRoomCount + 2;
        if (leafSlots.Count < specialsNeeded)
            return null;

        if (!TryAssignSpecialRooms(bases, leafSlots, cfg.MobRoomCount, rng,
                out var specials, out var mobPlacementOrder, out var leafIterationOrder))
            return null;

        var allCells = new HashSet<Int2>(bases);
        foreach (var p in specials.Keys)
            allCells.Add(p);

        var cellType = new Dictionary<Int2, RoomType>();
        foreach (var b in bases)
            cellType[b] = RoomType.Base;
        foreach (var kv in specials)
            cellType[kv.Key] = kv.Value;

        var plan = new TopologyPlan { AllCells = allCells, CellType = cellType };
        var baseList = bases.OrderBy(p => p.X).ThenBy(p => p.Z).ToList();
        var trace = new DungeonTopologyTrace
        {
            BaseCellPositions = baseList,
            LeafSlotIterationOrder = leafIterationOrder,
            MobPlacementOrder = mobPlacementOrder,
            PlugCellPositions = Array.Empty<Int2>()
        };
        return new TopologyBuildResult(plan, trace);
    }

    private static bool TryGrowPolyomino(int targetCount, Random rng, out HashSet<Int2> bases)
    {
        bases = new HashSet<Int2> { Int2.Zero };
        if (targetCount < 1)
        {
            bases.Clear();
            return false;
        }

        int maxSteps = targetCount * GridSteps.PolyominoGrowthGuardMultiplier;
        int steps = 0;
        while (bases.Count < targetCount && steps++ < maxSteps)
        {
            if (!TryAppendOneRandomNeighbor(bases, rng))
            {
                bases.Clear();
                return false;
            }
        }

        return bases.Count == targetCount;
    }

    private static bool TryAppendOneRandomNeighbor(HashSet<Int2> bases, Random rng)
    {
        var cells = bases.ToList();
        Shuffle(cells, rng);
        foreach (var cell in cells)
        {
            var emptyNeighbors = new List<Int2>();
            foreach (var step in GridSteps.Cardinal)
            {
                var n = cell + step;
                if (!bases.Contains(n))
                    emptyNeighbors.Add(n);
            }

            if (emptyNeighbors.Count == 0)
                continue;
            bases.Add(emptyNeighbors[rng.Next(emptyNeighbors.Count)]);
            return true;
        }

        return false;
    }

    /// <summary>Leaf-слот граничит ровно с одной базой.</summary>
    private static Int2 LeafAdjacentBase(Int2 leaf, HashSet<Int2> bases)
    {
        foreach (var step in GridSteps.Cardinal)
        {
            var q = leaf + step;
            if (bases.Contains(q))
                return q;
        }

        throw new InvalidOperationException($"Инвариант нарушен: leaf {leaf} не примыкает ни к одной базе.");
    }

    private static bool TryAssignSpecialRooms(
        HashSet<Int2> bases,
        IReadOnlyList<Int2> leafSlots,
        int mobCount,
        Random rng,
        out Dictionary<Int2, RoomType> specials,
        out List<Int2> mobPlacementOrder,
        out List<Int2> leafIterationOrder)
    {
        specials = new Dictionary<Int2, RoomType>();
        mobPlacementOrder = new List<Int2>();
        leafIterationOrder = new List<Int2>();

        int specialsNeeded = mobCount + 2;
        if (leafSlots.Count < specialsNeeded)
            return false;

        var basePolyDegrees = ComputeDegreesWithinSet(bases, bases);

        leafIterationOrder = leafSlots.ToList();
        Shuffle(leafIterationOrder, rng);

        if (!TryPickStartEndMaxDistance(bases, leafSlots, rng, out var startPos, out var endPos))
            return false;

        var chosenSpecials = new List<Int2> { startPos, endPos };
        var used = new HashSet<Int2> { startPos, endPos };

        if (!TryPickMobsMaximin(bases, leafIterationOrder, used, chosenSpecials, mobCount, mobPlacementOrder))
            return false;

        specials[startPos] = RoomType.Start;
        specials[endPos] = RoomType.End;
        foreach (var mobPos in mobPlacementOrder)
            specials[mobPos] = RoomType.Mob;

        return ValidateBaseDegreeInvariants(bases, basePolyDegrees, specials);
    }

    private static Dictionary<Int2, int> ComputeDegreesWithinSet(HashSet<Int2> subject, HashSet<Int2> neighborSet)
    {
        var deg = new Dictionary<Int2, int>();
        foreach (var cell in subject)
        {
            int c = 0;
            foreach (var step in GridSteps.Cardinal)
            {
                if (neighborSet.Contains(cell + step))
                    c++;
            }

            deg[cell] = c;
        }

        return deg;
    }

    /// <summary>Максимальная графовая дистанция по индуцированному графу bases ∪ {a,b}.</summary>
    private static int LeafPairDistance(HashSet<Int2> bases, Int2 a, Int2 b)
    {
        var verts = new HashSet<Int2>(bases) { a, b };
        return GridBfs.ShortestPathEdgeCount(verts, a, b);
    }

    private static bool TryPickStartEndMaxDistance(
        HashSet<Int2> bases,
        IReadOnlyList<Int2> leafSlots,
        Random rng,
        out Int2 startPos,
        out Int2 endPos)
    {
        startPos = default;
        endPos = default;
        int bestDistance = -1;
        var bestPairs = new List<(Int2 A, Int2 B)>();

        for (int i = 0; i < leafSlots.Count; i++)
        for (int j = i + 1; j < leafSlots.Count; j++)
        {
            var a = leafSlots[i];
            var b = leafSlots[j];
            int d = LeafPairDistance(bases, a, b);
            if (d < 0)
                continue;
            if (d > bestDistance)
            {
                bestDistance = d;
                bestPairs.Clear();
                bestPairs.Add((a, b));
            }
            else if (d == bestDistance)
            {
                bestPairs.Add((a, b));
            }
        }

        if (bestPairs.Count == 0)
            return false;

        var pick = bestPairs[rng.Next(bestPairs.Count)];
        startPos = pick.A;
        endPos = pick.B;
        return true;
    }

    /// <summary>Жадный maximin по графовой дистанции; при равенстве счёта — лексикографически меньшая клетка (детерминизм для валидатора).</summary>
    private static bool TryPickMobsMaximin(
        HashSet<Int2> bases,
        IReadOnlyList<Int2> candidateVisitOrder,
        HashSet<Int2> used,
        List<Int2> chosenSpecials,
        int mobCount,
        List<Int2> mobPlacementOrder)
    {
        for (int k = 0; k < mobCount; k++)
        {
            Int2? best = null;
            int bestScore = -1;
            foreach (var cand in candidateVisitOrder)
            {
                if (used.Contains(cand))
                    continue;

                int score = SpecialRoomScoring.MaximinAmongAnchors(bases, chosenSpecials, cand);
                if (score < 0)
                    continue;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = cand;
                }
                else if (score == bestScore && best is not null && Int2Comparers.Lexical(cand, best.Value) < 0)
                {
                    best = cand;
                }
            }

            if (best is null)
                return false;
            used.Add(best.Value);
            chosenSpecials.Add(best.Value);
            mobPlacementOrder.Add(best.Value);
        }

        return true;
    }

    private static bool ValidateBaseDegreeInvariants(
        HashSet<Int2> bases,
        Dictionary<Int2, int> basePolyDegree,
        Dictionary<Int2, RoomType> specials)
    {
        var specialsAttachedToBase = new Dictionary<Int2, int>();
        foreach (var b in bases)
            specialsAttachedToBase[b] = 0;

        foreach (var kv in specials)
        {
            var parentBase = LeafAdjacentBase(kv.Key, bases);
            specialsAttachedToBase[parentBase]++;
        }

        foreach (var b in bases)
        {
            int total = basePolyDegree[b] + specialsAttachedToBase[b];
            if (total is < 2 or > 4)
                return false;
        }

        return true;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
