class_name InputComponent extends Node

@export var debug:=false


var move_dir: Vector2 = Vector2.ZERO
var jump_pressed: bool = false
var sprint_pressed: bool = false

func update():
	move_dir=Input.get_vector("move_left","move_right","move_up","move_down")
	jump_pressed=Input.is_action_just_pressed("jump")
	sprint_pressed=Input.is_action_pressed("sprint")
