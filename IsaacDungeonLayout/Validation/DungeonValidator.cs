namespace IsaacDungeonLayout;

public static class DungeonValidator
{
    public static void ValidateOrThrow(DungeonLayout layout, DungeonGenerationConfig cfg)
    {
        var err = Validate(layout, cfg);
        if (err is not null)
            throw new DungeonLayoutValidationException(err);
    }

    public static string? Validate(DungeonLayout layout, DungeonGenerationConfig cfg)
    {
        var rooms = layout.Rooms;
        int expectedTotal = cfg.BaseRoomCount + cfg.MobRoomCount + 2;
        if (rooms.Count != expectedTotal)
            return $"Ожидалось {expectedTotal} комнат, получено {rooms.Count}.";

        var byPos = new Dictionary<Int2, PlacedRoom>();
        foreach (var r in rooms)
        {
            if (byPos.ContainsKey(r.GridPosition))
                return $"Дубль позиции {r.GridPosition}.";
            byPos[r.GridPosition] = r;
        }

        if (!ValidateRoomTypeCounts(rooms, cfg, out var countErr))
            return countErr;

        var occupied = new HashSet<Int2>(byPos.Keys);
        if (!GridBfs.IsConnected(occupied))
            return "Граф комнат несвязный.";

        foreach (var r in rooms)
        {
            var err = ValidateRoomExitsAndDegree(r, occupied);
            if (err is not null)
                return err;
        }

        foreach (var r in rooms)
        {
            foreach (var d in r.FinalOutsDir)
            {
                var q = r.GridPosition + d;
                var other = byPos[q];
                var back = r.GridPosition - q;
                if (!other.FinalOutsDir.Contains(back))
                    return $"Нет встречного выхода {r.GridPosition}->{q}.";
            }
        }

        var start = rooms.Single(x => x.RoomType == RoomType.Start);
        var end = rooms.Single(x => x.RoomType == RoomType.End);
        int dist = GridBfs.ShortestPathEdgeCount(occupied, start.GridPosition, end.GridPosition);
        if (dist != layout.StartEndGraphDistance)
            return $"Несовпадение дистанции start-end: в отчёте {layout.StartEndGraphDistance}, по BFS {dist}.";
        if (dist < 0)
            return "Start и End не соединены.";

        if (!start.GridPosition.Equals(layout.StartPosition))
            return "StartPosition в результате не совпадает с комнатой Start.";
        if (!end.GridPosition.Equals(layout.EndPosition))
            return "EndPosition в результате не совпадает с комнатой End.";

        var mobSet = new HashSet<Int2>(layout.MobPositions);
        foreach (var r in rooms.Where(x => x.RoomType == RoomType.Mob))
        {
            if (!mobSet.Contains(r.GridPosition))
                return $"Mob {r.GridPosition} отсутствует в MobPositions.";
        }

        var topoErr = ValidateTopologyInvariants(layout, start.GridPosition, end.GridPosition, dist);
        if (topoErr is not null)
            return topoErr;

        return null;
    }

    private static bool ValidateRoomTypeCounts(
        IReadOnlyList<PlacedRoom> rooms,
        DungeonGenerationConfig cfg,
        out string? error)
    {
        int b = 0, m = 0, s = 0, e = 0;
        foreach (var r in rooms)
        {
            switch (r.RoomType)
            {
                case RoomType.Base: b++; break;
                case RoomType.Mob: m++; break;
                case RoomType.Start: s++; break;
                case RoomType.End: e++; break;
            }
        }

        if (b != cfg.BaseRoomCount) { error = $"Базовых комнат {b}, ожидалось {cfg.BaseRoomCount}."; return false; }
        if (m != cfg.MobRoomCount) { error = $"Mob: {m}, ожидалось {cfg.MobRoomCount}."; return false; }
        if (s != 1) { error = $"Start: {s}, ожидалось 1."; return false; }
        if (e != 1) { error = $"End: {e}, ожидалось 1."; return false; }
        error = null;
        return true;
    }

    private static string? ValidateRoomExitsAndDegree(PlacedRoom r, HashSet<Int2> occupied)
    {
        var neigh = new List<Int2>();
        foreach (var d in r.FinalOutsDir)
        {
            var q = r.GridPosition + d;
            if (!occupied.Contains(q))
                return $"Комната {r.GridPosition}: выход {d} не ведёт в занятую клетку.";
            neigh.Add(q);
        }

        if (neigh.Count != r.ConnectedNeighborPositions.Count)
            return $"Комната {r.GridPosition}: несогласованность числа соседей.";
        foreach (var c in r.ConnectedNeighborPositions)
        {
            if (!neigh.Contains(c))
                return $"Комната {r.GridPosition}: в connections лишний сосед {c}.";
        }

        int gridDeg = 0;
        foreach (var step in GridSteps.Cardinal)
        {
            if (occupied.Contains(r.GridPosition + step))
                gridDeg++;
        }

        if (gridDeg != r.FinalOutsDir.Count)
            return $"Комната {r.GridPosition}: степень сетки {gridDeg} != числу выходов {r.FinalOutsDir.Count}.";

        return r.RoomType switch
        {
            RoomType.Base when gridDeg is < 2 or > 4 =>
                $"База {r.GridPosition}: степень {gridDeg} не в [2,4].",
            RoomType.Mob or RoomType.Start or RoomType.End when gridDeg != 1 =>
                $"Комната {r.RoomType} {r.GridPosition}: степень должна быть 1, сейчас {gridDeg}.",
            _ => null
        };
    }

