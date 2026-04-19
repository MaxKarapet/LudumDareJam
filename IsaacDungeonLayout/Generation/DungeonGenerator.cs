namespace IsaacDungeonLayout;

public sealed class DungeonGenerator
{
    public DungeonGenerationOutcome Generate(DungeonGenerationConfig cfg)
    {
        cfg.Validate();
        cfg.DiagnosticLog?.Invoke($"Generate: seed={cfg.Seed}, n={cfg.BaseRoomCount}, m={cfg.MobRoomCount}, maxAttempts={cfg.MaxAttempts}");

        for (int attempt = 0; attempt < cfg.MaxAttempts; attempt++)
        {
            int platformIndependentSeed = unchecked((cfg.Seed * 397) ^ attempt);
            var rng = new Random(platformIndependentSeed);
            var built = TopologyPlanner.TryBuild(cfg, rng);
            if (built is null)
            {
                cfg.DiagnosticLog?.Invoke($"attempt {attempt + 1}: топология не собрана");
                continue;
            }

            var topo = built.Value.Plan;
            var trace = built.Value.Trace;

            if (!TryAssignTemplates(topo, cfg, out var rooms, out var templateFail))
            {
                cfg.DiagnosticLog?.Invoke($"attempt {attempt + 1}: шаблоны — {templateFail}");
                continue;
            }

            var start = rooms.Single(r => r.RoomType == RoomType.Start);
            var end = rooms.Single(r => r.RoomType == RoomType.End);
            var mobSorted = rooms.Where(r => r.RoomType == RoomType.Mob)
                .Select(r => r.GridPosition)
                .OrderBy(p => p.X)
                .ThenBy(p => p.Z)
                .ToList();

            var occupied = new HashSet<Int2>(rooms.Select(r => r.GridPosition));
            int dist = GridBfs.ShortestPathEdgeCount(occupied, start.GridPosition, end.GridPosition);

            var result = new DungeonLayout
            {
                Rooms = rooms.OrderBy(r => r.GridPosition.X).ThenBy(r => r.GridPosition.Z).ToList(),
                StartPosition = start.GridPosition,
                EndPosition = end.GridPosition,
                MobPositions = mobSorted,
                StartEndGraphDistance = dist,
                Topology = trace,
                Source = DungeonLayoutSource.Generated,
            };

            var err = DungeonValidator.ValidateGenerated(result, cfg);
            if (err is not null)
            {
                cfg.DiagnosticLog?.Invoke($"attempt {attempt + 1}: валидация — {err}");
                continue;
            }

            var enriched = DungeonLayoutEnricher.Enrich(result);
            cfg.DiagnosticLog?.Invoke($"attempt {attempt + 1}: OK, комнат={enriched.Rooms.Count}, SE={dist}");
            return DungeonGenerationOutcome.Ok(enriched, attempt + 1);
        }

        return DungeonGenerationOutcome.Fail(
            $"Не удалось сгенерировать валидный layout за {cfg.MaxAttempts} попыток (топология, шаблоны или инварианты).",
            cfg.MaxAttempts);
    }

    /// <summary>Режим перестановки: готовый граф клеток, фиксированный старт, пул слотов; финиш максимально далеко по BFS.</summary>
    public DungeonGenerationOutcome Shuffle(ShuffleDungeonInput input) =>
        DungeonShuffleSolver.TryShuffle(input);

    public DungeonLayout GenerateOrThrow(DungeonGenerationConfig cfg)
    {
        var outcome = Generate(cfg);
        if (!outcome.Success)
            throw new DungeonGenerationException(outcome.Failure!.Value.Reason, outcome.AttemptsUsed);
        return outcome.Result!;
    }

    private static bool TryAssignTemplates(
        TopologyPlan topo,
        DungeonGenerationConfig cfg,
        out List<PlacedRoom> rooms,
        out string? failReason)
    {
        rooms = new List<PlacedRoom>();
        failReason = null;
        var templates = cfg.Templates;
        Dictionary<string, int>? remaining = null;
        if (cfg.TemplateUsageCapsById is { Count: > 0 } caps)
        {
            remaining = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in caps)
                remaining[kv.Key] = kv.Value;
        }

        var all = topo.AllCells;

        foreach (var pos in all.OrderBy(p => p.X).ThenBy(p => p.Z))
        {
            var type = topo.CellType[pos];
            var req = TemplateMatcher.RequiredDirectionsFromNeighbors(pos, all);
            var candidates = templates.Where(t => t.Type == type);
            if (remaining is not null)
                candidates = candidates.Where(t => remaining.GetValueOrDefault(t.Id, 0) > 0);

            if (!TemplateMatcher.TryMatch(type, candidates.ToList(), req, out var tmpl, out var rot))
            {
                failReason = $"Нет шаблона для {type} @ {pos} с выходами [{string.Join(", ", req)}].";
                return false;
            }

            if (remaining is not null)
                remaining[tmpl!.Id]--;

            var finalDirs = RotationHelper.RotateDirections(tmpl!.OutsDir, rot).ToList();
            var neigh = finalDirs.Select(d => pos + d).ToList();

            rooms.Add(new PlacedRoom
            {
                TemplateId = tmpl.Id,
                RoomType = type,
                GridPosition = pos,
                RotationSteps90 = rot,
                FinalOutsDir = finalDirs,
                ConnectedNeighborPositions = neigh,
            });
        }

        return true;
    }
}
