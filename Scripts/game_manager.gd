class_name GameManager
extends Node

func _ready() -> void:
	Globals.connect("GHOST_TIMER_END", _restart_level)
func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("dev_quit"):
		get_tree().quit()
	if event.is_action_pressed("dev_reload"):
		get_tree().reload_current_scene()

func _restart_level() -> void:
	get_tree().reload_current_scene()
