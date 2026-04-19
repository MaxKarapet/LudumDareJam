extends PlayerState


func _on_grounded_state_physics_processing(delta: float) -> void:
	if player.input_component.jump_pressed and player.is_on_floor():
		player.stamina-=15
		player.movement_component.jump()
		player.state_chart.send_event("onAirborne")
	if not player.is_on_floor():
		player.state_chart.send_event("onAirborne")
