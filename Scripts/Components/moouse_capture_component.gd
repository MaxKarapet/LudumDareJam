class_name MouseCaptureComponent
extends Node

@export var debug:=false
@export var current_mouse_mode:= Input.MOUSE_MODE_CAPTURED
@export var mouse_sensitivity:= 0.2

var capture_mouse:=false
var mouse_input:=Vector2.ZERO

func _input_tick(event: InputEvent) -> void:
	capture_mouse = event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED
	if capture_mouse:
		mouse_input=event.relative*mouse_sensitivity
	else:
		mouse_input=Vector2.ZERO

	if debug:
		print("Mouse Input: ", mouse_input)

func _ready() -> void:
	Input.mouse_mode=current_mouse_mode
	
