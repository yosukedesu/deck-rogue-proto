using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    // Depth-tested transparent geometry in the stage camera, before world post-processing/UI.
    // The atlas is original generated art; no source-game assets are used.
    public static class WorldBattleFx
    {
        sealed class Slot {
            public Transform T; public MeshRenderer R,Particles,Glow,Dust,Projectile,ProjectileGlow; public bool Busy; public int Version; public float Started; public long AllocationSerial;
            public string Attack,Presentation,DamageTier,VariantRequested,VariantApplied,VariantFallback; public int Frame,ProjectileFrame=-1; public bool Landed; public float Age,Fade,ProjectileBlend;
            public StageFx.ContractLightPlayback ContractLight;
        }
        static readonly Slot[] Pool = new Slot[24];
        static int _contractEpoch;static long _allocationSerial;
        static ParticleSystem _embers; static Material _emberMaterial;
        public static int ParticleCount => _embers!=null?_embers.particleCount:0;
        static Mesh _quad; static Material _material, _elementMaterial, _luminousMaterial, _attackGlowMaterial; static Texture2D _atlas, _silver, _arcane, _guard, _trail;
        static readonly MaterialPropertyBlock Props = new MaterialPropertyBlock();
        internal static void CollectElementRenderers(System.Collections.Generic.List<Renderer> output) {
            output.Clear();
            foreach(var slot in Pool)if(slot!=null && slot.Busy) {
                AddPostRenderer(output,slot.R);AddPostRenderer(output,slot.Particles);AddPostRenderer(output,slot.Dust);AddPostRenderer(output,slot.Projectile);
            }
        }
        static void AddPostRenderer(System.Collections.Generic.List<Renderer> output,MeshRenderer renderer) {
            if(renderer!=null && renderer.gameObject.activeInHierarchy && (renderer.sharedMaterial==_elementMaterial || renderer.sharedMaterial==_luminousMaterial))output.Add(renderer);
        }
        public static int LuminousEmittedCount {get;private set;}
        public static int LegacyElementUses {get;private set;}
        internal static void RestoreLegacyElements(){
            foreach(var slot in Pool)if(slot!=null && slot.Busy) {
                RestoreLegacyRenderer(slot.R);RestoreLegacyRenderer(slot.Particles);RestoreLegacyRenderer(slot.Dust);RestoreLegacyRenderer(slot.Projectile);
            }
        }
        static void RestoreLegacyRenderer(MeshRenderer renderer) {
            if(renderer!=null && (renderer.sharedMaterial==_elementMaterial || renderer.sharedMaterial==_luminousMaterial)){renderer.sharedMaterial=_material;LegacyElementUses++;}
        }
        public static int EmittedCount {get;private set;}
        public static int PoolRecycledCount {get;private set;}
        public static int RecycledAttackCount {get;private set;}
        public static int StaleAttackContactCount {get;private set;}
        public static void ConfigureLightningR9Candidate(bool enabled)=>LightningR9CandidateFx.Configure(enabled);
        public static void ConfigurePhysicalMediumCandidate(bool enabled)=>PhysicalMediumCandidateFx.Configure(enabled);
        public static void ConfigureWaterPbfCandidate(bool enabled)=>WaterPbfFx.Configure(enabled);
        public static bool WaterPbfCandidateReady=>WaterPbfFx.Ready;
        public static void Cancel() {
            _contractEpoch++;Hd2dAttackFx.Cancel();LightningR9CandidateFx.Cancel();PhysicalMediumCandidateFx.Cancel();WaterPbfFx.Cancel();BoundedContactFx.Cancel();LocalMaterialProfileFx.Cancel();WideProfileFx.Cancel();ScatterProfileFx.Cancel();LightningDischargeFx.Cancel();EarthStompFx.Cancel();IceCrystalFx.Cancel();LightRayFx.Cancel();HealContinuousFx.Cancel();DarkDrainFx.Cancel();FireContinuousFx.Cancel();WaterContinuousFx.Cancel();PhysicalSlashFx.Cancel();VineSlashFx.Cancel();EnemyContactFx.Cancel();PlayerContactFx.Cancel();SystemOutcomeFx.Cancel();
            if(_embers!=null)_embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var slot in Pool)if(slot!=null){slot.ContractLight?.Cancel();slot.ContractLight=null;slot.Version++;slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}
        }
        public static int ActiveCount { get {int n=0;foreach(var slot in Pool)if(slot!=null && slot.Busy)n++;return n+Hd2dAttackFx.ActiveCount+LightningR9CandidateFx.ActiveCount+PhysicalMediumCandidateFx.ActiveCount+WaterPbfFx.ActiveCount+ParticleCount+BoundedContactFx.ActiveCount+LocalMaterialProfileFx.ActiveCount+WideProfileFx.ActiveCount+ScatterProfileFx.ActiveCount+LightningDischargeFx.ActiveCount+EarthStompFx.ActiveCount+IceCrystalFx.ActiveCount+LightRayFx.ActiveCount+HealContinuousFx.ActiveCount+DarkDrainFx.ActiveCount+FireContinuousFx.ActiveCount+WaterContinuousFx.ActiveCount+PhysicalSlashFx.ActiveCount+VineSlashFx.ActiveCount+EnemyContactFx.ActiveCount+PlayerContactFx.ActiveCount+SystemOutcomeFx.ActiveCount;} }
        static bool Ready()
        {
            if (!Tween.SchedulingAllowed || !Stage.CombatWorld || Stage.Camera == null || !Stage.Camera.isActiveAndEnabled) return false;
            if (_material == null)
            {
                var shader = Shader.Find("DeckRogue/WorldBattleFx");
                _atlas = Resources.Load<Texture2D>("Art/fx/codex-vfx-atlas");
                if (shader == null || !shader.isSupported || _atlas == null) return false;
                _silver=Resources.Load<Texture2D>("Art/fx/cloud-silver-8f");
                _arcane=Resources.Load<Texture2D>("Art/fx/cloud-arcane_impact-8f");
                _guard=Resources.Load<Texture2D>("Art/fx/cloud-guard_break-8f");
                _material = new Material(shader); _material.SetTexture("_BaseMap", _atlas);
                _trail=new Texture2D(64,16,TextureFormat.RGBA32,false);_trail.name="world-strike-ribbon";_trail.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[64*16];for(int y=0;y<16;y++)for(int x=0;x<64;x++) {
                    float u=(x+.5f)/64f,v=Mathf.Abs((y+.5f)/16f*2-1);
                    pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Sin(u*Mathf.PI),.45f)*Mathf.Pow(1-v,2));
                }_trail.SetPixels(pixels);_trail.Apply(false,true);
                _quad = new Mesh { name = "combat-fx-quad" };
                _quad.vertices = new[] { new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0) };
                _quad.colors = new[]{Color.white,Color.white,Color.white,Color.white};
                _quad.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
                _quad.triangles = new[] { 0,2,1,0,3,2 }; _quad.RecalculateBounds();
            }
            return true;
        }
        static Slot AcquireSlot() {
            Slot slot=null;
            int capacity=GfxQuality.Level==GfxLevel.Low ? 12 : 24;
            for(int i=0;i<capacity;i++) { if(Pool[i]==null)Pool[i]=new Slot(); if(Pool[i].T==null || !Pool[i].Busy){slot=Pool[i];break;} }
            // Under rapid AoE, recycle the oldest residue instead of dropping the newest hit.
            if(slot==null){slot=Pool[0];for(int i=1;i<capacity;i++)if(Pool[i].AllocationSerial<slot.AllocationSerial)slot=Pool[i];PoolRecycledCount++;if(slot.Attack!=null)RecycledAttackCount++;}
            slot.AllocationSerial=++_allocationSerial;
            if(slot.T==null)
            {
                var go=new GameObject("world-combat-fx"); slot.T=go.transform;
                go.AddComponent<MeshFilter>().sharedMesh=_quad; slot.R=go.AddComponent<MeshRenderer>();
                slot.R.sharedMaterial=_material; slot.R.shadowCastingMode=ShadowCastingMode.Off; slot.R.receiveShadows=false;
            }
            slot.ContractLight?.Cancel();slot.ContractLight=null;slot.Attack=null;slot.Landed=false;slot.ProjectileFrame=-1;
            if(slot.Particles!=null)slot.Particles.gameObject.SetActive(false);
            if(slot.Glow!=null)slot.Glow.gameObject.SetActive(false);
            if(slot.Dust!=null)slot.Dust.gameObject.SetActive(false);
            if(slot.Projectile!=null)slot.Projectile.gameObject.SetActive(false);
            if(slot.ProjectileGlow!=null)slot.ProjectileGlow.gameObject.SetActive(false);
            return slot;
        }
        static void Emit(Vector3 from, Vector3 to, int cell, Color tint, float size, float duration, float angle, float sweep, bool projectile=false, Texture2D flipbook=null, bool ground=false, float gain=1.3f, float aspect=1f, bool authored=false, bool luminous=false,float scalarGain=1f)
        {
            if (!Ready()) return;
            var slot=AcquireSlot();
            if(luminous && _luminousMaterial==null){var shader=Shader.Find("DeckRogue/ElementLuminousFx");if(shader!=null&&shader.isSupported)_luminousMaterial=new Material(shader);}
            if(luminous && _luminousMaterial!=null && ElementVfxPass.CanUsePost(Stage.Camera)){slot.R.sharedMaterial=_luminousMaterial;LuminousEmittedCount++;}
            else if(authored && ElementVfxPass.CanUsePost(Stage.Camera)) {
                if(_elementMaterial==null){var shader=Shader.Find("DeckRogue/ElementBattleFx");if(shader!=null && shader.isSupported)_elementMaterial=new Material(shader);}
                slot.R.sharedMaterial=_elementMaterial!=null?_elementMaterial:_material;
            } else {slot.R.sharedMaterial=_material;if(authored)LegacyElementUses++;}
            EmittedCount++;
            int version=++slot.Version; slot.Busy=true; slot.Started=Time.unscaledTime; slot.T.gameObject.SetActive(true);
            float inset=.5f/_atlas.width;
            Vector4 uv=new Vector4(.5f-2*inset,.5f-2*inset,(cell%2)*.5f+inset,cell<2?.5f+inset:inset);
            var facing=ground?Quaternion.Euler(90,0,0):Stage.LayoutRotation;
            Tween.Run(duration,k=>
            {
                if(slot.T==null || slot.Version!=version)return;
                float q=1-Mathf.Pow(1-k,2);
                if(flipbook!=null){int frame=authored?ElementBattleFx.Frame(k):Mathf.Min(7,Mathf.FloorToInt(k*7)+1);if(authored)ElementBattleFx.Observe(flipbook.name,frame);float px=.5f/flipbook.width,py=.5f/flipbook.height;uv=new Vector4(.25f-2*px,.5f-2*py,(frame%4)*.25f+px,frame<4?.5f+py:py);}
                if(flipbook==_trail)uv=new Vector4(1,1,0,0);
                slot.T.position=Vector3.Lerp(from,to,projectile?k:0);
                slot.T.rotation=facing*Quaternion.Euler(0,0,angle+(flipbook==null?sweep*q:0));
                slot.T.localScale=new Vector3(1,aspect,1)*size*(authored||projectile?1:Mathf.Lerp(flipbook!=null?.96f:.65f,1.08f,q));
                float alpha=authored?1:projectile?(flipbook==_trail?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,k)):1):flipbook!=null ? 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(flipbook==_silver?.28f:.68f,1,k)) : Mathf.Min(1,k*24)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,k)));
                Props.Clear(); Props.SetTexture("_BaseMap",flipbook!=null?flipbook:_atlas); Props.SetVector("_AtlasRect",uv); Props.SetColor("_Tint",new Color(tint.r*gain,tint.g*gain,tint.b*gain,tint.a*alpha)); Props.SetFloat("_BodyGain",scalarGain); slot.R.SetPropertyBlock(Props);
            },Ease.Linear,()=>{if(slot.Version==version){slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}},()=>{if(slot.Version==version){slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}},()=>slot.T!=null && slot.Busy && slot.Version==version);
        }
        // Approved V2 production contract. Time is relative to the actual HP/contact callback.
        // Frame 2 is an artistic image hold; it never pauses lights, actors or sequence deadlines.
        internal sealed class AttackSpec {
            public readonly float[] Boundaries;
            public readonly float SizePerHeight,BodyGain,EmissionGain,LightHold,TargetTau,FloorTau,FootLiftPerHeight,LightReleasePhase;
            public readonly Color TargetColor,FloorColor;
            public readonly string[] Layers;
            public string AssetFamily,MaxCombineGroup; public System.Func<float,float> Envelope; public AttackSpec Projectile; public bool ContractLighting,FeetAnchored,StandardContractPower;
            public float TargetLightHeightFraction=-1f;
            public float TargetPeakMultiplier=1f,FloorPeakMultiplier=1f,TargetPreGain,FloorPreGain,TargetPreExponent=1f,FloorPreExponent=1f;
            public float End=>Boundaries[8];
            public AttackSpec(float[] boundaries,float size,float bodyGain,float emissionGain,Color target,Color floor,float hold,float targetTau,float floorTau,float footLift=0f,float releasePhase=0f,string[] layers=null) {
                Boundaries=boundaries;SizePerHeight=size;BodyGain=bodyGain;EmissionGain=emissionGain;TargetColor=target;FloorColor=floor;
                LightHold=hold;TargetTau=targetTau;FloorTau=floorTau;FootLiftPerHeight=footLift;LightReleasePhase=releasePhase;
                Layers=layers??(footLift>0f?new[]{"body","particles","emission","dust"}:new[]{"body","particles","emission"});
            }
            public int Frame(float age) {for(int i=7;i>0;i--)if(age>=Boundaries[i])return i;return 0;}
            // Alpha/erosion already live in the pixels. Only the final cell needs an endpoint fade.
            public float Fade(float age)=>1f-Mathf.InverseLerp(Boundaries[7],End,age);
            public float ReleaseBlend(float age)=>Frame(age)==6?Mathf.InverseLerp(Boundaries[6],Boundaries[7],age):0f;
        }
        static readonly AttackSpec SlashAttack=new AttackSpec(
            new[]{-.066666667f,-.033333333f,0f,.066666667f,.100f,.150f,.216666667f,.283333333f,.333333333f},
            4.6f/2.30f,1.05f,3.55f,new Color(1f,.83f,.60f),new Color(1f,.83f,.64f),.066666667f,.046f,.046f,0f,.016666667f);
        static readonly AttackSpec WaterAttack=new AttackSpec(
            new[]{-.075f,-.0375f,0f,.075f,.125f,.183333333f,.266666667f,.333333333f,.390f},
            5.0f/2.30f,.94f,4.8f,new Color(.56f,.76f,1f),new Color(.56f,.76f,1f),.075f,.043f,.072f);
        static readonly AttackSpec StompAttack=new AttackSpec(
            new[]{-.075f,-.0375f,0f,.120f,.170f,.216666667f,.316666667f,.400f,.490f},
            5.9f/2.30f,.94f,4.4f,new Color(1f,.94f,.84f),new Color(1f,.94f,.84f),.120f,.052f,.085f,.08f/2.30f);
        // Frozen fire/light/dark asset contract plus the reviewed 13/23 anticipation-only remap.
        // No HP contact or old showcase timing is changed. Dark is absorption-specific.
        static readonly AttackSpec FireProjectile=new AttackSpec(
            new[]{-.130f,-.200f*13f/23f,-.170f*13f/23f,-.140f*13f/23f,-.110f*13f/23f,-.080f*13f/23f,-.050f*13f/23f,-.020f*13f/23f,0f},
            2.8f/2.3f,.96f,5f,Color.white,Color.white,0f,0f,0f,layers:new[]{"body","emission"}) {AssetFamily="fire-projectile"};
        static readonly AttackSpec FireAttack=new AttackSpec(
            new[]{-.130f,-.065f,0f,.067f,.125f,.200f,.300f,.400f,.460f},
            4.8f/2.3f,.96f,6.5f,new Color(1f,.22f,.028f),new Color(1f,.22f,.028f),.067f,.055f,.095f)
            {AssetFamily="fire",Projectile=FireProjectile,ContractLighting=true,TargetPreGain=.16f,FloorPreGain=.11f,TargetPreExponent=5f,FloorPreExponent=4f};
        static readonly AttackSpec LightAttack=new AttackSpec(
            new[]{-.100f,-.050f,0f,.050f,.090f,.140f,.215f,.285f,.340f},
            4.2f/2.3f,.96f,7.5f,new Color(.82f,.83f,1f),new Color(.82f,.83f,1f),.050f,.036f,.060f)
            {AssetFamily="light",ContractLighting=true,TargetPeakMultiplier=.65f,FloorPeakMultiplier=.80f};
        static readonly AttackSpec DarkDrainAttack=new AttackSpec(
            new[]{-.130f,-.065f,0f,.050f,.110f,.210f,.360f,.500f,.590f},
            6f/2.3f,.96f,8f,new Color(.49f,.07f,1f),new Color(.49f,.07f,1f),.050f,.048f,.080f)
            {AssetFamily="dark",ContractLighting=true,TargetPreGain=.30f,FloorPreGain=.24f,TargetPreExponent=2f,FloorPreExponent=2f};
        static readonly AttackSpec IceLanceAttack=new AttackSpec(
            new[]{-.130f,-.065f,0f,.075f,.125f,.200f,.300f,.400f,.490f},
            4.8f/2.3f,.96f,5.5f,new Color(.35f,.70f,1f),new Color(.35f,.70f,1f),.075f,.050f,.075f)
            {AssetFamily="ice",ContractLighting=true,TargetPeakMultiplier=.86f,TargetPreGain=.10f,FloorPreGain=.06f,TargetPreExponent=2f,FloorPreExponent=2f};
        static readonly AttackSpec VineLashAttack=new AttackSpec(
            new[]{-.120f,-.060f,0f,.050f,.100f,.170f,.260f,.360f,.430f},
            5.5f/2.3f,.96f,5.5f,new Color(.36f,1f,.09f),new Color(.36f,1f,.09f),.050f,.042f,.070f)
            {AssetFamily="vine",ContractLighting=true,TargetPreGain=.08f,FloorPreGain=.03f,TargetPreExponent=3f,FloorPreExponent=3f};
        static readonly AttackSpec EarthFissureAttack=new AttackSpec(
            new[]{-.140f,-.070f,0f,.090f,.150f,.240f,.360f,.490f,.590f},
            6.6f/2.3f,.96f,4.6f,new Color(1f,.64f,.27f),new Color(1f,.64f,.27f),.090f,.075f,.110f,.08f/2.3f,layers:new[]{"body","particles","emission"})
            {AssetFamily="earth",ContractLighting=true,FeetAnchored=true,TargetLightHeightFraction=.30f,TargetPeakMultiplier=.70f,FloorPeakMultiplier=.70f,TargetPreGain=.10f,FloorPreGain=.20f,TargetPreExponent=2f,FloorPreExponent=2f};
        static readonly AttackSpec LightningDischargeAttack=new AttackSpec(
            new[]{-.100f,-.050f,0f,.033333333f,.065f,.110f,.180f,.260f,.320f},
            4.2f/2.3f,.96f,6.8f,new Color(.34f,.40f,1f),new Color(.34f,.40f,1f),.033333333f,.028f,.054f)
            {AssetFamily="lightning",ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.80f,FloorPeakMultiplier=.90f,TargetPreGain=.06f,FloorPreGain=.025f,TargetPreExponent=4f,FloorPreExponent=4f};
        static readonly AttackSpec HealUpliftAttack=new AttackSpec(
            new[]{-.130f,-.065f,0f,.100f,.180f,.280f,.390f,.500f,.590f},
            4.4f/2.3f,.96f,3f,new Color(.62f,1f,.38f),new Color(.62f,1f,.38f),.100f,.150f,.150f)
            {AssetFamily="heal",ContractLighting=true,StandardContractPower=true,TargetPeakMultiplier=.80f,FloorPeakMultiplier=.80f,TargetPreGain=1f,FloorPreGain=1f,TargetPreExponent=2f,FloorPreExponent=2f};
        internal static AttackSpec AttackProfile(string attack)=>attack=="slash"?SlashAttack:attack=="water"?WaterAttack:attack=="stomp"?StompAttack:attack=="fire"?FireAttack:attack=="light"?LightAttack:attack=="dark_drain"?DarkDrainAttack:attack=="ice_lance"?IceLanceAttack:attack=="vine_lash"?VineLashAttack:attack=="earth_fissure"?EarthFissureAttack:attack=="lightning_discharge"?LightningDischargeAttack:attack=="heal_uplift"?HealUpliftAttack:null;
        public static bool ContractAttack(string attack)=>AttackProfile(attack)?.ContractLighting==true;
        public static string[] AuthoredProjectileLayers(string attack)=>AttackProfile(attack)?.Projectile?.Layers??System.Array.Empty<string>();
        internal static Vector2 FireProjectileOffset(float age,float height) {
            float q=Mathf.Clamp01((age+.130f)/.130f);
            return new Vector2(-2.70f/2.30f,-.38f/2.30f)*(1f-q)*height;
        }
        public static string[] AuthoredAttackLayers(string attack)=>AttackProfile(attack)?.Layers ?? System.Array.Empty<string>();
        public static int AuthoredAttackFrame(string attack,float age)=>AttackProfile(attack)?.Frame(age)??-1;
        public static float AuthoredAttackDuration(string attack) {if(BoundedContactFx.IsFamily(attack))return BoundedContactFx.Duration(attack);if(LocalMaterialProfileFx.IsFamily(attack))return LocalMaterialProfileFx.Duration(attack);if(WideProfileFx.IsFamily(attack))return WideProfileFx.Duration(attack);if(ScatterProfileFx.IsFamily(attack))return ScatterProfileFx.Duration(attack);if(LightningDischargeFx.IsFamily(attack))return (float)LightningDischargeGeometry.End();if(EarthStompFx.IsFamily(attack))return (float)EarthStompGeometry.End(EarthStompFx.Family(attack));if(IceCrystalFx.IsFamily(attack))return (float)IceCrystalGeometry.End();if(LightRayFx.IsFamily(attack))return (float)LightRayGeometry.End();if(HealContinuousFx.IsFamily(attack))return (float)HealContinuousGeometry.End();if(DarkDrainFx.IsFamily(attack))return (float)DarkDrainGeometry.End();if(FireContinuousFx.IsFamily(attack))return (float)FireContinuousGeometry.End(FireContinuousFx.Mode(attack));if(attack=="water")return (float)WaterContinuousGeometry.End();if(attack=="slash")return (float)PhysicalSlashGeometry.End("medium");if(PlayerContactFx.IsFamily(attack))return (float)PlayerContactGeometry.End(PlayerContactFx.Family(attack));if(EnemyContactFx.IsFamily(attack))return (float)EnemyContactGeometry.End(EnemyContactFx.Family(attack));var p=AttackProfile(attack);return p!=null?p.End-p.Boundaries[0]:0f;}
        public static float AuthoredAttackLead(ResolvedCombatFx.Ticket ticket)=>Hd2dAttackFx.Eligible(ticket)?Hd2dAttackFx.Lead(ticket):ticket?.FixedSmallContact==true?0f:ticket?.LightningR9Binding!=null?(float)LightningR9Clock.NativeLead:BoundedContactFx.Eligible(ticket)?0f:LocalMaterialProfileFx.Eligible(ticket)?0f:WideProfileFx.Eligible(ticket)?WideProfileFx.Lead(ticket):ScatterProfileFx.Eligible(ticket)?0f:LightningDischargeFx.Eligible(ticket)?LightningDischargeFx.Lead(ticket):EarthStompFx.Eligible(ticket)?EarthStompFx.Lead(ticket):IceCrystalFx.Eligible(ticket)?IceCrystalFx.Lead(ticket):LightRayFx.Eligible(ticket)?LightRayFx.Lead(ticket):DarkDrainFx.Eligible(ticket)?DarkDrainFx.Lead(ticket):FireContinuousFx.Eligible(ticket)?FireContinuousFx.Lead(ticket):WaterContinuousFx.Eligible(ticket)?(float)WaterContinuousGeometry.Lead():PhysicalSlashFx.Eligible(ticket)?PhysicalSlashFx.Lead(ticket):AuthoredAttackLead(ticket?.AttackFamily);
        public static float AuthoredAttackLead(string attack)=>BoundedContactFx.IsFamily(attack)?0f:LocalMaterialProfileFx.IsFamily(attack)?0f:WideProfileFx.IsFamily(attack)?(float)WideProfileGeometry.Lead():ScatterProfileFx.IsFamily(attack)?0f:LightningDischargeFx.IsFamily(attack)?(float)LightningDischargeGeometry.Lead():PlayerContactFx.IsFamily(attack)?(float)PlayerContactGeometry.Anticipation:attack=="enemy_beam"?(float)EnemyContactGeometry.BeamAnticipation:attack=="vine_slash"?(float)VineSlashGeometry.Anticipation:-(AttackProfile(attack)?.Boundaries[0]??0f);
        static readonly System.Collections.Generic.Dictionary<string,Texture2D[]> AttackTextures=new System.Collections.Generic.Dictionary<string,Texture2D[]>();
        public sealed class AttackPlayback {
            internal System.Func<bool> Contact;
            internal System.Func<object> Read;
            public bool Land()=>Contact!=null && Contact();
            public object StateForReview()=>Read?.Invoke();
        }
        public static int AuthoredAttackEmittedCount {get;private set;}
        static MeshRenderer AttackLayer(Slot slot,string name) {
            var go=new GameObject(name);go.transform.SetParent(slot.T,false);
            go.AddComponent<MeshFilter>().sharedMesh=_quad;
            var renderer=go.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return renderer;
        }
        static void SetAttackFrame(MeshRenderer renderer,Texture2D texture,int frame,float gain,float opacity,float releaseBlend) {
            float px=.5f/texture.width,py=.5f/texture.height;
            var uv=new Vector4(.25f-2*px,.5f-2*py,(frame%4)*.25f+px,frame<4?.5f+py:py);
            Props.Clear();Props.SetTexture("_BaseMap",texture);Props.SetVector("_AtlasRect",uv);
            int next=Mathf.Min(7,frame+1);
            Props.SetVector("_NextAtlasRect",new Vector4(.25f-2*px,.5f-2*py,(next%4)*.25f+px,next<4?.5f+py:py));
            Props.SetFloat("_FrameBlend",releaseBlend);
            // Scalars are already linear. SetColor would apply an unwanted sRGB conversion.
            Props.SetVector("_Tint",new Vector4(gain,gain,gain,opacity));Props.SetFloat("_BodyGain",gain);renderer.SetPropertyBlock(Props);
            ElementBattleFx.Observe(texture.name,frame);
        }
        static bool LoadAttackTextures(string family,AttackSpec spec,out Texture2D[] textures) {
            if(!AttackTextures.TryGetValue(family,out textures)) {
                textures=new Texture2D[spec.Layers.Length];
                for(int i=0;i<textures.Length;i++)textures[i]=Resources.Load<Texture2D>("Art/fx/attack-"+family+"-"+spec.Layers[i]+"-v2-8f");
                AttackTextures[family]=textures;
            }
            if(textures==null || textures.Length!=spec.Layers.Length)return false;
            foreach(var texture in textures)if(texture==null || texture.width!=1024 || texture.height!=512)return false;
            return true;
        }
        // Explicit opt-in for reviewed same-timeline shape variants. Empty until production
        // assets are accepted. Variable-duration variants require the later scheduling patch.
        static readonly System.Collections.Generic.Dictionary<string,string> SameTimelineVariants=new System.Collections.Generic.Dictionary<string,string>();
        static string SelectVariant(string attack,AttackSpec spec,string effectiveTier,ref Texture2D[] textures,out string requested,out string fallback) {
            requested=effectiveTier==null?null:attack+":"+effectiveTier;fallback=null;
            string family=spec.AssetFamily??attack;
            if(requested==null || effectiveTier=="medium")return family; // Reviewed base art is the medium variant.
            if(!SameTimelineVariants.TryGetValue(requested,out var candidate)){fallback="variant_unregistered";return family;}
            if(!LoadAttackTextures(candidate,spec,out var complete)){fallback="variant_resource_unavailable";return family;}
            textures=complete;return candidate; // All required layers switch atomically.
        }
        public static bool AuthoredAttackAvailable(string attack) {
            if(BoundedContactFx.IsFamily(attack))return BoundedContactFx.Ready();
            if(LocalMaterialProfileFx.IsFamily(attack))return LocalMaterialProfileFx.Available(attack);
            if(WideProfileFx.IsFamily(attack))return WideProfileFx.Available(attack);
            if(ScatterProfileFx.IsFamily(attack))return ScatterProfileFx.Ready();
            if(EarthStompFx.IsFamily(attack))return EarthStompFx.Ready(); // No legacy flat-ground sheet on failed proof.
            if(LightningDischargeFx.IsFamily(attack) && LightningDischargeFx.Ready())return true;
            if(IceCrystalFx.IsFamily(attack) && IceCrystalFx.Ready())return true;
            if(LightRayFx.IsFamily(attack) && LightRayFx.Ready())return true;
            if(HealContinuousFx.IsFamily(attack) && HealContinuousFx.Ready())return true;
            if(DarkDrainFx.IsFamily(attack) && DarkDrainFx.Ready())return true;
            if(FireContinuousFx.IsFamily(attack) && FireContinuousFx.Ready())return true;
            if(attack=="water" && WaterContinuousFx.Ready())return true;
            if(attack=="slash" && PhysicalSlashFx.Ready())return true;
            if(PlayerContactFx.IsFamily(attack))return PlayerContactFx.Ready(attack);
            if(attack=="vine_slash")return VineSlashFx.Ready();
            if(EnemyContactFx.IsFamily(attack))return EnemyContactFx.Ready(attack);
            var spec=AttackProfile(attack);if(spec==null || !Ready() || ElementBattleFx.ForceMissingForTest)return false;
            if(!LoadAttackTextures(spec.AssetFamily??attack,spec,out var textures))return false;
            if(spec.Projectile!=null && !LoadAttackTextures(spec.Projectile.AssetFamily,spec.Projectile,out var projectile))return false;
            var glow=Shader.Find("DeckRogue/AttackGlowFx");return glow!=null && glow.isSupported;
        }
        // Explicit presentation tiers only. They never inspect damage, source names or RNG.
        // Default values preserve all reviewed prototypes. Small is a provisional spatial/
        // exposure calibration; shape, alpha, anchors and every time boundary are unchanged.
        internal static float AuthoredSizeScale(string presentation)=>presentation=="small"?.72f:1f;
        internal static float AuthoredEmissionScale(string presentation)=>presentation=="small"?.75f:1f;
        internal static float AuthoredLightScale(string presentation)=>presentation=="small"?.65f:1f;
        // All render layers own one pool slot and clock. Light envelopes own the StageFx clock.
        public static AttackPlayback AuthoredAttackAt(Vector3 impact,Vector3 feet,float height,string attack,float initialElapsed=-1f,float residue=0f,string target=null,bool heavy=false,object actorIdentity=null,string presentation=null,DamageVfxIntensity intensity=null) {
            if(EarthStompFx.IsFamily(attack))return null; // Requires receipt-owned certified actual terrain.
            var spec=AttackProfile(attack);
            if(spec==null || !Ready() || ElementBattleFx.ForceMissingForTest)return null;
            if(!LoadAttackTextures(spec.AssetFamily??attack,spec,out var textures))return null;
            var visual=DamageVfxIntensity.VisualFor(intensity,presentation);
            string variant=SelectVariant(attack,spec,visual?.Tier,ref textures,out var requestedVariant,out var variantFallback);
            Texture2D[] projectileTextures=null;
            if(spec.Projectile!=null && !LoadAttackTextures(spec.Projectile.AssetFamily,spec.Projectile,out projectileTextures))return null;
            if(_attackGlowMaterial==null){var shader=Shader.Find("DeckRogue/AttackGlowFx");if(shader==null || !shader.isSupported)return null;_attackGlowMaterial=new Material(shader);}
            if(_elementMaterial==null){var shader=Shader.Find("DeckRogue/ElementBattleFx");if(shader!=null && shader.isSupported)_elementMaterial=new Material(shader);}
            var slot=AcquireSlot();
            if(slot.Particles==null)slot.Particles=AttackLayer(slot,"attack-particles");
            if(slot.Glow==null)slot.Glow=AttackLayer(slot,"attack-emission");
            if(textures.Length==4 && slot.Dust==null)slot.Dust=AttackLayer(slot,"attack-dust");
            if(projectileTextures!=null) {
                if(slot.Projectile==null)slot.Projectile=AttackLayer(slot,"attack-projectile");
                if(slot.ProjectileGlow==null)slot.ProjectileGlow=AttackLayer(slot,"attack-projectile-emission");
            }
            bool post=_elementMaterial!=null && ElementVfxPass.CanUsePost(Stage.Camera);
            slot.R.sharedMaterial=post?_elementMaterial:_material;slot.Particles.sharedMaterial=slot.R.sharedMaterial;
            slot.Glow.sharedMaterial=_attackGlowMaterial;
            if(projectileTextures!=null){slot.Projectile.sharedMaterial=slot.R.sharedMaterial;slot.ProjectileGlow.sharedMaterial=_attackGlowMaterial;}
            if(textures.Length==4){slot.Dust.sharedMaterial=slot.R.sharedMaterial;slot.Dust.gameObject.SetActive(true);}
            if(!post)LegacyElementUses+=textures.Length-1;
            slot.Attack=attack;slot.Presentation=presentation;slot.DamageTier=intensity?.Tier;slot.VariantRequested=requestedVariant;slot.VariantApplied=variant;slot.VariantFallback=variantFallback;slot.Particles.gameObject.SetActive(true);slot.Glow.gameObject.SetActive(true);
            // Pivot (128,128) is the actual origin in every cell. Stomp's perspective is already
            // baked: upright billboard, foot-relative .08/2.30 height clearance, no V1 image lift.
            float size=height*spec.SizePerHeight*(visual?.Size??AuthoredSizeScale(presentation));
            slot.T.position=spec.FeetAnchored?feet+Vector3.up*(height*spec.FootLiftPerHeight):attack=="stomp"?feet+Vector3.up*(height*spec.FootLiftPerHeight)-(Stage.LayoutRotation*Vector3.forward)*.28f:impact;
            slot.T.rotation=Stage.LayoutRotation;slot.T.localScale=Vector3.one*size;
            slot.Particles.transform.localPosition=new Vector3(0,0,-.001f);
            slot.Glow.transform.localPosition=new Vector3(0,0,-.002f);
            if(textures.Length==4)slot.Dust.transform.localPosition=new Vector3(0,0,-.003f);
            int version=++slot.Version;slot.Busy=true;slot.Started=Time.unscaledTime;slot.T.gameObject.SetActive(true);
            AuthoredAttackEmittedCount++;EmittedCount++;
            float lead=-spec.Boundaries[0],offset=Mathf.Clamp(initialElapsed<0f?lead:initialElapsed,0f,lead);
            if(spec.ContractLighting && target!=null)slot.ContractLight=StageFx.BeginContractAttackLight(target,actorIdentity,_contractEpoch,impact,feet,height,spec,spec.Boundaries[0]+offset,heavy,()=>slot.T!=null && slot.Busy && slot.Version==version,AuthoredLightScale(presentation),visual?.Light);
            System.Action<float> frameAt=age=>{
                int frame=spec.Frame(age);float fade=spec.Fade(age),blend=spec.ReleaseBlend(age);slot.Frame=frame;slot.Age=age;slot.Fade=fade;
                SetAttackFrame(slot.R,textures[0],frame,spec.BodyGain,fade,blend);
                SetAttackFrame(slot.Particles,textures[1],frame,visual?.Particles??1f,fade,blend);
                SetAttackFrame(slot.Glow,textures[2],frame,spec.EmissionGain*(visual?.Emission??AuthoredEmissionScale(presentation)),fade,blend);
                if(textures.Length==4)SetAttackFrame(slot.Dust,textures[3],frame,visual?.Particles??1f,fade,blend);
                if(projectileTextures!=null) {
                    var track=spec.Projectile;bool active=age>=track.Boundaries[0] && age<0f;
                    slot.Projectile.gameObject.SetActive(active);slot.ProjectileGlow.gameObject.SetActive(active);slot.ProjectileFrame=active?track.Frame(age):-1;slot.ProjectileBlend=active?track.ReleaseBlend(age):0f;
                    if(active) {
                        var move=FireProjectileOffset(age,height);float ratio=track.SizePerHeight/spec.SizePerHeight;
                        slot.Projectile.transform.localPosition=new Vector3(move.x/size,move.y/size,-.004f);
                        slot.ProjectileGlow.transform.localPosition=new Vector3(move.x/size,move.y/size,-.005f);
                        slot.Projectile.transform.localScale=slot.ProjectileGlow.transform.localScale=Vector3.one*ratio;
                        SetAttackFrame(slot.Projectile,projectileTextures[0],slot.ProjectileFrame,track.BodyGain,track.Fade(age),track.ReleaseBlend(age));
                        SetAttackFrame(slot.ProjectileGlow,projectileTextures[1],slot.ProjectileFrame,track.EmissionGain*(visual?.Emission??AuthoredEmissionScale(presentation)),track.Fade(age),track.ReleaseBlend(age));
                    }
                }
            };
            System.Action finish=()=>{if(slot.Version==version){slot.ContractLight?.Cancel();slot.ContractLight=null;slot.Busy=false;if(slot.T!=null)slot.T.gameObject.SetActive(false);}};
            var playback=new AttackPlayback {Contact=()=>{
                if(slot.T==null || !slot.Busy || slot.Version!=version){StaleAttackContactCount++;return false;}
                if(!Tween.SchedulingAllowed || !Stage.CombatWorld || Stage.Camera==null || !Stage.Camera.isActiveAndEnabled){finish();return false;}
                if(slot.Landed)return true;
                // Start at frame 2 synchronously even after a hitch. No callback from the preview
                // may overwrite this contact. Existing residue gates are a lifetime floor only.
                slot.Landed=true;
                if(spec.ContractLighting && target!=null && (slot.ContractLight==null || !slot.ContractLight.Land())) {
                    slot.ContractLight=StageFx.BeginContractAttackLight(target,actorIdentity,_contractEpoch,impact,feet,height,spec,0f,heavy,()=>slot.T!=null && slot.Busy && slot.Version==version,AuthoredLightScale(presentation),visual?.Light);
                    slot.ContractLight?.Land();
                }
                float tail=Mathf.Max(spec.End,residue);
                Tween.Run(tail,k=>{if(slot.T!=null && slot.Version==version)frameAt(k*tail);},Ease.Linear,finish,finish,()=>slot.T!=null && slot.Busy && slot.Version==version);
                return true;
            },Read=()=>slot.T!=null && slot.Busy && slot.Version==version ? AttackState(slot) : null};

            // Wait budget survives a delayed contact, but is bounded if a ticket is never landed.
            // Its expiry may only retire a preview, never an already-landed contact tail.
            float remaining=lead-offset+spec.End;
            Tween.Run(remaining,k=>{
                if(slot.T==null || slot.Version!=version || slot.Landed)return;
                frameAt(Mathf.Min(-.000001f,spec.Boundaries[0]+offset+k*remaining));
            },Ease.Linear,()=>{if(!slot.Landed)finish();},finish,()=>slot.T!=null && slot.Busy && slot.Version==version);
            return playback;
        }
        public static void BeginAuthoredAttack(ResolvedCombatFx.Ticket ticket,float contactAt) {
            if(ticket==null || ticket.Shown || !ticket.HasWorld || ticket.Outcome!="hit")return;
            // HD-2D の攻撃演出 (2026-10-27・dot の Godot 版 checkpoint 192): 登録のある札は家族の名前を持たなくても新しい描き手が先に受ける
            if(Hd2dAttackFx.Eligible(ticket)){ticket.AttackPlayback=Hd2dAttackFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(ticket.AttackFamily==null)return;
            if(ticket.FixedSmallContact){ticket.DeliveryPreparation="noncard_fixed_small_contact";return;}
            string attack=ticket.AttackFamily;
            if(BoundedContactFx.Eligible(ticket)){ticket.DeliveryPreparation="native_contact_only";return;}
            if(LocalMaterialProfileFx.Eligible(ticket)){ticket.DeliveryPreparation="native_contact_only";return;}
            if(WideProfileFx.Eligible(ticket)){ticket.AttackPlayback=WideProfileFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(ScatterProfileFx.Eligible(ticket)){ticket.DeliveryPreparation="native_contact_only";return;}
            if(EarthStompFx.Eligible(ticket)){if(ticket.AttackFamily=="stomp"){ticket.DeliveryPreparation="native_contact_only";return;}ticket.AttackPlayback=EarthStompFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(LightningDischargeFx.Eligible(ticket)){ticket.AttackPlayback=LightningR9CandidateFx.Begin(ticket,contactAt)??LightningDischargeFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(IceCrystalFx.Eligible(ticket)){ticket.AttackPlayback=IceCrystalFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(LightRayFx.Eligible(ticket)){ticket.AttackPlayback=LightRayFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(DarkDrainFx.Eligible(ticket)){ticket.AttackPlayback=DarkDrainFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(FireContinuousFx.Eligible(ticket)){if(ticket.AttackFamily=="fire_localburst"){ticket.DeliveryPreparation="native_contact_only";return;}ticket.AttackPlayback=FireContinuousFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(WaterContinuousFx.Eligible(ticket)){ticket.AttackPlayback=WaterPbfFx.Begin(ticket,contactAt)??WaterContinuousFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(PhysicalSlashFx.Eligible(ticket)){ticket.AttackPlayback=PhysicalSlashFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(PlayerContactFx.IsFamily(attack)){ticket.AttackPlayback=PlayerContactFx.Begin(ticket,contactAt);ticket.DeliveryPreparation=ticket.AttackPlayback!=null?"preview_started":"preview_unavailable";return;}
            if(attack=="vine_slash"){ticket.AttackPlayback=VineSlashFx.Begin(ticket,contactAt);return;}
            if(EnemyContactFx.IsFamily(attack)){ticket.AttackPlayback=EnemyContactFx.Begin(ticket,contactAt);return;}
            float elapsed=Mathf.Max(0f,Time.time-(contactAt-AuthoredAttackLead(attack)));
            ticket.AttackPlayback=AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,attack,elapsed,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        internal static AttackPlayback StandardLightningContact(ResolvedCombatFx.Ticket t)=>AuthoredAttackAt(t.Position,t.Feet,t.Height,"lightning_discharge",-1f,0f,t.Target,t.Heavy,t.Impact?.Target,t.AuthoredPresentation,t.Intensity);
        public static AttackPlayback AuthoredAttackContact(ResolvedCombatFx.Ticket ticket) {
            if(Hd2dAttackFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return Hd2dAttackFx.Begin(ticket);}
            if(ticket?.FixedSmallContact==true)ticket.DeliveryPreparation="noncard_fixed_small_contact";
            if(BoundedContactFx.Eligible(ticket)){ticket.DeliveryPreparation="native_contact_only";return BoundedContactFx.Begin(ticket);}
            if(LocalMaterialProfileFx.Eligible(ticket)){ticket.DeliveryPreparation="native_contact_only";return LocalMaterialProfileFx.Begin(ticket);}
            if(WideProfileFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="wide_profile_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return WideProfileFx.Begin(ticket);}
            if(ScatterProfileFx.Eligible(ticket)){ticket.DeliveryPreparation="native_contact_only";return ScatterProfileFx.Begin(ticket);}
            if(LightningDischargeFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="lightning_discharge_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return LightningDischargeFx.Begin(ticket);}
            if(EarthStompFx.Eligible(ticket)){if(ticket.AttackFamily=="stomp")ticket.DeliveryPreparation="native_contact_only";else if(ticket.DeliveryPreparation=="waiting_numeric_dependency")ticket.DeliveryPreparation="contact_only_preparation_unready";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return EarthStompFx.Begin(ticket);}
            if(IceCrystalFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="ice_crystal_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return IceCrystalFx.Begin(ticket);}
            if(LightRayFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="light_ray_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return LightRayFx.Begin(ticket);}
            if(DarkDrainFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="dark_drain_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return DarkDrainFx.Begin(ticket);}
            if(FireContinuousFx.Eligible(ticket)){if(ticket.AttackFamily=="fire_localburst")ticket.DeliveryPreparation="native_contact_only";else if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="fire_flow_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return FireContinuousFx.Begin(ticket);}
            if(WaterContinuousFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="water_flow_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return WaterContinuousFx.Begin(ticket);}
            if(PhysicalSlashFx.Eligible(ticket)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="slash_tier_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return PhysicalSlashFx.Begin(ticket);}
            if(PlayerContactFx.IsFamily(ticket.AttackFamily)){if(ticket.DeliveryPreparation=="waiting_numeric_dependency"){ticket.DeliveryPreparation="contact_only_preparation_unready";ticket.RenderFallbackReason="delivery_contact_only_preparation_unready";}else if(ticket.DeliveryPreparation=="preview_started")ticket.DeliveryPreparation="contact_only_preview_unavailable";else if(ticket.DeliveryPreparation==null||ticket.DeliveryPreparation=="ready")ticket.DeliveryPreparation="contact_only_no_preview";return PlayerContactFx.Begin(ticket);}
            if(ticket.AttackFamily=="vine_slash")return VineSlashFx.Begin(ticket);
            if(EnemyContactFx.IsFamily(ticket.AttackFamily))return EnemyContactFx.Begin(ticket);
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,ticket.AttackFamily,-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        // Called once by the receipt owner after Begin OR Land failure.
        internal static AttackPlayback StandardSlashContact(ResolvedCombatFx.Ticket ticket) {
            if(!PhysicalSlashFx.Eligible(ticket))return null;ticket.AuthoredRenderer=null;
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,"slash",-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        internal static AttackPlayback StandardWaterContact(ResolvedCombatFx.Ticket ticket) {
            if(!WaterContinuousFx.Eligible(ticket))return null;ticket.AuthoredRenderer=null;
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,"water",-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        internal static AttackPlayback StandardFireContact(ResolvedCombatFx.Ticket ticket) {
            if(!FireContinuousFx.Eligible(ticket)||ticket.AttackFamily!="fire")return null;ticket.AuthoredRenderer=null;
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,"fire",-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        internal static AttackPlayback StandardDarkDrainContact(ResolvedCombatFx.Ticket ticket) {
            if(!DarkDrainFx.Eligible(ticket))return null;ticket.AuthoredRenderer=null;
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,"dark_drain",-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        internal static AttackPlayback StandardLightRayContact(ResolvedCombatFx.Ticket ticket) {
            if(!LightRayFx.Eligible(ticket))return null;ticket.AuthoredRenderer=null;
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,"light",-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        internal static AttackPlayback StandardIceCrystalContact(ResolvedCombatFx.Ticket ticket) {
            if(!IceCrystalFx.Eligible(ticket))return null;ticket.AuthoredRenderer=null;
            return AuthoredAttackAt(ticket.Position,ticket.Feet,ticket.Height,"ice_lance",-1f,ticket.Showcase?.Residue??0f,ticket.Target,ticket.Heavy,ticket.Impact?.Target,ticket.AuthoredPresentation,ticket.Intensity);
        }
        static object AttackState(Slot slot) {
            return new {attack=slot.Attack,presentation=slot.Presentation??"standard",damageTier=slot.DamageTier,variantRequested=slot.VariantRequested,variantApplied=slot.VariantApplied,variantFallback=slot.VariantFallback,frame=slot.Frame,age=slot.Age,fade=slot.Fade,position=new[]{slot.T.position.x,slot.T.position.y,slot.T.position.z},size=slot.T.localScale.x,
                body=slot.R.sharedMaterial.shader.name,particles=slot.Particles.sharedMaterial.shader.name,emission=slot.Glow.sharedMaterial.shader.name,
                dust=slot.Dust!=null && slot.Dust.gameObject.activeSelf?slot.Dust.sharedMaterial.shader.name:null,projectileFrame=slot.ProjectileFrame,projectileBlend=slot.ProjectileBlend,projectileOffset=slot.Projectile!=null?new[]{slot.Projectile.transform.localPosition.x*slot.T.localScale.x,slot.Projectile.transform.localPosition.y*slot.T.localScale.x}:null,projectileActive=slot.Projectile!=null && slot.Projectile.gameObject.activeSelf};
        }
        internal static object[] AuthoredStatesForReview() {
            var result=new System.Collections.Generic.List<object>();
            foreach(var slot in Pool)if(slot!=null && slot.Busy && slot.Attack!=null && slot.T!=null)result.Add(AttackState(slot));
            return result.ToArray();
        }
        // Geometry snapshot only: no textures, materials, coroutines or legacy renderer initialization.
        // This is an explicit board-center fallback, not a weapon/eye socket.
        internal static bool TryCaptureBoardCenter(string key,out Vector3 point) {
            point=default;
            if(string.IsNullOrEmpty(key) || !Stage.TryGetUnitBox(key,out var feet,out var height) || height<=0f || float.IsNaN(height) || float.IsInfinity(height))return false;
            point=feet+(Stage.LayoutRotation*Vector3.up)*height*.5f-(Stage.LayoutRotation*Vector3.forward)*.28f;
            return !float.IsNaN(point.x)&&!float.IsNaN(point.y)&&!float.IsNaN(point.z)&&!float.IsInfinity(point.x)&&!float.IsInfinity(point.y)&&!float.IsInfinity(point.z);
        }
        static bool Socket(string key, out Vector3 point, out float height)
        {
            height=0f; Vector3 feet; if(!Ready() || !Stage.TryGetUnitBox(key,out feet,out height)){point=default;return false;}
            point=feet+(Stage.LayoutRotation*Vector3.up)*height*.5f-(Stage.LayoutRotation*Vector3.forward)*.28f;
            return true;
        }
        public static bool Hit(string target,string style,Color color,bool heavy,int index=0)
        {
            if(!Socket(target,out var p,out var h))return false;
            return HitAt(p,h,style,color,heavy,index,target);
        }
        public static bool HitAt(Vector3 p,float h,string style,Color color,bool heavy,int index=0,string target=null,DamageVfxIntensity.Visual visual=null)
        {
            if(!Ready())return false;
            color=CombatPalette.ForFx(style,color);
            float geometry=h*(visual?.Size??1f),gain=visual?.Emission??1f;
            bool legacyHeavy=visual==null && heavy;
            bool spell=style=="spell"||style=="light"||style=="spark"||style=="beam"||style=="throw";
            if(spell) {
                Emit(p,p,2,color,geometry*(legacyHeavy?2.0f:1.65f),.42f,0,0,false,_arcane,false,1.3f*gain);
                if(target!=null && Stage.TryGetUnitBox(target,out var feet,out var height)) Emit(feet+Vector3.up*.035f,feet,2,new Color(color.r,color.g,color.b,.65f),geometry*1.1f,.28f,0,0,false,_arcane,true,1.15f*gain);
            } else if(style=="stomp" || style=="vine") {
                var impact=style=="vine"?new Color(.48f,.86f,.55f,.82f):new Color(.82f,.66f,.44f,.82f);
                if(target!=null && Stage.TryGetUnitBox(target,out var feet,out var height)) Emit(feet+Vector3.up*.045f,feet,2,impact,geometry*1.6f,.36f,0,0,false,_arcane,true,1.05f*gain);
                Emit(p,p,1,impact,geometry*.48f,.16f,0,15,false,null,false,1.3f*gain);
            } else { Sparks(p,geometry,color,visual); Emit(p,p,0,new Color(color.r,color.g,color.b,.95f),geometry*(legacyHeavy?2.6f:2.15f),.30f,index%2==0?0:90,0,false,_silver,false,2.0f*gain); }
            Embers(p,geometry,color,heavy,0f,visual);
            Emit(p,p,1,new Color(color.r,color.g,color.b,.65f),geometry*(legacyHeavy?.70f:.48f),.19f,15,12,false,null,false,1.3f*gain);
            return true;
        }
        public static bool ElementAt(Vector3 p,float h,string element,DamageVfxIntensity.Visual visual=null) {
            if(!Ready() || !ElementBattleFx.TryGet(element,out var texture,out var duration))return false;
            Emit(p,p,2,Color.white,h*(element=="heal"?1.25f:1.6f)*(visual?.Size??1f),duration,0,0,false,texture,false,visual?.Emission??1f,1f,true,ElementBattleFx.Luminous(element),visual?.Emission??1f);
            return true;
        }
        public static void Trace(string target,Color? color=null) {
            if(target=="player")return;
            if(!Socket("player",out var a,out var ah)||!Socket(target,out var b,out var bh))return;
            var delta=Quaternion.Inverse(Stage.LayoutRotation)*(b-a);
            float width=new Vector2(delta.x,delta.y).magnitude;if(width<.2f)return;
            var start=Vector3.Lerp(a,b,.15f);var end=Vector3.Lerp(a,b,.86f);
            Emit(start,end,0,new Color((color??CombatPalette.Silver).r,(color??CombatPalette.Silver).g,(color??CombatPalette.Silver).b,.82f),width*.40f,.075f,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,0,true,_trail,false,2.1f,Mathf.Min(ah,bh)*.018f/(width*.40f));
        }
        static void Sparks(Vector3 p,float h,Color color,DamageVfxIntensity.Visual visual=null) {
            // A small asymmetric fan: bright contact then outward fragments, not a full-screen flash.
            float[] angles=visual?.Tier=="small"?new float[]{18,153,318}:visual?.Tier=="large"?new float[]{18,69,113,153,226,273,318}:new float[]{18,69,153,226,318};
            for(int i=0;i<angles.Length;i++) {
                float a=angles[i]*Mathf.Deg2Rad;var dir=Stage.LayoutRotation*new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                float len=h*(i%2==0?.65f:.43f);
                Emit(p+dir*h*.08f,p+dir*h*(i%2==0?.85f:.6f),0,new Color(color.r,color.g,color.b,.95f),len,.19f+(angles.Length==3?i*2:i%5)*.021f,angles[i],0,true,_trail,false,4f*(visual?.FallbackParticleGain??1f),.022f);
            }
        }
        static void Embers(Vector3 p,float h,Color color,bool heavy,float residue=0f,DamageVfxIntensity.Visual visual=null) {
            if(_embers==null) {
                var go=new GameObject("world-hit-embers");_embers=go.AddComponent<ParticleSystem>();
                _embers.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=_embers.main;main.loop=false;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
                main.maxParticles=GfxQuality.Level==GfxLevel.Low?64:128;main.gravityModifier=0;main.startSpeed=0;main.startLifetime=.4f;
                var emission=_embers.emission;emission.enabled=false;var shape=_embers.shape;shape.enabled=false;
                var size=_embers.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var fade=_embers.colorOverLifetime;fade.enabled=true;var grad=new Gradient();
                grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.9f,.2f),new GradientAlphaKey(0,1)});fade.color=grad;
                var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[256];for(int y=0;y<16;y++)for(int x=0;x<16;x++) {float rad=new Vector2((x-7.5f)/7.5f,(y-7.5f)/7.5f).magnitude;pixels[y*16+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-rad),1.6f));}
                texture.SetPixels(pixels);texture.Apply(false,true);
                _emberMaterial=new Material(_material);_emberMaterial.SetTexture("_BaseMap",texture);_emberMaterial.SetVector("_AtlasRect",new Vector4(1,1,0,0));_emberMaterial.SetColor("_Tint",new Color(5,5,5,1));
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=_emberMaterial;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                _embers.useAutoRandomSeed=false;_embers.randomSeed=4242;
            }
            _embers.Play();int fast=visual!=null?visual.Fragments*3/4:heavy?18:12,drift=visual!=null?visual.Fragments/4:heavy?6:4;
            for(int i=0;i<fast+drift;i++) {
                bool slow=i>=fast;float a=(i*137.508f+17)*Mathf.Deg2Rad;
                var dir=Stage.LayoutRotation*new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.7f+.25f,0);
                var e=new ParticleSystem.EmitParams {position=p,velocity=dir*h*(slow?.35f:2.2f+(i%3)*.6f),startLifetime=slow?Mathf.Max(.32f+(drift==2?(i-fast)*2:i%3)*.06f,residue):.18f+(i%4)*.03f,startSize=h*(slow?.018f:.024f),startColor=Color.Lerp(color,Color.white,slow?.15f:.45f)};
                if(visual!=null){Color c=e.startColor;e.startColor=new Color(c.r*visual.FallbackParticleGain,c.g*visual.FallbackParticleGain,c.b*visual.FallbackParticleGain,c.a);}
                _embers.Emit(e,1);
            }
        }
        public static void ShowcaseCharge(string source,Color color,CardVfxProfile.Showcase profile) {
            if(profile==null || !Socket(source,out var p,out var h))return;
            // Local gathering light, only before contact. No screen overlay or world-light reduction.
            Emit(p,p,2,new Color(color.r,color.g,color.b,.42f),h*(profile.Heavy?.65f:.36f),profile.Contact,0,0,false,_arcane,false,1.4f);
        }
        public static void ShowcaseResidue(Vector3 p,float h,Color color,CardVfxProfile.Showcase profile,DamageVfxIntensity.Visual visual=null) {
            if(profile==null || !Ready())return;
            // Particles do not consume contact quad slots, even in a four-target two-wave attack.
            Embers(p,h*(visual?.Size??1f),color,profile.Heavy,profile.Residue,visual);
        }
        public static void Cast(string source,string target,Color color) {
            if(!Ready())return;
            foreach(var key in new[]{source,target}) if(Stage.TryGetUnitBox(key,out var feet,out var h)) {
                var p=feet+Vector3.up*.035f;
                Emit(p,p,2,new Color(color.r,color.g,color.b,key==source?.48f:.25f),h*.85f,.3f,0,0,false,_arcane,true,1f);
            }
        }
        public sealed class ProjectileSnapshot { public Vector3 From,To; public float Height; }
        public static ProjectileSnapshot CaptureProjectile(string source,string target) {
            if(!Socket(source,out var a,out var ah)||!Socket(target,out var b,out var bh))return null;
            return new ProjectileSnapshot{From=a,To=b,Height=Mathf.Min(ah,bh)};
        }
        public static void TraceAt(ProjectileSnapshot snapshot,Color color) {
            if(snapshot==null || !Ready())return;
            var delta=Quaternion.Inverse(Stage.LayoutRotation)*(snapshot.To-snapshot.From);
            float width=new Vector2(delta.x,delta.y).magnitude;if(width<.2f)return;
            Emit(Vector3.Lerp(snapshot.From,snapshot.To,.15f),Vector3.Lerp(snapshot.From,snapshot.To,.86f),0,
                new Color(color.r,color.g,color.b,.82f),width*.40f,.075f,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,0,true,_trail,false,2.1f,snapshot.Height*.018f/(width*.40f));
        }
        public static bool ProjectileAt(ProjectileSnapshot snapshot,Color color,float duration) {
            if(snapshot==null || !Ready())return false;
            Emit(snapshot.From,snapshot.To,1,color,snapshot.Height*.23f,duration,0,160,true);return true;
        }
        public static bool Projectile(string source,string target,Color color,float duration)
        {
            if(!Socket(source,out var a,out var ah)||!Socket(target,out var b,out var bh))return false;
            Emit(a,b,1,color,Mathf.Min(ah,bh)*.23f,duration,0,160,true);return true;
        }
        public static bool Guard(string target)
        {
            if(!Socket(target,out var p,out var h))return false;
            return GuardAt(p,h,target=="player"?1:-1);
        }
        public static bool GuardAt(Vector3 p,float h,float side)
        {
            if(!Ready())return false;
            p+=(Stage.LayoutRotation*Vector3.right)*h*.28f*side;
            Emit(p,p,3,new Color(.6f,.78f,1,.8f),h*.85f,.3f,side<0?180:0,0,false,_guard);return true;
        }
    }
}
