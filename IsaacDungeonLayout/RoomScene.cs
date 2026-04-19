using Godot;
using Godot.Collections;

[GlobalClass]
public partial class RoomScene : Node3D
{
    // "base", "start", "end", "mob"
    [Export] public string RoomType = "base";


    [Export] public Array<Vector2I> OutsDir;
}
