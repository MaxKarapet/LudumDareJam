namespace IsaacDungeonLayout;

/// <summary>Режим перестановки: фиксированный старт, финиш на максимальной BFS-дистанции среди допустимых назначений слотов.</summary>
public static class DungeonShuffleSolver
{
    public static DungeonGenerationOutcome TryShuffle(ShuffleDungeonInput input)
    {
        try
        {
            input.Validate();
        }
        catch (InvalidOperationException ex)
        {
            return DungeonGenerationOutcome.Fail(ex.Message, 1);
        }

        var occ = input.OccupiedCells;
        int seed = input.Seed;
        var expect = ShuffleTypeExpectation.FromSlots(input.Slots);
        var startSlot = input.Slots.Single(s => s.RoomType == RoomType.Start);
        var endSlot = input.Slots.Single(s => s.RoomType == RoomType.End);
        var otherSlots = input.Slots.Where(s => s.RoomType is not (RoomType.Start or RoomType.End)).ToList();

        var reqAtStart = TemplateMatcher.RequiredDirectionsFromNeighbors(input.StartPosition, occ);
        var startTemplates = FilterCatalog(startSlot, input.Templates);
        var startMatches = TemplateMatcher.EnumerateMatchesForDirections(startTemplates, reqAtStart).ToList();
        if (startMatches.Count == 0)
            return DungeonGenerationOutcome.Fail("Нет шаблона для Start на фиксированной клетке с данными выходами.", 1);

        var distFromStart = GridBfs.DistancesFrom(occ, input.StartPosition);
        var endCandidates = occ
            .Where(p => !p.Equals(input.StartPosition) && GridBfs.CellDegree(p, occ) == 1)
            .OrderByDescending(p => distFromStart.GetValueOrDefault(p, -1))
            .ThenBy(p => TieBreak(seed, p))
            .ThenBy(p => p.X)
            .ThenBy(p => p.Z)
            .ToList();

        if (endCandidates.Count == 0)
            return DungeonGenerationOutcome.Fail("Нет кандидата под End (клетка степени 1, не старт).", 1);

        foreach (var endPos in endCandidates)
        {
            var reqAtEnd = TemplateMatcher.RequiredDirectionsFromNeighbors(endPos, occ);
            var endTemplates = FilterCatalog(endSlot, input.Templates);
            var endMatches = TemplateMatcher.EnumerateMatchesForDirections(endTemplates, reqAtEnd).ToList();
            if (endMatches.Count == 0)
                continue;

            foreach (var (tmplS, rotS) in startMatches)
            {
                var startRoom = MakePlacedRoom(input.StartPosition, RoomType.Start, tmplS, rotS, occ);
                foreach (var (tmplE, rotE) in endMatches)
                {
                    var endRoom = MakePlacedRoom(endPos, RoomType.End, tmplE, rotE, occ);
                    var remainingCells = occ
                        .Where(p => !p.Equals(input.StartPosition) && !p.Equals(endPos))
                        .OrderBy(p => TieBreak(seed, p))
                        .ThenBy(p => p.X)
                        .ThenBy(p => p.Z)
                        .ToList();
                    var pool = new List<RoomSlotDescriptor>(otherSlots);
                    var map = new Dictionary<Int2, PlacedRoom>
                    {
                        [input.StartPosition] = startRoom,
                        [endPos] = endRoom,
                    };

                    if (!TryFillRemaining(remainingCells, 0, pool, occ, input.Templates, map, seed))
                        continue;

                    int seDist = GridBfs.ShortestPathEdgeCount(occ, input.StartPosition, endPos);
                    var mobSorted = map.Values
                        .Where(r => r.RoomType == RoomType.Mob)
                        .Select(r => r.GridPosition)
                        .OrderBy(p => p.X)
                        .ThenBy(p => p.Z)
                        .ToList();

                    var emptyTrace = new DungeonTopologyTrace([], [], []);
                    var layout = new DungeonLayout
                    {
                        Rooms = map.Values.OrderBy(r => r.GridPosition.X).ThenBy(r => r.GridPosition.Z).ToList(),
                        StartPosition = input.StartPosition,
                        EndPosition = endPos,
                        MobPositions = mobSorted,
                        StartEndGraphDistance = seDist,
                        Topology = emptyTrace,
                        Source = DungeonLayoutSource.Shuffled,
                    };

                    var err = DungeonValidator.ValidateShuffled(layout, expect, occ);
                    if (err is not null)
                    {
                        input.DiagnosticLog?.Invoke($"shuffle: внутренняя валидация {err}");
                        continue;
                    }

                    var enriched = DungeonLayoutEnricher.Enrich(layout);
                    input.DiagnosticLog?.Invoke($"shuffle: OK, SE={seDist}, end={endPos}");
                    return DungeonGenerationOutcome.Ok(enriched, 1);
                }
            }
        }

        return DungeonGenerationOutcome.Fail(
            "Не удалось подобрать перестановку комнат с допустимыми шаблонами и максимально далёким End от Start.",
            1);
    }

