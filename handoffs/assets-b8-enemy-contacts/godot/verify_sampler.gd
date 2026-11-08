extends SceneTree
var checks := 0
var failures: Array[String] = []
func check(ok: bool, label: String) -> void:
	checks += 1
	if not ok: failures.append(label)
func _initialize() -> void:
	var root := ProjectSettings.globalize_path("res://")
	var script = load(root.path_join("godot/enemy_contact_sampler.gd"))
	for effect in ["acid", "poison", "curse", "roar", "bell", "song", "cold", "lightning"]:
		var sampler = script.new()
		check(sampler.load_effect(root, effect) == OK, effect + " load")
		check(sampler.frames.size() == 8, effect + " eight meshes")
		check(sampler.frame_at_age(-0.01) == -1, effect + " negative age hidden")
		check(sampler.frame_at_age(INF) == -1, effect + " nonfinite hidden")
		var ms := 0.0
		for f in range(8):
			check(sampler.frame_at_age((ms + 0.001) / 1000.0) == f, effect + " boundary " + str(f))
			check(sampler.frames[f].get_surface_count() == (0 if f == 7 else 1), effect + " surface " + str(f))
			ms += float(sampler.durations_ms[f])
		check(sampler.frame_at_age(ms / 1000.0 + 0.00001) == -1, effect + " finished hidden")
		check(sampler.mesh_at_age(-1.0) == null, effect + " released mesh")
	var result := {"checks": checks, "failures": failures, "godot": Engine.get_version_info()["string"], "runtime_game_integration_tested": false, "gpu_render_tested": false}
	FileAccess.open(root.path_join("qa/godot_verification.json"), FileAccess.WRITE).store_string(JSON.stringify(result, "  ") + "\n")
	print(JSON.stringify(result))
	quit(0 if failures.is_empty() else 1)
