class_name Player extends CharacterBody3D



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



var direction := Vector3.ZERO
func _ready() -> void:
	health_component.died.connect(_on_died)

func _unhandled_input(event: InputEvent) -> void:
	#HACK ИЗ-ЗА ВКЛЮЧЕННОГО В НАСТРОЙКАХ ПРОКТА Physics->Interpolation МОГУТ БЫТЬ НЕТОЧНЫЕ ДВИЖЕНИЯ МЫШИ!!!
	#MOUSE CONTROL    
	mouse_capture_component._input_tick(event)
	camera_controller_component.update_camera_rotation(mouse_capture_component.mouse_input)
	camera_anchor.rotation_degrees=camera_controller_component.camera_rotation
	rotation_degrees=camera_controller_component.player_rotation

func _physics_process(delta: float) -> void:
	#READ CONTROLS
	input_component.update()
	direction=(transform.basis*Vector3(input_component.move_dir.x,0,input_component.move_dir.y)).normalized()
	
	#READ MOVEMENT
	movement_component.direction.x=direction.x

	movement_component.direction.y=direction.z
	movement_component.tick(delta)
	if is_on_floor():
		step_handler.handle_step_climbing()
	

func _on_died() -> void:
	print("die")

func jump() -> void:
	movement_component.wants_jump=input_component.jump_pressed
