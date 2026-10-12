using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/StageWorld.cs")]
public class StageWorld : Node3D
{
	private sealed class Geometry
	{
		private readonly List<Vector3> _vertices = new List<Vector3>();

		private readonly List<Vector3> _normals = new List<Vector3>();

		private readonly List<Vector2> _uv = new List<Vector2>();

		private readonly List<Color> _colors = new List<Color>();

		private readonly List<int> _indices = new List<int>();

		public void Face(Vector3[] points, Vector3 outward, float ao = 1f, Vector2[]? uvs = null)
		{
			if (points.Length >= 3)
			{
				Vector3 normal = (points[1] - points[0]).Cross(points[2] - points[0]).Normalized();
				if (normal.LengthSquared() < 0.001f)
				{
					normal = outward.Normalized();
				}
				if (normal.Dot(outward) < 0f)
				{
					normal = -normal;
				}
				int start = _vertices.Count;
				for (int i = 0; i < points.Length; i++)
				{
					_vertices.Add(points[i]);
					_normals.Add(normal);
					_uv.Add((uvs == null) ? new Vector2(points[i].X, points[i].Z) : uvs[i]);
					_colors.Add(new Color(ao, ao, ao));
				}
				bool reverse = (points[1] - points[0]).Cross(points[2] - points[0]).Dot(normal) > 0f;
				for (int j = 1; j < points.Length - 1; j++)
				{
					_indices.Add(start);
					_indices.Add(start + (reverse ? (j + 1) : j));
					_indices.Add(start + (reverse ? j : (j + 1)));
				}
			}
		}

		public void Append(Godot.Collections.Array arrays, Transform3D transform)
		{
			Vector3[] vertices = arrays[0].AsVector3Array();
			Vector3[] normals = arrays[1].AsVector3Array();
			Vector2[] uv = arrays[4].AsVector2Array();
			Color[] colors = ((arrays[3].VariantType == Variant.Type.Nil) ? System.Array.Empty<Color>() : arrays[3].AsColorArray());
			int[] indices = ((arrays[12].VariantType == Variant.Type.Nil) ? System.Array.Empty<int>() : arrays[12].AsInt32Array());
			int start = _vertices.Count;
			Basis normalTransform = transform.Basis.Inverse().Transposed();
			for (int i = 0; i < vertices.Length; i++)
			{
				_vertices.Add(transform * vertices[i]);
				_normals.Add((normalTransform * ((i < normals.Length) ? normals[i] : Vector3.Up)).Normalized());
				_uv.Add((i < uv.Length) ? uv[i] : Vector2.Zero);
				_colors.Add((i < colors.Length) ? colors[i] : Colors.White);
			}
			if (indices.Length == 0)
			{
				for (int j = 0; j < vertices.Length; j++)
				{
					_indices.Add(start + j);
				}
				return;
			}
			int[] array = indices;
			foreach (int i2 in array)
			{
				_indices.Add(start + i2);
			}
		}

