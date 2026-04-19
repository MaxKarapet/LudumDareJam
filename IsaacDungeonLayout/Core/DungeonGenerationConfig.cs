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

    public void Validate()
    {
        if (Templates.Count == 0)
            throw new InvalidOperationException("Нужен хотя бы один шаблон.");
        if (BaseRoomCount < GridSteps.MinBaseRoomCount)
            throw new InvalidOperationException(
                $"BaseRoomCount (n) должен быть >= {GridSteps.MinBaseRoomCount} (одна база не может удовлетворить степени 2..4).");
        if (MobRoomCount < 0)
            throw new InvalidOperationException("MobRoomCount (m) не может быть отрицательным.");
        if (MaxAttempts < 1)
            throw new InvalidOperationException("MaxAttempts >= 1.");

        foreach (var t in Templates)
            t.Validate();

        static bool Any(IEnumerable<RoomTemplate> ts, RoomType rt) => ts.Any(x => x.Type == rt);
        if (!Any(Templates, RoomType.Base))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа Base.");
        if (!Any(Templates, RoomType.Mob))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа Mob.");
        if (!Any(Templates, RoomType.Start))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа Start.");
        if (!Any(Templates, RoomType.End))
            throw new InvalidOperationException("Нужен хотя бы один шаблон типа End.");
    }
}
