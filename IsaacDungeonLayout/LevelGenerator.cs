
using Godot;
using System.Linq;
using System.Collections.Generic;
using IsaacDungeonLayout; 

public partial class LevelGenerator : Node3D
{
    [Export] public PackedScene[] RoomScenes;
    [Export] public int BaseRooms = 15;
    [Export] public int MobRooms = 4;
    [Export] public int Seed = 42;
    [Export] public float CellSize = 20.0f; 

 
    private Dictionary<string, PackedScene> _sceneCache = new();

    public override void _Ready()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
     
        var templates = new List<RoomTemplate>();
        foreach (var scene in RoomScenes)
        {
            var instance = scene.Instantiate<RoomScene>();
            _sceneCache[instance.Name] = scene;

            var roomTypeVal = instance.RoomType switch
            {
                "start" => RoomType.Start,
                "end" => RoomType.End,
                "mob" => RoomType.Mob,
                _ => IsaacDungeonLayout.RoomType.Base
            };

            
            var outs = instance.OutsDir.Select(v => new Int2(v.X, v.Y)).ToArray();

            templates.Add(new RoomTemplate
            {
                Id = instance.Name,
                Type = roomTypeVal,
                OutsNum = outs.Length,
                OutsDir = outs
            });

            instance.QueueFree();
        }

     
        var config = new DungeonGenerationConfig
        {
            Templates = templates,
            BaseRoomCount = BaseRooms,
            MobRoomCount = MobRooms,
            Seed = Seed,
            MaxAttempts = 100,
            DiagnosticLog = GD.Print
        };

        var generator = new DungeonGenerator();
        var outcome = generator.Generate(config);

        if (!outcome.Success)
        {
            GD.PrintErr("Failed to generate level: " + outcome.Failure!.Value.Reason);
            return;
        }

        SpawnRooms(outcome.Result);
    }

    private void SpawnRooms(DungeonLayout layout)
    {
        foreach (var room in layout.Rooms)
        {
            var scene = _sceneCache[room.TemplateId];
            var instance = scene.Instantiate<Node3D>();

            
            instance.Position = new Vector3(
                room.GridPosition.X * CellSize,
                0,
                room.GridPosition.Z * CellSize
            );

       
            float rotationY = - room.RotationSteps90 * (Mathf.Pi / 2f);

          
            instance.Rotation = new Vector3(0, rotationY, 0);

            AddChild(instance);
        }
    }
}