    /// <summary>
    /// Проверка: старт/финиш — пара листовых слотов с максимальной достижимой дистанцией;
    /// mob выбраны жадным maximin в порядке обхода из трассировки.
    /// </summary>
    private static string? ValidateTopologyInvariants(
        DungeonLayout layout,
        Int2 start,
        Int2 end,
        int reportedSeDistance)
    {
        var trace = layout.Topology;
        var bases = new HashSet<Int2>(trace.BaseCellPositions);
        if (bases.Count != trace.BaseCellPositions.Count)
            return "Topology.BaseCellPositions содержит дубликаты.";

        var basesFromLayout = layout.Rooms.Where(r => r.RoomType == RoomType.Base).Select(r => r.GridPosition).ToHashSet();
        if (!basesFromLayout.SetEquals(bases))
            return "Topology.BaseCellPositions не совпадает с Base-комнатами в layout.";

        var leafOrder = trace.LeafSlotIterationOrder;
        var leafSet = new HashSet<Int2>(leafOrder);
        if (leafSet.Count != leafOrder.Count)
            return "Topology.LeafSlotIterationOrder должен быть перестановкой без повторов.";

        var canonicalLeaves = new HashSet<Int2>(LeafSlotGeometry.EnumerateLeafSlots(bases));
        if (!canonicalLeaves.SetEquals(leafSet))
            return "Состав leaf-слотов в трассировке не совпадает с геометрией базового полимино.";

        if (!leafSet.Contains(start) || !leafSet.Contains(end))
            return "Start или End не входят в множество leaf-слотов полимино.";

        int maxLeafPair = ComputeMaxLeafPairDistance(bases, leafSet.ToList());
        if (maxLeafPair < 0)
            return "Не удалось вычислить max дистанцию по парам leaf-слотов.";
        var vertsSe = new HashSet<Int2>(bases) { start, end };
        int seOnBackbone = GridBfs.ShortestPathEdgeCount(vertsSe, start, end);
        if (seOnBackbone != maxLeafPair)
            return $"Start–End не максимальная пара leaf-слотов: dist={seOnBackbone}, max по парам={maxLeafPair}.";
        if (seOnBackbone != reportedSeDistance)
            return $"Дистанция SE на полном графе {reportedSeDistance} != дистанции на bases∪{{S,E}} {seOnBackbone}.";

        var mobTrace = new HashSet<Int2>(trace.MobPlacementOrder);
        if (mobTrace.Count != trace.MobPlacementOrder.Count)
            return "Topology.MobPlacementOrder содержит дубликаты.";
        if (!mobTrace.SetEquals(layout.MobPositions))
            return "Набор mob в Topology не совпадает с MobPositions layout.";

        if (!ReplayMaximinMatches(bases, leafOrder, start, end, trace.MobPlacementOrder))
            return "Расстановка mob не соответствует жадной maximin-эвристике при заданном порядке обхода leaf-слотов.";

        return null;
    }

    private static int ComputeMaxLeafPairDistance(HashSet<Int2> bases, IReadOnlyList<Int2> leafSlots)
    {
        int best = -1;
        for (int i = 0; i < leafSlots.Count; i++)
        for (int j = i + 1; j < leafSlots.Count; j++)
        {
            var verts = new HashSet<Int2>(bases) { leafSlots[i], leafSlots[j] };
            int d = GridBfs.ShortestPathEdgeCount(verts, leafSlots[i], leafSlots[j]);
            if (d < 0)
                continue;
            if (d > best)
                best = d;
        }

        return best;
    }

    private static bool ReplayMaximinMatches(
        HashSet<Int2> bases,
        IReadOnlyList<Int2> leafVisitOrder,
        Int2 start,
        Int2 end,
        IReadOnlyList<Int2> expectedMobs)
    {
        var used = new HashSet<Int2> { start, end };
        var chosen = new List<Int2> { start, end };

        foreach (var expectedMob in expectedMobs)
        {
            Int2? best = null;
            int bestScore = -1;
            foreach (var cand in leafVisitOrder)
            {
                if (used.Contains(cand))
                    continue;
                int score = SpecialRoomScoring.MaximinAmongAnchors(bases, chosen, cand);
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

            if (best is null || !best.Value.Equals(expectedMob))
                return false;
            used.Add(best.Value);
            chosen.Add(best.Value);
        }

        return true;
    }
}
