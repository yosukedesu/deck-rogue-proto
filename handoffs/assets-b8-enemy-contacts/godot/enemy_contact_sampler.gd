extends RefCounted
## Independent handoff sampler, not game integration. All ages are supplied by caller.
## In a port, transform local X towards the proven attacker, Y along screen-up,
## scale by visible receiver height and translate to the locked receiving contact.

var frames: Array[ArrayMesh] = []
var durations_ms: Array = []
var total_ms: float = 0.0
var material: ShaderMaterial

func load_effect(root: String, effect: String) -> Error:
	frames.clear()
	durations_ms.clear()
	total_ms = 0.0
	if not effect in ["acid", "poison", "curse", "roar", "bell", "song", "cold", "lightning"]:
		return ERR_INVALID_PARAMETER
	var text := FileAccess.get_file_as_string(root.path_join("Geometry/enemy_" + effect + "_contact.json"))
	var parsed = JSON.parse_string(text)
	if not parsed is Dictionary or parsed.get("schema", "") != "deckrogue.contact-mesh.v1":
		return ERR_PARSE_ERROR
	durations_ms = parsed["duration_ms"]
	for ms in durations_ms:
		total_ms += float(ms)
	var image := Image.load_from_file(root.path_join("Art/fx/enemy_" + effect + "_contact_8f.png"))
	if image == null or image.is_empty():
		return ERR_FILE_CORRUPT
	material = ShaderMaterial.new()
	material.shader = load(root.path_join("godot/enemy_contact.gdshader"))
	material.set_shader_parameter("contact_atlas", ImageTexture.create_from_image(image))
	for frame in parsed["frames"]:
		var mesh := ArrayMesh.new()
		if not frame["vertices"].is_empty():
			var vertices := PackedVector3Array()
			var uvs := PackedVector2Array()
			for v in frame["vertices"]:
				vertices.append(Vector3(float(v[0]), float(v[1]), float(v[2])))
			for uv in frame["uv"]:
				uvs.append(Vector2(float(uv[0]), float(uv[1])))
			var arrays := []
			arrays.resize(Mesh.ARRAY_MAX)
			arrays[Mesh.ARRAY_VERTEX] = vertices
			arrays[Mesh.ARRAY_TEX_UV] = uvs
			arrays[Mesh.ARRAY_INDEX] = PackedInt32Array(frame["indices"])
			mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
			mesh.surface_set_material(0, material)
		frames.append(mesh)
	return OK

func frame_at_age(age_seconds: float) -> int:
	if not is_finite(age_seconds) or age_seconds < 0.0 or age_seconds * 1000.0 >= total_ms:
		return -1
	var end := 0.0
	for index in range(durations_ms.size()):
		end += float(durations_ms[index])
		if age_seconds * 1000.0 < end:
			return index
	return -1

func mesh_at_age(age_seconds: float) -> ArrayMesh:
	var index := frame_at_age(age_seconds)
	if index < 0:
		return null
	return frames[index]
