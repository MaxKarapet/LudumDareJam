namespace IsaacDungeonLayout;

public static class DungeonStressTests
{
    public static int Run()
    {
        Console.WriteLine("=== Stress Tests (Seeds 1..100) ===");
        var templates = DemoTemplates.BuildDefault();
        var gen = new DungeonGenerator();
        int successCount = 0;
        int failCount = 0;
        var failedSeeds = new List<(int, string)>();

        for (int seed = 1; seed <= 100; seed++)
        {
            var cfg = new DungeonGenerationConfig
            {
                Templates = templates,
                BaseRoomCount = 15,
                MobRoomCount = 4,
                Seed = seed,
                MaxAttempts = 500
            };

            var outcome = gen.Generate(cfg);
            if (outcome.Success)
            {
                var layout = outcome.Result!;
                try
                {
                    DungeonValidator.ValidateOrThrow(layout, cfg);
                    successCount++;
                }
                catch (DungeonLayoutValidationException ex)
                {
                    failCount++;
                    failedSeeds.Add((seed, "Validation failed: " + ex.Message));
                }
            }
            else
            {
                failCount++;
                failedSeeds.Add((seed, "Generation failed: " + outcome.Failure!.Value.Reason));
            }
        }

        Console.WriteLine($"=== Stress Tests Result: {successCount} success, {failCount} failed ===");
        if (failCount > 0)
        {
            Console.WriteLine("Failed seeds:");
            foreach (var (seed, reason) in failedSeeds)
            {
                Console.WriteLine($"  Seed {seed}: {reason}");
            }
            return 1;
        }
        return 0;
    }
}
