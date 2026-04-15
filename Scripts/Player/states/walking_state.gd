extends PlayerState


func _on_walking_state_physics_processing(delta: float) -> void:
	if player.input_component.sprint_pressed:
		player.state_chart.send_event("onSprinting")


func _on_walking_state_entered() -> void:
	player.movement_component.walk()
