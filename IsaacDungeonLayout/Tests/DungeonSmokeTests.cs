namespace IsaacDungeonLayout;

/// <summary>Мини-раннер без внешних тестовых библиотек.</summary>
public static class DungeonSmokeTests
{
    public static int Run(IReadOnlyList<RoomTemplate>? templates = null)
    {
        templates ??= DemoTemplates.BuildDefault();
        var gen = new DungeonGenerator();
        int failed = 0;

        void Case(string name, DungeonGenerationConfig cfg, bool expectFail = false)
        {
            var outcome = gen.Generate(cfg);
            if (expectFail)
            {
                if (outcome.Success)
                {
                    failed++;
                    Console.WriteLine($"[FAIL] {name}: ожидался сбой генерации, получен успех");
                }
                else
                    Console.WriteLine($"[OK] {name}: ожидаемый сбой — {outcome.Failure!.Value.Reason}");
                return;
            }

            if (outcome.Success)
            {
                var L = outcome.Result!;
                Console.WriteLine(
                    $"[OK] {name}: attempts={outcome.AttemptsUsed}, rooms={L.Rooms.Count}, SE={L.StartEndGraphDistance}, mobs={L.MobPositions.Count}");
                Console.WriteLine($"     bounds: {DescribeBounds(L)}");
                Console.WriteLine($"     mob: {string.Join(" ", L.MobPositions)}");
                try
                {
                    DungeonValidator.ValidateOrThrow(L, cfg);
                }
                catch (DungeonLayoutValidationException ex)
                {
                    failed++;
                    Console.WriteLine($"[FAIL] {name}: пост-валидация: {ex.Message}");
                }
            }
            else
            {
                failed++;
                Console.WriteLine($"[FAIL] {name}: {outcome.Failure!.Value.Reason}");
            }
        }

        Console.WriteLine("=== DungeonSmokeTests ===");
        Case("n=1 minimal polyomino", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 1,
            MobRoomCount = 0,
            Seed = 0,
            MaxAttempts = 2000,
        });

        Case("n=2 minimal", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 2,
            MobRoomCount = 0,
            Seed = 11,
            MaxAttempts = 500,
        });

        Case("seed small", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 5,
            MobRoomCount = 1,
            Seed = 1,
            MaxAttempts = 300,
        });
        Case("seed mid", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 12,
            MobRoomCount = 3,
            Seed = 42,
            MaxAttempts = 400,
        });
        Case("seed alt", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 10,
            MobRoomCount = 2,
            Seed = 999,
            MaxAttempts = 400,
        });
        Case("large n/m", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 22,
            MobRoomCount = 6,
            Seed = 7,
            MaxAttempts = 800,
        });
        Case("m zero", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 8,
            MobRoomCount = 0,
            Seed = 3,
            MaxAttempts = 400,
        });
        Case("m too big", new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = 6,
            MobRoomCount = 50,
            Seed = 1,
            MaxAttempts = 50,
        }, expectFail: true);

        for (int s = 0; s < 5; s++)
        {
            Case($"multi-seed s={s}", new DungeonGenerationConfig
            {
                Templates = templates,
                BaseRoomCount = 9,
                MobRoomCount = 2,
                Seed = s,
                MaxAttempts = 500,
            });
        }

        Console.WriteLine(failed == 0 ? "=== smoke: OK ===" : $"=== smoke: failures={failed} ===");
        return failed > 0 ? 1 : 0;
    }

    private static string DescribeBounds(DungeonLayout L)
    {
        var xs = L.Rooms.Select(r => r.GridPosition.X);
        var zs = L.Rooms.Select(r => r.GridPosition.Z);
        return $"X[{xs.Min()}..{xs.Max()}] Z[{zs.Min()}..{zs.Max()}]";
    }
}
