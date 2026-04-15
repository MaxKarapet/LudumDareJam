extends PlayerState


func _on_airborne_state_physics_processing(delta: float) -> void:
	if player.is_on_floor():
		if player.movement_component.check_fall_speed():
			player.camera_3d.add_fall_kick(2.0)
		player.state_chart.send_event("onGrounded")
		
	player.movement_component.current_fall_velocity=player.velocity.y