    /// <summary>Детерминированный ключ для порядка перебора при равных BFS-дистанциях и в backtracking.</summary>
    private static int TieBreak(int seed, Int2 p) =>
        unchecked((seed * 397) ^ (p.X * 7919) ^ (p.Z * 65537));

    private static List<RoomTemplate> FilterCatalog(RoomSlotDescriptor slot, IReadOnlyList<RoomTemplate> catalog)
    {
        IEnumerable<RoomTemplate> q = catalog
            .Where(t => t.Type == slot.RoomType)
            .OrderBy(t => t.Id, StringComparer.Ordinal);
        if (slot.RequiredTemplateId is { } id)
            q = q.Where(t => t.Id == id);
        return q.ToList();
    }

    private static bool TypeFitsDegree(RoomType t, int gd) =>
        t == RoomType.Base ? gd is >= 2 and <= 4 : gd == 1;

    private static PlacedRoom MakePlacedRoom(Int2 pos, RoomType type, RoomTemplate tmpl, int rot, HashSet<Int2> occ)
    {
        var finalDirs = RotationHelper.RotateDirections(tmpl.OutsDir, rot).ToList();
        var neigh = finalDirs.Select(d => pos + d).ToList();
        return new PlacedRoom
        {
            TemplateId = tmpl.Id,
            RoomType = type,
            GridPosition = pos,
            RotationSteps90 = rot,
            FinalOutsDir = finalDirs,
            ConnectedNeighborPositions = neigh,
        };
    }

    private static bool TryFillRemaining(
        IReadOnlyList<Int2> cells,
        int idx,
        List<RoomSlotDescriptor> pool,
        HashSet<Int2> occ,
        IReadOnlyList<RoomTemplate> catalog,
        Dictionary<Int2, PlacedRoom> map,
        int shuffleSeed)
    {
        if (idx >= cells.Count)
            return true;

        var cell = cells[idx];
        int gd = GridBfs.CellDegree(cell, occ);
        var slotOrder = Enumerable.Range(0, pool.Count)
            .OrderBy(pi => TieBreak(unchecked(shuffleSeed + idx * 1009 + pi * 17), new Int2(pi, (int)pool[pi].RoomType)))
            .ThenBy(pi => pi)
            .ToList();

        foreach (var pi in slotOrder)
        {
            var slot = pool[pi];
            if (!TypeFitsDegree(slot.RoomType, gd))
                continue;

            var req = TemplateMatcher.RequiredDirectionsFromNeighbors(cell, occ);
            var filt = FilterCatalog(slot, catalog);
            if (filt.Count == 0)
                continue;

            foreach (var (tmpl, rot) in TemplateMatcher.EnumerateMatchesForDirections(filt, req))
            {
                var room = MakePlacedRoom(cell, slot.RoomType, tmpl, rot, occ);
                pool.RemoveAt(pi);
                map[cell] = room;
                if (TryFillRemaining(cells, idx + 1, pool, occ, catalog, map, shuffleSeed))
                    return true;
                map.Remove(cell);
                pool.Insert(pi, slot);
            }
        }

        return false;
    }
}
