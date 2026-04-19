extends CanvasLayer
#$Main/Stamina
var ghost_started=false
var initial_time:float
func _ready() -> void:
	$Main/TimerText.label_settings=LabelSettings.new()
	Globals.connect("GHOST_TIMER_START",_start_ghost_timer)
	Globals.connect("GHOST_ENTERED",_hide_ghost_things)
func _start_ghost_timer():
	$Main/GhostLayout.mouse_filter=2
	$Main/TimerText.visible=true
	initial_time=$Main/GhostTime.wait_time
	$Main/GhostTime.start()
	ghost_started=true
	var tween = create_tween()
	tween.set_parallel()
	tween.tween_property($Main/TimerText.label_settings, "font_size", 80, initial_time)\
		.set_trans(Tween.TRANS_QUAD)\
		.set_ease(Tween.EASE_OUT)
	tween.tween_property($Main/TimerText.label_settings, "font_color", Color(1,0,0,1), initial_time)\
		.set_trans(Tween.TRANS_QUAD)\
		.set_ease(Tween.EASE_OUT)
func _process(delta: float) -> void:
	$Main/Stamina.value=lerpf($Main/Stamina.value, Globals.player.stamina,0.1)
	if ghost_started:
		var time = $Main/GhostTime.time_left
		$Main/TimerText.text="%02d:%03d" % [int(time)%60,fmod(time,1)*1000]

func _on_ghost_time_timeout() -> void:
	$Main/TimerText.text="00:000"
	$Main/ColorRect.visible=true
	$Timer.start()
	get_tree().paused=true


func _on_timer_timeout() -> void:
	get_tree().paused=false
	Globals.emit_signal("GHOST_TIMER_END")

func _hide_ghost_things():
	$Main/GhostLayout.visible=true
	$Main/Stamina.visible=false
