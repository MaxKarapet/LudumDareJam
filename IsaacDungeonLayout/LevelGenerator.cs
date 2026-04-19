
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using IsaacDungeonLayout;

public partial class LevelGenerator : Node3D
{
    private const string DungeonTemplateIdMeta = "dungeon_template_id";

    [Export] public PackedScene[] RoomScenes;
    /// <summary>Опционально: тупиковая заглушка (1 выход), вне колоды <see cref="RoomScenes"/>; включает <see cref="DungeonGenerationConfig.AllowTopologyPlugExpansion"/>.</summary>
    [Export] public PackedScene PlugScene;
    [Export] public int Seed = 42;
    [Export] public int MaxGenerationAttempts = 500;
    [Export] public float CellSize = 20.0f;

    private Dictionary<string, PackedScene> _sceneCache = new();

    public override void _Ready()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        if (!TryBuildDeckFromRoomScenes(out var catalog, out var caps, out int baseCount, out int mobCount, out var deckErr))
        {
            GD.PrintErr("LevelGenerator: " + deckErr);
            return;
        }

        if (catalog.Count == 0)
        {
            GD.PrintErr("LevelGenerator: нет шаблонов (RoomScenes пуст или только null).");
            return;
        }

        var catalogList = catalog.ToList();
        string? plugTemplateId = null;
        if (PlugScene != null)
        {
            var plugInst = PlugScene.Instantiate<RoomScene>();
            plugTemplateId = plugInst.Name;
            _sceneCache[plugTemplateId] = PlugScene;
            var outs = plugInst.OutsDir.Select(v => new Int2(v.X, v.Y)).ToArray();
            catalogList.Add(new RoomTemplate
            {
                Id = plugTemplateId,
                Type = RoomType.Plug,
                OutsNum = outs.Length,
                OutsDir = outs
            });
            plugInst.QueueFree();
        }

        var config = new DungeonGenerationConfig
        {
            Templates = catalogList,
            BaseRoomCount = baseCount,
            MobRoomCount = mobCount,
            Seed = Seed,
            MaxAttempts = Math.Max(MaxGenerationAttempts, (baseCount + mobCount + 2) * 40),
            DiagnosticLog = GD.Print,
            TemplateUsageCapsById = caps,
            PlugTemplateId = plugTemplateId,
            AllowTopologyPlugExpansion = plugTemplateId is not null,
            MaxTopologyPlugExpansions = 48
        };

        var generator = new DungeonGenerator();
        var outcome = generator.Generate(config);

        if (!outcome.Success)
        {
            GD.PrintErr("Failed to generate level: " + outcome.Failure!.Value.Reason);
            return;
        }

