namespace IsaacDungeonLayout;

/// <summary>Эвристики совместимости колоды с типичными степенями базовых клеток в полимино.</summary>
public static class DeckFeasibility
{
    /// <summary>Жёсткая проверка перед ретраями: заведомый провал подбора шаблонов.</summary>
    public static string? TryGetBlockingReason(DungeonGenerationConfig cfg)
    {
        int minOut = MinOutsAmongBases(cfg);
        if (minOut == int.MaxValue)
            return "Нет ни одного шаблона типа Base в колоде.";

        if (cfg.BaseRoomCount == 1 && minOut > 2)
            return
                "При одной базовой клетке (n=1) у неё в графе будет степень 2 (два leaf-слота Start/End); в колоде нет Base-шаблона с ровно 2 выходами. Добавьте такую комнату, увеличьте n или включите AllowTopologyPlugExpansion с PlugTemplateId.";

        return null;
    }

    /// <summary>Предупреждения в лог (не блокируют генерацию).</summary>
    public static void LogSoftWarnings(DungeonGenerationConfig cfg, Action<string>? log)
    {
        if (log is null)
            return;

        int minOut = MinOutsAmongBases(cfg);
        if (cfg.BaseRoomCount >= 2 && minOut > 2)
            log(
                "DeckFeasibility: в колоде нет Base с 2 выходами — частые полимино требуют степень 2 у базы. При сбоях подбора шаблонов добавьте 2-way комнату или включите AllowTopologyPlugExpansion.");

        int maxOut = cfg.Templates.Where(x => x.Type == RoomType.Base).Select(t => t.OutsNum).DefaultIfEmpty(0).Max();
        if (cfg.BaseRoomCount >= 4 && maxOut < 4)
            log("DeckFeasibility: нет Base с 4 выходами при n>=4 — внутренние клетки полимино могут потребовать крестовину.");
    }

    private static int MinOutsAmongBases(DungeonGenerationConfig cfg)
    {
        var caps = cfg.TemplateUsageCapsById;
        int? min = null;
        foreach (var t in cfg.Templates.Where(x => x.Type == RoomType.Base))
        {
            int c = caps is { Count: > 0 } ? caps.GetValueOrDefault(t.Id, 0) : 1;
            if (c < 1)
                continue;
            min = min is null ? t.OutsNum : Math.Min(min.Value, t.OutsNum);
        }

        return min ?? int.MaxValue;
    }
}
