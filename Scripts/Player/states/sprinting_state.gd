extends PlayerState



func _on_sprinting_state_physics_processing(delta: float) -> void:
	
	if not player.input_component.sprint_pressed:
		player.state_chart.send_event("onWalking")


func _on_sprinting_state_entered() -> void:
	player.movement_component.sprint()
