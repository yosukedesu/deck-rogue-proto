extends Node3D
# Asset-only viewer. Deliberately no production StageWorld/engine dependency.
var display: Node3D
var title: Label
var manifest: Dictionary

func _ready() -> void:
	manifest = JSON.parse_string(FileAccess.get_file_as_string("res://manifest.json"))
	var environment := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("101d25")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("9aaab8")
	env.ambient_light_energy = 0.5
	environment.environment = env
	add_child(environment)
	var light := DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-55,-35,0)
	light.light_color = Color("ffe2b2")
	light.light_energy = 1.5
	light.shadow_enabled = true
	add_child(light)
	var fill := DirectionalLight3D.new()
	fill.rotation_degrees = Vector3(-30,145,0)
	fill.light_color = Color("9ac9e8")
	fill.light_energy = 0.5
	add_child(fill)
	var camera := Camera3D.new()
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 29
	camera.position = Vector3(15,23,27)
	add_child(camera)
	camera.look_at(Vector3(0,2.2,-3.5))
	camera.current = true
	var ground := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(34,30)
	ground.mesh = plane
	ground.position = Vector3(0,-0.025,-3.5)
	var surface := StandardMaterial3D.new()
	surface.albedo_color = Color("18262c")
	surface.roughness = 1.0
	ground.material_override = surface
	add_child(ground)
	var canvas := CanvasLayer.new()
	add_child(canvas)
	title = Label.new()
	title.position = Vector2(24,20)
	title.add_theme_color_override("font_color",Color("d0b574"))
	title.add_theme_font_size_override("font_size",22)
	canvas.add_child(title)
	_show_act(2)

func _show_act(act: int) -> void:
	if is_instance_valid(display):
		remove_child(display)
		display.queue_free()
	display = Node3D.new()
	add_child(display)
	var index := 0
	for item in manifest["modules"]:
		if int(item["act"]) != act:
			continue
		var packed = load("res://"+str(item["glb"])) as PackedScene
		assert(packed != null,"Missing GLB: "+str(item["glb"]))
		var module = packed.instantiate() as Node3D
		module.position = Vector3((index%4-1.5)*6.2,0,-floori(index/4.0)*8)
		display.add_child(module)
		var label := Label3D.new()
		label.text = str(item["id"]).replace("act%d_" % act,"")
		label.font_size = 32
		label.pixel_size = 0.009
		label.position = module.position + Vector3(0,0.07,2.1)
		label.rotation_degrees.x = -90
		label.modulate = Color("d0b574")
		label.no_depth_test = true
		display.add_child(label)
		index += 1
	title.text = "C5 / ACT %d / 8 MODULES   [1] ACT2   [2] ACT3\n1 unit = 1 metre / asset preview only / not production game" % act

func _unhandled_key_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_1:
			_show_act(2)
		elif event.keycode == KEY_2:
			_show_act(3)
