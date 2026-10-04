using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DeckRogue.Game;

namespace DeckRogue.EditorTools
{
    public static class CodexGraphicsCheck
    {
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        public static void Run()
        {
            int code = 0;
            try
            {
                const int size = 96;
                var type = typeof(StageLook);
                var flags = BindingFlags.Static | BindingFlags.NonPublic;
                var pattern = type.GetMethod("CookiePattern", flags);
                var canopy = type.GetMethod("AddCanopyCookie", flags);
                var a = (double[])pattern.Invoke(null, new object[] {size,20260930,4,0.64,0.18,0.62,0.16});
                var original = (double[])a.Clone(); var b = (double[])a.Clone(); var zero = (double[])a.Clone();
                canopy.Invoke(null,new object[]{a,size,20260930,0.64,0.18,0.62,0.9});
                canopy.Invoke(null,new object[]{b,size,20260930,0.64,0.18,0.62,0.9});
                canopy.Invoke(null,new object[]{zero,size,20260930,0.64,0.18,0.62,0.0});
                int changed = 0;
                for(int j=0;j<size;j++) for(int i=0;i<size;i++)
                {
                    int n=j*size+i;
                    Require(!double.IsNaN(a[n]) && a[n]>=0.38-1e-9 && a[n]<=1.0,"canopy range");
                    Require(a[n]==b[n],"canopy must be deterministic");
                    Require(zero[n]==original[n],"zero canopy must preserve old rendering");
                    double x=(i+0.5)/size*2-1,y=(j+0.5)/size*2-1;
                    if(x*x+y*y<0.64*0.64) Require(a[n]>=0.82-1e-9,"seat contrast exceeds 18%");
                    if(Math.Abs(a[n]-original[n])>0.01) changed++;
                }
                Require(changed>size*size/4,"canopy has no visible effect");
                var atlas=Resources.Load<Texture2D>("Art/fx/codex-vfx-atlas");
                Require(atlas!=null,"generated atlas not loaded");
                Require(atlas.width==atlas.height && atlas.width<=1024,"atlas dimensions/budget");
                Require(atlas.filterMode==FilterMode.Bilinear && atlas.mipmapCount==1,"atlas filtering");
                Require(!atlas.isReadable,"atlas CPU copy should be released");
                Require(CodexBattleFx.Ready,"VFX loader failed");
                foreach(var name in new[]{"cloud-silver-8f","cloud-arcane_impact-8f","cloud-guard_break-8f"}) {
                    var frames=Resources.Load<Texture2D>("Art/fx/"+name);
                    Require(frames!=null && frames.width==1024 && frames.height==512,"flipbook 4x2 dimensions: "+name);
                    Require(frames.filterMode==FilterMode.Bilinear && frames.mipmapCount==1 && !frames.isReadable,"flipbook filtering/memory: "+name);
                }
                Require(Resources.Load<GameObject>("CloudKit/ForestMasonry")!=null,"cloud masonry prefab");
                var worldFx=Shader.Find("DeckRogue/WorldBattleFx");
                Require(worldFx!=null && !ShaderUtil.ShaderHasError(worldFx),"world VFX shader compile");
                var unit=Shader.Find("DeckRogue/StageUnitLit");
                Require(unit!=null && !ShaderUtil.ShaderHasError(unit),"unit shader compile");
                var sub=ShaderUtil.GetShaderData(unit).GetSubshader(0);
                var names=new System.Collections.Generic.List<string>();
                for(int i=0;i<sub.PassCount;i++) names.Add(sub.GetPass(i).Name);
                Require(names.Contains("ShadowCaster") && names.Contains("DepthOnly"),"unit depth/shadow metadata passes");
                Debug.Log("[CodexGraphicsCheck] shader passes="+string.Join(",",names)+" device="+SystemInfo.graphicsDeviceType);
                Debug.Log("[CodexGraphicsCheck] PASS: deterministic canopy, legacy off, bounded seat contrast, visible coverage, atlas load/filter/memory; changed="+changed);
            }
            catch(Exception e) { Debug.LogError("[CodexGraphicsCheck] FAIL: "+e); code=1; }
            if(Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
