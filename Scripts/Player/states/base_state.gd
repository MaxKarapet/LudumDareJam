class_name PlayerState extends Node

@export var debug:=false

var player:Player
func _ready() -> void:
	if %StateMachine and %StateMachine is PlayerStateMachine:
		player=%StateMachine.player
