namespace IsaacDungeonLayout;

/// <summary>Геометрия примыкания leaf-слотов к полимино баз (общая для планировщика и валидатора).</summary>
public static class LeafSlotGeometry
{
    public static IReadOnlyList<Int2> EnumerateLeafSlots(HashSet<Int2> baseCells)
    {
        var adjacentEmpty = new HashSet<Int2>();
        foreach (var b in baseCells)
        foreach (var step in GridSteps.Cardinal)
        {
            var p = b + step;
            if (!baseCells.Contains(p))
                adjacentEmpty.Add(p);
        }

        var leafSlots = new List<Int2>();
        foreach (var p in adjacentEmpty)
        {
            int occ = 0;
            foreach (var step in GridSteps.Cardinal)
            {
                if (baseCells.Contains(p + step))
                    occ++;
            }

            if (occ == 1)
                leafSlots.Add(p);
        }

        return leafSlots;
    }
}
