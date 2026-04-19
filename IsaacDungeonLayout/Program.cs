namespace IsaacDungeonLayout;

internal static class Program
{
    private static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0].TrimStart('-') : "test";
        return mode switch
        {
            "smoke" => DungeonSmokeTests.Run(),
            "shuffle" => ShuffleDungeonTests.Run(),
            "helper" => HelperTests.Run(),
            "metadata" => MetadataEnrichmentTests.Run(),
            "stress" => DungeonStressTests.Run(),
            "deck" => DeckFeasibilityTests.Run(),
            _ => RunDefaultSuite(),
        };
    }

    /// <summary>Как <c>dotnet run -- test</c> в документации: smoke, затем metadata, затем shuffle.</summary>
    private static int RunDefaultSuite()
    {
        int code = DungeonSmokeTests.Run();
        if (code != 0)
            return code;
        code = MetadataEnrichmentTests.Run();
        if (code != 0)
            return code;
        code = DeckFeasibilityTests.Run();
        if (code != 0)
            return code;
        return ShuffleDungeonTests.Run();
    }
}
