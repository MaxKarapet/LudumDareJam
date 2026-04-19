namespace IsaacDungeonLayout;

public sealed class DungeonGenerationConfig
{
    public const int DefaultMaxAttempts = 200;

    public required IReadOnlyList<RoomTemplate> Templates { get; init; }
    public required int BaseRoomCount { get; init; }
    public required int MobRoomCount { get; init; }
    public int Seed { get; init; }
    public int MaxAttempts { get; init; } = DefaultMaxAttempts;
    /// <summary>Опционально: пошаговые сообщения генерации (попытки, сбои шаблонов).</summary>
    public Action<string>? DiagnosticLog { get; init; }

    /// <summary>
    /// Если задано, каждый <see cref="RoomTemplate.Id"/> можно использовать не больше указанного числа раз (мультимножество «колоды»).
    /// Сумма значений должна совпадать с <c>BaseRoomCount + MobRoomCount + 2</c>, а суммы по типам — с числом клеток этого типа в топологии.
    /// </summary>
    public IReadOnlyDictionary<string, int>? TemplateUsageCapsById { get; init; }

    public void Validate()
    {
        if (Templates.Count == 0)
            throw new InvalidOperationException("Нужен хотя бы один шаблон.");
        if (BaseRoomCount < GridSteps.MinBaseRoomCount)
            throw new InvalidOperationException(
                $"BaseRoomCount (n) должен быть >= {GridSteps.MinBaseRoomCount}.");
        if (MobRoomCount < 0)
            throw new InvalidOperationException("MobRoomCount (m) не может быть отрицательным.");
        if (MaxAttempts < 1)
            throw new InvalidOperationException("MaxAttempts >= 1.");

        foreach (var t in Templates)
            t.Validate();

        static bool Any(IEnumerable<RoomTemplate> ts, RoomType rt) => ts.Any(x => x.Type == rt);
        if (!Any(Templates, RoomType.Base))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа Base.");
        if (MobRoomCount > 0 && !Any(Templates, RoomType.Mob))
            throw new InvalidOperationException("При MobRoomCount > 0 нужен хотя бы один шаблон типа Mob.");
        if (!Any(Templates, RoomType.Start))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа Start.");
        if (!Any(Templates, RoomType.End))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа End.");

        if (TemplateUsageCapsById is { Count: > 0 } caps)
            ValidateTemplateCaps(caps);
    }

    private void ValidateTemplateCaps(IReadOnlyDictionary<string, int> caps)
    {
        var byId = Templates.ToDictionary(t => t.Id, StringComparer.Ordinal);
        foreach (var kv in caps)
        {
            if (!byId.ContainsKey(kv.Key))
                throw new InvalidOperationException($"TemplateUsageCapsById: неизвестный Id «{kv.Key}».");
            if (kv.Value < 1)
                throw new InvalidOperationException($"TemplateUsageCapsById: для «{kv.Key}» указано {kv.Value}, ожидается >= 1.");
        }

        int total = caps.Values.Sum();
        int expected = BaseRoomCount + MobRoomCount + 2;
        if (total != expected)
            throw new InvalidOperationException(
                $"Сумма лимитов по Id ({total}) должна равняться числу комнат {expected} (Base+Mob+Start+End).");

        static int SumCapsForType(IReadOnlyDictionary<string, int> c, IReadOnlyDictionary<string, RoomTemplate> ids, RoomType rt)
        {
            int s = 0;
            foreach (var kv in c)
            {
                if (ids[kv.Key].Type == rt)
                    s += kv.Value;
            }

            return s;
        }

        if (SumCapsForType(caps, byId, RoomType.Base) != BaseRoomCount)
            throw new InvalidOperationException("Сумма лимитов по шаблонам типа Base должна равняться BaseRoomCount.");
        if (SumCapsForType(caps, byId, RoomType.Mob) != MobRoomCount)
            throw new InvalidOperationException("Сумма лимитов по шаблонам типа Mob должна равняться MobRoomCount.");
        if (SumCapsForType(caps, byId, RoomType.Start) != 1)
            throw new InvalidOperationException("В колоде должен быть ровно один экземпляр Start (сумма лимитов по Id типа Start = 1).");
        if (SumCapsForType(caps, byId, RoomType.End) != 1)
            throw new InvalidOperationException("В колоде должен быть ровно один экземпляр End (сумма лимитов по Id типа End = 1).");
    }
}
