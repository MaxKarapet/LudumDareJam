extends Node

signal DUNGEON_GENERATED
signal PLAYER_SPAWNED(emitter)
signal GHOST_ENTERED
signal GHOST_TIMER_START
signal GHOST_TIMER_END
var player: Player
var player_pos: Vector3
var room_array: Array[Node3D]
var player_stamina: float
var in_ghost_mode: bool=false
func _reset():
	player=null
	player_pos=Vector3.ZERO
	room_array=[]
	player_stamina=0.0
	in_ghost_mode=false
