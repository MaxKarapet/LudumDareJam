namespace IsaacDungeonLayout;

/// <summary>Графовый maximin для размещения mob: min по якорям графовой дистанции в bases ∪ anchors ∪ candidate.</summary>
internal static class SpecialRoomScoring
{
    /// <returns>Минимальная дистанция до якоря; -1 если кандидат не связан с каким-либо якорем в построенном графе.</returns>
    internal static int MaximinAmongAnchors(
        HashSet<Int2> baseCells,
        IReadOnlyList<Int2> anchorSpecialCells,
        Int2 candidateLeaf)
    {
        var verts = new HashSet<Int2>(baseCells);
        foreach (var x in anchorSpecialCells)
            verts.Add(x);
        verts.Add(candidateLeaf);

        int score = int.MaxValue;
        foreach (var anchor in anchorSpecialCells)
        {
            int d = GridBfs.ShortestPathEdgeCount(verts, candidateLeaf, anchor);
            if (d < 0)
                return -1;
            score = Math.Min(score, d);
        }

        return score;
    }
}
