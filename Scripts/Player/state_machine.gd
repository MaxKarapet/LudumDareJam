class_name PlayerStateMachine extends Node

@export var debug:=false
@export_category("References")
@export var player: Player

func _process(delta: float) -> void:
	if debug:
		player.state_chart.set_expression_property("Player Y Velocity", player.velocity.y)
		player.state_chart.set_expression_property("Player Speed", Vector2(player.velocity.x,player.velocity.z).length())
		player.state_chart.set_expression_property("Looking At:", player.interaction_raycast.current_object)
