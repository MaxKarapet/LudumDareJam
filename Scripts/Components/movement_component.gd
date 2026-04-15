class_name MovementComponent
extends Node

@export var debug:=false
@export_category("References")
@export var body: CharacterBody3D
@export var model: Node3D

@export_category("Jump Settings")
@export var jump_velocity:=5.0
@export var fall_velocity_threshold:=-10.0
@export var double_jump_count:=1
@export var gravity_multiplier:=1.0
@export var air_capacity:=0.3
@export_category("Move Settings")
@export var sprint_mod:=2.0
@export var max_walk_speed:=5.0
@export var acceleration:=0.15
@export var deceleration:=0.20

var current_sprint_mod:=1.0
var wants_sprint:=false
var direction:= Vector2.ZERO
var current_velocity:=Vector2.ZERO
var current_fall_velocity:=0.0
func tick(delta: float) -> void:
	#Если не привязан body
	if body == null:
		return
	
	var moded_speed=max_walk_speed*current_sprint_mod
	
	if not body.is_on_floor():
		#Накладываем гравитацию
		body.velocity+=body.get_gravity()*delta*gravity_multiplier
		#Расчитываем вектор передвижения в воздухе
		if direction:
			current_velocity=lerp(current_velocity,Vector2(direction.x,direction.y)*moded_speed,acceleration*air_capacity)
		else:
			current_velocity=current_velocity.move_toward(Vector2.ZERO,deceleration/4.0)
	else:
		#Расчитываем вектор передвижения на земле
		if direction:
			current_velocity=lerp(current_velocity,Vector2(direction.x,direction.y)*moded_speed,acceleration)
		else:
			current_velocity=current_velocity.move_toward(Vector2.ZERO,deceleration)
	
	
	
	#Изменяем velocity
	body.velocity.x=current_velocity.x
	body.velocity.z=current_velocity.y

	#Выполняем движение
	body.move_and_slide()
	
	if debug:
		print("Velocity "+$"../..".name+" :", body.velocity)
	
#Прыжок
func jump(vel=jump_velocity) -> void:
	body.velocity.y=vel

#Моментально останавить движение
func stop_movement() -> void:
	current_velocity=Vector2.ZERO

func sprint() -> void:
	current_sprint_mod=sprint_mod
	
func walk() -> void:
	current_sprint_mod=1.0
	
func check_fall_speed() -> bool:
	
	if current_fall_velocity < fall_velocity_threshold:
		current_fall_velocity=0.0
		return true
	else:
		current_fall_velocity=0.0
		return false
