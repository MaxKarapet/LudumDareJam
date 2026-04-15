class_name CameraEffects
extends Camera3D

@export_category("References")
@export var player: Player

@export_category("Effects")
@export var enable_tilt:=true
@export var enable_fall_kick:=true

@export_category("Kick & Recoil Settings")
@export_group("Run Tilt")
@export var run_pitch:=0.1 #Degrees
@export var run_roll:=0.25
@export var max_pitch:=1.0
@export var max_roll:=2.5
@export_group("Camera Kick")
@export_group("Fall Kick")
@export var fall_time:=0.3

var _fall_value:=0.0
var _fall_timer:=0.0
func _physics_process(delta: float) -> void:
	calculate_view_offset(delta)

func calculate_view_offset(delta: float) -> void:
	if not player:
		return
	_fall_timer-=delta
	
	var velocity = player.velocity
	var angles = Vector3.ZERO

	var offset=Vector3.ZERO
	
	#Run Tilt
	if enable_tilt:
		var forward_dot = velocity.dot(global_transform.basis.z)
		var forward_tilt = clampf(forward_dot * deg_to_rad(run_pitch), deg_to_rad(-max_pitch), deg_to_rad(max_pitch))
		angles.x+=forward_tilt
		
		var right_dot = velocity.dot(global_transform.basis.x)
		var side_tilt = clampf(right_dot * deg_to_rad(run_roll), deg_to_rad(-max_roll), deg_to_rad(max_roll))
		angles.z-=side_tilt
	
	#Fall Kick
	if enable_fall_kick:
		angles.x -= max(0.0, _fall_timer/fall_time) * _fall_value
		offset.y -= max(0.0, _fall_timer/fall_time) * _fall_value

	position=offset
	rotation=angles
	
func add_fall_kick(fall_strenght: float) -> void:
	_fall_value = deg_to_rad(fall_strenght)
	_fall_timer = fall_time
	