        SpawnRooms(outcome.Result!);
    }

    /// <summary>
    /// Режим shuffle: граф клеток и слоты берутся из уже размещённых потомков <see cref="RoomScene"/>.
    /// <see cref="ShuffleDungeonInput.OccupiedCells"/> — это <c>round(Position.X,Z / CellSize)</c> для каждой комнаты (та же сетка, что у <see cref="PlacedRoom.GridPosition"/>), не «занятость» навигации.
    /// </summary>
    /// <remarks>Нужна ровно одна комната с <c>RoomType == "start"</c>; по одному слоту на каждого потомка <see cref="RoomScene"/>; две комнаты в одной клетке сетки дадут неверный вход.</remarks>
    public DungeonGenerationOutcome TryShuffleCurrentLayout(int gameSeed)
    {
        if (!TryBuildDeckFromRoomScenes(out var catalog, out _, out _, out _, out var deckErr))
            return DungeonGenerationOutcome.Fail(deckErr, 0);

        if (catalog.Count == 0)
            return DungeonGenerationOutcome.Fail("Нет шаблонов (RoomScenes пуст).", 0);

        var roomNodes = GetChildren().OfType<RoomScene>().ToArray();
        if (roomNodes.Length == 0)
            return DungeonGenerationOutcome.Fail("Нет потомков RoomScene для shuffle.", 0);

        bool needPlugSlots = roomNodes.Any(r =>
            string.Equals(r.RoomType, "plug", StringComparison.OrdinalIgnoreCase));
        if (needPlugSlots && PlugScene == null)
            return DungeonGenerationOutcome.Fail(
                "В сцене есть комнаты с RoomType «plug» — задайте Export PlugScene для shuffle.", 0);

        if (PlugScene != null)
            TryAppendPlugTemplateForShuffle(ref catalog);

        int startCount = roomNodes.Count(r =>
            string.Equals(r.RoomType, "start", StringComparison.OrdinalIgnoreCase));
        if (startCount != 1)
            return DungeonGenerationOutcome.Fail(
                $"Ожидается ровно одна стартовая комната (RoomType \"start\"), сейчас: {startCount}.", 0);

        var startRoom = roomNodes.First(r =>
            string.Equals(r.RoomType, "start", StringComparison.OrdinalIgnoreCase));
        var startCell = WorldToGrid(startRoom.Position);

        var occ = new HashSet<Int2>();
        foreach (var room in roomNodes)
            occ.Add(WorldToGrid(room.Position));

        var slots = new List<RoomSlotDescriptor>(roomNodes.Length);
        foreach (var room in roomNodes)
        {
            slots.Add(new RoomSlotDescriptor
            {
                RoomType = MapExportRoomTypeString(room.RoomType)
            });
        }

        var input = new ShuffleDungeonInput
        {
            OccupiedCells = occ,
            StartPosition = startCell,
            Slots = slots,
            Templates = catalog,
            Seed = gameSeed,
            DiagnosticLog = GD.Print,
        };

        return new DungeonGenerator().Shuffle(input);
    }

    /// <summary>Успешный shuffle: обновляет уже существующие <see cref="RoomScene"/> (поворот, мета; замена сцены только при смене <c>TemplateId</c>).</summary>
    public DungeonGenerationOutcome TryShuffleCurrentLayoutInPlace(int gameSeed)
    {
        var outcome = TryShuffleCurrentLayout(gameSeed);
        if (!outcome.Success)
            return outcome;

        var err = ApplyShuffleLayoutInPlace(outcome.Result!);
        return err is null
            ? outcome
            : DungeonGenerationOutcome.Fail(err, outcome.AttemptsUsed);
    }

    /// <summary>Алиас: то же, что <see cref="TryShuffleCurrentLayoutInPlace"/>.</summary>
    public DungeonGenerationOutcome TryShuffleCurrentLayoutAndRespawn(int gameSeed) =>
        TryShuffleCurrentLayoutInPlace(gameSeed);

    /// <summary>Полное удаление и пересоздание всех комнат (старое поведение).</summary>
    public DungeonGenerationOutcome TryShuffleCurrentLayoutWithFullRespawn(int gameSeed)
    {
        var outcome = TryShuffleCurrentLayout(gameSeed);
        if (!outcome.Success)
            return outcome;

        RemoveAllRoomSceneChildren();
        SpawnRooms(outcome.Result!);
        return outcome;
    }

    private void RemoveAllRoomSceneChildren()
    {
        foreach (var child in GetChildren().OfType<RoomScene>().ToArray())
        {
            RemoveChild(child);
            child.Free();
        }
    }

    /// <summary>Добавляет шаблон Plug в каталог для shuffle (если задан <see cref="PlugScene"/>).</summary>
    private void TryAppendPlugTemplateForShuffle(ref List<RoomTemplate> catalog)
    {
        if (PlugScene == null)
            return;

        var plugInst = PlugScene.Instantiate<RoomScene>();
        try
        {
            string plugId = plugInst.Name;
            if (catalog.Any(t => t.Id == plugId))
                return;

            var outs = plugInst.OutsDir.Select(v => new Int2(v.X, v.Y)).ToArray();
            _sceneCache[plugId] = PlugScene;
            catalog.Add(new RoomTemplate
            {
                Id = plugId,
                Type = RoomType.Plug,
                OutsNum = outs.Length,
                OutsDir = outs
            });
        }
        finally
        {
            plugInst.QueueFree();
        }
    }

    private string? ApplyShuffleLayoutInPlace(DungeonLayout layout)
    {
        var byGrid = new Dictionary<Int2, RoomScene>();
        foreach (var child in GetChildren().OfType<RoomScene>())
        {
            var g = WorldToGrid(child.Position);
            if (byGrid.ContainsKey(g))
                return $"Две комнаты в одной клетке сетки {g} — in-place shuffle невозможен.";
            byGrid[g] = child;
        }

        if (byGrid.Count != layout.Rooms.Count)
            return $"Число RoomScene ({byGrid.Count}) не совпадает с layout ({layout.Rooms.Count}).";

        foreach (var room in layout.Rooms)
        {
            if (!byGrid.TryGetValue(room.GridPosition, out var node))
                return $"Нет ноды на клетке {room.GridPosition}.";

            string prevId = node.HasMeta(DungeonTemplateIdMeta)
                ? node.GetMeta(DungeonTemplateIdMeta).AsString()
                : string.Empty;

            var pos = new Vector3(room.GridPosition.X * CellSize, 0, room.GridPosition.Z * CellSize);
            float rotationY = -room.RotationSteps90 * (Mathf.Pi / 2f);

            if (room.TemplateId != prevId)
            {
                if (!_sceneCache.TryGetValue(room.TemplateId, out var scene))
                    return $"Нет сцены в кэше для шаблона {room.TemplateId}.";

                RemoveChild(node);
                node.QueueFree();

                node = scene.Instantiate<RoomScene>();
                AddChild(node);
                byGrid[room.GridPosition] = node;
            }

            node.Position = pos;
            node.Rotation = new Vector3(0, rotationY, 0);
            node.SetMeta(DungeonTemplateIdMeta, room.TemplateId);
            ApplyGameplayMetadataToRoom(node, room);
        }

        return null;
    }

    /// <summary>
    /// Колоды из <see cref="RoomScenes"/>: по одному использованию на каждый элемент массива с одинаковым <see cref="RoomScene.Name"/> (мультимножество).
    /// Число базовых / моб-комнат в топологии = числу соответствующих сцен в массиве; ровно один start и один end.
    /// </summary>
    private bool TryBuildDeckFromRoomScenes(
        out List<RoomTemplate> catalog,
        out Dictionary<string, int> usageCaps,
        out int baseRoomCount,
        out int mobRoomCount,
        out string error)
    {
        catalog = new List<RoomTemplate>();
        usageCaps = new Dictionary<string, int>(StringComparer.Ordinal);
        baseRoomCount = 0;
        mobRoomCount = 0;
        error = "";

        _sceneCache.Clear();

        if (RoomScenes == null || RoomScenes.Length == 0)
        {
            error = "RoomScenes пуст.";
            return false;
        }

        var byId = new Dictionary<string, RoomTemplate>(StringComparer.Ordinal);
        int starts = 0;
        int ends = 0;

        foreach (var scene in RoomScenes)
        {
            if (scene == null)
                continue;

            var instance = scene.Instantiate<RoomScene>();
            string id = instance.Name;
            _sceneCache[id] = scene;

            var type = MapExportRoomTypeString(instance.RoomType);
            if (type == RoomType.Plug)
            {
                instance.QueueFree();
                error = "Тип «plug» в RoomScenes не поддерживается — используйте Export PlugScene.";
                return false;
            }

            switch (type)
            {
                case RoomType.Start:
                    starts++;
                    break;
                case RoomType.End:
                    ends++;
                    break;
                case RoomType.Mob:
                    mobRoomCount++;
                    break;
                default:
                    baseRoomCount++;
                    break;
            }

            if (!byId.ContainsKey(id))
            {
                var outs = instance.OutsDir.Select(v => new Int2(v.X, v.Y)).ToArray();
                byId[id] = new RoomTemplate
                {
                    Id = id,
                    Type = type,
                    OutsNum = outs.Length,
                    OutsDir = outs
                };
            }

            usageCaps.TryGetValue(id, out var c);
            usageCaps[id] = c + 1;

            instance.QueueFree();
        }

        if (starts != 1)
        {
            error = $"В RoomScenes должно быть ровно одно «start», сейчас: {starts}.";
            return false;
        }

        if (ends != 1)
        {
            error = $"В RoomScenes должно быть ровно одно «end», сейчас: {ends}.";
            return false;
        }

        if (usageCaps.Count == 0)
        {
            error = "После фильтрации null в RoomScenes не осталось сцен.";
            return false;
        }

        catalog = byId.Values.OrderBy(t => t.Id, StringComparer.Ordinal).ToList();
        return true;
    }

    private static RoomType MapExportRoomTypeString(string s) =>
        s switch
        {
            "start" => RoomType.Start,
            "end" => RoomType.End,
            "mob" => RoomType.Mob,
            "plug" => RoomType.Plug,
            _ => RoomType.Base
        };

    private Int2 WorldToGrid(Vector3 pos) =>
        new(Mathf.RoundToInt(pos.X / CellSize), Mathf.RoundToInt(pos.Z / CellSize));

    private static void ApplyGameplayMetadataToRoom(RoomScene node, PlacedRoom room)
    {
        if (room.GameplayMetadata is not { } meta)
            return;

        node.SetMeta("gameplay_dist_from_start", meta.DistanceFromStartEdges);
        node.SetMeta("gameplay_dist_to_end", meta.DistanceToEndEdges);
        node.SetMeta("gameplay_on_shortest_path", meta.OnShortestPathStartToEnd);
        node.SetMeta("gameplay_dist_to_nearest_mob", meta.DistanceToNearestMobEdges);
        foreach (var kv in meta.NeighborCountByType)
            node.SetMeta($"gameplay_neighbor_{kv.Key}", kv.Value);
    }

    private void SpawnRooms(DungeonLayout layout)
    {
        foreach (var room in layout.Rooms)
        {
            if (!_sceneCache.TryGetValue(room.TemplateId, out var scene))
            {
                GD.PrintErr($"Нет сцены в кэше для шаблона {room.TemplateId}");
                continue;
            }

            var instance = scene.Instantiate<RoomScene>();

            instance.Position = new Vector3(
                room.GridPosition.X * CellSize,
                0,
                room.GridPosition.Z * CellSize
            );

            float rotationY = -room.RotationSteps90 * (Mathf.Pi / 2f);
            instance.Rotation = new Vector3(0, rotationY, 0);
            instance.SetMeta(DungeonTemplateIdMeta, room.TemplateId);
            ApplyGameplayMetadataToRoom(instance, room);

            AddChild(instance);
        }
    }
}