		public ArrayMesh ToMesh()
		{
			ArrayMesh mesh = new ArrayMesh();
			if (_vertices.Count == 0)
			{
				return mesh;
			}
			Godot.Collections.Array arrays = new Godot.Collections.Array();
			arrays.Resize(13);
			arrays[0] = _vertices.ToArray();
			arrays[1] = _normals.ToArray();
			arrays[4] = _uv.ToArray();
			arrays[3] = _colors.ToArray();
			arrays[12] = _indices.ToArray();
			Vector3[] tangent = new Vector3[_vertices.Count];
			Vector3[] bitangent = new Vector3[_vertices.Count];
			for (int i = 0; i + 2 < _indices.Count; i += 3)
			{
				int a = _indices[i];
				int b = _indices[i + 1];
				int c = _indices[i + 2];
				Vector3 e1 = _vertices[b] - _vertices[a];
				Vector3 e2 = _vertices[c] - _vertices[a];
				Vector2 d1 = _uv[b] - _uv[a];
				Vector2 d2 = _uv[c] - _uv[a];
				float det = d1.X * d2.Y - d1.Y * d2.X;
				if (!(Math.Abs(det) < 1E-06f))
				{
					Vector3 t = (e1 * d2.Y - e2 * d1.Y) / det;
					Vector3 bt = (e2 * d1.X - e1 * d2.X) / det;
					int[] array = new int[3] { a, b, c };
					foreach (int v in array)
					{
						tangent[v] += t;
						bitangent[v] += bt;
					}
				}
			}
			float[] tangents = new float[_vertices.Count * 4];
			for (int k = 0; k < _vertices.Count; k++)
			{
				Vector3 n = _normals[k];
				Vector3 t2 = tangent[k] - n * n.Dot(tangent[k]);
				if (t2.LengthSquared() < 1E-06f)
				{
					t2 = ((Math.Abs(n.Dot(Vector3.Up)) > 0.95f) ? Vector3.Right : Vector3.Up.Cross(n));
				}
				t2 = t2.Normalized();
				tangents[k * 4] = t2.X;
				tangents[k * 4 + 1] = t2.Y;
				tangents[k * 4 + 2] = t2.Z;
				tangents[k * 4 + 3] = ((!(n.Cross(t2).Dot(bitangent[k]) < 0f)) ? 1 : (-1));
			}
			arrays[2] = tangents;
			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, (Mesh.ArrayFormat)0L);
			return mesh;
		}
	}

	public new class MethodName : Node3D.MethodName
	{
		public static readonly StringName Build = "Build";

		public static readonly StringName UpdateCameraFraming = "UpdateCameraFraming";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName MakeLighting = "MakeLighting";

		public static readonly StringName MakeForestLighting = "MakeForestLighting";

		public static readonly StringName MakeForestMaterial = "MakeForestMaterial";

		public static readonly StringName BuildForest = "BuildForest";

		public static readonly StringName BuildForestBackdrop = "BuildForestBackdrop";

		public static readonly StringName BuildLayout = "BuildLayout";

		public static readonly StringName OnPath = "OnPath";

		public static readonly StringName GroundY = "GroundY";

		public static readonly StringName Surface = "Surface";

		public static readonly StringName TileRotation = "TileRotation";

		public static readonly StringName ResolveTiles = "ResolveTiles";

		public static readonly StringName AddMesh = "AddMesh";

		public static readonly StringName Footprint = "Footprint";

		public static readonly StringName Box = "Box";

		public static readonly StringName ReliefMesh = "ReliefMesh";

		public static readonly StringName BatchStaticMeshes = "BatchStaticMeshes";

		public static readonly StringName AddLayoutLights = "AddLayoutLights";

		public static readonly StringName Rgb = "Rgb";

		public static readonly StringName ResetForestSourceLights = "ResetForestSourceLights";

		public static readonly StringName BindForestSourceLights = "BindForestSourceLights";
	}

	public new class PropertyName : Node3D.PropertyName
	{
		public static readonly StringName Lighting = "Lighting";

		public static readonly StringName Camera = "Camera";

		public static readonly StringName Act = "Act";

		public static readonly StringName PhoneLayout = "PhoneLayout";

		public static readonly StringName ForestAttackLightCount = "ForestAttackLightCount";

		public static readonly StringName _mobilePerformance = "_mobilePerformance";

		public static readonly StringName _forestShader = "_forestShader";

		public static readonly StringName _forestDistanceScale = "_forestDistanceScale";

		public static readonly StringName _pathAngle = "_pathAngle";

		public static readonly StringName _static = "_static";

		public static readonly StringName _dynamic = "_dynamic";

		public static readonly StringName _surfaceShader = "_surfaceShader";

		public static readonly StringName _airShader = "_airShader";

		public static readonly StringName _elapsed = "_elapsed";

		public static readonly StringName _forestPositions = "_forestPositions";

		public static readonly StringName _forestColors = "_forestColors";
	}

	public new class SignalName : Node3D.SignalName
	{
	}

	private bool _mobilePerformance;

	private JObject _layout = new JObject();

	private JObject _forestLook = new JObject();

	private Shader _forestShader = null;

	private float _forestDistanceScale = 1f;

	private float _pathAngle = Mathf.DegToRad(22f);

	private Node3D _static = null;

	private Node3D _dynamic = null;

	private readonly System.Collections.Generic.Dictionary<string, Material> _materials = new System.Collections.Generic.Dictionary<string, Material>();

	private readonly System.Collections.Generic.Dictionary<string, Texture2D> _textures = new System.Collections.Generic.Dictionary<string, Texture2D>();

	private readonly System.Collections.Generic.Dictionary<string, Mesh> _reliefMeshes = new System.Collections.Generic.Dictionary<string, Mesh>();

	private readonly System.Collections.Generic.Dictionary<string, Node3D> _parts = new System.Collections.Generic.Dictionary<string, Node3D>();

	private readonly List<JObject> _slabs = new List<JObject>();

	private readonly List<(OmniLight3D light, float energy, float amount, float phase)> _lamps = new List<(OmniLight3D, float, float, float)>();

	private readonly List<(Node3D node, Vector3 position, float sway, float phase)> _sway = new List<(Node3D, Vector3, float, float)>();

	private Shader _surfaceShader = null;

	private Shader _airShader = null;

	private double _elapsed;

	private static readonly Vector2[] QuadUv = new Vector2[4]
	{
		Vector2.Zero,
		Vector2.Right,
		Vector2.One,
		Vector2.Down
	};

	private const string ForestShader = "\nshader_type spatial;\nrender_mode cull_disabled, diffuse_lambert, ambient_light_disabled, fog_disabled;\nuniform sampler2D albedo_texture : source_color, filter_linear_mipmap_anisotropic, repeat_enable;\nuniform vec4 base_color : source_color = vec4(1.0);\nuniform float alpha_cutoff=0.0;\nuniform float static_neutral=0.5;\nuniform float receive=0.8;\nuniform float fog_weight=1.0;\nuniform vec3 static_tint=vec3(1.0);\nuniform float static_saturation=1.0;\nuniform vec3 ambient_floor=vec3(0.06325,0.0805,0.09775);\nuniform vec3 ambient_sky=vec3(0.128,0.222,0.339);\nuniform vec3 ambient_equator=vec3(0.084,0.145,0.221);\nuniform vec3 ambient_ground=vec3(0.042,0.065,0.097);\nuniform float grade_weight=0.18;\nuniform vec4 night_color=vec4(0.0331,0.0685,0.1406,1.0);\nuniform float night_strength=0.7;\nuniform float vignette_weight=0.6;\nuniform float vignette_side=0.5;\nuniform float vignette_top=0.06;\nuniform float lower_fade=0.0;\nuniform float hue_study=1.0;\nuniform float distance_scale=1.67;\nuniform float path_angle=0.383972;\n// Numeric source colors are linear on BOTH rendering paths. Texture samples\n// alone need explicit decoding on Compatibility (OUTPUT_IS_SRGB).\nuniform vec4 fog_color=vec4(0.1065,0.1626,0.2738,1.0);\nuniform vec3 height_color=vec3(0.1065,0.1548,0.2330);\nuniform vec4 height_range=vec4(-0.15,1.2,26.0,42.0);\nuniform float height_density=0.18;\nuniform vec3 far_fog=vec3(32.0,58.0,0.6);\nuniform vec3 lobe_color=vec3(0.2352,0.3684,0.6084);\nuniform vec3 lobe_shape=vec3(650.0,90.0,1.0);\nuniform vec3 seat_light_color=vec3(0.8284,1.3996,2.6);\nuniform vec2 fog_range=vec2(22.0,48.0);\nuniform vec3 lobe_position=vec3(0.0,2.2,-80.0);\nuniform vec3 lamp_emission=vec3(0.0);\n// Source intensity/color, independent of native Godot Omni attenuation.\nuniform int forest_light_count=0;\nuniform vec4 forest_light_position_range[8];\nuniform vec4 forest_light_color_kind[8];\nvarying vec3 forest_position;\nvarying vec3 forest_normal;\nvarying vec3 forest_surface;\nvarying vec3 forest_original;\nvarying float forest_visibility;\nvarying float forest_hue;\nfloat luminance(vec3 c) { return dot(c,vec3(0.2126,0.7152,0.0722)); }\nvec3 forest_decode(vec3 c) {\n    return mix(pow(max((c+vec3(0.055))/1.055,vec3(0.0)),vec3(2.4)),c/12.92,lessThanEqual(c,vec3(0.04045)));\n}\nvec3 forest_output(vec3 c) {\n    if(!OUTPUT_IS_SRGB)return c;\n    return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-vec3(0.055),c*12.92,lessThanEqual(c,vec3(0.0031308)));\n}\nvec3 night_grade(vec3 col) {\n    float l=luminance(col); float bright=smoothstep(0.05,0.32,l);\n    vec3 navy=night_color.rgb*l/max(luminance(night_color.rgb),0.0001);\n    return mix(col,navy,night_strength*(1.0-bright)*grade_weight);\n}\nvoid vertex() { forest_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; forest_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL); }\nvoid fragment() {\n    vec4 tex=texture(albedo_texture,UV);vec4 tint=base_color;\n    if(OUTPUT_IS_SRGB){tex.rgb=forest_decode(tex.rgb);tint.rgb=forest_decode(tint.rgb);}\n    tex*=tint;\n    if(tex.a<alpha_cutoff)discard;\n    vec3 n=normalize(forest_normal); if(!FRONT_FACING)n=-n;\n    forest_original=tex.rgb;\n    forest_surface=mix(vec3(luminance(tex.rgb)),tex.rgb,static_saturation)*static_tint;\n    float c=cos(path_angle),s=sin(path_angle);\n    vec2 path=vec2(forest_position.x*c-forest_position.z*s,-forest_position.x*s-forest_position.z*c);\n    float edge=min(min(forest_position.x+9.0,-3.0-forest_position.x),min(-forest_position.z+1.0,7.0+forest_position.z));\n    forest_hue=hue_study*smoothstep(0.0,0.75,edge);\n    vec3 amb=n.y>=0.0?mix(ambient_equator,ambient_sky,n.y):mix(ambient_equator,ambient_ground,-n.y);\n    amb=max(mix(amb,vec3(luminance(amb)),static_neutral),ambient_floor);\n    amb*=mix(vec3(1.0),mix(vec3(0.90,0.80,1.12),vec3(0.88,0.98,1.10),max(0.0,n.y)),forest_hue);\n    // Source seat lamp mask and pool in path coordinates, restricted to surfaces.\n    float dt=max(max(-6.5-path.x,path.x-15.0),0.0);\n    float ds=path.y< -1.4?(-1.4-path.y)/3.2:max(0.0,(path.y-1.8)/4.0);\n    float band=(1.0-smoothstep(0.0,1.1,dt))*(1.0-smoothstep(0.0,1.0,ds));\n    vec2 pool=(path-vec2(3.0,4.8))/vec2(6.5,5.0); pool.x-=pool.y*0.4;\n    float pool_weight=1.0-smoothstep(0.65,1.0,length(pool));\n    vec3 key_dir=normalize(vec3(-8.0,17.0,14.0));\n    float ndl=max(0.0,dot(n,key_dir));\n    vec3 key=seat_light_color*ndl*(0.05+0.95*band)*(0.6+0.7*pool_weight);\n    key=mix(key,vec3(luminance(key)),static_neutral*(1.0-forest_hue));\n    float shade=mix(1.0,mix(0.10,1.0,smoothstep(-3.0,0.05,forest_position.y)),lower_fade);\n    vec3 ordinary_points=vec3(0.0),attack_points=vec3(0.0);\n    for(int i=0;i<8;i++) {\n        if(i>=forest_light_count)break;\n        vec3 delta=forest_light_position_range[i].xyz-forest_position;\n        float distance2=max(dot(delta,delta),0.00006103515625);\n        float range=max(forest_light_position_range[i].w,0.0001);\n        float ratio=distance2/(range*range);\n        float smooth_factor=max(0.0,1.0-ratio*ratio);\n        float attenuation=smooth_factor*smooth_factor/distance2;\n        float point_ndl=max(0.0,dot(n,delta*inversesqrt(distance2)));\n        vec3 contribution=forest_light_color_kind[i].rgb*(attenuation*point_ndl);\n        if(forest_light_color_kind[i].w>0.5)attack_points+=contribution;\n        else ordinary_points+=contribution;\n    }\n    vec3 neutral_points=mix(ordinary_points,vec3(luminance(ordinary_points)),static_neutral*(1.0-forest_hue));\n    vec3 col=forest_surface*(amb+receive*(key+neutral_points))*shade;\n    col+=forest_original*receive*attack_points;\n    vec3 unfogged=col;\n    vec3 view_pos=(VIEW_MATRIX*vec4(forest_position,1.0)).xyz; float depth=-view_pos.z;\n    vec3 view_dir=normalize(forest_position-CAMERA_POSITION_WORLD),lobe_dir=normalize(lobe_position-CAMERA_POSITION_WORLD);\n    float dv=asin(clamp(view_dir.y,-1.0,1.0))-asin(clamp(lobe_dir.y,-1.0,1.0));\n    // FOREST_FOG_SOURCE_AZIMUTH: reflect Godot -Z back to authored Unity +Z.\n    // Keep the source rear +/-PI branch away from the forward field of view.\n    float dh=atan(view_dir.x,-view_dir.z)-atan(lobe_dir.x,-lobe_dir.z);\n    float lobe=exp(-0.5*(lobe_shape.x*dv*dv+lobe_shape.y*dh*dh));\n    float fog_edge=mix(lobe_shape.z,1.0,lobe);\n    vec3 fc=fog_color.rgb*fog_edge+lobe_color*lobe;\n    float hf=height_density*clamp((height_range.y-forest_position.y)/max(height_range.y-height_range.x,0.001),0.0,1.0)*clamp((depth/distance_scale-height_range.z)/max(height_range.w-height_range.z,0.001),0.0,1.0);\n    float df=clamp((depth/distance_scale-fog_range.x)/(fog_range.y-fog_range.x),0.0,1.0);\n    float ff=far_fog.z*clamp((depth/distance_scale-far_fog.x)/max(far_fog.y-far_fog.x,0.001),0.0,1.0);\n    forest_visibility=(1.0-hf)*(1.0-df)*(1.0-ff);\n    col=mix(col,height_color*fog_edge,hf); col=mix(col,fc,df); col=mix(col,fc,ff);\n    col=mix(unfogged,col,fog_weight);forest_visibility=mix(1.0,forest_visibility,fog_weight);\n    vec4 clip=PROJECTION_MATRIX*vec4(view_pos,1.0); vec2 screen=clip.xy/max(clip.w,0.001);\n    float side=smoothstep(0.3,0.5,abs(screen.x)*0.5);\n    float seat_dt=max(max(-6.5-path.x,path.x-15.0),0.0),seat_ds=max(max(-1.9-path.y,path.y-2.3),0.0);\n    side*=1.0-(1.0-smoothstep(0.0,1.6,seat_dt))*(1.0-smoothstep(0.0,1.6,seat_ds));\n    float top=1.0-smoothstep(0.0,0.14,0.5-screen.y*0.5);\n    float vignette=mix(1.0,(1.0-vignette_side*side)*(1.0-vignette_top*top),vignette_weight);\n    forest_visibility*=vignette*shade;\n    ALBEDO=vec3(1.0); ROUGHNESS=0.92; SPECULAR=0.0;\n    EMISSION=forest_output(night_grade(col*vignette)+lamp_emission*forest_visibility);\n    // FOREST_MATERIAL_FACTORS_LINEAR: custom light() receives linear LIGHT_COLOR\n    // on both renderers. Godot decodes ALBEDO/EMISSION at its Compatibility\n    // boundary, but does not decode these user varyings. Keep their source-linear\n    // values; only the EMISSION output above needs forest_output().\n}\nvoid light() {\n    // Point lights were combined in linear source space before fog/grade above.\n    // Suppress their native contribution, including attack endpoints, to avoid\n    // double counting. Native moon shadow visibility is retained independently;\n    // its composition and source backlight shadows remain optical-parity gaps.\n    if(LIGHT_IS_DIRECTIONAL) {\n        float ndl=max(0.0,dot(NORMAL,LIGHT));\n        vec3 direct=mix(LIGHT_COLOR,vec3(luminance(LIGHT_COLOR)),static_neutral*(1.0-forest_hue));\n        vec3 contribution=forest_surface*receive*direct*ATTENUATION*ndl*forest_visibility/3.14159265;\n        DIFFUSE_LIGHT+=!OUTPUT_IS_SRGB?night_grade(contribution):contribution;\n    }\n}";

	private const string SurfaceShader = "\nshader_type spatial;\nrender_mode cull_disabled, diffuse_lambert;\nuniform sampler2D side0 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D side1 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D side2 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D side3 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top0 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top1 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top2 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top3 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform vec4 tint : source_color = vec4(1.0);\nuniform float path_angle = 0.383972;\nuniform float top_threshold = 0.65;\nuniform float top_blend = 0.10;\nuniform int side_rotation = 0;\nuniform int top_rotation = 0;\nvarying vec3 stage_position;\nvarying vec3 stage_normal;\nfloat hash2(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }\nvec2 tile_uv(vec2 p,int rotation) {\n    vec2 cell=floor(p); vec2 uv=fract(p);\n    float r=hash2(cell+17.3);\n    if(rotation==1 && r>0.5) uv.x=1.0-uv.x;\n    if(rotation==2) { if(r<0.25) uv=vec2(uv.y,1.0-uv.x); else if(r<0.5) uv=1.0-uv; else if(r<0.75) uv=vec2(1.0-uv.y,uv.x); }\n    return uv;\n}\nvec3 side_sample(vec2 p) {\n    vec2 uv=tile_uv(p,side_rotation); float i=floor(hash2(floor(p))*4.0);\n    if(i<1.0)return texture(side0,uv).rgb;\n    if(i<2.0)return texture(side1,uv).rgb;\n    if(i<3.0)return texture(side2,uv).rgb;\n    return texture(side3,uv).rgb;\n}\nvec3 top_sample(vec2 p) {\n    vec2 uv=tile_uv(p,top_rotation); float i=floor(hash2(floor(p))*4.0);\n    if(i<1.0)return texture(top0,uv).rgb;\n    if(i<2.0)return texture(top1,uv).rgb;\n    if(i<3.0)return texture(top2,uv).rgb;\n    return texture(top3,uv).rgb;\n}\nvoid vertex() {\n    stage_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;\n    stage_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL);\n}\nvoid fragment() {\n    float c=cos(path_angle),s=sin(path_angle);\n    vec3 p=vec3(stage_position.x*c-stage_position.z*s,stage_position.y,-stage_position.x*s-stage_position.z*c);\n    vec3 n=vec3(stage_normal.x*c-stage_normal.z*s,stage_normal.y,-stage_normal.x*s-stage_normal.z*c);\n    vec2 side_uv=abs(n.z)>=abs(n.x)?vec2(p.x,-p.y):vec2(p.z,-p.y);\n    float top_mix=smoothstep(top_threshold-top_blend,top_threshold+top_blend,n.y);\n    ALBEDO=mix(side_sample(side_uv*0.390625),top_sample(p.xz*0.390625),top_mix)*tint.rgb*COLOR.rgb;\n    ROUGHNESS=0.96; SPECULAR=0.12;\n}";

	private const string AirShader = "\nshader_type spatial;\nrender_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;\nuniform vec4 air_color : source_color = vec4(0.4,0.5,0.6,0.1);\nuniform int mode=0;\nuniform float flow=0.012;\nuniform float phase=0.0;\nuniform float core=0.35;\nvoid fragment() {\n    float mask=0.0;\n    if(mode==0) { float d=length((UV-0.5)*2.0); mask=pow(max(0.0,1.0-d),2.2); }\n    else if(mode==2) { float edge=sin(UV.x*3.14159); mask=pow(max(0.0,edge),3.0)*smoothstep(0.0,0.16,UV.y)*(1.0-smoothstep(0.7,1.0,UV.y)); }\n    else { float n=0.65+0.18*sin(UV.x*61.0+TIME*flow*20.0+phase)*sin(UV.y*9.0+phase); mask=n*sin(UV.y*3.14159)*smoothstep(0.0,0.06,UV.x)*(1.0-smoothstep(0.94,1.0,UV.x)); }\n    ALBEDO=air_color.rgb; ALPHA=clamp(mask*air_color.a,0.0,0.6);\n}";

	private const int ForestLightLimit = 8;

	private readonly List<ShaderMaterial> _forestMaterials = new List<ShaderMaterial>();

	private readonly List<(OmniLight3D Node, float SourceIntensity, float NativeBase)> _forestSteadyLights = new List<(OmniLight3D, float, float)>();

	private readonly List<ActorMaterialBinding.PointLight> _forestAttackLights = new List<ActorMaterialBinding.PointLight>(2);

	private readonly List<ActorMaterialBinding.PointLight> _forestSourceLights = new List<ActorMaterialBinding.PointLight>(8);

	private readonly Vector4[] _forestPositions = new Vector4[8];

	private readonly Vector4[] _forestColors = new Vector4[8];

	public Godot.Environment Lighting { get; private set; } = null;

	public Camera3D Camera { get; private set; } = null;

	public int Act { get; private set; }

	public bool PhoneLayout { get; private set; }

	public SourceCameraLayoutResult CameraLayout { get; private set; }

	public IReadOnlyList<ActorMaterialBinding.PointLight> ForestSourceLights => _forestSourceLights;

	public int ForestAttackLightCount => _forestAttackLights.Count;

	public event Action? CameraLayoutChanged;

	public void Build(int act)
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}
		_materials.Clear();
		_textures.Clear();
		_reliefMeshes.Clear();
		_parts.Clear();
		_slabs.Clear();
		_lamps.Clear();
		_sway.Clear();
		_elapsed = 0.0;
		ResetForestSourceLights();
		Act = Math.Clamp(act, 1, 3);
		string[] args = OS.GetCmdlineUserArgs();
		_mobilePerformance = OS.HasFeature("android") || OS.HasFeature("ios") || args.Contains("--ui-phone");
		PhoneLayout = _mobilePerformance;
		for (int i = 0; i + 1 < args.Length; i++)
		{
			if (args[i] == "-uiscale" && float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var uiScale))
			{
				PhoneLayout = uiScale > 1.001f;
			}
		}
		if (Act > 1)
		{
			_surfaceShader = new Shader
			{
				Code = "\nshader_type spatial;\nrender_mode cull_disabled, diffuse_lambert;\nuniform sampler2D side0 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D side1 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D side2 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D side3 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top0 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top1 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top2 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform sampler2D top3 : source_color, filter_nearest_mipmap, repeat_enable;\nuniform vec4 tint : source_color = vec4(1.0);\nuniform float path_angle = 0.383972;\nuniform float top_threshold = 0.65;\nuniform float top_blend = 0.10;\nuniform int side_rotation = 0;\nuniform int top_rotation = 0;\nvarying vec3 stage_position;\nvarying vec3 stage_normal;\nfloat hash2(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }\nvec2 tile_uv(vec2 p,int rotation) {\n    vec2 cell=floor(p); vec2 uv=fract(p);\n    float r=hash2(cell+17.3);\n    if(rotation==1 && r>0.5) uv.x=1.0-uv.x;\n    if(rotation==2) { if(r<0.25) uv=vec2(uv.y,1.0-uv.x); else if(r<0.5) uv=1.0-uv; else if(r<0.75) uv=vec2(1.0-uv.y,uv.x); }\n    return uv;\n}\nvec3 side_sample(vec2 p) {\n    vec2 uv=tile_uv(p,side_rotation); float i=floor(hash2(floor(p))*4.0);\n    if(i<1.0)return texture(side0,uv).rgb;\n    if(i<2.0)return texture(side1,uv).rgb;\n    if(i<3.0)return texture(side2,uv).rgb;\n    return texture(side3,uv).rgb;\n}\nvec3 top_sample(vec2 p) {\n    vec2 uv=tile_uv(p,top_rotation); float i=floor(hash2(floor(p))*4.0);\n    if(i<1.0)return texture(top0,uv).rgb;\n    if(i<2.0)return texture(top1,uv).rgb;\n    if(i<3.0)return texture(top2,uv).rgb;\n    return texture(top3,uv).rgb;\n}\nvoid vertex() {\n    stage_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;\n    stage_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL);\n}\nvoid fragment() {\n    float c=cos(path_angle),s=sin(path_angle);\n    vec3 p=vec3(stage_position.x*c-stage_position.z*s,stage_position.y,-stage_position.x*s-stage_position.z*c);\n    vec3 n=vec3(stage_normal.x*c-stage_normal.z*s,stage_normal.y,-stage_normal.x*s-stage_normal.z*c);\n    vec2 side_uv=abs(n.z)>=abs(n.x)?vec2(p.x,-p.y):vec2(p.z,-p.y);\n    float top_mix=smoothstep(top_threshold-top_blend,top_threshold+top_blend,n.y);\n    ALBEDO=mix(side_sample(side_uv*0.390625),top_sample(p.xz*0.390625),top_mix)*tint.rgb*COLOR.rgb;\n    ROUGHNESS=0.96; SPECULAR=0.12;\n}"
			};
			_airShader = new Shader
			{
				Code = "\nshader_type spatial;\nrender_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;\nuniform vec4 air_color : source_color = vec4(0.4,0.5,0.6,0.1);\nuniform int mode=0;\nuniform float flow=0.012;\nuniform float phase=0.0;\nuniform float core=0.35;\nvoid fragment() {\n    float mask=0.0;\n    if(mode==0) { float d=length((UV-0.5)*2.0); mask=pow(max(0.0,1.0-d),2.2); }\n    else if(mode==2) { float edge=sin(UV.x*3.14159); mask=pow(max(0.0,edge),3.0)*smoothstep(0.0,0.16,UV.y)*(1.0-smoothstep(0.7,1.0,UV.y)); }\n    else { float n=0.65+0.18*sin(UV.x*61.0+TIME*flow*20.0+phase)*sin(UV.y*9.0+phase); mask=n*sin(UV.y*3.14159)*smoothstep(0.0,0.06,UV.x)*(1.0-smoothstep(0.94,1.0,UV.x)); }\n    ALBEDO=air_color.rgb; ALPHA=clamp(mask*air_color.a,0.0,0.6);\n}"
			};
		}
		_static = new Node3D
		{
			Name = "Static"
		};
		AddChild(_static, forceReadableName: false, InternalMode.Disabled);
		_dynamic = new Node3D
		{
			Name = "Dynamic"
		};
		AddChild(_dynamic, forceReadableName: false, InternalMode.Disabled);
		Camera = new Camera3D
		{
			Name = "BattleCamera",
			Position = new Vector3(0f, 3.421816f, 27.58734f),
			RotationDegrees = new Vector3(-5f, 0f, 0f),
			Fov = 22f,
			KeepAspect = Camera3D.KeepAspectEnum.Height,
			Near = 0.05f,
			Far = 160f,
			Current = true
		};
		AddChild(Camera, forceReadableName: false, InternalMode.Disabled);
		UpdateCameraFraming();
		MakeLighting(Act);
		if (Act == 1)
		{
			BuildForest();
		}
		else
		{
			BuildLayout(Act);
		}
	}

	private void UpdateCameraFraming()
	{
		if (Camera == null)
		{
			return;
		}
		Vector2 size = GetViewport().GetVisibleRect().Size;
		if (size.Y <= 0f || size.X <= 0f)
		{
			return;
		}
		SourceCameraLayoutResult layout = SourceCameraLayout.Compute(size, PhoneLayout);
		if (CameraLayout == layout && Camera.Position == layout.Position && Camera.Fov == layout.Fov)
		{
			return;
		}
		CameraLayout = layout;
		Camera.Position = layout.Position;
		Camera.RotationDegrees = layout.RotationDegrees;
		Camera.Fov = layout.Fov;
		if (Act == 1 && _forestMaterials.Count > 0)
		{
			_forestDistanceScale = layout.ReferenceDistance / Num(_forestLook, "baseCameraDistance", 16.62f);
			foreach (ShaderMaterial material in _forestMaterials)
			{
				material.SetShaderParameter("distance_scale", _forestDistanceScale);
			}
			JObject fog = _forestLook["fog"] as JObject;
			Lighting.FogDepthBegin = Num(fog, "start", 22f) * _forestDistanceScale;
			Lighting.FogDepthEnd = Num(fog, "end", 48f) * _forestDistanceScale;
		}
		this.CameraLayoutChanged?.Invoke();
	}

	public override void _Process(double delta)
	{
		UpdateCameraFraming();
		_elapsed += delta;
		foreach (var lamp in _lamps)
		{
			OmniLight3D light = lamp.light;
			float energy = lamp.energy;
			float amount = lamp.amount;
			float phase = lamp.phase;
			light.LightEnergy = energy * (1f + amount * (0.62f * Mathf.Sin((float)_elapsed * 7.3f + phase) + 0.38f * Mathf.Sin((float)_elapsed * 13.1f + phase * 2.2f)));
		}
		foreach (var item in _sway)
		{
			Node3D node = item.node;
			Vector3 position = item.position;
			float sway = item.sway;
			float phase2 = item.phase;
			node.Position = position + Vector3.Right * (sway * Mathf.Sin((float)_elapsed * 1.7f + phase2));
		}
		BindForestSourceLights();
	}

	private void MakeLighting(int act)
	{
		if (act == 1)
		{
			MakeForestLighting();
			return;
		}
		Lighting = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = ((act == 1) ? new Color(0.075f, 0.09f, 0.11f) : new Color(0.035f, 0.045f, 0.055f)),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = ((act == 1) ? new Color(0.64f, 0.68f, 0.72f) : new Color(0.47f, 0.5f, 0.55f)),
			AmbientLightEnergy = ((act == 1) ? 0.58f : 0.48f),
			AmbientLightSkyContribution = 0f,
			ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
			TonemapExposure = 1.12f,
			FogEnabled = true,
			FogLightColor = ((act == 1) ? new Color(0.13f, 0.17f, 0.2f) : new Color(0.12f, 0.15f, 0.18f)),
			FogDensity = ((act == 1) ? 0.007f : 0.008f),
			FogSkyAffect = 0f,
			GlowEnabled = true,
			GlowIntensity = 0.22f,
			GlowHdrThreshold = 1.4f
		};
		AddChild(new WorldEnvironment
		{
			Name = "StageEnvironment",
			Environment = Lighting
		}, forceReadableName: false, InternalMode.Disabled);
		DirectionalLight3D moon = new DirectionalLight3D
		{
			Name = "MoonKey",
			RotationDegrees = new Vector3(-48f, -32f, 0f),
			LightColor = new Color(0.88f, 0.92f, 1f),
			LightEnergy = ((act == 1) ? 0.9f : 0.6f),
			ShadowEnabled = true,
			DirectionalShadowMaxDistance = 90f,
			ShadowBias = 0.025f,
			ShadowNormalBias = 0.6f,
			LightAngularDistance = 1.5f
		};
		AddChild(moon, forceReadableName: false, InternalMode.Disabled);
		OmniLight3D fill = new OmniLight3D
		{
			Name = "BattleReadabilityFill",
			Position = new Vector3(-1f, 7f, 6f),
			LightColor = new Color(0.95f, 0.96f, 1f),
			LightEnergy = ((act == 1) ? 1.8f : 2.1f),
			OmniRange = 22f,
			OmniAttenuation = 1.3f,
			ShadowEnabled = false
		};
		AddChild(fill, forceReadableName: false, InternalMode.Disabled);
	}

	private void MakeForestLighting()
	{
		_forestLook = new JObject();
		List<string> files = new List<string> { "look_act1", "look_act1.char", "look_act1.dof", "look_act1_preview_night" };
		if (_mobilePerformance)
		{
			files.Add("look_act1.phone");
		}
		files.Add("look_act1_integrated");
		foreach (string file in files)
		{
			string path = "res://assets/Stage/" + file + ".json";
			if (FileAccess.FileExists(path))
			{
				_forestLook.Merge(JObject.Parse(FileAccess.GetFileAsString(path)), new JsonMergeSettings
				{
					MergeArrayHandling = MergeArrayHandling.Replace,
					MergeNullValueHandling = MergeNullValueHandling.Ignore
				});
			}
		}
		_forestShader = new Shader
		{
			Code = "\nshader_type spatial;\nrender_mode cull_disabled, diffuse_lambert, ambient_light_disabled, fog_disabled;\nuniform sampler2D albedo_texture : source_color, filter_linear_mipmap_anisotropic, repeat_enable;\nuniform vec4 base_color : source_color = vec4(1.0);\nuniform float alpha_cutoff=0.0;\nuniform float static_neutral=0.5;\nuniform float receive=0.8;\nuniform float fog_weight=1.0;\nuniform vec3 static_tint=vec3(1.0);\nuniform float static_saturation=1.0;\nuniform vec3 ambient_floor=vec3(0.06325,0.0805,0.09775);\nuniform vec3 ambient_sky=vec3(0.128,0.222,0.339);\nuniform vec3 ambient_equator=vec3(0.084,0.145,0.221);\nuniform vec3 ambient_ground=vec3(0.042,0.065,0.097);\nuniform float grade_weight=0.18;\nuniform vec4 night_color=vec4(0.0331,0.0685,0.1406,1.0);\nuniform float night_strength=0.7;\nuniform float vignette_weight=0.6;\nuniform float vignette_side=0.5;\nuniform float vignette_top=0.06;\nuniform float lower_fade=0.0;\nuniform float hue_study=1.0;\nuniform float distance_scale=1.67;\nuniform float path_angle=0.383972;\n// Numeric source colors are linear on BOTH rendering paths. Texture samples\n// alone need explicit decoding on Compatibility (OUTPUT_IS_SRGB).\nuniform vec4 fog_color=vec4(0.1065,0.1626,0.2738,1.0);\nuniform vec3 height_color=vec3(0.1065,0.1548,0.2330);\nuniform vec4 height_range=vec4(-0.15,1.2,26.0,42.0);\nuniform float height_density=0.18;\nuniform vec3 far_fog=vec3(32.0,58.0,0.6);\nuniform vec3 lobe_color=vec3(0.2352,0.3684,0.6084);\nuniform vec3 lobe_shape=vec3(650.0,90.0,1.0);\nuniform vec3 seat_light_color=vec3(0.8284,1.3996,2.6);\nuniform vec2 fog_range=vec2(22.0,48.0);\nuniform vec3 lobe_position=vec3(0.0,2.2,-80.0);\nuniform vec3 lamp_emission=vec3(0.0);\n// Source intensity/color, independent of native Godot Omni attenuation.\nuniform int forest_light_count=0;\nuniform vec4 forest_light_position_range[8];\nuniform vec4 forest_light_color_kind[8];\nvarying vec3 forest_position;\nvarying vec3 forest_normal;\nvarying vec3 forest_surface;\nvarying vec3 forest_original;\nvarying float forest_visibility;\nvarying float forest_hue;\nfloat luminance(vec3 c) { return dot(c,vec3(0.2126,0.7152,0.0722)); }\nvec3 forest_decode(vec3 c) {\n    return mix(pow(max((c+vec3(0.055))/1.055,vec3(0.0)),vec3(2.4)),c/12.92,lessThanEqual(c,vec3(0.04045)));\n}\nvec3 forest_output(vec3 c) {\n    if(!OUTPUT_IS_SRGB)return c;\n    return mix(1.055*pow(max(c,vec3(0.0)),vec3(1.0/2.4))-vec3(0.055),c*12.92,lessThanEqual(c,vec3(0.0031308)));\n}\nvec3 night_grade(vec3 col) {\n    float l=luminance(col); float bright=smoothstep(0.05,0.32,l);\n    vec3 navy=night_color.rgb*l/max(luminance(night_color.rgb),0.0001);\n    return mix(col,navy,night_strength*(1.0-bright)*grade_weight);\n}\nvoid vertex() { forest_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; forest_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL); }\nvoid fragment() {\n    vec4 tex=texture(albedo_texture,UV);vec4 tint=base_color;\n    if(OUTPUT_IS_SRGB){tex.rgb=forest_decode(tex.rgb);tint.rgb=forest_decode(tint.rgb);}\n    tex*=tint;\n    if(tex.a<alpha_cutoff)discard;\n    vec3 n=normalize(forest_normal); if(!FRONT_FACING)n=-n;\n    forest_original=tex.rgb;\n    forest_surface=mix(vec3(luminance(tex.rgb)),tex.rgb,static_saturation)*static_tint;\n    float c=cos(path_angle),s=sin(path_angle);\n    vec2 path=vec2(forest_position.x*c-forest_position.z*s,-forest_position.x*s-forest_position.z*c);\n    float edge=min(min(forest_position.x+9.0,-3.0-forest_position.x),min(-forest_position.z+1.0,7.0+forest_position.z));\n    forest_hue=hue_study*smoothstep(0.0,0.75,edge);\n    vec3 amb=n.y>=0.0?mix(ambient_equator,ambient_sky,n.y):mix(ambient_equator,ambient_ground,-n.y);\n    amb=max(mix(amb,vec3(luminance(amb)),static_neutral),ambient_floor);\n    amb*=mix(vec3(1.0),mix(vec3(0.90,0.80,1.12),vec3(0.88,0.98,1.10),max(0.0,n.y)),forest_hue);\n    // Source seat lamp mask and pool in path coordinates, restricted to surfaces.\n    float dt=max(max(-6.5-path.x,path.x-15.0),0.0);\n    float ds=path.y< -1.4?(-1.4-path.y)/3.2:max(0.0,(path.y-1.8)/4.0);\n    float band=(1.0-smoothstep(0.0,1.1,dt))*(1.0-smoothstep(0.0,1.0,ds));\n    vec2 pool=(path-vec2(3.0,4.8))/vec2(6.5,5.0); pool.x-=pool.y*0.4;\n    float pool_weight=1.0-smoothstep(0.65,1.0,length(pool));\n    vec3 key_dir=normalize(vec3(-8.0,17.0,14.0));\n    float ndl=max(0.0,dot(n,key_dir));\n    vec3 key=seat_light_color*ndl*(0.05+0.95*band)*(0.6+0.7*pool_weight);\n    key=mix(key,vec3(luminance(key)),static_neutral*(1.0-forest_hue));\n    float shade=mix(1.0,mix(0.10,1.0,smoothstep(-3.0,0.05,forest_position.y)),lower_fade);\n    vec3 ordinary_points=vec3(0.0),attack_points=vec3(0.0);\n    for(int i=0;i<8;i++) {\n        if(i>=forest_light_count)break;\n        vec3 delta=forest_light_position_range[i].xyz-forest_position;\n        float distance2=max(dot(delta,delta),0.00006103515625);\n        float range=max(forest_light_position_range[i].w,0.0001);\n        float ratio=distance2/(range*range);\n        float smooth_factor=max(0.0,1.0-ratio*ratio);\n        float attenuation=smooth_factor*smooth_factor/distance2;\n        float point_ndl=max(0.0,dot(n,delta*inversesqrt(distance2)));\n        vec3 contribution=forest_light_color_kind[i].rgb*(attenuation*point_ndl);\n        if(forest_light_color_kind[i].w>0.5)attack_points+=contribution;\n        else ordinary_points+=contribution;\n    }\n    vec3 neutral_points=mix(ordinary_points,vec3(luminance(ordinary_points)),static_neutral*(1.0-forest_hue));\n    vec3 col=forest_surface*(amb+receive*(key+neutral_points))*shade;\n    col+=forest_original*receive*attack_points;\n    vec3 unfogged=col;\n    vec3 view_pos=(VIEW_MATRIX*vec4(forest_position,1.0)).xyz; float depth=-view_pos.z;\n    vec3 view_dir=normalize(forest_position-CAMERA_POSITION_WORLD),lobe_dir=normalize(lobe_position-CAMERA_POSITION_WORLD);\n    float dv=asin(clamp(view_dir.y,-1.0,1.0))-asin(clamp(lobe_dir.y,-1.0,1.0));\n    // FOREST_FOG_SOURCE_AZIMUTH: reflect Godot -Z back to authored Unity +Z.\n    // Keep the source rear +/-PI branch away from the forward field of view.\n    float dh=atan(view_dir.x,-view_dir.z)-atan(lobe_dir.x,-lobe_dir.z);\n    float lobe=exp(-0.5*(lobe_shape.x*dv*dv+lobe_shape.y*dh*dh));\n    float fog_edge=mix(lobe_shape.z,1.0,lobe);\n    vec3 fc=fog_color.rgb*fog_edge+lobe_color*lobe;\n    float hf=height_density*clamp((height_range.y-forest_position.y)/max(height_range.y-height_range.x,0.001),0.0,1.0)*clamp((depth/distance_scale-height_range.z)/max(height_range.w-height_range.z,0.001),0.0,1.0);\n    float df=clamp((depth/distance_scale-fog_range.x)/(fog_range.y-fog_range.x),0.0,1.0);\n    float ff=far_fog.z*clamp((depth/distance_scale-far_fog.x)/max(far_fog.y-far_fog.x,0.001),0.0,1.0);\n    forest_visibility=(1.0-hf)*(1.0-df)*(1.0-ff);\n    col=mix(col,height_color*fog_edge,hf); col=mix(col,fc,df); col=mix(col,fc,ff);\n    col=mix(unfogged,col,fog_weight);forest_visibility=mix(1.0,forest_visibility,fog_weight);\n    vec4 clip=PROJECTION_MATRIX*vec4(view_pos,1.0); vec2 screen=clip.xy/max(clip.w,0.001);\n    float side=smoothstep(0.3,0.5,abs(screen.x)*0.5);\n    float seat_dt=max(max(-6.5-path.x,path.x-15.0),0.0),seat_ds=max(max(-1.9-path.y,path.y-2.3),0.0);\n    side*=1.0-(1.0-smoothstep(0.0,1.6,seat_dt))*(1.0-smoothstep(0.0,1.6,seat_ds));\n    float top=1.0-smoothstep(0.0,0.14,0.5-screen.y*0.5);\n    float vignette=mix(1.0,(1.0-vignette_side*side)*(1.0-vignette_top*top),vignette_weight);\n    forest_visibility*=vignette*shade;\n    ALBEDO=vec3(1.0); ROUGHNESS=0.92; SPECULAR=0.0;\n    EMISSION=forest_output(night_grade(col*vignette)+lamp_emission*forest_visibility);\n    // FOREST_MATERIAL_FACTORS_LINEAR: custom light() receives linear LIGHT_COLOR\n    // on both renderers. Godot decodes ALBEDO/EMISSION at its Compatibility\n    // boundary, but does not decode these user varyings. Keep their source-linear\n    // values; only the EMISSION output above needs forest_output().\n}\nvoid light() {\n    // Point lights were combined in linear source space before fog/grade above.\n    // Suppress their native contribution, including attack endpoints, to avoid\n    // double counting. Native moon shadow visibility is retained independently;\n    // its composition and source backlight shadows remain optical-parity gaps.\n    if(LIGHT_IS_DIRECTIONAL) {\n        float ndl=max(0.0,dot(NORMAL,LIGHT));\n        vec3 direct=mix(LIGHT_COLOR,vec3(luminance(LIGHT_COLOR)),static_neutral*(1.0-forest_hue));\n        vec3 contribution=forest_surface*receive*direct*ATTENUATION*ndl*forest_visibility/3.14159265;\n        DIFFUSE_LIGHT+=!OUTPUT_IS_SRGB?night_grade(contribution):contribution;\n    }\n}"
		};
		_forestDistanceScale = Camera.Position.Dot(Camera.Basis.Z) / Num(_forestLook, "baseCameraDistance", 16.62f);
		JObject fog = _forestLook["fog"] as JObject;
		JObject moon = _forestLook["moon"] as JObject;
		JObject ambient = _forestLook["ambient"] as JObject;
		JObject bloom = ((_forestLook["post"] is JObject post) ? post["bloom"] : null) as JObject;
		Lighting = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = Col(_forestLook["camera"]?["background"], new Color(0.06f, 0.09f, 0.12f)),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = Col(ambient?["equator"], new Color(0.084f, 0.145f, 0.221f)),
			AmbientLightEnergy = 1f,
			AmbientLightSkyContribution = 0f,
			ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
			TonemapExposure = 1.12f,
			FogEnabled = true,
			FogMode = Godot.Environment.FogModeEnum.Depth,
			FogLightColor = Col(fog?["color"], new Color(0.36f, 0.44f, 0.56f)),
			FogDepthBegin = Num(fog, "start", 22f) * _forestDistanceScale,
			FogDepthEnd = Num(fog, "end", 48f) * _forestDistanceScale,
			FogDepthCurve = 1f,
			FogDensity = 1f,
			FogSkyAffect = 0f,
			GlowEnabled = true,
			GlowIntensity = Num(bloom, "intensity", 0.48f),
			GlowHdrThreshold = Num(bloom, "threshold", 1.3f)
		};
		AddChild(new WorldEnvironment
		{
			Name = "StageEnvironment",
			Environment = Lighting
		}, forceReadableName: false, InternalMode.Disabled);
		JArray euler = moon?["euler"] as JArray;
		AddChild(new DirectionalLight3D
		{
			Name = "MoonKey",
			RotationDegrees = ((euler == null) ? new Vector3(-48f, -38f, 0f) : new Vector3(0f - (float)euler[0], 0f - (float)euler[1], (float)euler[2])),
			LightColor = Col(moon?["color"], new Color(0.57f, 0.68f, 0.88f)),
			LightEnergy = Num(moon, "intensity", 0.32f),
			ShadowEnabled = true,
			DirectionalShadowMaxDistance = Num(_forestLook["shadow"] as JObject, "distance", 42f) * _forestDistanceScale,
			ShadowBias = Num(moon, "shadowBias", 0.05f),
			ShadowNormalBias = Num(moon, "shadowNormalBias", 0.4f),
			LightAngularDistance = 1.5f
		}, forceReadableName: false, InternalMode.Disabled);
		JObject back = _forestLook["backlight"] as JObject;
		if (Bool(back, "on", fallback: true))
		{
			AddLamp("ForestBacklight", OnPath(Num(back, "t", 9.5f), Num(back, "s", 23f), Num(back, "y", 5.2f)), Col(back?["color"], new Color(0.5f, 0.64f, 0.9f)), Num(back, "illum", 1.4f), Num(back, "range", 16f), 0f, 4f, Num(back, "illum", 1.4f) * Mathf.Pow(Num(back, "illumDist", 5f), 2f));
		}
	}

	private Material MakeForestMaterial(StandardMaterial3D source)
	{
		string name = source.ResourceName.ToLowerInvariant();
		bool foliage = name.Contains("cutout") || name.Contains("foliage");
		JObject preview = _forestLook["forestPreview"] as JObject;
		ShaderMaterial material = new ShaderMaterial
		{
			Shader = _forestShader,
			ResourceName = source.ResourceName + "_AdoptedNight"
		};
		_forestMaterials.Add(material);
		Texture2D texture = source.AlbedoTexture;
		if (texture == null)
		{
			Image white = Image.CreateEmpty(1, 1, useMipmaps: false, Image.Format.Rgba8);
			white.Fill(Colors.White);
			texture = ImageTexture.CreateFromImage(white);
		}
		material.SetShaderParameter("albedo_texture", texture);
		material.SetShaderParameter("base_color", source.AlbedoColor);
		material.SetShaderParameter("alpha_cutoff", foliage ? 0.4f : 0f);
		material.SetShaderParameter("receive", foliage ? 0.55f : 0.8f);
		material.SetShaderParameter("static_neutral", Num(preview, "staticNeutral", 0.5f));
		material.SetShaderParameter("grade_weight", Num(preview, "gradeWeight", 0.18f));
		material.SetShaderParameter("vignette_weight", Num(preview, "vignetteWeight", 0.6f));
		material.SetShaderParameter("ambient_floor", (foliage ? new Vector3(0.12f, 0.15f, 0.17f) : new Vector3(0.055f, 0.07f, 0.085f)) * Num(preview, "ambientScale", 1.15f));
		bool moss = name.Contains("moss");
		bool paving = name.Contains("weathered");
		float scale = Num(preview, moss ? "mossStaticScale" : (paving ? "pavingStaticScale" : "surfaceStaticScale"), 1f);
		material.SetShaderParameter("static_tint", (paving ? new Vector3(0.85f, 0.94f, 1f) : Vector3.One) * scale);
		material.SetShaderParameter("static_saturation", Num(preview, moss ? "mossStaticSaturation" : (paving ? "pavingStaticSaturation" : "surfaceStaticSaturation"), 1f));
		material.SetShaderParameter("lower_fade", name.Contains("cliff_strata") ? 1f : 0f);
		material.SetShaderParameter("hue_study", Num(preview, "staticHueStudy", 1f));
		material.SetShaderParameter("distance_scale", _forestDistanceScale);
		material.SetShaderParameter("path_angle", _pathAngle);
		string[] array = new string[3] { "sky", "equator", "ground" };
		foreach (string key in array)
		{
			material.SetShaderParameter("ambient_" + key, Rgb(Col(_forestLook["ambient"]?[key], Colors.Black).SrgbToLinear()));
		}
		JObject fog = _forestLook["fog"] as JObject;
		material.SetShaderParameter("fog_color", Col(fog?["color"], new Color(0.36f, 0.44f, 0.56f)).SrgbToLinear());
		material.SetShaderParameter("fog_range", new Vector2(Num(fog, "start", 22f), Num(fog, "end", 48f)));
		JObject lobe = fog?["lobe"] as JObject;
		JObject height = fog?["height"] as JObject;
		JObject far2 = fog?["far2"] as JObject;
		material.SetShaderParameter("lobe_position", OnPath(Num(lobe, "t", 32.5f), Num(lobe, "s", 80f), Num(lobe, "y", 2.2f)));
		material.SetShaderParameter("lobe_color", Rgb(Col(lobe?["color"], new Color(0.48f, 0.59f, 0.74f)).SrgbToLinear()) * Num(lobe, "strength", 1.2f));
		material.SetShaderParameter("lobe_shape", new Vector3(Num(lobe, "powerV", 650f), Num(lobe, "powerH", 90f), Num(lobe, "edge", 1f)));
		material.SetShaderParameter("height_color", Rgb(Col(height?["color"], new Color(0.36f, 0.43f, 0.52f)).SrgbToLinear()));
		material.SetShaderParameter("height_range", new Vector4(Num(height, "base", -0.15f), Num(height, "top", 1.2f), Num(height, "depthStart", 26f), Num(height, "depthFull", 42f)));
		material.SetShaderParameter("height_density", Bool(height, "on", fallback: true) ? Num(height, "density", 0.18f) : 0f);
		material.SetShaderParameter("far_fog", new Vector3(Num(far2, "start", 32f), Num(far2, "end", 58f), Bool(far2, "on", fallback: true) ? Num(far2, "strength", 0.6f) : 0f));
		JObject lampLook = _forestLook["lamp"] as JObject;
		material.SetShaderParameter("seat_light_color", Rgb(Col(lampLook?["color"], new Color(0.6f, 0.76f, 1f)).SrgbToLinear()) * Num(lampLook, "illum", 2.6f));
		JObject vig = _forestLook["stageVignette"] as JObject;
		material.SetShaderParameter("vignette_side", Num(vig, "side", 0.5f));
		material.SetShaderParameter("vignette_top", Num(vig, "top", 0.06f));
		JObject night = _forestLook["nightGrade"] as JObject;
		material.SetShaderParameter("night_color", Col(night?["color"], new Color(0.2f, 0.29f, 0.41f)).SrgbToLinear());
		material.SetShaderParameter("night_strength", Num(night, "strength", 0.7f));
		material.SetShaderParameter("lamp_emission", name.Contains("lantern_glass") ? (new Vector3(1f, 0.32f, 0.05f) * 3f) : Vector3.Zero);
		return material;
	}

	private void BuildForest()
	{
		PackedScene packed = ResourceLoader.Load<PackedScene>("res://assets/stage/integrated_forest/mossbound_full_stage.glb", null, ResourceLoader.CacheMode.Reuse);
		if (packed == null)
		{
			GD.PushError("Approved integrated forest is missing: res://assets/stage/integrated_forest/mossbound_full_stage.glb");
			return;
		}
		Node3D forest = packed.Instantiate<Node3D>(PackedScene.GenEditState.Disabled);
		forest.Name = "IntegratedForest";
		_static.AddChild(forest, forceReadableName: false, InternalMode.Disabled);
		System.Collections.Generic.Dictionary<ulong, Material> duplicates = new System.Collections.Generic.Dictionary<ulong, Material>();
		Vector3? lantern = null;
		foreach (Node node in Descendants(forest))
		{
			if (node is Camera3D importedCamera)
			{
				importedCamera.Current = false;
			}
			if (node is Light3D importedLight)
			{
				importedLight.Visible = false;
			}
			if (!(node is MeshInstance3D { Mesh: not null } instance))
			{
				continue;
			}
			for (int i = 0; i < instance.Mesh.GetSurfaceCount(); i++)
			{
				if (instance.GetActiveMaterial(i) is StandardMaterial3D source)
				{
					if (!duplicates.TryGetValue(source.GetInstanceId(), out var material))
					{
						material = MakeForestMaterial(source);
						duplicates[source.GetInstanceId()] = material;
					}
					instance.SetSurfaceOverrideMaterial(i, material);
					if (source.ResourceName.Contains("lantern_glass", StringComparison.OrdinalIgnoreCase))
					{
						lantern = base.GlobalTransform.AffineInverse() * instance.GlobalTransform * instance.GetAabb().GetCenter();
					}
				}
			}
		}
		JObject preview = _forestLook["forestPreview"] as JObject;
		if (lantern.HasValue)
		{
			Vector3 lamp = lantern.GetValueOrDefault();
			if (true)
			{
				AddLamp("ForestLantern", lamp, new Color(1f, 0.55f, 0.25f), Num(preview, "lanternIntensity", 10f), Num(preview, "lanternRange", 5.5f), 0f, 1f);
				Vector3 bounce = lamp + Vector3.Back * 1.5f;
				bounce.Y = Num(preview, "warmWorldY", 1.322433f);
				AddLamp("LanternGroundBounce", bounce, new Color(1f, 0.48f, 0.18f), Num(preview, "warmGround", 5f), Num(preview, "warmRange", 4.5f), 0f, 2f);
			}
		}
		AddLamp("BattleMoonFill", new Vector3(0f, 3f, 0f), new Color(0.45f, 0.58f, 0.85f), Num(preview, "battleFill", 2f), 13f, 0f, 3f);
		BuildForestBackdrop();
		BindForestSourceLights();
		Camera.Current = true;
	}

	private void BuildForestBackdrop()
	{
		JObject preview = _forestLook["forestPreview"] as JObject;
		if (!(Num(preview, "hideBackdrop") > 0f))
		{
			StandardMaterial3D source = new StandardMaterial3D
			{
				ResourceName = "Distant forest atmosphere",
				AlbedoTexture = GD.Load<Texture2D>("res://assets/stage/integrated_forest/distant_forest_haze.png"),
				AlbedoColor = new Color(0.2f, 0.28f, 0.36f) * Num(preview, "backdropScale", 1f)
			};
			ShaderMaterial material = (ShaderMaterial)MakeForestMaterial(source);
			material.SetShaderParameter("receive", 0f);
			material.SetShaderParameter("fog_weight", Num(preview, "backdropFog", 0.35f));
			Godot.Collections.Array arrays = new Godot.Collections.Array();
			arrays.Resize(13);
			arrays[0] = new Vector3[4]
			{
				new Vector3(-80f, -7f, -85f),
				new Vector3(-80f, 63f, -85f),
				new Vector3(80f, 63f, -85f),
				new Vector3(80f, -7f, -85f)
			};
			arrays[1] = new Vector3[4]
			{
				Vector3.Back,
				Vector3.Back,
				Vector3.Back,
				Vector3.Back
			};
			arrays[4] = new Vector2[4]
			{
				new Vector2(0f, 1f),
				new Vector2(0f, -1f),
				new Vector2(4f, -1f),
				new Vector2(4f, 1f)
			};
			arrays[12] = new int[6] { 0, 2, 1, 0, 3, 2 };
			ArrayMesh mesh = new ArrayMesh();
			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, (Mesh.ArrayFormat)0L);
			MeshInstance3D node = new MeshInstance3D
			{
				Name = "DistantAtmosphere",
				Mesh = mesh,
				MaterialOverride = material,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
			};
			node.SetMeta("source_generator", "FullStageSetup.cs:126-136");
			_static.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static IEnumerable<Node> Descendants(Node node)
	{
		yield return node;
		foreach (Node child in node.GetChildren())
		{
			foreach (Node item in Descendants(child))
			{
				yield return item;
			}
		}
	}

	private void BuildLayout(int act)
	{
		string path = $"res://assets/Stage/act{act}_layout.json";
		if (!FileAccess.FileExists(path))
		{
			GD.PushError("Stage layout missing: " + path);
			return;
		}
		_layout = JObject.Parse(FileAccess.GetFileAsString(path));
		_pathAngle = Mathf.DegToRad(0f - Num(_layout, "pathYaw", -22f));
		foreach (JObject part in (JArray)_layout["parts"])
		{
			if (Str(part, "kind") == "slab")
			{
				_slabs.Add(part);
			}
		}
		int index = 0;
		foreach (JObject part2 in (JArray)_layout["parts"])
		{
			BuildPart(part2, index++);
		}
		if (Bool(_layout, "staticBatch", fallback: true))
		{
			BatchStaticMeshes();
		}
		AddLayoutLights(act);
		GD.Print($"[StageWorld] Act {act}: {index} source parts, {_parts.Count} named nodes, {_slabs.Count} ground slabs.");
	}

	private Vector3 OnPath(float t, float s, float y = 0f)
	{
		return new Vector3(t * Mathf.Cos(_pathAngle) - s * Mathf.Sin(_pathAngle), y, 0f - (t * Mathf.Sin(_pathAngle) + s * Mathf.Cos(_pathAngle)));
	}

	private float GroundY(float t, float s)
	{
		float top = float.NegativeInfinity;
		float lowest = 0f;
		foreach (JObject slab in _slabs)
		{
			JArray front = (JArray)slab["front"];
			float y = Num(slab, "top");
			lowest = Math.Min(lowest, y);
			if (t < (float)front[0][0] || t > (float)front[front.Count - 1][0] || s > Num(slab, "back"))
			{
				continue;
			}
			for (int i = 1; i < front.Count; i++)
			{
				float a = (float)front[i - 1][0];
				float b = (float)front[i][0];
				if (!(t < a) && !(t > b))
				{
					float edge = Mathf.Lerp((float)front[i - 1][1], (float)front[i][1], (t - a) / Math.Max(0.0001f, b - a));
					if (s >= edge - 0.0001f)
					{
						top = Math.Max(top, y);
					}
					break;
				}
			}
		}
		return float.IsNegativeInfinity(top) ? lowest : top;
	}

	private void BuildPart(JObject p, int index)
	{
		string kind = Str(p, "kind");
		string name = Str(p, "name", $"{kind}-{index}");
		if (PhoneLayout && kind != "slab" && p["phone"] is JObject phone)
		{
			if (Bool(phone, "hide") || Num(phone, "hide") > 0.5f)
			{
				return;
			}
			p = (JObject)p.DeepClone();
			string[] array = new string[4] { "t", "s", "y", "scale" };
			foreach (string key in array)
			{
				if (phone[key] != null)
				{
					p[key] = phone[key].DeepClone();
				}
			}
			if (kind == "halo")
			{
				string[] array2 = new string[2] { "r", "gain" };
				foreach (string key2 in array2)
				{
					if (phone[key2] != null)
					{
						p[key2] = phone[key2].DeepClone();
					}
				}
			}
			if (kind == "mist")
			{
				string[] array3 = new string[5] { "w", "h", "v0", "alpha", "flow" };
				foreach (string key3 in array3)
				{
					if (phone[key3] != null)
					{
						p[key3] = phone[key3].DeepClone();
					}
				}
			}
		}
		Node3D root = new Node3D
		{
			Name = name
		};
		bool flag;
		switch (kind)
		{
		case "mist":
		case "shaft":
		case "halo":
		case "fog":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		(flag ? _dynamic : _static).AddChild(root, forceReadableName: false, InternalMode.Disabled);
		root.SetMeta("source_part_index", index);
		root.SetMeta("source_kind", kind);
		_parts[name] = root;
		float t = Num(p, "t");
		float s = Num(p, "s");
		float y = Num(p, "y");
		root.Position = OnPath(t, s, y + (Bool(p, "abs") ? 0f : GroundY(t, s)));
		switch (kind)
		{
		case "block":
		case "rail":
		case "arch":
		case "pillar":
		case "fence":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		bool aligned = flag;
		root.RotationDegrees = new Vector3(0f, 0f - Num(p, "yaw") + (aligned ? Mathf.RadToDeg(_pathAngle) : 0f), 0f);
		root.Scale = Vector3.One * ((kind == "litter") ? 1f : Num(p, "scale", 1f));
		JObject j2 = p;
		if (1 == 0)
		{
		}
		string fallback;
		switch (kind)
		{
		case "slab":
			fallback = "terrain";
			break;
		case "block":
		case "rock":
		case "arch":
		case "pillar":
			fallback = "rock";
			break;
		case "rail":
		case "fence":
			fallback = "wood";
			break;
		default:
			fallback = "relief";
			break;
		}
		if (1 == 0)
		{
		}
		string surface = Str(j2, "surface", fallback);
		Color tint = Col(p["tint"], Colors.White);
		JObject j3 = p;
		switch (kind)
		{
		case "litter":
		case "card":
		case "halo":
		case "mist":
		case "shaft":
		case "fog":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		bool shadow = Bool(j3, "shadow", !flag);
		Geometry b = new Geometry();
		switch (kind)
		{
		case "slab":
			root.Transform = Transform3D.Identity;
			Slab(b, p);
			break;
		case "block":
			Block(b, p);
			break;
		case "rock":
			Rock(b, p);
			break;
		case "pillar":
			Pillar(root, p, surface, tint);
			return;
		case "arch":
			Arch(b, p);
			break;
		case "rail":
			Rail(root, p, surface, tint);
			return;
		case "fence":
			Fence(root, p, surface, tint);
			return;
		case "relief":
		case "card":
		case "litter":
			Relief(root, p, kind, tint, shadow);
			return;
		case "halo":
		case "mist":
		case "shaft":
		case "fog":
			Air(root, p, kind);
			return;
		default:
			GD.PushWarning("Unsupported stage part " + kind + ": " + name);
			return;
		}
		AddMesh(root, "Mesh", b.ToMesh(), Surface(surface, tint), shadow);
	}

	private Material Surface(string key, Color tint)
	{
		string cache = key + ":" + tint;
		if (_materials.TryGetValue(cache, out Material existing))
		{
			return existing;
		}
		JObject s = _layout["surfaces"]?[key] as JObject;
		if (s == null)
		{
			GD.PushWarning("Missing stage surface: " + key);
			s = new JObject();
		}
		ShaderMaterial material = new ShaderMaterial
		{
			Shader = _surfaceShader
		};
		Texture2D[] side = ResolveTiles(Str(s, "side"));
		Texture2D[] top = ResolveTiles(Str(s, "top", Str(s, "side")));
		for (int i = 0; i < 4; i++)
		{
			material.SetShaderParameter("side" + i, side[i]);
			material.SetShaderParameter("top" + i, top[i]);
		}
		material.SetShaderParameter("tint", tint * Col(s["tint"], Colors.White));
		material.SetShaderParameter("path_angle", _pathAngle);
		material.SetShaderParameter("top_threshold", Num(s, "topThreshold", 0.65f));
		material.SetShaderParameter("top_blend", Num(s, "topBlend", 0.1f));
		material.SetShaderParameter("side_rotation", TileRotation(Str(s, "side")));
		material.SetShaderParameter("top_rotation", TileRotation(Str(s, "top", Str(s, "side"))));
		_materials[cache] = material;
		return material;
	}

	private int TileRotation(string key)
	{
		string text = Str(_layout["tiles"]?[key] as JObject, "rot");
		if (1 == 0)
		{
		}
		int result = ((text == "flipX") ? 1 : ((text == "rot4") ? 2 : 0));
		if (1 == 0)
		{
		}
		return result;
	}

	private Texture2D[] ResolveTiles(string key)
	{
		JObject def = _layout["tiles"]?[key] as JObject;
		JArray variants = def?["variants"] as JArray;
		List<Texture2D> textures = new List<Texture2D>();
		if (variants != null)
		{
			foreach (JToken variant in variants)
			{
				Texture2D texture = ResolveArt(variant);
				if (texture != null)
				{
					textures.Add(texture);
				}
			}
		}
		if (textures.Count == 0)
		{
			Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
			JArray fallback = def?["fallback"] as JArray;
			image.Fill((fallback == null) ? new Color(0.38f, 0.4f, 0.42f) : new Color((float)fallback[0] / 255f, (float)fallback[1] / 255f, (float)fallback[2] / 255f));
			textures.Add(ImageTexture.CreateFromImage(image));
		}
		return (from i in Enumerable.Range(0, 4)
			select textures[i % textures.Count]).ToArray();
	}

	private Texture2D? ResolveArt(JToken? token)
	{
		if (token is JArray array)
		{
			foreach (JToken candidate in array)
			{
				Texture2D t = ResolveArt(candidate);
				if (t != null)
				{
					return t;
				}
			}
			return null;
		}
		if (token is JObject obj)
		{
			return ResolveArt(obj["art"]);
		}
		if (token == null || token.Type != JTokenType.String)
		{
			return null;
		}
		string path = "res://assets/" + token.Value<string>() + ".png";
		if (_textures.TryGetValue(path, out Texture2D cached))
		{
			return cached;
		}
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		Texture2D texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
		if (texture != null)
		{
			_textures[path] = texture;
		}
		return texture;
	}

	private static MeshInstance3D AddMesh(Node3D parent, string name, Mesh mesh, Material material, bool shadow = true)
	{
		MeshInstance3D instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = (GeometryInstance3D.ShadowCastingSetting)(shadow ? 1 : 0)
		};
		parent.AddChild(instance, forceReadableName: false, InternalMode.Disabled);
		return instance;
	}

	private void Slab(Geometry b, JObject p)
	{
		Vector2[] points = ((JArray)p["front"]).Select((JToken jToken) => new Vector2((float)jToken[0], (float)jToken[1])).ToArray();
		float top = Num(p, "top");
		float bottom = Num(p, "bottom", -1f);
		float back = Num(p, "back");
		float chamfer = Num(p, "chamfer");
		for (int i = 0; i < points.Length - 1; i++)
		{
			Vector2 a = points[i];
			Vector2 c = points[i + 1];
			int nt = Math.Max(1, Mathf.CeilToInt((c.X - a.X) / Math.Max(0.32f, Num(p, "gridT", 1.28f))));
			int ns = Math.Max(1, Mathf.CeilToInt((back - Math.Min(a.Y, c.Y)) / Math.Max(0.32f, Num(p, "grid", 1.28f))));
			for (int u = 0; u < nt; u++)
			{
				Vector2 f0 = a.Lerp(c, (float)u / (float)nt);
				Vector2 f1 = a.Lerp(c, (float)(u + 1) / (float)nt);
				for (int v = 0; v < ns; v++)
				{
					float z00 = Mathf.Lerp(f0.Y + chamfer, back, (float)v / (float)ns);
					float z1 = Mathf.Lerp(f0.Y + chamfer, back, (float)(v + 1) / (float)ns);
					float z10 = Mathf.Lerp(f1.Y + chamfer, back, (float)v / (float)ns);
					float z11 = Mathf.Lerp(f1.Y + chamfer, back, (float)(v + 1) / (float)ns);
					float ao = 1f - 0.32f * Mathf.Clamp(GroundY((f0.X + f1.X) * 0.5f, (z00 + z1) * 0.5f + 0.7f) - top, 0f, 1f);
					b.Face(new Vector3[4]
					{
						OnPath(f0.X, z00, top),
						OnPath(f0.X, z1, top),
						OnPath(f1.X, z11, top),
						OnPath(f1.X, z10, top)
					}, Vector3.Up, ao);
				}
			}
			Vector3 normal = OnPath(c.Y - a.Y, 0f - (c.X - a.X)).Normalized();
			b.Face(new Vector3[4]
			{
				OnPath(a.X, a.Y, bottom),
				OnPath(a.X, a.Y, top - chamfer),
				OnPath(c.X, c.Y, top - chamfer),
				OnPath(c.X, c.Y, bottom)
			}, normal, 0.8f);
			if (chamfer > 0f)
			{
				b.Face(new Vector3[4]
				{
					OnPath(a.X, a.Y, top - chamfer),
					OnPath(a.X, a.Y + chamfer, top),
					OnPath(c.X, c.Y + chamfer, top),
					OnPath(c.X, c.Y, top - chamfer)
				}, (normal + Vector3.Up).Normalized());
			}
		}
		int[] array = new int[2]
		{
			0,
			points.Length - 1
		};
		foreach (int i2 in array)
		{
			Vector2 a2 = points[i2];
			b.Face(new Vector3[4]
			{
				OnPath(a2.X, a2.Y, bottom),
				OnPath(a2.X, a2.Y, top),
				OnPath(a2.X, back, top),
				OnPath(a2.X, back, bottom)
			}, OnPath((i2 != 0) ? 1 : (-1), 0f), 0.8f);
		}
	}

	private static Vector2[] Footprint(float w, float d, float cut, int seed)
	{
		Random r = new Random(seed);
		float hw = w * 0.5f;
		float hd = d * 0.5f;
		float a = C();
		float b = C();
		float c = C();
		float e = C();
		return new Vector2[8]
		{
			new Vector2(0f - hw + a, 0f - hd),
			new Vector2(hw - b, 0f - hd),
			new Vector2(hw, 0f - hd + b),
			new Vector2(hw, hd - c),
			new Vector2(hw - c, hd),
			new Vector2(0f - hw + e, hd),
			new Vector2(0f - hw, hd - e),
			new Vector2(0f - hw, 0f - hd + a)
		};
		float C()
		{
			return Math.Min(Math.Min(hw, hd) * 0.8f, Mathf.Round(cut * (0.5f + (float)r.NextDouble()) / 0.08f) * 0.08f);
		}
	}

	private static void Block(Geometry b, JObject p)
	{
		Extrude(b, Footprint(Num(p, "w", 1.6f), Num(p, "d", 1.2f), Num(p, "cut", 0.32f), (int)Num(p, "seed")), 0f - Num(p, "sink", 0.2f), Num(p, "h", 1f), Num(p, "chamfer", 0.12f), Ao(p));
	}

	private static void Extrude(Geometry b, Vector2[] footprint, float bottom, float top, float chamfer, float ao = 1f)
	{
		float c = Mathf.Clamp(chamfer, 0f, (top - bottom) * 0.45f);
		Vector3[] tops = new Vector3[footprint.Length];
		Vector2 center = footprint.Aggregate(Vector2.Zero, (Vector2 sum, Vector2 v) => sum + v) / footprint.Length;
		for (int i = 0; i < footprint.Length; i++)
		{
			int j = (i + 1) % footprint.Length;
			Vector2 p = footprint[i];
			Vector2 q = footprint[j];
			Vector2 a = p.MoveToward(center, c);
			Vector2 d = q.MoveToward(center, c);
			Vector3 normal = new Vector3((p.X + q.X) / 2f - center.X, 0f, 0f - ((p.Y + q.Y) / 2f - center.Y)).Normalized();
			b.Face(new Vector3[4]
			{
				P(p.X, bottom, p.Y),
				P(p.X, top - c, p.Y),
				P(q.X, top - c, q.Y),
				P(q.X, bottom, q.Y)
			}, normal, 1f - 0.2f * ao);
			if (c > 0f)
			{
				b.Face(new Vector3[4]
				{
					P(p.X, top - c, p.Y),
					P(a.X, top, a.Y),
					P(d.X, top, d.Y),
					P(q.X, top - c, q.Y)
				}, (normal + Vector3.Up).Normalized(), 0.97f);
			}
			tops[i] = P(a.X, top, a.Y);
		}
		b.Face(tops, Vector3.Up);
		static Vector3 P(float x, float y, float z)
		{
			return new Vector3(x, y, 0f - z);
		}
	}

	private static float Ao(JObject p)
	{
		JToken? jToken = p["ao"];
		if (jToken != null && jToken.Type == JTokenType.Boolean && !(bool)p["ao"])
		{
			return 0f;
		}
		JToken? jToken2 = p["ao"];
		float fallback;
		if (jToken2 == null || jToken2.Type != JTokenType.Float)
		{
			JToken? jToken3 = p["ao"];
			if (jToken3 == null || jToken3.Type != JTokenType.Integer)
			{
				fallback = 1f;
				goto IL_0095;
			}
		}
		fallback = Num(p, "ao", 1f);
		goto IL_0095;
		IL_0095:
		return Num(p, "aoAmount", fallback);
	}

	private static void Rock(Geometry b, JObject p)
	{
		Random random = new Random((int)Num(p, "seed"));
		int sides = Math.Clamp((int)Num(p, "sides", 6f), 4, 9);
		float radius = Num(p, "r", 0.6f);
		float h = Num(p, "h", 0.7f);
		float squash = Num(p, "squash", 0.8f);
		float start = R(0f, (float)Math.PI * 2f);
		Vector3[] bottom = new Vector3[sides];
		Vector3[] middle = new Vector3[sides];
		Vector3[] top = new Vector3[sides];
		for (int i = 0; i < sides; i++)
		{
			float a = start + ((float)i + R(-0.28f, 0.28f)) * ((float)Math.PI * 2f) / (float)sides;
			float r = radius * R(0.82f, 1.12f);
			Vector3 dir = new Vector3(Mathf.Cos(a), 0f, (0f - Mathf.Sin(a)) * squash);
			bottom[i] = dir * r + Vector3.Up * ((0f - h) * 0.12f);
			middle[i] = dir * r * R(0.86f, 1f) + Vector3.Up * h * R(0.5f, 0.72f);
			top[i] = dir * r * R(0.42f, 0.62f) + Vector3.Up * h * R(0.9f, 1.05f);
		}
		for (int j = 0; j < sides; j++)
		{
			int j2 = (j + 1) % sides;
			Vector3 n = (bottom[j] + bottom[j2]).Normalized();
			b.Face(new Vector3[4]
			{
				bottom[j],
				middle[j],
				middle[j2],
				bottom[j2]
			}, n, 0.84f);
			b.Face(new Vector3[4]
			{
				middle[j],
				top[j],
				top[j2],
				middle[j2]
			}, (n + Vector3.Up).Normalized());
		}
		b.Face(top, Vector3.Up);
		float R(float num, float c)
		{
			return Mathf.Lerp(num, c, (float)random.NextDouble());
		}
	}

	private void Box(Node3D root, string name, Vector3 center, Vector3 size, string surface, Color tint)
	{
		BoxMesh mesh = new BoxMesh
		{
			Size = size
		};
		MeshInstance3D n = AddMesh(root, name, mesh, Surface(surface, tint));
		n.Position = center;
	}

	private void Pillar(Node3D root, JObject p, string surface, Color tint)
	{
		float w = Num(p, "w", 0.9f);
		float d = Num(p, "d", 0.9f);
		float h = Num(p, "h", 5f);
		float bh = Num(p, "baseH", 0.3f);
		float bo = Num(p, "baseOver", 0.12f);
		float ch = Num(p, "capH", 0.35f);
		float co = Num(p, "capOver", 0.15f);
		JToken? jToken = p["broken"];
		float broken = ((jToken == null || jToken.Type != JTokenType.Boolean) ? Num(p, "broken") : (Bool(p, "broken") ? (h * 0.3f) : 0f));
		Geometry b = new Geometry();
		Extrude(b, Footprint(w + bo * 2f, d + bo * 2f, 0.05f, 1), 0f - Num(p, "sink", 0.15f), bh, 0.05f, Ao(p));
		Vector2[] ring = Footprint(w, d, Num(p, "chamfer", 0.06f), 1);
		if (broken > 0f)
		{
			Random random = new Random((int)Num(p, "seed"));
			Vector3[] edge = new Vector3[ring.Length];
			for (int i = 0; i < edge.Length; i++)
			{
				edge[i] = new Vector3(ring[i].X, h - broken * (float)random.NextDouble(), 0f - ring[i].Y);
			}
			int high = random.Next(edge.Length);
			edge[high] = new Vector3(edge[high].X, h - broken * 0.08f, edge[high].Z);
			for (int j = 0; j < edge.Length; j++)
			{
				int j2 = (j + 1) % edge.Length;
				Vector3 normal = new Vector3((edge[j].X + edge[j2].X) / 2f, 0f, (edge[j].Z + edge[j2].Z) / 2f).Normalized();
				b.Face(new Vector3[4]
				{
					new Vector3(edge[j].X, bh - 0.01f, edge[j].Z),
					edge[j],
					edge[j2],
					new Vector3(edge[j2].X, bh - 0.01f, edge[j2].Z)
				}, normal, 1f - 0.2f * Ao(p));
			}
			Vector3 center = edge.Aggregate(Vector3.Zero, (Vector3 sum, Vector3 v) => sum + v) / edge.Length;
			for (int i2 = 0; i2 < edge.Length; i2++)
			{
				b.Face(new Vector3[3]
				{
					center,
					edge[i2],
					edge[(i2 + 1) % edge.Length]
				}, Vector3.Up);
			}
		}
		else
		{
			Extrude(b, ring, bh - 0.01f, h - ch, 0.025f, Ao(p));
			Extrude(b, Footprint(w + co * 2f, d + co * 2f, 0.05f, 1), h - ch, h, 0.05f, Ao(p));
		}
		AddMesh(root, "Masonry", b.ToMesh(), Surface(surface, tint));
	}

	private static void Arch(Geometry b, JObject p)
	{
		float w = Num(p, "w", 4f);
		float h = Num(p, "h", 4.5f);
		float depth = Num(p, "d", 1f);
		float pier = Num(p, "pier", 0.8f);
		float outer = w / 2f;
		float inner = outer - pier;
		float rise = Num(p, "rise", inner);
		float crown = Num(p, "crown", pier * 0.75f);
		float spring = h - crown - rise;
		int n = Math.Max(2, (int)Num(p, "segs", 10f));
		float ao = Ao(p);
		Vector2[] ring = new Vector2[4]
		{
			new Vector2(0f - outer, (0f - depth) / 2f),
			new Vector2(0f - inner, (0f - depth) / 2f),
			new Vector2(0f - inner, depth / 2f),
			new Vector2(0f - outer, depth / 2f)
		};
		Extrude(b, ring, 0f - Num(p, "sink", 0.15f), spring, 0f, ao);
		if (!Bool(p, "half"))
		{
			Extrude(b, ring.Select((Vector2 v) => new Vector2(0f - v.X, v.Y)).ToArray(), 0f - Num(p, "sink", 0.15f), spring, 0f, ao);
		}
		float end = (Bool(p, "half") ? ((float)Math.PI / 2f) : 0f);
		for (int i = 0; i < n; i++)
		{
			float a = Mathf.Lerp((float)Math.PI, end, (float)i / (float)n);
			float c = Mathf.Lerp((float)Math.PI, end, (float)(i + 1) / (float)n);
			Vector2 i2 = P(inner, rise, a);
			Vector2 i3 = P(inner, rise, c);
			Vector2 o0 = P(outer, h - spring, a);
			Vector2 o1 = P(outer, h - spring, c);
			float[] array = new float[2]
			{
				(0f - depth) / 2f,
				depth / 2f
			};
			foreach (float z in array)
			{
				b.Face(new Vector3[4]
				{
					V(i2, z),
					V(o0, z),
					V(o1, z),
					V(i3, z)
				}, new Vector3(0f, 0f, Math.Sign(z)), 1f - 0.1f * ao);
			}
			b.Face(new Vector3[4]
			{
				V(i2, (0f - depth) / 2f),
				V(i3, (0f - depth) / 2f),
				V(i3, depth / 2f),
				V(i2, depth / 2f)
			}, new Vector3(0f - (i2.X + i3.X), -1f, 0f), 0.8f);
			b.Face(new Vector3[4]
			{
				V(o0, (0f - depth) / 2f),
				V(o1, (0f - depth) / 2f),
				V(o1, depth / 2f),
				V(o0, depth / 2f)
			}, new Vector3(o0.X + o1.X, 1f, 0f));
		}
		if (Bool(p, "keystone"))
		{
			Extrude(b, Footprint(pier * 0.8f, depth + 0.12f, 0.03f, 1), h - crown - 0.06f, h + 0.1f, 0.02f, ao);
		}
		Vector2 P(float r, float num2, float angle)
		{
			return new Vector2(r * Mathf.Cos(angle), spring + num2 * Mathf.Sin(angle));
		}
		static Vector3 V(Vector2 v, float z2)
		{
			return new Vector3(v.X, v.Y, z2);
		}
	}

	private void Rail(Node3D root, JObject p, string surface, Color tint)
	{
		float length = Num(p, "length", 8f);
		float gauge = Num(p, "gauge", 1.2f);
		float sw = Num(p, "sleeperW", 0.22f);
		float sh = Num(p, "sleeperH", 0.07f);
		float sl = Num(p, "sleeperLen", gauge + 0.6f);
		float rh = Num(p, "railH", 0.1f);
		float rw = Num(p, "railW", 0.08f);
		Random r = new Random((int)Num(p, "seed"));
		JArray gap = p["sleepers"] as JArray;
		float min = ((gap == null) ? 0.8f : ((float)gap[0]));
		float max = ((gap == null) ? 1.3f : ((float)gap[1]));
		int i = 0;
		for (float x = (0f - length) / 2f + 0.2f; x < length / 2f; x += Mathf.Lerp(min, max, (float)r.NextDouble()))
		{
			Box(root, "Sleeper" + i++, new Vector3(x, sh / 2f, 0f), new Vector3(sw, sh, sl), surface, tint);
		}
		float[] array = new float[2]
		{
			(0f - gauge) / 2f,
			gauge / 2f
		};
		foreach (float z in array)
		{
			Box(root, (z < 0f) ? "RailNear" : "RailFar", new Vector3(0f, sh + rh / 2f, z), new Vector3(length, rh, rw), Str(p, "railSurface", "iron"), tint);
		}
	}

	private void Fence(Node3D root, JObject p, string surface, Color tint)
	{
		float length = Num(p, "len", 3f);
		float h = Num(p, "h", 0.9f);
		int count = Math.Max(2, (int)Num(p, "posts", 4f));
		for (int i = 0; i < count; i++)
		{
			Box(root, "Post" + i, new Vector3(Mathf.Lerp((0f - length) / 2f, length / 2f, (float)i / (float)(count - 1)), h / 2f, 0f), new Vector3(0.14f, h, 0.14f), surface, tint);
		}
		for (int j = 0; j < 2; j++)
		{
			Box(root, "Crossbar" + j, new Vector3(0f, h * ((j == 0) ? 0.35f : 0.8f), 0f), new Vector3(length, 0.11f, 0.1f), surface, tint);
		}
	}

	private void Relief(Node3D root, JObject p, string kind, Color tint, bool shadow)
	{
		string sourceName = Str(p, "src");
		JObject source = _layout["sources"]?[sourceName] as JObject;
		Texture2D texture = ResolveArt(source?["art"]);
		if (texture == null)
		{
			GD.PushWarning("Stage source art missing: " + sourceName);
			return;
		}
		bool flip = Bool(p, "flip");
		bool flag = ((kind == "card" || kind == "litter") ? true : false);
		float depth = ((flag || Bool(source, "flat")) ? 0f : Num(p, "depth", Num(source, "depth", 0.12f)));
		string cache = sourceName + "|" + flip + "|" + depth;
		if (!_reliefMeshes.TryGetValue(cache, out Mesh mesh))
		{
			mesh = ReliefMesh(texture, depth, (int)Num(source, "cells", 40f), flip);
			_reliefMeshes[cache] = mesh;
		}
		string materialKey = "relief|" + sourceName + "|" + tint;
		if (!_materials.TryGetValue(materialKey, out Material material))
		{
			StandardMaterial3D m = new StandardMaterial3D
			{
				AlbedoTexture = texture,
				AlbedoColor = tint,
				Roughness = 1f,
				MetallicSpecular = 0f,
				Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor,
				AlphaScissorThreshold = 0.4f,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
				TextureFilter = BaseMaterial3D.TextureFilterEnum.NearestWithMipmaps,
				VertexColorUseAsAlbedo = true
			};
			string path = texture.ResourcePath;
			object obj;
			if (!path.EndsWith(".png", StringComparison.Ordinal))
			{
				obj = "";
			}
			else
			{
				string text = path;
				obj = text.Substring(0, text.Length - 4) + "_n.png";
			}
			string normal = (string)obj;
			if (normal.Length > 0 && ResourceLoader.Exists(normal))
			{
				m.NormalEnabled = true;
				m.NormalTexture = ResourceLoader.Load<Texture2D>(normal, null, ResourceLoader.CacheMode.Reuse);
				m.NormalScale = 0.6f;
			}
			material = (_materials[materialKey] = m);
		}
		AddMesh(root, "Relief", mesh, material, shadow && (float)texture.GetHeight() / 25f >= 1.6f);
		float sway = Num(p, "sway");
		if (sway > 0f)
		{
			_sway.Add((root, root.Position, sway * 0.03f, Num(p, "seed") % 19f));
		}
	}

	private static ArrayMesh ReliefMesh(Texture2D texture, float depthRatio, int cells, bool flip)
	{
		int w;
		int h;
		bool[] inside;
		float[] distances;
		float ramp;
		float depth;
		using (Image image = texture.GetImage())
		{
			if (image.IsCompressed())
			{
				image.Decompress();
			}
			w = image.GetWidth();
			h = image.GetHeight();
			int cell = Math.Max(1, Mathf.CeilToInt((float)Math.Max(w, h) / (float)Math.Max(4, cells)));
			inside = new bool[w * h];
			distances = new float[w * h];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int i = y * w + x;
					inside[i] = image.GetPixel(x, y).A > 0.4f;
					distances[i] = (inside[i] ? 100000 : 0);
				}
			}
			for (int j = 0; j < h; j++)
			{
				for (int k = 0; k < w; k++)
				{
					distances[j * w + k] = Math.Min(distances[j * w + k], Math.Min(Math.Min(At(k - 1, j), At(k, j - 1)) + 1f, Math.Min(At(k - 1, j - 1), At(k + 1, j - 1)) + 1.33333f));
				}
			}
			for (int y2 = h - 1; y2 >= 0; y2--)
			{
				for (int x2 = w - 1; x2 >= 0; x2--)
				{
					distances[y2 * w + x2] = Math.Min(distances[y2 * w + x2], Math.Min(Math.Min(At(x2 + 1, y2), At(x2, y2 + 1)) + 1f, Math.Min(At(x2 + 1, y2 + 1), At(x2 - 1, y2 + 1)) + 1.33333f));
				}
			}
			ramp = Math.Max(2f, Math.Min(distances.Max() * 0.9f, depthRatio * (float)w * 1.6f));
			depth = Math.Min(depthRatio * (float)w, ramp / 1.6f) / 25f;
			Geometry b = new Geometry();
			for (int l = 0; l < h; l += cell)
			{
				for (int m = 0; m < w; m += cell)
				{
					int right = Math.Min(w, m + cell);
					int bottom = Math.Min(h, l + cell);
					bool any = false;
					for (int yy = l; yy < bottom; yy++)
					{
						if (any)
						{
							break;
						}
						for (int xx = m; xx < right; xx++)
						{
							if (inside[yy * w + xx])
							{
								any = true;
								break;
							}
						}
					}
					if (any)
					{
						Vector3[] v = new Vector3[4]
						{
							Vertex(m, l),
							Vertex(right, l),
							Vertex(right, bottom),
							Vertex(m, bottom)
						};
						Vector2[] uv = new Vector2[4]
						{
							new Vector2((float)m / (float)w, (float)l / (float)h),
							new Vector2((float)right / (float)w, (float)l / (float)h),
							new Vector2((float)right / (float)w, (float)bottom / (float)h),
							new Vector2((float)m / (float)w, (float)bottom / (float)h)
						};
						Vector3 normal = (v[1] - v[0]).Cross(v[2] - v[0]).Normalized();
						if (normal.Z < 0f)
						{
							normal = -normal;
						}
						b.Face(v, normal, 1f, uv);
					}
				}
			}
			return b.ToMesh();
		}
		float At(int num, int num2)
		{
			return (num < 0 || num2 < 0 || num >= w || num2 >= h) ? 0f : distances[num2 * w + num];
		}
		float Height(int num, int num2)
		{
			if (num < 0 || num2 < 0 || num >= w || num2 >= h || !inside[num2 * w + num])
			{
				return 0f;
			}
			float k2 = 1f - Mathf.Clamp(distances[num2 * w + num] / ramp, 0f, 1f);
			return depth * Mathf.Sqrt(1f - k2 * k2);
		}
		Vector3 Vertex(int num, int num2)
		{
			return new Vector3(((float)num - (float)w * 0.5f) / 25f * (float)((!flip) ? 1 : (-1)), (float)(h - num2) / 25f, 0.25f * (Height(num - 1, num2 - 1) + Height(num, num2 - 1) + Height(num - 1, num2) + Height(num, num2)));
		}
	}

	private void Air(Node3D root, JObject p, string kind)
	{
		ShaderMaterial material = new ShaderMaterial
		{
			Shader = _airShader
		};
		Color color = Col(p["color"] ?? p["tint"], (kind == "shaft") ? new Color(0.68f, 0.78f, 0.77f) : new Color(0.4f, 0.48f, 0.53f));
		float alpha = ((kind == "halo") ? (Num(p, "gain", 1f) * 0.36f) : ((kind == "shaft") ? (Num(p, "gain", 1f) * 0.13f) : Num(p, "alpha", 0.14f)));
		material.SetShaderParameter("air_color", new Color(color.R, color.G, color.B, alpha));
		material.SetShaderParameter("mode", (!(kind == "halo")) ? ((!(kind == "shaft")) ? 1 : 2) : 0);
		material.SetShaderParameter("flow", Num(p, "flow", 0.012f));
		material.SetShaderParameter("phase", Num(p, "seed") % 97f);
		material.SetShaderParameter("core", Num(p, "core", 0.35f));
		Geometry b = new Geometry();
		if (kind == "halo")
		{
			float r = Num(p, "r", 0.8f);
			b.Face(new Vector3[4]
			{
				new Vector3(0f - r, r, 0f),
				new Vector3(r, r, 0f),
				new Vector3(r, 0f - r, 0f),
				new Vector3(0f - r, 0f - r, 0f)
			}, Vector3.Back, 1f, QuadUv);
			if (Str(p, "facing", "camera") == "camera")
			{
				root.Rotation = Camera.Rotation;
			}
			root.Position += (Camera.Position - root.Position).Normalized() * 0.08f;
		}
		else if (kind == "shaft")
		{
			float top = Num(p, "top", 0.8f) / 2f;
			float bottom = Num(p, "bottom", 2.4f) / 2f;
			float length = Num(p, "len", 12f);
			Vector3 dir = new Vector3(0.35f, -1f, -0.3f);
			if (p["dir"] is JArray d)
			{
				dir = new Vector3((float)d[0], (float)d[1], 0f - (float)d[2]);
			}
			dir = dir.Normalized();
			Vector3 across = dir.Cross(Camera.Position - root.Position).Normalized();
			if (across.LengthSquared() < 0.1f)
			{
				across = Vector3.Right;
			}
			b.Face(new Vector3[4]
			{
				-across * top,
				across * top,
				dir * length + across * bottom,
				dir * length - across * bottom
			}, Vector3.Back, 1f, QuadUv);
			root.Rotation = Vector3.Zero;
		}
		else
		{
			float w = Num(p, "w", 64f);
			float h = Num(p, "h", 2f);
			b.Face(new Vector3[4]
			{
				new Vector3((0f - w) / 2f, h, 0f),
				new Vector3(w / 2f, h, 0f),
				new Vector3(w / 2f, 0f, 0f),
				new Vector3((0f - w) / 2f, 0f, 0f)
			}, Vector3.Back, 1f, QuadUv);
		}
		AddMesh(root, "Atmosphere", b.ToMesh(), material, shadow: false);
	}

	private void BatchStaticMeshes()
	{
		System.Collections.Generic.Dictionary<string, (Geometry, Material, bool)> groups = new System.Collections.Generic.Dictionary<string, (Geometry, Material, bool)>();
		MeshInstance3D[] array = Descendants(_static).OfType<MeshInstance3D>().ToArray();
		foreach (MeshInstance3D instance in array)
		{
			if (instance.Mesh != null && instance.MaterialOverride != null && !_sway.Any<(Node3D, Vector3, float, float)>(((Node3D node, Vector3 position, float sway, float phase) s) => s.node == instance.GetParent()))
			{
				Transform3D transform = base.GlobalTransform.AffineInverse() * instance.GlobalTransform;
				Vector3 center = transform * instance.GetAabb().GetCenter();
				bool shadow = instance.CastShadow != GeometryInstance3D.ShadowCastingSetting.Off;
				string key = instance.MaterialOverride.GetInstanceId() + ":" + shadow + ":" + Mathf.FloorToInt(center.X / 16f) + ":" + Mathf.FloorToInt(center.Z / 16f);
				if (!groups.TryGetValue(key, out var group))
				{
					group = (new Geometry(), instance.MaterialOverride, shadow);
				}
				for (int i2 = 0; i2 < instance.Mesh.GetSurfaceCount(); i2++)
				{
					group.Item1.Append(instance.Mesh.SurfaceGetArrays(i2), transform);
				}
				groups[key] = group;
				instance.Visible = false;
				instance.SetMeta("static_batch", key);
			}
		}
		Node3D batches = new Node3D
		{
			Name = "GeometryBatches"
		};
		_static.AddChild(batches, forceReadableName: false, InternalMode.Disabled);
		int index = 0;
		foreach (var group2 in groups.Values)
		{
			AddMesh(batches, "Batch" + index++, group2.Item1.ToMesh(), group2.Item2, group2.Item3);
		}
	}

	private void AddLayoutLights(int act)
	{
		string path = $"res://assets/Stage/look_act{act}.json";
		if (!FileAccess.FileExists(path))
		{
			return;
		}
		JObject look = JObject.Parse(FileAccess.GetFileAsString(path));
		List<JObject> lights = (look["lights"] as JArray)?.OfType<JObject>().ToList() ?? new List<JObject>();
		if (look["backlight"] is JObject back && Bool(back, "on", fallback: true))
		{
			lights.Add(back);
		}
		foreach (JObject sourceLight in lights)
		{
			JObject l = sourceLight;
			JObject phone = l["phone"] as JObject;
			if (!_mobilePerformance || Bool(phone, "on", fallback: true))
			{
				string at = Str(l, "at");
				string name = Str(l, "name", "Backlight");
				float t = Num(l, "t");
				float s = Num(l, "s");
				Node3D target;
				Vector3 position = (_parts.TryGetValue(at, out target) ? target.Position : OnPath(t, s, GroundY(t, s)));
				if (l["y"] != null)
				{
					position.Y = Num(l, "y");
				}
				if (PhoneLayout && phone != null)
				{
					float c = Mathf.Cos(_pathAngle);
					float a = Mathf.Sin(_pathAngle);
					t = position.X * c - position.Z * a;
					s = (0f - position.X) * a - position.Z * c;
					position = OnPath(Num(phone, "t", t), Num(phone, "s", s), Num(phone, "y", position.Y));
				}
				position += OnPath(Num(l, "dt"), Num(l, "ds"), Num(l, "dy"));
				AddLamp(name, position, Col(l["color"], new Color(1f, 0.65f, 0.35f)), Num(l, "illum", 1f) * 0.55f * (_mobilePerformance ? Num(phone, "illumMul", 1f) : 1f), Num(l, "range", 6f) * (_mobilePerformance ? Num(phone, "rangeMul", 1f) : 1f), Num(l, "flicker"), name.Length);
			}
		}
	}

	private void AddLamp(string name, Vector3 position, Color color, float energy, float range, float flicker, float phase, float? sourceIntensity = null)
	{
		OmniLight3D light = new OmniLight3D
		{
			Name = name,
			Position = position,
			LightColor = color,
			LightEnergy = energy,
			OmniRange = range,
			OmniAttenuation = 1.5f,
			ShadowEnabled = false
		};
		_dynamic.AddChild(light, forceReadableName: false, InternalMode.Disabled);
		_lamps.Add((light, energy, flicker, phase));
		if (Act == 1)
		{
			_forestSteadyLights.Add((light, sourceIntensity ?? energy, energy));
		}
	}

	private static float Num(JObject? j, string key, float fallback = 0f)
	{
		JTokenType? jTokenType = j?[key]?.Type;
		bool flag;
		if (jTokenType.HasValue)
		{
			JTokenType valueOrDefault = jTokenType.GetValueOrDefault();
			if ((uint)(valueOrDefault - 6) <= 1u)
			{
				flag = true;
				goto IL_004e;
			}
		}
		flag = false;
		goto IL_004e;
		IL_004e:
		return flag ? ((float)j[key]) : fallback;
	}

	private static string Str(JObject? j, string key, string fallback = "")
	{
		return (j != null && j[key]?.Type == JTokenType.String) ? ((string?)j[key]) : fallback;
	}

	private static bool Bool(JObject? j, string key, bool fallback = false)
	{
		return (j != null && j[key]?.Type == JTokenType.Boolean) ? ((bool)j[key]) : fallback;
	}

	private static Vector3 Rgb(Color color)
	{
		return new Vector3(color.R, color.G, color.B);
	}

	private static Color Col(JToken? j, Color fallback)
	{
		if (j is JArray { Count: >=3 } a)
		{
			return new Color((float)a[0], (float)a[1], (float)a[2], (a.Count > 3) ? ((float)a[3]) : 1f);
		}
		JTokenType? jTokenType = j?.Type;
		bool flag;
		if (jTokenType.HasValue)
		{
			JTokenType valueOrDefault = jTokenType.GetValueOrDefault();
			if ((uint)(valueOrDefault - 6) <= 1u)
			{
				flag = true;
				goto IL_00a8;
			}
		}
		flag = false;
		goto IL_00a8;
		IL_00a8:
		if (flag)
		{
			float v = (float)j;
			return new Color(v, v, v);
		}
		return fallback;
	}

	private void ResetForestSourceLights()
	{
		_forestMaterials.Clear();
		_forestSteadyLights.Clear();
		_forestAttackLights.Clear();
		_forestSourceLights.Clear();
	}

	public void SetForestAttackLights(IReadOnlyList<ActorMaterialBinding.PointLight>? lights)
	{
		_forestAttackLights.Clear();
		if (lights != null)
		{
			foreach (ActorMaterialBinding.PointLight light in lights)
			{
				if (light.IsAttack && _forestAttackLights.Count < 2 && light.Range > 0f && light.Position.IsFinite() && light.LinearRadiance.IsFinite())
				{
					_forestAttackLights.Add(light);
				}
			}
		}
		BindForestSourceLights();
	}

	private void BindForestSourceLights()
	{
		_forestSourceLights.Clear();
		if (Act != 1)
		{
			return;
		}
		foreach (ActorMaterialBinding.PointLight light in _forestAttackLights)
		{
			_forestSourceLights.Add(light);
		}
		foreach (var (node, sourceIntensity, nativeBase) in _forestSteadyLights)
		{
			if (_forestSourceLights.Count == 8)
			{
				break;
			}
			if (GodotObject.IsInstanceValid(node) && node.IsInsideTree() && node.Visible && !(node.LightEnergy <= 0f) && !(nativeBase <= 0f))
			{
				Color color = node.LightColor.SrgbToLinear();
				float intensity = sourceIntensity * node.LightEnergy / nativeBase;
				_forestSourceLights.Add(new ActorMaterialBinding.PointLight(node.GlobalPosition, node.OmniRange, new Vector3(color.R, color.G, color.B) * intensity, IsAttack: false));
			}
		}
		for (int i = 0; i < _forestSourceLights.Count; i++)
		{
			ActorMaterialBinding.PointLight light2 = _forestSourceLights[i];
			_forestPositions[i] = new Vector4(light2.Position.X, light2.Position.Y, light2.Position.Z, light2.Range);
			_forestColors[i] = new Vector4(light2.LinearRadiance.X, light2.LinearRadiance.Y, light2.LinearRadiance.Z, light2.IsAttack ? 1 : 0);
		}
		foreach (ShaderMaterial material in _forestMaterials)
		{
			material.SetShaderParameter("forest_light_count", _forestSourceLights.Count);
			material.SetShaderParameter("forest_light_position_range", _forestPositions);
			material.SetShaderParameter("forest_light_color_kind", _forestColors);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(23);
		methods.Add(new MethodInfo(MethodName.Build, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "act", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.UpdateCameraFraming, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.MakeLighting, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "act", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.MakeForestLighting, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.MakeForestMaterial, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Material"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "source", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("StandardMaterial3D"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.BuildForest, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.BuildForestBackdrop, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.BuildLayout, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "act", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.OnPath, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "t", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "s", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "y", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.GroundY, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "t", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "s", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Surface, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Material"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Color, "tint", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.TileRotation, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ResolveTiles, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.AddMesh, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("MeshInstance3D"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false),
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Object, "mesh", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Mesh"), exported: false),
			new PropertyInfo(Variant.Type.Object, "material", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Material"), exported: false),
			new PropertyInfo(Variant.Type.Bool, "shadow", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Footprint, new PropertyInfo(Variant.Type.PackedVector2Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "w", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "d", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "cut", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "seed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Box, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "root", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false),
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Vector3, "center", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Vector3, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "surface", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Color, "tint", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ReliefMesh, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("ArrayMesh"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "texture", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false),
			new PropertyInfo(Variant.Type.Float, "depthRatio", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "cells", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Bool, "flip", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.BatchStaticMeshes, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.AddLayoutLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "act", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Rgb, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ResetForestSourceLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.BindForestSourceLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Build && args.Count == 1)
		{
			Build(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.UpdateCameraFraming && args.Count == 0)
		{
			UpdateCameraFraming();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.MakeLighting && args.Count == 1)
		{
			MakeLighting(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.MakeForestLighting && args.Count == 0)
		{
			MakeForestLighting();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.MakeForestMaterial && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Material>(MakeForestMaterial(VariantUtils.ConvertTo<StandardMaterial3D>(in args[0])));
			return true;
		}
		if (method == MethodName.BuildForest && args.Count == 0)
		{
			BuildForest();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BuildForestBackdrop && args.Count == 0)
		{
			BuildForestBackdrop();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BuildLayout && args.Count == 1)
		{
			BuildLayout(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.OnPath && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<Vector3>(OnPath(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2])));
			return true;
		}
		if (method == MethodName.GroundY && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<float>(GroundY(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1])));
			return true;
		}
		if (method == MethodName.Surface && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Material>(Surface(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1])));
			return true;
		}
		if (method == MethodName.TileRotation && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(TileRotation(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.ResolveTiles && args.Count == 1)
		{
			Texture2D[] callRet = ResolveTiles(VariantUtils.ConvertTo<string>(in args[0]));
			GodotObject[] array = callRet;
			ret = VariantUtils.CreateFromSystemArrayOfGodotObject(array);
			return true;
		}
		if (method == MethodName.AddMesh && args.Count == 5)
		{
			ret = VariantUtils.CreateFrom<MeshInstance3D>(AddMesh(VariantUtils.ConvertTo<Node3D>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Mesh>(in args[2]), VariantUtils.ConvertTo<Material>(in args[3]), VariantUtils.ConvertTo<bool>(in args[4])));
			return true;
		}
		if (method == MethodName.Footprint && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<Vector2[]>(Footprint(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<int>(in args[3])));
			return true;
		}
		if (method == MethodName.Box && args.Count == 6)
		{
			Box(VariantUtils.ConvertTo<Node3D>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Vector3>(in args[2]), VariantUtils.ConvertTo<Vector3>(in args[3]), VariantUtils.ConvertTo<string>(in args[4]), VariantUtils.ConvertTo<Color>(in args[5]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ReliefMesh && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<ArrayMesh>(ReliefMesh(VariantUtils.ConvertTo<Texture2D>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3])));
			return true;
		}
		if (method == MethodName.BatchStaticMeshes && args.Count == 0)
		{
			BatchStaticMeshes();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.AddLayoutLights && args.Count == 1)
		{
			AddLayoutLights(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Rgb && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Vector3>(Rgb(VariantUtils.ConvertTo<Color>(in args[0])));
			return true;
		}
		if (method == MethodName.ResetForestSourceLights && args.Count == 0)
		{
			ResetForestSourceLights();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BindForestSourceLights && args.Count == 0)
		{
			BindForestSourceLights();
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.AddMesh && args.Count == 5)
		{
			ret = VariantUtils.CreateFrom<MeshInstance3D>(AddMesh(VariantUtils.ConvertTo<Node3D>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<Mesh>(in args[2]), VariantUtils.ConvertTo<Material>(in args[3]), VariantUtils.ConvertTo<bool>(in args[4])));
			return true;
		}
		if (method == MethodName.Footprint && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<Vector2[]>(Footprint(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<int>(in args[3])));
			return true;
		}
		if (method == MethodName.ReliefMesh && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<ArrayMesh>(ReliefMesh(VariantUtils.ConvertTo<Texture2D>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3])));
			return true;
		}
		if (method == MethodName.Rgb && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Vector3>(Rgb(VariantUtils.ConvertTo<Color>(in args[0])));
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.Build)
		{
			return true;
		}
		if (method == MethodName.UpdateCameraFraming)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.MakeLighting)
		{
			return true;
		}
		if (method == MethodName.MakeForestLighting)
		{
			return true;
		}
		if (method == MethodName.MakeForestMaterial)
		{
			return true;
		}
		if (method == MethodName.BuildForest)
		{
			return true;
		}
		if (method == MethodName.BuildForestBackdrop)
		{
			return true;
		}
		if (method == MethodName.BuildLayout)
		{
			return true;
		}
		if (method == MethodName.OnPath)
		{
			return true;
		}
		if (method == MethodName.GroundY)
		{
			return true;
		}
		if (method == MethodName.Surface)
		{
			return true;
		}
		if (method == MethodName.TileRotation)
		{
			return true;
		}
		if (method == MethodName.ResolveTiles)
		{
			return true;
		}
		if (method == MethodName.AddMesh)
		{
			return true;
		}
		if (method == MethodName.Footprint)
		{
			return true;
		}
		if (method == MethodName.Box)
		{
			return true;
		}
		if (method == MethodName.ReliefMesh)
		{
			return true;
		}
		if (method == MethodName.BatchStaticMeshes)
		{
			return true;
		}
		if (method == MethodName.AddLayoutLights)
		{
			return true;
		}
		if (method == MethodName.Rgb)
		{
			return true;
		}
		if (method == MethodName.ResetForestSourceLights)
		{
			return true;
		}
		if (method == MethodName.BindForestSourceLights)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.Lighting)
		{
			Lighting = VariantUtils.ConvertTo<Godot.Environment>(in value);
			return true;
		}
		if (name == PropertyName.Camera)
		{
			Camera = VariantUtils.ConvertTo<Camera3D>(in value);
			return true;
		}
		if (name == PropertyName.Act)
		{
			Act = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.PhoneLayout)
		{
			PhoneLayout = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._mobilePerformance)
		{
			_mobilePerformance = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._forestShader)
		{
			_forestShader = VariantUtils.ConvertTo<Shader>(in value);
			return true;
		}
		if (name == PropertyName._forestDistanceScale)
		{
			_forestDistanceScale = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._pathAngle)
		{
			_pathAngle = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._static)
		{
			_static = VariantUtils.ConvertTo<Node3D>(in value);
			return true;
		}
		if (name == PropertyName._dynamic)
		{
			_dynamic = VariantUtils.ConvertTo<Node3D>(in value);
			return true;
		}
		if (name == PropertyName._surfaceShader)
		{
			_surfaceShader = VariantUtils.ConvertTo<Shader>(in value);
			return true;
		}
		if (name == PropertyName._airShader)
		{
			_airShader = VariantUtils.ConvertTo<Shader>(in value);
			return true;
		}
		if (name == PropertyName._elapsed)
		{
			_elapsed = VariantUtils.ConvertTo<double>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.Lighting)
		{
			value = VariantUtils.CreateFrom<Godot.Environment>(Lighting);
			return true;
		}
		if (name == PropertyName.Camera)
		{
			value = VariantUtils.CreateFrom<Camera3D>(Camera);
			return true;
		}
		int from;
		if (name == PropertyName.Act)
		{
			from = Act;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.PhoneLayout)
		{
			value = VariantUtils.CreateFrom<bool>(PhoneLayout);
			return true;
		}
		if (name == PropertyName.ForestAttackLightCount)
		{
			from = ForestAttackLightCount;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName._mobilePerformance)
		{
			value = VariantUtils.CreateFrom(in _mobilePerformance);
			return true;
		}
		if (name == PropertyName._forestShader)
		{
			value = VariantUtils.CreateFrom(in _forestShader);
			return true;
		}
		if (name == PropertyName._forestDistanceScale)
		{
			value = VariantUtils.CreateFrom(in _forestDistanceScale);
			return true;
		}
		if (name == PropertyName._pathAngle)
		{
			value = VariantUtils.CreateFrom(in _pathAngle);
			return true;
		}
		if (name == PropertyName._static)
		{
			value = VariantUtils.CreateFrom(in _static);
			return true;
		}
		if (name == PropertyName._dynamic)
		{
			value = VariantUtils.CreateFrom(in _dynamic);
			return true;
		}
		if (name == PropertyName._surfaceShader)
		{
			value = VariantUtils.CreateFrom(in _surfaceShader);
			return true;
		}
		if (name == PropertyName._airShader)
		{
			value = VariantUtils.CreateFrom(in _airShader);
			return true;
		}
		if (name == PropertyName._elapsed)
		{
			value = VariantUtils.CreateFrom(in _elapsed);
			return true;
		}
		if (name == PropertyName._forestPositions)
		{
			value = VariantUtils.CreateFrom(in _forestPositions);
			return true;
		}
		if (name == PropertyName._forestColors)
		{
			value = VariantUtils.CreateFrom(in _forestColors);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName.Lighting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName.Camera, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.Act, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.PhoneLayout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._mobilePerformance, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._forestShader, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._forestDistanceScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._pathAngle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._static, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._dynamic, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._surfaceShader, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._airShader, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._elapsed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.PackedVector4Array, PropertyName._forestPositions, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.PackedVector4Array, PropertyName._forestColors, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.ForestAttackLightCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.Lighting, Variant.From<Godot.Environment>(Lighting));
		info.AddProperty(PropertyName.Camera, Variant.From<Camera3D>(Camera));
		info.AddProperty(PropertyName.Act, Variant.From<int>(Act));
		info.AddProperty(PropertyName.PhoneLayout, Variant.From<bool>(PhoneLayout));
		info.AddProperty(PropertyName._mobilePerformance, Variant.From(in _mobilePerformance));
		info.AddProperty(PropertyName._forestShader, Variant.From(in _forestShader));
		info.AddProperty(PropertyName._forestDistanceScale, Variant.From(in _forestDistanceScale));
		info.AddProperty(PropertyName._pathAngle, Variant.From(in _pathAngle));
		info.AddProperty(PropertyName._static, Variant.From(in _static));
		info.AddProperty(PropertyName._dynamic, Variant.From(in _dynamic));
		info.AddProperty(PropertyName._surfaceShader, Variant.From(in _surfaceShader));
		info.AddProperty(PropertyName._airShader, Variant.From(in _airShader));
		info.AddProperty(PropertyName._elapsed, Variant.From(in _elapsed));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.Lighting, out var _value_Lighting))
		{
			Lighting = _value_Lighting.As<Godot.Environment>();
		}
		if (info.TryGetProperty(PropertyName.Camera, out var _value_Camera))
		{
			Camera = _value_Camera.As<Camera3D>();
		}
		if (info.TryGetProperty(PropertyName.Act, out var _value_Act))
		{
			Act = _value_Act.As<int>();
		}
		if (info.TryGetProperty(PropertyName.PhoneLayout, out var _value_PhoneLayout))
		{
			PhoneLayout = _value_PhoneLayout.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._mobilePerformance, out var _value__mobilePerformance))
		{
			_mobilePerformance = _value__mobilePerformance.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._forestShader, out var _value__forestShader))
		{
			_forestShader = _value__forestShader.As<Shader>();
		}
		if (info.TryGetProperty(PropertyName._forestDistanceScale, out var _value__forestDistanceScale))
		{
			_forestDistanceScale = _value__forestDistanceScale.As<float>();
		}
		if (info.TryGetProperty(PropertyName._pathAngle, out var _value__pathAngle))
		{
			_pathAngle = _value__pathAngle.As<float>();
		}
		if (info.TryGetProperty(PropertyName._static, out var _value__static))
		{
			_static = _value__static.As<Node3D>();
		}
		if (info.TryGetProperty(PropertyName._dynamic, out var _value__dynamic))
		{
			_dynamic = _value__dynamic.As<Node3D>();
		}
		if (info.TryGetProperty(PropertyName._surfaceShader, out var _value__surfaceShader))
		{
			_surfaceShader = _value__surfaceShader.As<Shader>();
		}
		if (info.TryGetProperty(PropertyName._airShader, out var _value__airShader))
		{
			_airShader = _value__airShader.As<Shader>();
		}
		if (info.TryGetProperty(PropertyName._elapsed, out var _value__elapsed))
		{
			_elapsed = _value__elapsed.As<double>();
		}
	}
}
