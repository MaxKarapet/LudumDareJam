extends PlayerState



func _on_sprinting_state_physics_processing(delta: float) -> void:
	player.stamina-=delta*player.sps
	if player.stamina<=0.0:
		player.can_regenerate_stamina=false
		%Stamina.start()
		player.state_chart.send_event("onWalking")
	if not player.input_component.sprint_pressed:
		player.state_chart.send_event("onWalking")


func _on_sprinting_state_entered() -> void:
	player.movement_component.sprint()
