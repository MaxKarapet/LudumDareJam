extends PlayerState

@export
var stun_time=3.0

func _on_ghost_state_physics_processing(delta: float) -> void:
	pass


func _on_ghost_state_entered() -> void:
	player.collision_mask=1
	player.collision_layer=64
	player.movement_component.sprint()
	Globals.emit_signal("GHOST_ENTERED")
	Globals.in_ghost_mode=true
	player.can_move=false
	%Stun.wait_time=stun_time
	%Stun.start()
