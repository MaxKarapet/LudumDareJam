namespace IsaacDungeonLayout;

public static class DeckFeasibilityTests
{
    public static int Run()
    {
        int failed = 0;

        void Fail(string msg)
        {
            Console.WriteLine($"[FAIL] {msg}");
            failed++;
        }

        Console.WriteLine("=== DeckFeasibilityTests ===");

        var templates = DemoTemplates.BuildDefault();
        var capsOnlyCross = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["base_cross"] = 1,
            ["special_exit_e"] = 1,
            ["special_exit_e_end"] = 1,
        };

        var badCfg = new DungeonGenerationConfig
        {
            Templates = templates.Where(t => t.Id is "base_cross" or "special_exit_e" or "special_exit_e_end").ToList(),
            BaseRoomCount = 1,
            MobRoomCount = 0,
            Seed = 0,
            MaxAttempts = 1,
            TemplateUsageCapsById = capsOnlyCross,
        };

        var block = DeckFeasibility.TryGetBlockingReason(badCfg);
        if (block is null)
            Fail("ожидалась блокировка n=1 без base с 2 выходами");
        else
            Console.WriteLine($"[OK] блокировка: {block[..Math.Min(80, block.Length)]}…");

        var plan = new TopologyPlan
        {
            AllCells = new HashSet<Int2> { new(0, 0) },
            CellType = new Dictionary<Int2, RoomType> { [new Int2(0, 0)] = RoomType.Base }
        };
        var trace = new DungeonTopologyTrace
        {
            BaseCellPositions = [new Int2(0, 0)],
            LeafSlotIterationOrder = [],
            MobPlacementOrder = [],
            PlugCellPositions = []
        };
        var rng = new Random(0);
        if (!TopologyPlugExpander.TryAddOnePlug(plan, trace, rng, out var np, out var nt, out var added))
            Fail("TopologyPlugExpander: ожидалось добавление Plug рядом с изолированной базой");
        else if (np.AllCells.Count != 2 || nt.PlugCellPositions.Count != 1)
            Fail($"TopologyPlugExpander: ожидалось 2 клетки и 1 plug, получено cells={np.AllCells.Count}, plugs={nt.PlugCellPositions.Count}");
        else
            Console.WriteLine($"[OK] PlugExpander добавил {added}");

        Console.WriteLine(failed == 0 ? "=== DeckFeasibilityTests: OK ===" : $"=== DeckFeasibilityTests: failures={failed} ===");
        return failed > 0 ? 1 : 0;
    }
}
