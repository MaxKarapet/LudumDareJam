namespace IsaacDungeonLayout;

/// <summary>Инварианты <see cref="RoomGameplayMetadata"/> после генерации.</summary>
public static class MetadataEnrichmentTests
{
    public static int Run()
    {
        Console.WriteLine("=== MetadataEnrichmentTests ===");
        int failed = 0;

        void Fail(string msg)
        {
            Console.WriteLine($"[FAIL] {msg}");
            failed++;
        }

        var templates = DemoTemplates.BuildDefault();
        var gen = new DungeonGenerator();
        var cfg = new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 8,
            MobRoomCount = 2,
            Seed = 42,
            MaxAttempts = 500,
        };

        var outcome = gen.Generate(cfg);
        if (!outcome.Success)
        {
            Fail($"генерация: {outcome.Failure!.Value.Reason}");
            Console.WriteLine(failed > 0 ? "=== metadata: failures ===" : "=== metadata: OK ===");
            return failed > 0 ? 1 : 0;
        }

        var L = outcome.Result!;
        var start = L.Rooms.Single(r => r.RoomType == RoomType.Start);
        var end = L.Rooms.Single(r => r.RoomType == RoomType.End);

        foreach (var r in L.Rooms)
        {
            if (r.GameplayMetadata is null)
                Fail($"null GameplayMetadata @ {r.GridPosition}");
        }

        if (start.GameplayMetadata is { } gs)
        {
            if (gs.DistanceFromStartEdges != 0)
                Fail("Start: DistanceFromStartEdges должен быть 0");
            if (!gs.OnShortestPathStartToEnd)
                Fail("Start: должен лежать на кратчайшем пути к End");
        }

        if (end.GameplayMetadata is { } ge)
        {
            if (ge.DistanceToEndEdges != 0)
                Fail("End: DistanceToEndEdges должен быть 0");
            if (!ge.OnShortestPathStartToEnd)
                Fail("End: должен лежать на кратчайшем пути от Start");
        }

        int se = L.StartEndGraphDistance;
        foreach (var r in L.Rooms)
        {
            var g = r.GameplayMetadata;
            if (g is null)
                continue;
            if (g.DistanceFromStartEdges + g.DistanceToEndEdges < se)
                Fail($"треугольник S–p–E: {r.GridPosition} dS+dE < SE");
            if (g.OnShortestPathStartToEnd != (g.DistanceFromStartEdges + g.DistanceToEndEdges == se))
                Fail($"флаг кратчайшего пути не совпал с dS+dE==SE @ {r.GridPosition}");

            int sumNeighbors = g.NeighborCountByType.Values.Sum();
            if (sumNeighbors != r.ConnectedNeighborPositions.Count)
                Fail($"сумма соседей по типам != числу соседей @ {r.GridPosition}");
        }

        if (L.MobPositions.Count > 0)
        {
            foreach (var r in L.Rooms.Where(x => x.RoomType == RoomType.Mob))
            {
                var g = r.GameplayMetadata!;
                if (g.DistanceToNearestMobEdges != 0)
                    Fail($"Mob @ {r.GridPosition}: расстояние до ближайшего моба должно быть 0");
            }
        }
        else
        {
            foreach (var r in L.Rooms)
            {
                if (r.GameplayMetadata!.DistanceToNearestMobEdges != -1)
                    Fail($"без мобов ожидается -1 @ {r.GridPosition}");
            }
        }

        // Ручной мини-layout: линия из трёх клеток — проверка enricher напрямую
        var manual = BuildLineLayoutForTest();
        var enrichedManual = DungeonLayoutEnricher.Enrich(manual);
        var s0 = enrichedManual.Rooms.Single(x => x.RoomType == RoomType.Start);
        var e0 = enrichedManual.Rooms.Single(x => x.RoomType == RoomType.End);
        if (s0.GameplayMetadata!.DistanceFromStartEdges != 0 || e0.GameplayMetadata!.DistanceToEndEdges != 0)
            Fail("линейный layout 3 клетки: дистанции старт/финиш");
        if (enrichedManual.StartEndGraphDistance != 2)
            Fail("линейный layout: SE должно быть 2 рёбра");
        var mid = enrichedManual.Rooms.Single(x => x.RoomType == RoomType.Base);
        if (mid.GameplayMetadata!.DistanceFromStartEdges != 1 || mid.GameplayMetadata.DistanceToEndEdges != 1)
            Fail("середина линии: ожидается 1+1 к End/Start");

        Console.WriteLine(failed == 0 ? "=== MetadataEnrichmentTests: OK ===" : $"=== MetadataEnrichmentTests: failures={failed} ===");
        return failed > 0 ? 1 : 0;
    }

    private static DungeonLayout BuildLineLayoutForTest()
    {
        var p0 = new Int2(0, 0);
        var p1 = new Int2(1, 0);
        var p2 = new Int2(2, 0);
        var rooms = new List<PlacedRoom>
        {
            new()
            {
                TemplateId = "t",
                RoomType = RoomType.Start,
                GridPosition = p0,
                RotationSteps90 = 0,
                FinalOutsDir = new[] { new Int2(1, 0) },
                ConnectedNeighborPositions = new[] { p1 },
            },
            new()
            {
                TemplateId = "t",
                RoomType = RoomType.Base,
                GridPosition = p1,
                RotationSteps90 = 0,
                FinalOutsDir = new[] { new Int2(-1, 0), new Int2(1, 0) },
                ConnectedNeighborPositions = new[] { p0, p2 },
            },
            new()
            {
                TemplateId = "t",
                RoomType = RoomType.End,
                GridPosition = p2,
                RotationSteps90 = 0,
                FinalOutsDir = new[] { new Int2(-1, 0) },
                ConnectedNeighborPositions = new[] { p1 },
            },
        };
        var trace = new DungeonTopologyTrace
        {
            BaseCellPositions = [],
            LeafSlotIterationOrder = [],
            MobPlacementOrder = [],
            PlugCellPositions = []
        };
        return new DungeonLayout
        {
            Rooms = rooms,
            StartPosition = p0,
            EndPosition = p2,
            MobPositions = [],
            StartEndGraphDistance = 2,
            Topology = trace,
        };
    }
}
