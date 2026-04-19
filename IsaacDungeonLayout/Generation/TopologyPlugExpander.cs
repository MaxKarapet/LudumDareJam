namespace IsaacDungeonLayout;

/// <summary>Добавляет одну клетку <see cref="RoomType.Plug"/> на пустую позицию, смежную ровно с одной <see cref="RoomType.Base"/> (тупик к базе).</summary>
internal static class TopologyPlugExpander
{
    internal static bool TryAddOnePlug(
        TopologyPlan plan,
        DungeonTopologyTrace trace,
        Random rng,
        out TopologyPlan newPlan,
        out DungeonTopologyTrace newTrace,
        out Int2 addedPosition)
    {
        newPlan = plan;
        newTrace = trace;
        addedPosition = default;

        var occ = plan.AllCells;
        var candidates = new HashSet<Int2>();

        foreach (var p in occ)
        {
            foreach (var step in GridSteps.Cardinal)
            {
                var q = p + step;
                if (occ.Contains(q))
                    continue;

                var neighborsInOcc = new List<Int2>();
                foreach (var d in GridSteps.Cardinal)
                {
                    var n = q + d;
                    if (occ.Contains(n))
                        neighborsInOcc.Add(n);
                }

                if (neighborsInOcc.Count != 1)
                    continue;

                var only = neighborsInOcc[0];
                if (!plan.CellType.TryGetValue(only, out var rt) || rt != RoomType.Base)
                    continue;

                int baseDeg = GridBfs.CellDegree(only, occ);
                if (baseDeg >= 4)
                    continue;

                candidates.Add(q);
            }
        }

        if (candidates.Count == 0)
            return false;

        var list = candidates.ToList();
        addedPosition = list[rng.Next(list.Count)];

        var all = new HashSet<Int2>(occ) { addedPosition };
        var cellType = new Dictionary<Int2, RoomType>(plan.CellType)
        {
            [addedPosition] = RoomType.Plug
        };

        newPlan = new TopologyPlan { AllCells = all, CellType = cellType };

        var plugs = trace.PlugCellPositions.ToList();
        plugs.Add(addedPosition);
        newTrace = trace with { PlugCellPositions = plugs };
        return true;
    }
}
