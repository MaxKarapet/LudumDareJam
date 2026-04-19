using IsaacDungeonLayout;

var argv = Environment.GetCommandLineArgs();
var mode = argv.Length > 1 ? argv[1].ToLowerInvariant() : "test";

switch (mode)
{
    case "stress":
        Environment.Exit(DungeonStressTests.Run());
        break;
    case "helper":
        Environment.Exit(HelperTests.Run());
        break;
    case "metadata":
        Environment.Exit(MetadataEnrichmentTests.Run());
        break;
    case "smoke":
        Environment.Exit(DungeonSmokeTests.Run());
        break;
    case "test":
    case "full":
    {
        int smoke = DungeonSmokeTests.Run();
        if (smoke != 0)
            Environment.Exit(smoke);
        int meta = MetadataEnrichmentTests.Run();
        Environment.Exit(meta);
        break;
    }
    default:
        Console.WriteLine("Использование: dotnet run -- [test|full|smoke|stress|helper|metadata]");
        Environment.Exit(DungeonSmokeTests.Run());
        break;
}
