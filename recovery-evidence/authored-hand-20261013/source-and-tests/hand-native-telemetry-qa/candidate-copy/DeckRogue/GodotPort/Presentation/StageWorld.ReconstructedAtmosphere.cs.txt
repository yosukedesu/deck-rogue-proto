using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

// Reconstructed from three accepted native images and surviving checkpoint docs.
// The lost b05a0da code bodies are unavailable. This is a fresh, bounded rebuild.
public partial class StageWorld
{
    private readonly List<ShaderMaterial> _recoveredAirMaterials = new();
    private readonly List<(Node3D node, Vector3 origin, float phase)> _recoveredMotes = new();
    private Shader _recoveredAirShader;
    private bool _recoveredAtmosphereActive;

    private void ResetReconstructedAtmosphere()
    {
        _recoveredAirMaterials.Clear();
        _recoveredMotes.Clear();
        _recoveredAirShader = null;
        _recoveredAtmosphereActive = false;
    }

    private bool ReconstructedAtmosphereSupported()
    {
        var args = OS.GetCmdlineUserArgs();
        bool baseline = args.Contains("--stage-atmosphere-baseline") ||
            (Act == 1 && args.Contains("--forest-atmosphere-baseline")) ||
            (Act > 1 && args.Contains("--other-stage-atmosphere-baseline"));
        bool profilesMatch = true;
        foreach (var entry in ReconstructedAtmospherePolicy.SourceHashes)
        {
            if (!entry.Key.Contains($"act{Act}")) continue;
            string path = "res://" + entry.Key;
            if (!FileAccess.FileExists(path) || FileAccess.GetSha256(path) != entry.Value)
            {
                profilesMatch = false;
                GD.Print($"[AtmosphereRecovery] Baseline: profile differs at {entry.Key}");
                break;
            }
        }
        profilesMatch &= Lighting != null && Lighting.TonemapMode == Godot.Environment.ToneMapper.Filmic
            && MathF.Abs(Lighting.TonemapExposure - 1.12f) < .000001f
            && MathF.Abs(Lighting.TonemapWhite - 1.0f) < .000001f
            && !Lighting.AdjustmentEnabled && Camera?.Attributes == null;
        return ReconstructedAtmospherePolicy.Allows(Act,
            RenderingServer.GetCurrentRenderingMethod(), baseline, profilesMatch);
    }

    private void BuildReconstructedAtmosphere()
    {
        SetMeta("stage_atmosphere_active", false);
        SetMeta("stage_atmosphere_version", ReconstructedAtmospherePolicy.Version);
        if (!ReconstructedAtmosphereSupported()) return;
        // Resource guard is atomic: never leave a half-applied atmosphere profile.
        string surfacePath = Act == 1 ? "res://shaders/recovered_forest_surface.gdshader" : "res://shaders/recovered_stage_surface.gdshader";
        if (!ResourceLoader.Exists(surfacePath) || GD.Load<Shader>(surfacePath) == null ||
            (Act > 1 && !ResourceLoader.Exists("res://shaders/recovered_stage_profile.gdshaderinc")))
        {
            GD.PushWarning("[AtmosphereRecovery] Baseline: required stage profile shader is missing.");
            return;
        }
        _recoveredAirShader = GD.Load<Shader>("res://shaders/recovered_stage_air.gdshader");
        if (_recoveredAirShader == null) return;
        _recoveredAtmosphereActive = true;
        SetMeta("stage_atmosphere_active", true);
        if (Act == 1) BuildRecoveredForestAir();
        else BuildRecoveredOtherStage();
        GD.Print($"[AtmosphereRecovery] {ReconstructedAtmospherePolicy.Version}, act {Act}, stage-only");
    }

