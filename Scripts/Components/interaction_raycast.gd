extends RayCast3D
@export var debug:=false
var current_object

func _physics_process(delta: float) -> void:
	if is_colliding():
		var object=get_collider()
		if object==current_object:
			return
		else:
			current_object=object
	else:
		current_object=null
	
	if debug:
		print(current_object)
