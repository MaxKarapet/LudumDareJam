extends PlayerState



func _on_moving_state_physics_processing(delta: float) -> void:
	if player.direction.length()==0 and player.velocity.length()<0.5:
		player.state_chart.send_event("onIdle")
