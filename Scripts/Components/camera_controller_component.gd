class_name CameraController
extends Node

@export var debug:=false
@export var body: CharacterBody3D
@export_range(-90,-60) var tilt_lower_limit:=-60
@export_range(60,90) var tilt_upper_limit:=60

var rotation:=Vector3.ZERO
var player_rotation:=Vector3.ZERO
var camera_rotation:=Vector3.ZERO
func update_camera_rotation(input: Vector2) -> void:
	rotation.x-=input.y
	rotation.y-=input.x
	rotation.x=clamp(rotation.x,tilt_lower_limit,tilt_upper_limit)
	player_rotation=Vector3(0.0,rotation.y,0.0)
	camera_rotation=Vector3(rotation.x,0.0,0.0)
	
