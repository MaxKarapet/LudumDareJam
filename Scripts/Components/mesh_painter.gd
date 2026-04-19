extends Node
class_name MeshPainter
@export var paint_color: Color = Color.RED
@export var brush_radius_uv: float = 0.04

var paint_image: Image
var paint_texture: ImageTexture
var mesh_instance: MeshInstance3D

var _verts: PackedVector3Array
var _uvs: PackedVector2Array
var _indices: PackedInt32Array
var _dirty := false

func _ready() -> void:
	mesh_instance = get_parent() as MeshInstance3D
	
	_setup_texture()
	_cache_mesh_data()

func _process(_delta: float) -> void:
	if _dirty:
		paint_texture.update(paint_image)
		_dirty = false

func _setup_texture() -> void:
	var size := _calculate_texture_size()
	
	# Получаем оригинальный материал или создаем новый
	var mat = mesh_instance.get_surface_override_material(0)
	if mat == null:
		mat = StandardMaterial3D.new()

	var has_original_texture := false

	# Проверяем, есть ли на материале текстура
	if mat.albedo_texture != null:
		# Извлекаем Image из текстуры
		var orig_image = mat.albedo_texture.get_image()
		
		if orig_image != null:
			# ВАЖНО 1: Дублируем изображение, чтобы не изменить исходный файл в памяти
			paint_image = orig_image.duplicate()
			
			# ВАЖНО 2: Импортированные текстуры часто сжаты для видеокарты (VRAM).
			# Для попиксельного рисования их обязательно нужно конвертировать в RGBA8.
			if paint_image.get_format() != Image.FORMAT_RGBA8:
				paint_image.convert(Image.FORMAT_RGBA8)
				
			# Опционально: подгоняем под нужный вам размер, если текстура другого разрешения
			if paint_image.get_size() != Vector2i(size, size):
				paint_image.resize(size, size, Image.INTERPOLATE_BILINEAR)
				
			has_original_texture = true

	# Если оригинальной текстуры не оказалось, создаем чистую белую (как у вас раньше)
	if not has_original_texture:
		paint_image = Image.create(size, size, false, Image.FORMAT_RGBA8)
		paint_image.fill(Color.WHITE)

	# Создаем обновляемую текстуру из нашего Image
	paint_texture = ImageTexture.create_from_image(paint_image)

	# ВАЖНО 3: Дублируем материал перед назначением.
	# Если этого не сделать, вы измените материал для всех объектов в игре, 
	# на которых он висит.
	var override_mat = mat.duplicate()
	override_mat.albedo_texture = paint_texture
	mesh_instance.material_override = override_mat

func _calculate_texture_size() -> int:
	var aabb := mesh_instance.get_aabb()
	var area := (aabb.size.x * aabb.size.y + aabb.size.y * aabb.size.z + aabb.size.x * aabb.size.z) * 2.0
	if area < 2.0:  return 128
	if area < 10.0: return 256
	if area < 50.0: return 512
	return 1024

func _cache_mesh_data() -> void:
	var arrays := mesh_instance.mesh.surface_get_arrays(0)
	_verts   = arrays[Mesh.ARRAY_VERTEX]
	_uvs     = arrays[Mesh.ARRAY_TEX_UV]
	_indices = arrays[Mesh.ARRAY_INDEX]

func paint_at_face(world_hit: Vector3, world_ray_origin: Vector3) -> void:
	var inv := mesh_instance.global_transform.affine_inverse()
	var local_hit    := inv * world_hit
	var local_origin := inv * world_ray_origin
	var ray_dir := (local_hit - local_origin).normalized()
	
	var uv := _find_uv_by_ray(local_origin, ray_dir)
	if uv.x < 0.0:
		return
	
	_draw_circle(uv)
	_dirty = true

# Полноценная трассировка луча через все треугольники
func _find_uv_by_ray(origin: Vector3, dir: Vector3) -> Vector2:
	var face_count := _indices.size() / 3 if _indices.size() > 0 else _verts.size() / 3
	var has_idx := _indices.size() > 0
	
	var best_t := INF
	var best_uv := Vector2(-1, -1)
	
	for i in range(face_count):
		var i0 := _indices[i*3]     if has_idx else i*3
		var i1 := _indices[i*3 + 1] if has_idx else i*3 + 1
		var i2 := _indices[i*3 + 2] if has_idx else i*3 + 2
		
		var v0 := _verts[i0]
		var v1 := _verts[i1]
		var v2 := _verts[i2]
		
		# Алгоритм Мёллера–Трумбора
		var t := _ray_triangle_intersect(origin, dir, v0, v1, v2)
		
		if t > 0.0 and t < best_t:
			best_t = t
			var hit_point = origin + dir * t
			var b := _bary(hit_point, v0, v1, v2)
			best_uv = _uvs[i0] * b.x + _uvs[i1] * b.y + _uvs[i2] * b.z
	
	return best_uv
# Пересечение луча с треугольником (Möller–Trumbore)
func _ray_triangle_intersect(o: Vector3, d: Vector3, v0: Vector3, v1: Vector3, v2: Vector3) -> float:
	var edge1 := v1 - v0
	var edge2 := v2 - v0
	var h := d.cross(edge2)
	var a := edge1.dot(h)
	
	if abs(a) < 1e-6:
		return -1.0
	
	var f := 1.0 / a
	var s := o - v0
	var u := f * s.dot(h)
	
	if u < 0.0 or u > 1.0:
		return -1.0
	
	var q := s.cross(edge1)
	var v := f * d.dot(q)
	
	if v < 0.0 or u + v > 1.0:
		return -1.0
	
	return f * edge2.dot(q)
func _find_uv_brute(local_hit: Vector3) -> Vector2:
	var best_dist := INF
	var best_uv   := Vector2(-1, -1)
	var face_count := _indices.size() / 3 if _indices.size() > 0 else _verts.size() / 3
	for i in range(face_count):
		var has_idx := _indices.size() > 0
		var i0 := _indices[i*3] if has_idx else i*3
		var i1 := _indices[i*3+1] if has_idx else i*3+1
		var i2 := _indices[i*3+2] if has_idx else i*3+2
		var center := (_verts[i0] + _verts[i1] + _verts[i2]) / 3.0
		var dist   := local_hit.distance_to(center)
		if dist < best_dist:
			best_dist = dist
			var b := _bary(local_hit, _verts[i0], _verts[i1], _verts[i2])
			best_uv = _uvs[i0] * b.x + _uvs[i1] * b.y + _uvs[i2] * b.z
	return best_uv

func _bary(p: Vector3, a: Vector3, b: Vector3, c: Vector3) -> Vector3:
	var v0 := b-a; var v1 := c-a; var v2 := p-a
	var d00 := v0.dot(v0); var d01 := v0.dot(v1); var d11 := v1.dot(v1)
	var d20 := v2.dot(v0); var d21 := v2.dot(v1)
	var den := d00*d11 - d01*d01
	if abs(den) < 1e-6: return Vector3(1,0,0)
	var bv := (d11*d20 - d01*d21) / den
	var bw := (d00*d21 - d01*d20) / den
	return Vector3(1.0-bv-bw, bv, bw)

func _draw_circle(uv: Vector2) -> void:
	var size := paint_image.get_width()
	var cx   := int(uv.x * size)
	var cy   := int(uv.y * size)
	var r    := int(brush_radius_uv * size)
	for dy in range(-r, r+1):
		for dx in range(-r, r+1):
			if dx*dx + dy*dy <= r*r:
				paint_image.set_pixel(clamp(cx+dx, 0, size-1), clamp(cy+dy, 0, size-1), paint_color)