    private void BuildRecoveredForestAir()
    {
        // Preserve the inherited stage mesh, lantern, source lights and actor lane.
        // Only the stage material's distant air hues are nudged toward blue-violet.
        var forestProfile = GD.Load<Shader>("res://shaders/recovered_forest_surface.gdshader");
        if (forestProfile != null) _forestShader.Code = forestProfile.Code;
        var lantern = _lamps.FirstOrDefault(l => l.light.Name == "ForestLantern").light;
        var lanternPosition = lantern != null ? lantern.GlobalPosition : new Vector3(-6,2,-2);
        foreach (var material in _forestMaterials)
        {
            material.SetShaderParameter("recovered_lantern_position", lanternPosition);
            material.SetShaderParameter("fog_color", new Color(0.26f,0.30f,0.41f).SrgbToLinear());
            material.SetShaderParameter("height_color", Rgb(new Color(0.28f,0.33f,0.48f).SrgbToLinear()));
            material.SetShaderParameter("night_color", new Color(0.24f,0.29f,0.40f).SrgbToLinear());
            material.SetShaderParameter("lobe_color", Rgb(new Color(.40f,.46f,.61f).SrgbToLinear())*.8f);
            material.SetShaderParameter("grade_weight", .70f);
            material.SetShaderParameter("night_strength", .8f);
            var tint=material.GetShaderParameter("static_tint").AsVector3();
            material.SetShaderParameter("static_tint",tint*new Vector3(.78f,.91f,.96f));
        }
        AddRecoveredSheet("ForestLowMistFar", new Vector3(0,0.6f,-24), 42,2.2f, new Color(.42f,.47f,.64f,.11f));
        AddRecoveredSheet("ForestLowMistNear", new Vector3(0,.30f,-12), 27,1.05f, new Color(.41f,.48f,.65f,.065f));
        AddRecoveredShaft("ForestMoonShaftA", new Vector3(3.2f,9,-20), new Vector3(-.6f,.5f,-15), .65f,2.1f, new Color(.63f,.69f,.87f,.13f));
        AddRecoveredShaft("ForestMoonShaftB", new Vector3(7.0f,9.7f,-23), new Vector3(2.4f,.8f,-17), .35f,1.15f, new Color(.68f,.73f,.90f,.065f));
        AddRecoveredMotes(18, new Vector3(1.0f,2.6f,-15), new Vector3(15,4.4f,8), new Color(.69f,.76f,.86f,.12f));
    }

    private void BuildRecoveredOtherStage()
    {
        // Swap only the stage surface program. Existing textures/normal maps,
        // geometry, Environment and all source lights are retained.
        var shader = GD.Load<Shader>("res://shaders/recovered_stage_surface.gdshader");
        if(shader != null)
        {
            foreach (var material in _materials.Values.OfType<ShaderMaterial>())
                if(material.Shader == _surfaceShader)
                {
                    material.Shader = shader;
                    material.SetShaderParameter("recovered_stage_act", Act);
                }
        }
        // Relief tint preserves texture, alpha scissor and normal response.
        foreach (var material in _materials.Values.OfType<StandardMaterial3D>())
            material.AlbedoColor *= Act == 2 ? new Color(.78f,.66f,.51f) : new Color(.71f,.72f,.82f);

        // Recolor existing authored air locally, avoiding a global fog/grade.
        foreach(var part in _parts)
        {
            string kind = part.Value.HasMeta("source_kind") ? part.Value.GetMeta("source_kind").AsString() : "";
            foreach(var mesh in Descendants(part.Value).OfType<MeshInstance3D>())
            {
                if(mesh.MaterialOverride is not ShaderMaterial air || air.Shader != _airShader) continue;
                Color color = air.GetShaderParameter("air_color").AsColor();
                if(kind == "mist")
                    air.SetShaderParameter("air_color", Act == 2 ? new Color(.42f,.32f,.23f,Math.Min(color.A,.12f)) : new Color(.45f,.47f,.64f,Math.Min(color.A,.09f)));
                if(kind == "shaft" && Act == 3)
                    air.SetShaderParameter("air_color", new Color(.73f,.77f,.91f,Math.Min(color.A,.10f)));
            }
        }
        if(Act == 2)
        {
            AddRecoveredSheet("MineWallSmoke", new Vector3(0,2.0f,-11), 34,1.9f,new Color(.45f,.32f,.21f,.055f));
            AddRecoveredMotes(12,new Vector3(-3,3.0f,-8),new Vector3(14,3.7f,4),new Color(.80f,.53f,.25f,.11f));
        }
        else
        {
            AddRecoveredSheet("CityTierMistLow",new Vector3(0,.60f,-8),30,.85f,new Color(.49f,.50f,.66f,.045f));
            AddRecoveredSheet("CityTierMistHigh",new Vector3(0,2.5f,-14),38,1.0f,new Color(.46f,.49f,.64f,.065f));
            // Lowered endpoints keep the visible part below the phone's top crop.
            AddRecoveredShaft("CitySilverShaft",new Vector3(-2.0f,7.6f,-8),new Vector3(-5.1f,1.0f,-7),.34f,1.2f,new Color(.76f,.79f,.92f,.17f));
            AddRecoveredMotes(14,new Vector3(-2,3.2f,-8),new Vector3(7,4.4f,3),new Color(.75f,.79f,.88f,.12f));
        }
    }

