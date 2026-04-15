extends PlayerState


func _on_idle_state_physics_processing(delta: float) -> void:
	if player and player.direction.length()>0:
		player.state_chart.send_event("onMoving")
