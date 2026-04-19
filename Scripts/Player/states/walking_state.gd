extends PlayerState


func _on_walking_state_physics_processing(delta: float) -> void:
	if player.can_regenerate_stamina:
		player.stamina=min(100.0,player.stamina+delta*player.sps*0.5)
	if player.input_component.sprint_pressed and player.stamina>5.0:
		player.state_chart.send_event("onSprinting")


func _on_walking_state_entered() -> void:
	player.movement_component.walk()
