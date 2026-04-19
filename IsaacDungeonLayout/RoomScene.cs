using Godot;
using Godot.Collections;

[GlobalClass]
public partial class RoomScene : Node3D
{
    // "base", "start", "end", "mob" (plug только через LevelGenerator.PlugScene, не в RoomScenes)
    [Export] public string RoomType = "base";


    [Export] public Array<Vector2I> OutsDir;
}