    private ShaderMaterial RecoveredAirMaterial(Color color,int kind,float phase=0)
    {
        var material = new ShaderMaterial { Shader = _recoveredAirShader };
        material.SetShaderParameter("air_color",color);
        material.SetShaderParameter("air_kind",kind);
        material.SetShaderParameter("elapsed",0.0f);
        material.SetShaderParameter("phase",phase);
        _recoveredAirMaterials.Add(material);
        return material;
    }

    private MeshInstance3D AddRecoveredQuad(string name, Vector3[] vertices, Color color, int kind, float phase=0)
    {
        var geometry=new Geometry();
        geometry.Face(vertices,Vector3.Back,1,QuadUv);
        var node=AddMesh(_dynamic,name,geometry.ToMesh(),RecoveredAirMaterial(color,kind,phase),false);
        node.SetMeta("reconstructed_atmosphere",ReconstructedAtmospherePolicy.Version);
        return node;
    }

    private void AddRecoveredSheet(string name,Vector3 origin,float width,float height,Color color)
    {
        AddRecoveredQuad(name,new[]{origin+new Vector3(-width/2,height,0),origin+new Vector3(width/2,height,0),origin+new Vector3(width/2,0,0),origin+new Vector3(-width/2,0,0)},color,0);
    }

    private void AddRecoveredShaft(string name,Vector3 top,Vector3 bottom,float topWidth,float bottomWidth,Color color)
    {
        Vector3 across=(bottom-top).Cross(Camera.Position-top).Normalized();
        AddRecoveredQuad(name,new[]{top-across*topWidth/2,top+across*topWidth/2,bottom+across*bottomWidth/2,bottom-across*bottomWidth/2},color,1);
    }

    private void AddRecoveredMotes(int count,Vector3 center,Vector3 size,Color color)
    {
        // Deterministic arithmetic sequence, isolated from all gameplay RNG.
        for(int i=0;i<count;i++)
        {
            float x=Fraction((i+1)*.618033989f), y=Fraction((i+1)*.754877666f), z=Fraction((i+1)*.569840296f);
            var origin=center+new Vector3((x-.5f)*size.X,(y-.5f)*size.Y,(z-.5f)*size.Z);
            float radius=.015f+.012f*Fraction((i+1)*.41421356f);
            var node=AddRecoveredQuad($"AtmosphereMote{i}",new[]{new Vector3(-radius,radius,0),new Vector3(radius,radius,0),new Vector3(radius,-radius,0),new Vector3(-radius,-radius,0)},color,2,i);
            node.Position=origin;
            _recoveredMotes.Add((node,origin,i*1.732f));
        }
    }
    private static float Fraction(float value)=>value-MathF.Floor(value);
    private void SetReconstructedAtmosphereTime(float time)
    {
        if(!_recoveredAtmosphereActive)return;
        foreach(var material in _recoveredAirMaterials) material.SetShaderParameter("elapsed",time);
        foreach(var mote in _recoveredMotes)
            mote.node.Position=mote.origin+new Vector3(.04f*MathF.Sin(time*.11f+mote.phase),.06f*MathF.Sin(time*.08f+mote.phase),0);
    }
}
