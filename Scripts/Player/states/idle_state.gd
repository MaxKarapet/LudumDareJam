extends PlayerState


func _on_idle_state_physics_processing(delta: float) -> void:
	if player.can_regenerate_stamina:
		player.stamina=min(100.0,player.stamina+delta*player.sps*1)
	if player and player.direction.length()>0:
		player.state_chart.send_event("onMoving")
