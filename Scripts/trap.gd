extends Node3D
class_name Trap
@export var trans_mat: StandardMaterial3D
@export var meshes: Array[MeshInstance3D]
func _ready() -> void:
	Globals.connect("GHOST_ENTERED",_become_transparent)
	Globals.connect("GHOST_TIMER_END",_become_normal)
	
func _become_transparent() -> void:
	for mesh in meshes:
		mesh.material_override=trans_mat
	
func _become_normal() -> void:
	for mesh in meshes:
		mesh.material_override=null
