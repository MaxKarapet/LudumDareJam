class_name Player extends CharacterBody3D

@onready var decal_scene=preload("res://Scenes/brush.tscn")

#COMPONENTS
@onready var input_component: InputComponent = %InputComponent
@onready var movement_component: MovementComponent = %MovementComponent
@onready var health_component: HealthComponent = %HealthComponent
@onready var mouse_capture_component: MouseCaptureComponent = %MouseCaptureComponent
@onready var camera_controller_component: CameraController = %CameraControllerComponent
@onready var step_handler: StepHandler = %StepHandler



#NODES
@export var debug:=false
@export_category("References")
@export var camera_anchor: Node3D
@export var camera_3d: CameraEffects
@export var state_chart: StateChart
@export var standing_collision: CollisionShape3D
@export var interaction_raycast: RayCast3D 
@export var red_decal_texture: Texture2D
@export_category("Properties")
@export var stamina:=100.0
@export var sps:=25.0


var can_move=true
var can_regenerate_stamina=true
var direction := Vector3.ZERO
func _ready() -> void:
	Globals.in_ghost_mode=false
	health_component.died.connect(_on_died)
	Globals.player=self
	Globals.emit_signal("PLAYER_SPAWNED", self)
func _unhandled_input(event: InputEvent) -> void:
	#HACK ИЗ-ЗА ВКЛЮЧЕННОГО В НАСТРОЙКАХ ПРОКТА Physics->Interpolation МОГУТ БЫТЬ НЕТОЧНЫЕ ДВИЖЕНИЯ МЫШИ!!!
	#MOUSE CONTROL    
	mouse_capture_component._input_tick(event)
	camera_controller_component.update_camera_rotation(mouse_capture_component.mouse_input)
	camera_anchor.rotation_degrees=camera_controller_component.camera_rotation
	rotation_degrees=camera_controller_component.player_rotation
	
func _physics_process(delta: float) -> void:
	if Input.is_action_just_pressed("dev_kill"):
		health_component.damage(9999)
	if not can_move:
		velocity=Vector3.ZERO
		return
	#READ CONTROLS
	input_component.update()
	if not Globals.in_ghost_mode:
		direction=(transform.basis*Vector3(input_component.move_dir.x,0,input_component.move_dir.y)).normalized()
	
		#READ MOVEMENT
		movement_component.direction.x=direction.x
		movement_component.direction.y=direction.z
		movement_component.tick(delta)
	else:
		var y = 0
		if input_component.up_pressed:
			y=min(1, y+1)
		if input_component.down_pressed:
			y=max(-1,y-1)
		direction=(transform.basis*Vector3(input_component.move_dir.x,y,input_component.move_dir.y)).normalized()
		var moded_speed=movement_component.max_walk_speed*movement_component.current_sprint_mod
		if direction:
			velocity=lerp(velocity, direction*moded_speed, movement_component.acceleration)
		else:
			velocity=velocity.move_toward(Vector3.ZERO,movement_component.deceleration)
		move_and_slide()
	if input_component.paint_pressed and interaction_raycast.current_object:
		_spawn_decal()
	#if is_on_floor():
	#	step_handler.handle_step_climbing()
	Globals.player_stamina=stamina
	Globals.player_pos=global_position
func _process(delta: float) -> void:
	"""
	if input_component.paint_pressed and interaction_raycast.current_object:
		var painter = _find_painter(interaction_raycast.current_object)
		if painter:
			painter.paint_at_face(
				interaction_raycast.get_collision_point(),
				interaction_raycast.global_position
			)
	"""
	pass
func _spawn_decal():

	# 1. Получаем данные о столкновении
	var pos = interaction_raycast.get_collision_point()
	var normal = interaction_raycast.get_collision_normal()
		
	# 2. Создаем экземпляр декали
	var decal = decal_scene.instantiate()
	get_tree().root.add_child(decal) # Добавляем в корень мира, чтобы она не двигалась за игроком
		
	# 3. Устанавливаем позицию
	decal.global_position = pos+ (normal * 0.01)
		
	# 4. Поворачиваем декаль
	align_decal(decal, normal)

func align_decal(node: Decal, normal: Vector3):
	# Если нормаль совпадает с вертикальной осью, используем другой вектор "вверх", 
	# чтобы избежать ошибок расчёта (Gimbal lock)
	var up_vector = Vector3.UP if abs(normal.dot(Vector3.UP)) < 0.99 else Vector3.FORWARD
	
	# Метод look_at заставляет ось -Z смотреть на цель.
	# Но так как декаль проецирует по оси Y, нам нужно повернуть её после look_at.
	node.look_at(node.global_position + normal, up_vector)
	node.rotate_object_local(Vector3.RIGHT, deg_to_rad(-90))
func _find_painter(node: Node) -> Node:
	# Ищем MeshPainter в самом коллайдере или его родителе
	if node.has_node("MeshPainter"):
		return node.get_node("MeshPainter")
	if node.get_parent() and node.get_parent().has_node("MeshPainter"):
		return node.get_parent().get_node("MeshPainter")
	return null
func _on_died() -> void:
	if not Globals.in_ghost_mode:
		state_chart.send_event("onGhost")

func jump() -> void:
	movement_component.wants_jump=input_component.jump_pressed


func _on_stamina_timeout() -> void:
	can_regenerate_stamina=true


func _on_stun_timeout() -> void:
	if Globals.in_ghost_mode:
		Globals.emit_signal("GHOST_TIMER_START")
	can_move=true
