namespace IsaacDungeonLayout;

public static class HelperTests
{
    public static int Run()
    {
        Console.WriteLine("=== Helper Tests ===");
        int failed = 0;

        void Assert(bool condition, string msg)
        {
            if (!condition)
            {
                Console.WriteLine($"[FAIL] {msg}");
                failed++;
            }
        }

        try
        {
            // 1. RotationHelper
            var dir = new Int2(1, 0); // East (+1, 0)
            Assert(DirectionExtensions.RotateVector(dir, 1).Equals(new Int2(0, 1)), "RotationHelper +90 (0,1)");
            Assert(DirectionExtensions.RotateVector(dir, 2).Equals(new Int2(-1, 0)), "RotationHelper +180 (-1,0)");
            Assert(DirectionExtensions.RotateVector(dir, 3).Equals(new Int2(0, -1)), "RotationHelper +270 (0,-1)");
            Assert(DirectionExtensions.RotateVector(dir, 4).Equals(new Int2(1, 0)), "RotationHelper +360 (1,0)");
            
            // 2. BFS distance
            var set = new HashSet<Int2> { new Int2(0,0), new Int2(1,0), new Int2(1,1), new Int2(0,1) }; 
            Assert(GridBfs.ShortestPathEdgeCount(set, new Int2(0,0), new Int2(1,1)) == 2, "BFS 2x2 distance");
            Assert(GridBfs.ShortestPathEdgeCount(set, new Int2(0,0), new Int2(2,2)) == -1, "BFS unreachable");

            var distFrom = GridBfs.DistancesFrom(set, new Int2(0, 0));
            Assert(distFrom[new Int2(1, 1)] == 2, "DistancesFrom угла 1,1");
            var line = new HashSet<Int2> { new Int2(0, 0), new Int2(1, 0), new Int2(2, 0) };
            var distMob = GridBfs.MultiSourceMinDistance(line, new[] { new Int2(2, 0) });
            Assert(distMob[new Int2(0, 0)] == 2, "MultiSource до правого конца");

            Assert(GridBfs.CellDegree(new Int2(0, 0), line) == 1, "CellDegree конец линии");
            Assert(GridBfs.CellDegree(new Int2(1, 0), line) == 2, "CellDegree середина линии");
            Assert(GridBfs.CellDegree(new Int2(1, 1), set) == 2, "CellDegree внутри 2x2");

            // 3. Leaf finding
            var simplePolyomino = new HashSet<Int2> { new Int2(0,0), new Int2(1,0), new Int2(0,1) };
            var leaves = new HashSet<Int2>(LeafSlotGeometry.EnumerateLeafSlots(simplePolyomino));
            Assert(leaves.Contains(new Int2(2,0)), "Leaf 2,0");
            Assert(leaves.Contains(new Int2(-1,0)), "Leaf -1,0");
            Assert(leaves.Contains(new Int2(0,2)), "Leaf 0,2");
            Assert(leaves.Contains(new Int2(0,-1)), "Leaf 0,-1");
            Assert(!leaves.Contains(new Int2(1,1)), "Internal corner 1,1 should not be leaf");
            
            // 4. TemplateMatcher
            var templates = DemoTemplates.BuildDefault();
            var reqMatch = new HashSet<Int2> { new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1) }; // e.g. T-junction
            bool matched = TemplateMatcher.TryMatch(RoomType.Base, templates, reqMatch, out var tmpl, out int rot);
            Assert(matched, "TemplateMatcher should find base room for 3 exits");
            if (matched)
            {
                var rotatedDirs = RotationHelper.RotateDirections(tmpl!.OutsDir, rot);
                var setRot = new HashSet<Int2>(rotatedDirs);
                Assert(setRot.SetEquals(reqMatch), "TemplateMatcher output should match requested exits");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Exception: {ex}");
            failed++;
        }

        Console.WriteLine(failed == 0 ? "=== Helper Tests: OK ===" : $"=== Helper Tests: failures={failed} ===");
        return failed > 0 ? 1 : 0;
    }
}
