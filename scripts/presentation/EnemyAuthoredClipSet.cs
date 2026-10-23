using Godot;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace DeckRogue.GodotPort.Presentation;
// Texture ownership and strict manifest loading only. No gameplay or action clock.
public sealed class EnemyAuthoredClipSet : IDisposable
{
 public readonly record struct CanvasContract(int FrameWidth,int FrameHeight,int AnchorX,int AnchorY,int SpriteOffsetX,float SpriteOffsetY,int GroundMaxY);
 public CanvasContract Canvas {get;private set;}
 public Dictionary<string,Texture2D[]> Textures {get;}=new();
 public Dictionary<string,float[]> Durations {get;}=new();
 private readonly Dictionary<Texture2D,Rect2I> _opaque=new();
 public Rect2I OpaqueEnvelope {get;private set;}
 public string ManifestSha256 {get;private set;}="";
 private static int Integer(JToken? t){if(t?.Type!=JTokenType.Integer)throw new InvalidOperationException("Authored contract requires exact integers");return checked((int)t.Value<long>());}
 private static int[] Pair(JObject m,string key){if(m[key] is not JArray a||a.Count!=2)throw new InvalidOperationException("Malformed authored pair: "+key);return new[]{Integer(a[0]),Integer(a[1])};}
 private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
 public static EnemyAuthoredClipSet Load(string enemyId,Texture2D original)
 {
  string folder=enemyId switch{"enemy_ember_furnace"=>"ember-furnace","enemy_chimera_3"=>"chimera-3","enemy_winch_warden"=>"winch-warden","enemy_chimera_1"=>"chimera-1","enemy_chimera_2"=>"chimera-2","enemy_seal_orb"=>"seal-orb","enemy_warden"=>"warden","enemy_set_breaker"=>"set-breaker","enemy_kin_priest"=>"kin-priest","enemy_moss_spawner"=>"moss-spawner","enemy_crab_cannon"=>"crab-cannon","enemy_kin_follower"=>"kin-follower","enemy_crab_crusher"=>"crab-crusher","enemy_moss_healer"=>"moss-healer","enemy_thunder_globe"=>"thunder-globe","enemy_raptor_egg"=>"raptor-egg","enemy_sludge_berserker"=>"sludge-berserker","enemy_elite_mirror_djinn"=>"mirror-djinn","enemy_elite_devourer"=>"devourer","enemy_elite_iron_egg"=>"iron-egg","enemy_elite_sentry"=>"sentry","enemy_mimic_imp"=>"mimic-imp","enemy_mimic_jester"=>"mimic-jester","enemy_frog_knight"=>"frog-knight","enemy_elite_slaver"=>"elite-slaver","enemy_gaping_maw"=>"gaping-maw","enemy_joker"=>"joker","enemy_maw_hunter"=>"maw-hunter","enemy_cog_construct"=>"cog-construct","enemy_bomber"=>"barrel-carrier","enemy_chomper"=>"chomper","enemy_cinder_imp"=>"cinder-imp","enemy_scald_gnat"=>"scald-gnat","enemy_slug"=>"acid-lizard","enemy_haze_stag"=>"haze-stag","enemy_axe_automaton"=>"axe-automaton","enemy_biting_scroll"=>"biting-scroll","enemy_elite_giant_face"=>"giant-face","enemy_elite_doom_chanter"=>"doom-chanter","enemy_whetstone_colossus"=>"whetstone-colossus","enemy_iron_clam"=>"iron-clam","enemy_mud_lump"=>"mud-lump","enemy_sludge_spider"=>"sludge-spider","enemy_strangler_serpent"=>"strangler-serpent","enemy_elite_stab_book"=>"stab-book","enemy_mudling"=>"mudling","enemy_lost_soul"=>"lost-soul","enemy_elite_husk_3"=>"husk-3","enemy_elite_husk_2"=>"husk-2","enemy_elite_husk_1"=>"husk-1","enemy_axe_ogre"=>"axe-ogre","enemy_forgotten_soul"=>"forgotten-soul","enemy_burrow_worm"=>"burrow-worm","enemy_nemesis_wraith"=>"nemesis-wraith","enemy_rock_beetle"=>"rock-beetle","enemy_elite_deathless"=>"deathless-knight","enemy_shell_guard"=>"shell-guard","enemy_moss"=>"moss","enemy_raptor_chick"=>"raptor-chick","enemy_axe_automaton_reboot"=>"axe-reboot","enemy_brood_toad"=>"brood-toad","enemy_broodling"=>"broodling","enemy_brood_raptor"=>"brood-raptor","enemy_bowl_bug"=>"bowl-bug","enemy_mourn_beast"=>"mourn-beast","enemy_devoted_sculptor"=>"sculptor","enemy_bond_wolf"=>"bond-wolf","enemy_elite_sergeant"=>"sergeant","enemy_big_slime"=>"big-slime","enemy_shield_squire"=>"shield-squire","enemy_wide_power"=>"wide-beast","enemy_elite_owl"=>"owl","enemy_elite_gold_raven"=>"gold-raven","enemy_moss_slime"=>"moss-slime","enemy_cultist"=>"cultist","enemy_apprentice_colossus"=>"colossus","enemy_brute"=>"brute","enemy_wolf"=>"wolf","enemy_crossbow_archer"=>"crossbow","enemy_probe"=>"probe","enemy_turtle"=>"turtle","enemy_drummer"=>"drummer","enemy_spore_cap"=>"spore","enemy_vine_walker"=>"vinewalker","enemy_hexer"=>"hexer","enemy_thorn_squirrel"=>"squirrel","enemy_snap_fruit"=>"fruit","enemy_set_wary"=>"wary","enemy_thief"=>"thief",_=>throw new ArgumentException("Unregistered authored enemy")};
  string root="res://authored-frames/"+folder+"/";byte[] bytes=EnemyResourceFile.ReadAllBytes(root+"clipset.json");var m=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
  if(Integer(m["schema"])!=1)throw new InvalidOperationException("Unsupported authored schema");
  var source=Pair(m,"sourceCanvas");var frame=Pair(m,"frameCanvas");var offset=Pair(m,"sourceOffset");var anchor=Pair(m,"anchor");var sprite=enemyId=="enemy_chimera_2"?Array.Empty<int>():Pair(m,"spriteOffset");int ground=Integer(m["groundMaxY"]);
  if(original.GetWidth()!=source[0]||original.GetHeight()!=source[1])throw new InvalidOperationException("Authored source grid mismatch");
  bool strictM34=enemyId is "enemy_brood_raptor" or "enemy_bowl_bug";bool strictM35=enemyId is "enemy_brood_toad" or "enemy_broodling";bool strictM36=enemyId is "enemy_raptor_chick" or "enemy_axe_automaton_reboot";bool strictM37=enemyId is "enemy_shell_guard" or "enemy_moss";bool strictM38=enemyId is "enemy_rock_beetle" or "enemy_elite_deathless";bool strictM39=enemyId is "enemy_burrow_worm" or "enemy_nemesis_wraith";bool strictM40=enemyId is "enemy_axe_ogre" or "enemy_forgotten_soul";bool strictM41=enemyId is "enemy_elite_husk_2" or "enemy_elite_husk_1";bool strictM42=enemyId=="enemy_elite_husk_3";bool strictM43=enemyId is "enemy_mudling" or "enemy_lost_soul";bool strictM44=enemyId is "enemy_strangler_serpent" or "enemy_elite_stab_book";bool strictM45=enemyId is "enemy_mud_lump" or "enemy_sludge_spider";bool strictM46=enemyId is "enemy_whetstone_colossus" or "enemy_iron_clam";bool strictM47=enemyId is "enemy_elite_giant_face" or "enemy_elite_doom_chanter";bool strictM48=enemyId is "enemy_axe_automaton" or "enemy_biting_scroll";bool strictM49=enemyId is "enemy_slug" or "enemy_haze_stag";bool strictM50=enemyId is "enemy_cinder_imp" or "enemy_scald_gnat";bool strictM51=enemyId is "enemy_bomber" or "enemy_chomper";bool strictM52=enemyId is "enemy_maw_hunter" or "enemy_cog_construct";bool strictM53=enemyId is "enemy_gaping_maw" or "enemy_joker";bool strictM55=enemyId is "enemy_mimic_imp" or "enemy_mimic_jester";bool strictM54=enemyId is "enemy_frog_knight" or "enemy_elite_slaver";bool strictM56=enemyId is "enemy_elite_iron_egg" or "enemy_elite_sentry";bool strictM57=enemyId is "enemy_elite_mirror_djinn" or "enemy_elite_devourer";bool strictM58=enemyId is "enemy_raptor_egg" or "enemy_sludge_berserker";bool strictM59=enemyId is "enemy_moss_healer" or "enemy_thunder_globe";bool strictM60=enemyId is "enemy_kin_follower" or "enemy_crab_crusher";bool strictM61=enemyId is "enemy_moss_spawner" or "enemy_crab_cannon";bool strictM62=enemyId is "enemy_set_breaker" or "enemy_kin_priest";bool strictM63=enemyId is "enemy_seal_orb" or "enemy_warden";bool strictM64=enemyId is "enemy_chimera_1" or "enemy_chimera_2";bool strictM65=enemyId is "enemy_chimera_3" or "enemy_winch_warden";bool strictM66=enemyId=="enemy_ember_furnace";HashSet<Color>? nativePalette=null;
  if(strictM34||strictM35||strictM36||strictM37||strictM38||strictM39||strictM40||strictM41||strictM42||strictM43||strictM44||strictM45||strictM46||strictM47||strictM48||strictM49||strictM50||strictM51||strictM52||strictM53||strictM54||strictM57||strictM66||strictM65||strictM64||strictM63||strictM62||strictM61||strictM60||strictM59||strictM58||strictM56||strictM55){nativePalette=new();using var paletteImage=EnemyResourceFile.LoadPng(original.ResourcePath);paletteImage.Convert(Image.Format.Rgba8);for(int y=0;y<paletteImage.GetHeight();y++)for(int x=0;x<paletteImage.GetWidth();x++){var c=paletteImage.GetPixel(x,y);if(c.A>0)nativePalette.Add(c);}}
  CanvasContract canvas;
  if(enemyId=="enemy_brute"){
   var b=BruteAuthoredCanvas.Validate(source[0],source[1],frame[0],frame[1],offset[0],offset[1],anchor[0],anchor[1],sprite[0],sprite[1],ground);
   canvas=new(b.FrameWidth,b.FrameHeight,b.AnchorX,b.AnchorY,b.SpriteOffsetX,b.SpriteOffsetY,b.GroundMaxY);
  }else if(strictM66){
   if(enemyId=="enemy_ember_furnace"){
    const int sw=208,sh=144,fw=224,fh=160,ox=8,oy=8,ax=112,ay=152,sx=0,sy=0,groundY=143;
    const string originalHash="fa2eb10ec7c4c82d77604f0e1b044fd7985f1296b40d553021c40c9f6339cecb",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="15d6bc3b3b927b95a0c49060fb7a2145b1abcd1b6ac167b83fe10bef086f0d0b",approvedArt="4f3e050675aa736c96df27847f23623809f0479b33657a066b8d6aa6d15b430d";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M66 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M66 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M66 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M66 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M66 profile");
  }else if(strictM65){
   if(enemyId=="enemy_chimera_3"){
    const int sw=224,sh=152,fw=288,fh=184,ox=48,oy=24,ax=160,ay=176,sx=-16,sy=8,groundY=163;
    const string originalHash="3fce14bfe294b40a63dde85d2efa4e88109031715f1746b72ab4f416b0e1802e",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="de5b9e3b5fd7bad0b55fc1392e237e48b64dcc9f1be8074a2cbfafb28a748b7c",approvedArt="0b0a76defd5a495d2f72a911d54577b2b785181a043a469888c89e438656b888";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M65 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M65 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M65 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M65 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_winch_warden"){
    const int sw=176,sh=144,fw=208,fh=160,ox=16,oy=8,ax=104,ay=152,sx=0,sy=0,groundY=143;
    const string originalHash="fe6fe46360f771b507f5cb93e6de23d83b18f8f31ae75ba2fab042cbfdaabca0",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="e08fce629ec557cdf725fd90a409db1d02e65d05b6e63134916d02aca32432d0",approvedArt="77687de968fce3b5fb933abefc1d1a52871c8129cea86528bfbf6b13f0108106";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M65 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M65 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M65 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M65 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M65 profile");
  }else if(strictM64){
   // Only pinned phase2 reads fractional Y; every older profile retains exact integer Pair validation.
   if(enemyId=="enemy_chimera_1"){
    const int sw=208,sh=144,fw=212,fh=144,ox=0,oy=0,ax=104,ay=144,sx=2,groundY=133;const float sy=0f;
    const string originalHash="0bd2e9e0b903e04825a405dfe56b357efdc5b8ccc67d08f30f613b53f686227b",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="cf57641109ba4e26548141b685262f46c2ca68edc5856b9d253b33cc9094a0c5",approvedArt="14d72334254a75fb5f60ca44f18942caf49d8fc00886c8c2430a7a86c2c0ba94";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M64 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||m["spriteOffset"] is not JArray exactSprite||exactSprite.Count!=2||exactSprite[0].Value<double>()!=sx||exactSprite[1].Value<double>()!=sy||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M64 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M64 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M64 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_chimera_2"){
    const int sw=224,sh=144,fw=244,fh=147,ox=20,oy=1,ax=132,ay=145,sx=-10,groundY=129;const float sy=-0.5f;
    const string originalHash="8989f54df0f8b4330465b033ecb06381fc9ddb30e89028cbae0325c51ead2b53",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="a6fa38aa8ee3caf8c1925f463fddf4ada8d43a8f2e0dbec5200e5cda37889d91",approvedArt="f0ddbd356004888a97f14d0b6c94d8aebad30c13ab168cfd53854b9f330a594c";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M64 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||m["spriteOffset"] is not JArray exactSprite||exactSprite.Count!=2||exactSprite[0].Value<double>()!=sx||exactSprite[1].Value<double>()!=sy||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M64 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M64 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M64 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M64 profile");
  }else if(strictM63){
   if(enemyId=="enemy_seal_orb"){
    const int sw=72,sh=64,fw=80,fh=64,ox=8,oy=0,ax=44,ay=64,sx=-4,sy=0,groundY=55;
    const string originalHash="a59d211d7dc86f090e212530a44d27ebe777d343c2254dc9cde68f69d069639a",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="596bd54b8084eb61e376991965ed6ebed1bb4132a0d183423b804ed4d0f26d0c",approvedArt="f479be9760d9fefc28514f211f17fa09bacbdcf938b44d301cfcb086f343eaab";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M63 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M63 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M63 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M63 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_warden"){
    const int sw=192,sh=152,fw=224,fh=200,ox=32,oy=32,ax=128,ay=184,sx=-16,sy=8,groundY=175;
    const string originalHash="f902558c5c8918f99d2232c040ff968a9e0d1cb5e02261fc235bd0f4cf398842",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="3df224e3d7867aad7aa6c1a2b3ecde65f1a623f74026640b7ccc5c99eb405db4",approvedArt="614cb4264b6695d1b88804465b9ecf6873f58a44dbedad3c96a7c06e264b4ad2";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M63 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M63 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M63 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M63 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M63 profile");
  }else if(strictM62){
   if(enemyId=="enemy_set_breaker"){
    const int sw=112,sh=88,fw=128,fh=104,ox=16,oy=16,ax=72,ay=104,sx=-8,sy=8,groundY=95;
    const string originalHash="7cd0c3102a1104c5cf06537170b9757b1570e15c05b566a5ba2f15975263c67c",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="cd0da8abaad96a448513650eb3109b50f89a0a209437251b6dc3a52ab4fd447d",approvedArt="7276200776d63972c37872a4de45135cd58e0d35798d12f114f09fed508e29e9";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M62 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M62 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M62 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M62 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_kin_priest"){
    const int sw=176,sh=144,fw=176,fh=144,ox=0,oy=0,ax=88,ay=144,sx=0,sy=0,groundY=135;
    const string originalHash="d6a210e79402644ec34763cc384b251f04cdc2abf1be4b6390dfb442b87f8568",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="ed138cc60651e5617a4ef43f0439b69ab7a565d58d5072f7a350b19784fb0e53",approvedArt="861ea581db037f147573cd8e1750a3df1645c991475f827d58ba98c01a0f53b7";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M62 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M62 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M62 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M62 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M62 profile");
  }else if(strictM61){
   if(enemyId=="enemy_moss_spawner"){
    const int sw=144,sh=112,fw=176,fh=128,ox=32,oy=16,ax=104,ay=128,sx=-16,sy=8,groundY=119;
    const string originalHash="91433444b0b8440a97b9cbbfc31004047cc8c511ebb3b5f9c77260d2c11dca83",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="0bb1665d6c37441c95271d124a7d2f3d9e3ca5bcd84376f6db1f87cbaac283c4",approvedArt="e0839610a2cacc8e09bdfb10b8a8dfa22485f0e0998adbaa069b6a75e84e375d";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M61 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M61 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M61 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M61 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_crab_cannon"){
    const int sw=256,sh=104,fw=288,fh=120,ox=32,oy=0,ax=160,ay=104,sx=-16,sy=-8,groundY=95;
    const string originalHash="0664171004225725051eefbe6e303517612e01d01c5a7beffb68aa5de519d3a2",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="4e44d9ad788e74af2ca08c0cd8475c9f96f612232c3824f8223388ae3d0f4138",approvedArt="c70bb3891a4da744431e2a280679598b8bd9b28311e20cc6eafc58bdf984caf4";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M61 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M61 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M61 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M61 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M61 profile");
  }else if(strictM60){
   if(enemyId=="enemy_kin_follower"){
    const int sw=160,sh=112,fw=160,fh=116,ox=0,oy=4,ax=80,ay=116,sx=0,sy=2,groundY=107;
    const string originalHash="d97d3e3f4e7d932255e5a28d7edb78ac5ff3f6abce44bce1e15354afaa49277d",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="3437427432af2ffdca2dfd5e092d23ea330fbfa3f29a7d3c3bf0e338111c1b6a",approvedArt="a79f006fc88a6b81b3bdff41e95a615333dbb44643928befb7d4d5a6014f2436";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M60 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M60 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M60 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M60 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_crab_crusher"){
    const int sw=224,sh=120,fw=256,fh=120,ox=32,oy=0,ax=144,ay=120,sx=-16,sy=0,groundY=111;
    const string originalHash="d8cb9dd29facdbe3eb92deb5e5c8767cb1afd7ccf948db340fc3cf038f4122b2",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="34d96407f7113ab8158fac67ab86e948e1007190e8b4200bac963fd07f810b97",approvedArt="c85c64d243868f92d5d578075d0978227149fa06cddefeee473044b92f5bf5a7";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M60 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M60 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M60 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M60 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M60 profile");
  }else if(strictM59){
   if(enemyId=="enemy_moss_healer"){
    const int sw=88,sh=88,fw=108,fh=88,ox=12,oy=0,ax=56,ay=88,sx=-2,sy=0,groundY=79;
    const string originalHash="019f847aa4f978f35b9428c02d123aa8ccc19854c48f96ddd5b71a2f87013a64",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="82b67447e3c5ee6006746cc7ce75ad0b239d07ae30d298f4133ef5dbc2232b68",approvedArt="aa5f66de7a6ae765b5a8172a4fabc800ca54200a6966b8bcb838c2e16370d1ac";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M59 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M59 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M59 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M59 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_thunder_globe"){
    const int sw=168,sh=112,fw=168,fh=112,ox=0,oy=0,ax=84,ay=112,sx=0,sy=0,groundY=103;
    const string originalHash="e7fdbddc945dd1a5bedd32efc0f504e032086a79486c3def97a16244d040bdbd",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="55c7dadb73f9983a1db8e139a1374ece61c92ecc5c039d516fa261d200edcc57",approvedArt="003cc4e8649dbb7fda2a42f1b97735dd383a8587a1704cb2696ade7ac344b1fe";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M59 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M59 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M59 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M59 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M59 profile");
  }else if(strictM58){
   if(enemyId=="enemy_raptor_egg"){
    const int sw=72,sh=64,fw=72,fh=64,ox=0,oy=0,ax=36,ay=64,sx=0,sy=0,groundY=57;
    const string originalHash="66d25c569f50f6805ab8d25d9315cd3ed718668d23161b6b3b9e7752c003e58c",restOriginal="rest.png",restFrame="rest.png",approvedManifest="34576e804b315016df2fe3c2a77f11b6718995d9cefc06ad77d0a874dd4e1279",approvedArt="67a131c6abb9268af9b8eff6eb724fdc05aa47755ce01a0b9f4dbaa7d49d9264";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M58 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M58 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M58 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M58 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_sludge_berserker"){
    const int sw=160,sh=112,fw=200,fh=112,ox=24,oy=0,ax=104,ay=112,sx=-4,sy=0,groundY=103;
    const string originalHash="123ed0f646a420bdb96747b935c22264f173ea6f0cb50d23f1e6f7e0adca8e33",restOriginal="rest-original.png",restFrame="rest.png",approvedManifest="69f51ce602b1c633989f2f9c5023d1ea9eb924655bd57f201ac550cd74ae396c",approvedArt="54c8f5d9ff7c96540fb7036b8cd2c31a70dab69ef4b6ce74e68e55d5d341dcc4";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M58 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M58 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M58 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M58 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M58 profile");
  }else if(strictM57){
   if(enemyId=="enemy_elite_mirror_djinn"){
    const int sw=176,sh=112,fw=176,fh=112,ox=0,oy=0,ax=88,ay=112,sx=0,sy=0,groundY=103;
    const string originalHash="64a698cc41f1933c29d0f7e7f73c0d2545c59f019b61a459292d94071bdbf39b",restOriginal="rest.png",restFrame="rest.png",approvedManifest="6fa159797ee35972694dcff901bdaf50a3f95bb32562d71eb488cc1ab0eb1214",approvedArt="75c34d9b467f92ce132ec136c835b1b23c75b30e7a33a55d23e3712412833848";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M57 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M57 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M57 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M57 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_elite_devourer"){
    const int sw=208,sh=112,fw=208,fh=128,ox=0,oy=16,ax=104,ay=128,sx=0,sy=8,groundY=119;
    const string originalHash="333e5b9daa417e8442939e173ff88f6bc89e3940da24d9d7fe2f55e34296ea34",restOriginal="source-rest.png",restFrame="rest.png",approvedManifest="3ba9460e94c53033e9ec2c7f6cc37ecaabbf5e873f87fbe91c475362fab518c9",approvedArt="d5ad9a17b5178b6600aab94c566a772fffa666837c08ffa96a73260aa2e61519";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M57 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M57 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M57 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M57 rest must preserve source RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M57 profile");
  }else if(strictM55){
   if(enemyId=="enemy_mimic_imp"){
    const int sw=88,sh=72,fw=88,fh=72,ox=0,oy=0,ax=44,ay=72,sx=0,sy=0,groundY=65;
    const string originalHash="51423b102e1170492c840cc895a90781bffe35e7024053b2963b221985895049",approvedManifest="e675289cdfc12bf3a7648479c7ae35f24717e9a08cc6dc58dc334c1d1c6acccb",approvedArt="52ca7e9e3f2128da688f7dc117af479361b7f3d7593eb9896f1bec17cfd3a23b";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M55 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash||m["restOriginalFile"]?.Value<string>()!="rest.png"||m["restFrameFile"]?.Value<string>()!="rest.png")throw new InvalidOperationException("Unapproved M55 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+"rest.png");sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M55 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M55 rest must retain exact source RGBA");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else if(enemyId=="enemy_mimic_jester"){
    const int sw=104,sh=88,fw=104,fh=88,ox=0,oy=0,ax=52,ay=88,sx=0,sy=0,groundY=79;
    const string originalHash="7edb4d0576286e8df14386d7021e840e6415b9b5f8d9c6c7b5206242e188430d",approvedManifest="c2fa9d8415be3d6ba475ce33e87fb0dc6381ae5c9f8e6cc843fb3fe32983d7c4",approvedArt="906e531ad8add2a4dd1202e59dbd95b3360d260af80cd01ba8e5f09a693968fb";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M55 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||m["approvedArtManifestSha256"]?.Value<string>()!=approvedArt||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash||m["restOriginalFile"]?.Value<string>()!="rest.png"||m["restFrameFile"]?.Value<string>()!="rest.png")throw new InvalidOperationException("Unapproved M55 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+"rest.png");sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M55 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M55 rest must retain exact source RGBA");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M55 profile");
  }else if(strictM56){
   if(enemyId=="enemy_elite_iron_egg"){
    const int sw=152,sh=112,fw=160,fh=112,ox=8,oy=0,ax=84,ay=112,sx=-4,sy=0,groundY=103;
    const string originalHash="5a96fe0c356b06605d3707f619f796643f48b7aefd1e8edd0d5fb350ecb24ca1",restOriginal="source-rest.png",restFrame="rest.png",approvedManifest="b115e6849c5470042966ec8fa3b3a9913ddd8fab6478d4d646f923fac3129968";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M56 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M56 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M56 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M56 rest must retain source RGBA plus transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_elite_sentry"){
    const int sw=144,sh=112,fw=144,fh=112,ox=0,oy=0,ax=72,ay=112,sx=0,sy=0,groundY=103;
    const string originalHash="26f478cd3bb4acc312027322d99725573898f8c7c68f3aacc690e5d063a29ac6",restOriginal="rest.png",restFrame="rest.png",approvedManifest="92322e5b573dcaa16ab5e961de9d637428e476110470d26c5e120dd4ec4a4963";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M56 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M56 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M56 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M56 rest must retain source RGBA plus transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M56 profile");
  }else if(strictM54){
   if(enemyId=="enemy_frog_knight"){
    const int sw=160,sh=88,fw=160,fh=88,ox=0,oy=0,ax=80,ay=88,sx=0,sy=0,groundY=79;
    const string originalHash="0daf426a24e5ef425a42ec9b77dc3158a3311a21018a249d11f72dd9c4b962b2",restOriginal="rest.png",restFrame="rest.png",approvedManifest="bae8b501356fab7d90342a8c52600857d6700fa88bb16c7940fd2de80f9e6b34";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M54 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M54 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M54 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M54 rest must retain source RGBA plus transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_elite_slaver"){
    const int sw=192,sh=112,fw=208,fh=112,ox=16,oy=0,ax=112,ay=112,sx=-8,sy=0,groundY=103;
    const string originalHash="d436587bdb1e596670d1b54b162340b66549e0fb2fbbb17c44c4575a0f9d4105",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="de703054300f850519d52847bc39fe3978b6779b83abffaa41f921da3fcac269";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M54 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M54 source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M54 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M54 rest must retain source RGBA plus transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M54 profile");
  }else if(strictM53){
   if(enemyId=="enemy_gaping_maw"){
    const int sw=112,sh=88,fw=112,fh=88,ox=0,oy=0,ax=56,ay=88,sx=0,sy=0,groundY=79;
    const string originalHash="14ac9fe54fa8f98fa705dd38b2798fc6af9a87ba15902b377df6323005d5ecda",restOriginal="rest.png",restFrame="rest.png",approvedManifest="1dd6fedcbb2f6a936b964ad57b269b3341542b4a82b3e9b98607053ce2647e7e";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M53 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M53 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M53 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M53 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_joker"){
    const int sw=96,sh=88,fw=96,fh=88,ox=0,oy=0,ax=48,ay=88,sx=0,sy=0,groundY=79;
    const string originalHash="8c54cc23fdd5d379517b431fe2f4a17135d046a02c8f36b63c66f1b309934f33",restOriginal="rest.png",restFrame="rest.png",approvedManifest="d8722e7acb7ac05ff7c2e19e9abcbdde1a694b4236e0dd9b9b53031083014772";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M53 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M53 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M53 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M53 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M53 profile");
  }else if(strictM52){
   if(enemyId=="enemy_maw_hunter"){
    const int sw=136,sh=88,fw=136,fh=88,ox=0,oy=0,ax=68,ay=88,sx=0,sy=0,groundY=79;
    const string originalHash="feba38a02962d100dbfb2649a01c7bac28bfd75de185bac714c6cd926b5d12e6",restOriginal="rest.png",restFrame="rest.png",approvedManifest="1456d6d4c2855add7514fc748e50abf408941bccb618f61de7bf75b60c97998a";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M52 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M52 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M52 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M52 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_cog_construct"){
    const int sw=104,sh=80,fw=104,fh=80,ox=0,oy=0,ax=52,ay=80,sx=0,sy=0,groundY=73;
    const string originalHash="cded57396501b37447b692d1fd81779ef4c05986b75e5db53d0449ef6474b32d",restOriginal="rest.png",restFrame="rest.png",approvedManifest="1a4ec5c6189dfa3f7a1fe9a207fc27baeb6b7cf92a179c408a27dc37bbd4409c";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M52 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M52 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M52 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M52 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M52 profile");
  }else if(strictM51){
   if(enemyId=="enemy_bomber"){
    const int sw=104,sh=88,fw=108,fh=88,ox=4,oy=0,ax=56,ay=88,sx=-2,sy=0,groundY=79;
    const string originalHash="3564783960bb54e046f927cf7248b2fccedc7681fe0fbd1f90f5b05fb624ee9f",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="81531d2f949f6dc9f527ebb66db7fea7ebd36969211698675ae5b87fd9e1cbc9";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M51 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M51 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M51 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M51 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_chomper"){
    const int sw=144,sh=88,fw=144,fh=96,ox=0,oy=8,ax=72,ay=96,sx=0,sy=4,groundY=87;
    const string originalHash="dfba9e1beac0a033de53d962c5efbd4aa87cbcd970a2ea2406eef2e33887436b",restOriginal="rest.png",restFrame="rest-registered.png",approvedManifest="0c79d6e4d4507836a3e731b2c4fa51b3a14fe6c09e6bd5f4e6a2373f4a79dc4a";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M51 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M51 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M51 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M51 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M51 profile");
  }else if(strictM50){
   if(enemyId=="enemy_cinder_imp"){
    const int sw=80,sh=72,fw=84,fh=72,ox=4,oy=0,ax=44,ay=72,sx=-2,sy=0,groundY=65;
    const string originalHash="e3083ec6e8099bccd91e79a8647f4c15cd2c6323fd85110cfcb8fa80e23ed2c9",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="91994b71902c8ce2f6a390f7aa39e466cc42eb127eba4f74cc1951a2b43ada55";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M50 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M50 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M50 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M50 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_scald_gnat"){
    const int sw=120,sh=64,fw=120,fh=64,ox=0,oy=0,ax=60,ay=64,sx=0,sy=0,groundY=57;
    const string originalHash="798866456b72a4b5e34c45e7d8b2836f7f1aa5c1c0fe12b3b06bbbb7bd254e64",restOriginal="rest.png",restFrame="rest.png",approvedManifest="1f7bfc542c0769628022961203cfa6feac7e634ad242bb590af497a8f8872f42";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M50 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M50 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M50 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M50 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M50 profile");
  }else if(strictM49){
   if(enemyId=="enemy_slug"){
    const int sw=160,sh=80,fw=186,fh=80,ox=26,oy=0,ax=106,ay=80,sx=-13,sy=0,groundY=73;
    const string originalHash="e1bfcb9620f6cd954d824e7344f0b2cd6b28f56c3520348a8ca9d94a837a48bb",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="d2632c41fc6954852b7e7b1b52a76065de6b1bb932d6a839ff67e1b2bcb19b22";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M49 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M49 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M49 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M49 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_haze_stag"){
    const int sw=192,sh=144,fw=208,fh=144,ox=8,oy=0,ax=104,ay=144,sx=0,sy=0,groundY=135;
    const string originalHash="828908dac7f5f1a574b6d1cd6f58e1b5e9a1433ef5a781013f9520d41b4f47c5",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="2219051ffd9786be3fdafb3ddabdcf98c953dd38112de09bf8cb04388c497ec4";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M49 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M49 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M49 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M49 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M49 profile");
  }else if(strictM48){
   if(enemyId=="enemy_axe_automaton"){
    const int sw=136,sh=88,fw=136,fh=104,ox=0,oy=16,ax=68,ay=104,sx=0,sy=8,groundY=95;
    const string originalHash="c54ef58c38e9e0a68f52d9807d234f7c02bf460c7df8e1f8e247d7cbd5b493d0",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="1d18247745364a51b19919a8023d691a97815f125597b24cdb17a3d8482bcdfb";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M48 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M48 original source/floating anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M48 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M48 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_biting_scroll"){
    const int sw=104,sh=64,fw=104,fh=64,ox=0,oy=0,ax=52,ay=64,sx=0,sy=0,groundY=57;
    const string originalHash="d653a1ae11dec1696fe5360c7de869cd7f34b19bb4111f9fe7101511cffd99c8",restOriginal="rest.png",restFrame="rest.png",approvedManifest="258705db005f86edf2c739091fea0b42e96990898aadbe08e3bc1dc0c24b7366";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M48 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M48 original source/floating anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M48 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M48 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M48 profile");
  }else if(strictM47){
   if(enemyId=="enemy_elite_giant_face"){
    const int sw=144,sh=112,fw=144,fh=112,ox=0,oy=0,ax=72,ay=112,sx=0,sy=0,groundY=103;
    const string originalHash="324fb32f363795ee58f10c71730348d94f6d9c61c242d6f2c88c2848387ef94e",restOriginal="rest.png",restFrame="rest.png",approvedManifest="2672754eb1a23d8402b4d94ba5d4f65e7bdcfd0b3bc08968b8c7cb3c7587f6e7";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M47 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M47 original source/floating anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M47 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M47 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_elite_doom_chanter"){
    const int sw=144,sh=112,fw=144,fh=112,ox=0,oy=0,ax=72,ay=112,sx=0,sy=0,groundY=103;
    const string originalHash="d1e86e2f39b4b1df4016c50a6e5bd97cf1f402b423e9b0c863351e6fd881695e",restOriginal="rest.png",restFrame="rest.png",approvedManifest="e6e86e94e6c05bb32afc7b09eb21cf2b14e9ea6e7e9d548e35a830049c7933f3";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M47 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M47 original source/floating anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M47 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M47 rest must preserve original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M47 profile");
  }else if(strictM46){
   if(enemyId=="enemy_whetstone_colossus"){
    const int sw=144,sh=120,fw=144,fh=130,ox=0,oy=10,ax=72,ay=130,sx=0,sy=5,groundY=121;
    const string originalHash="4ea18678bb0ca59fb48fd49087c6efc416b2d59c8c9f30f2b16c353df048e9a1",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="cc7fc54aba5f632d2378c1c9295f3d8d887d61da324e1ebd32d898843000e13d";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M46 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M46 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M46 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M46 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_iron_clam"){
    const int sw=112,sh=80,fw=112,fh=80,ox=0,oy=0,ax=56,ay=80,sx=0,sy=0,groundY=73;
    const string originalHash="fab9f40bfab25c48c7b788f006f0f0a1aad9a9436cc1570ee11f25f3650e91f2",restOriginal="rest.png",restFrame="rest.png",approvedManifest="c968e2af9772d0bae61d029499b68d864d22590d47ea81458e69e989889d42eb";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M46 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M46 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M46 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M46 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M46 profile");
  }else if(strictM45){
   if(enemyId=="enemy_mud_lump"){
    const int sw=96,sh=80,fw=96,fh=80,ox=0,oy=0,ax=48,ay=80,sx=0,sy=0,groundY=73;
    const string originalHash="21741ab1a4a1d7e981fc68a4d042c64e21911cb58d2b0b24133579a9d0e72efa",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="d38221d1550df5d6b372bfdd8916f7472f3d6d89b72caf3cf97d63a9ebccf4db";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M45 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M45 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M45 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M45 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_sludge_spider"){
    const int sw=128,sh=80,fw=128,fh=80,ox=0,oy=0,ax=64,ay=80,sx=0,sy=0,groundY=72;
    const string originalHash="b871902ce0fb6a23edc68dd117c3b7105b479a9e3401aadff4c3f35eb7dae062",restOriginal="rest.png",restFrame="rest.png",approvedManifest="01c3cc8d597b459ad5bfa44d8a9a8431dc65c6234ed14d9e84aeb1a05c78eae7";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M45 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M45 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M45 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M45 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M45 profile");
  }else if(strictM44){
   if(enemyId=="enemy_strangler_serpent"){
    const int sw=112,sh=88,fw=124,fh=88,ox=11,oy=0,ax=67,ay=88,sx=-5,sy=0,groundY=79;
    const string originalHash="d9c870ad80296c967bb94ccf7caece670f65c65bc75e3fd94232babd5a1d26f2",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="06e7c8f6c5f27d427c2e303a60a3c46af750768d69f6f26c8599cf650230be9b";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M44 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M44 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M44 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M44 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_elite_stab_book"){
    const int sw=192,sh=112,fw=192,fh=112,ox=0,oy=0,ax=96,ay=112,sx=0,sy=0,groundY=103;
    const string originalHash="5c44f7632f3a09e7b6a8d0b9930d0ee5a5759202fc28d2241e6c3c47fc4f2769",restOriginal="rest.png",restFrame="rest.png",approvedManifest="52468404c58babf2d7be4d3a2c0f4a021cda053ec55e2763155c3ecd9c76c9f2";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M44 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M44 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M44 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M44 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M44 profile");
  }else if(strictM43){
   if(enemyId=="enemy_mudling"){
    const int sw=64,sh=64,fw=70,fh=64,ox=4,oy=0,ax=36,ay=64,sx=-1,sy=0,groundY=59;
    const string originalHash="19211d02c50e855cf91c40e6c23a0ffc72ca4a48472385c2f36c54cce7be0b24",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="a8c9676e147e72f29349a59d034484983b63c0cee5c4ab1d355a4c82a24ceb96";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M43 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M43 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M43 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M43 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else    if(enemyId=="enemy_lost_soul"){
    const int sw=144,sh=88,fw=144,fh=88,ox=0,oy=0,ax=72,ay=88,sx=0,sy=0,groundY=79;
    const string originalHash="188d0b3e628278790beb08e6f015aa57274e18497082faf4e3a85faecb0dbef8",restOriginal="rest.png",restFrame="rest.png",approvedManifest="eae01725e0a581f6d771b1c950c2b251c0b55da7df3b83e2bed4b830077f387d";
    if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M43 exact runtime manifest");
    if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M43 original source/anchor/rest contract");
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
    if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M43 rest grid mismatch");
    for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+sw&&y>=oy&&y<oy+sh?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M43 rest must preserve all original RGBA with transparent padding only");}
    canvas=new(fw,fh,ax,ay,sx,sy,groundY);
   }
   else throw new InvalidOperationException("Unregistered M43 profile");
  }else if(strictM42){
   const int fw=184,fh=112,ox=8,oy=0,ax=96,ay=112,sx=-4,sy=0,groundY=103;
   const string originalHash="b2fc3483e70d13ab536ee7ba5a664133fb5322bd67d4740e67ad97ec75d3b939",restOriginal="rest.png",restFrame="rest_canvas.png",approvedManifest="c71faf59990cb035efe73c5551d071f74a9c284fc83cddadb6e22ad3b4bf340b";
   if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M42 exact runtime manifest");
   if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{176,112})||!frame.SequenceEqual(new[]{fw,fh})||!offset.SequenceEqual(new[]{ox,oy})||!sprite.SequenceEqual(new[]{sx,sy})||!anchor.SequenceEqual(new[]{ax,ay})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal||m["restFrameFile"]?.Value<string>()!=restFrame)throw new InvalidOperationException("Unapproved M42 source/canvas or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var restImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);restImage.Convert(Image.Format.Rgba8);
   if(restImage.GetWidth()!=fw||restImage.GetHeight()!=fh)throw new InvalidOperationException("M42 rest grid mismatch");
   for(int y=0;y<fh;y++)for(int x=0;x<fw;x++){var expected=x>=ox&&x<ox+176&&y>=oy&&y<oy+112?sourceImage.GetPixel(x-ox,y-oy):new Color(0,0,0,0);if(restImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M42 rest must preserve all original RGBA with transparent padding only");}
   canvas=new(fw,fh,ax,ay,sx,sy,groundY);
  }else if(strictM41){
   bool second=enemyId=="enemy_elite_husk_2";int fw=second?176:192,pad=second?0:8;string restOriginal=second?"rest.png":"rest_original.png",restFrame="rest.png";
   string originalHash=second?"bf13acc4608e7d9c29c3111ee72ee6cd1dbaa7b7d6f222484231aa1725d01604":"a49470c338eb653568dd5ffbd34ab5a0f18b8e4841467b32963071b21e3459d2";
   // Exact manifest hash pins all approved frame hashes, filenames and timings. Pending art fails closed.
   string approvedManifest=second?"f751b608cb0ad70e22012a9bddadda229e6fb5c7559ba92b382f942f1b572b1a":"176cc996ac6b2a78360cd3e8b431928b7f36ea509ebe7ed74444c02e8f1b0eae";
   if(Hash(bytes)!=approvedManifest)throw new InvalidOperationException("Unapproved M41 exact runtime manifest");
   if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{176,112})||!frame.SequenceEqual(new[]{fw,112})||!offset.SequenceEqual(new[]{pad,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{88+pad,112})||ground!=103||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+restOriginal))!=originalHash||m["restOriginalFile"]?.Value<string>()!=restOriginal)throw new InvalidOperationException("Unapproved M41 source/canvas or changed exact original rest");
   if(!second){
    using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+restFrame);sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
    if(paddedImage.GetWidth()!=192||paddedImage.GetHeight()!=112)throw new InvalidOperationException("M41 final husk padded rest grid mismatch");
    for(int y=0;y<112;y++)for(int x=0;x<192;x++){var expected=x>=8&&x<184?sourceImage.GetPixel(x-8,y):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M41 final husk rest must preserve all source RGBA at8,0 without scale");}
   }
   canvas=new(fw,112,88+pad,112,0,0,103);
  }else if(strictM40){
   bool axe=enemyId=="enemy_axe_ogre";int sw=axe?144:128,sh=axe?120:88,groundY=axe?111:79;
   string originalHash=axe?"257e05a08ce17c32b3ee5acc9a7ddd701744ad6998cf332cbbc438f8bac7f0c4":"07f495e9664efd41c7e358cc38b8983f60223097bbaaffa06ae0af4c1b184c0e";
   if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{sw/2,sh})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved M40 source/canvas registration or changed exact original rest");
   canvas=new(sw,sh,sw/2,sh,0,0,groundY);
  }else if(strictM39){
   bool worm=enemyId=="enemy_burrow_worm";int sw=worm?184:160,sh=120,groundY=worm?111:110;
   string originalHash=worm?"9d2b983ab3f1cae42c9dda07d4da19ca34dd0d72a322dc4c9b26a8683e39e799":"bd21fe19ca579206ad5f5e689cbaaa2f6b729660f2ac99c17f63acdba1c52418";
   if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{sw/2,sh})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved M39 source/canvas registration or changed exact original rest");
   canvas=new(sw,sh,sw/2,sh,0,0,groundY);
  }else if(strictM38){
   bool beetle=enemyId=="enemy_rock_beetle";int sw=beetle?112:176,sh=beetle?72:112,groundY=beetle?65:103;
   string originalHash=beetle?"d99444d2ce76d8828663fe156cacd989000373faaa6ef2a58fabf4f1a476b386":"14c36682456f8a3f364bd3c75f66154d7b5e7b0939e3cdf8b0e1a3ce8e910171";
   if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{sw/2,sh})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved M38 source/canvas registration or changed exact original rest");
   canvas=new(sw,sh,sw/2,sh,0,0,groundY);
  }else if(strictM37){
   bool shell=enemyId=="enemy_shell_guard";int sw=shell?128:152,sh=shell?104:112,fw=shell?128:160,pad=shell?0:8,sx=shell?0:-4,groundY=shell?95:103;
   string originalHash=shell?"cb40d9f1a73c2fb04deca93f59d326b3f27018842aa784e1902cd3ac8641dbbc":"599172b09ad17bdc3358339cb1e0de98bf286a712d33600186b513d99f8dc1b7";
   if(m["enemyId"]?.Value<string>()!=enemyId||!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,sh})||!offset.SequenceEqual(new[]{pad,0})||!sprite.SequenceEqual(new[]{sx,0})||!anchor.SequenceEqual(new[]{sw/2+pad,sh})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved M37 source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+(shell?"rest.png":"rest_canvas.png"));sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=fw||paddedImage.GetHeight()!=sh)throw new InvalidOperationException("M37 registered rest grid mismatch");
   for(int y=0;y<sh;y++)for(int x=0;x<fw;x++){var expected=x>=pad&&x<pad+sw?sourceImage.GetPixel(x-pad,y):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("M37 rest is not exact original RGBA registration");}
   canvas=new(fw,sh,sw/2+pad,sh,sx,0,groundY);
  }else if(enemyId=="enemy_bond_wolf"){
   const string originalHash="9515dd3e5ca9ee7ec29a02ce2e69643d85b4973e6f9e235be612fd2e45d2f8d9";
   if(!source.SequenceEqual(new[]{144,88})||!frame.SequenceEqual(new[]{144,88})||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{72,88})||ground!=79||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_bond_wolf source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=144||paddedImage.GetHeight()!=88)throw new InvalidOperationException("enemy_bond_wolf padded rest grid mismatch");
   for(int y=0;y<88;y++)for(int x=0;x<144;x++){var expected=x>=0&&x<144&&y>=0&&y<88?sourceImage.GetPixel(x-0,y-0):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_bond_wolf rest is not exact original RGBA padding");}
   canvas=new(144,88,72,88,0,0,79);
  }else if(enemyId=="enemy_elite_sergeant"){
   const string originalHash="7d95058689a67ec731ae6d3816f2fc59d914b797ee6bf1adebc07cf08eb29be9";
   if(!source.SequenceEqual(new[]{176,112})||!frame.SequenceEqual(new[]{200,112})||!offset.SequenceEqual(new[]{24,0})||!sprite.SequenceEqual(new[]{-12,0})||!anchor.SequenceEqual(new[]{112,112})||ground!=103||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_elite_sergeant source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=200||paddedImage.GetHeight()!=112)throw new InvalidOperationException("enemy_elite_sergeant padded rest grid mismatch");
   for(int y=0;y<112;y++)for(int x=0;x<200;x++){var expected=x>=24&&x<200&&y>=0&&y<112?sourceImage.GetPixel(x-24,y-0):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_elite_sergeant rest is not exact original RGBA padding");}
   canvas=new(200,112,112,112,-12,0,103);
  }else if(enemyId=="enemy_mourn_beast"){
   const string originalHash="0bd5941545101fdb0cc3de9e1f22baf131193c930de1f72f4536a6dbc1e0ab90";
   if(!source.SequenceEqual(new[]{128,88})||!frame.SequenceEqual(new[]{128,88})||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{64,88})||ground!=79||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_mourn_beast source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=128||paddedImage.GetHeight()!=88)throw new InvalidOperationException("enemy_mourn_beast padded rest grid mismatch");
   for(int y=0;y<88;y++)for(int x=0;x<128;x++){var expected=x>=0&&x<128&&y>=0&&y<88?sourceImage.GetPixel(x-0,y-0):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_mourn_beast rest is not exact original RGBA padding");}
   canvas=new(128,88,64,88,0,0,79);
  }else if(enemyId=="enemy_devoted_sculptor"){
   const string originalHash="f78f7e1b6ba1d4dd57192c760d38e9982fee2cd5163fce5cbbcbef83e31d5826";
   if(!source.SequenceEqual(new[]{136,88})||!frame.SequenceEqual(new[]{136,88})||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{68,88})||ground!=79||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_devoted_sculptor source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=136||paddedImage.GetHeight()!=88)throw new InvalidOperationException("enemy_devoted_sculptor padded rest grid mismatch");
   for(int y=0;y<88;y++)for(int x=0;x<136;x++){var expected=x>=0&&x<136&&y>=0&&y<88?sourceImage.GetPixel(x-0,y-0):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_devoted_sculptor rest is not exact original RGBA padding");}
   canvas=new(136,88,68,88,0,0,79);
  }else if(enemyId=="enemy_brood_raptor"){
   const string originalHash="5786f643d89c4eaedcd2ce0e1a6950c735ee55420f31a7a7800a830db95a96b7";
   if(!source.SequenceEqual(new[]{176,112})||!frame.SequenceEqual(new[]{192,128})||!offset.SequenceEqual(new[]{16,16})||!sprite.SequenceEqual(new[]{-8,8})||!anchor.SequenceEqual(new[]{104,128})||ground!=119||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_brood_raptor source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=192||paddedImage.GetHeight()!=128)throw new InvalidOperationException("enemy_brood_raptor padded rest grid mismatch");
   for(int y=0;y<128;y++)for(int x=0;x<192;x++){var expected=x>=16&&x<192&&y>=16&&y<128?sourceImage.GetPixel(x-16,y-16):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_brood_raptor rest is not exact original RGBA padding");}
   canvas=new(192,128,104,128,-8,8,119);
  }else if(enemyId=="enemy_bowl_bug"){
   const string originalHash="517a0bca398fe87b4af3208d1a11f094ebd3671590a5d1fadddf4d44b5859243";
   if(!source.SequenceEqual(new[]{80,72})||!frame.SequenceEqual(new[]{96,72})||!offset.SequenceEqual(new[]{8,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{48,72})||ground!=65||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_bowl_bug source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=96||paddedImage.GetHeight()!=72)throw new InvalidOperationException("enemy_bowl_bug padded rest grid mismatch");
   for(int y=0;y<72;y++)for(int x=0;x<96;x++){var expected=x>=8&&x<88&&y>=0&&y<72?sourceImage.GetPixel(x-8,y-0):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_bowl_bug rest is not exact original RGBA padding");}
   canvas=new(96,72,48,72,0,0,65);
  }else if(enemyId=="enemy_brood_toad"){
   const string originalHash="1634c5e232b745961cb250f496921ff3c912f7c0ea1f8c5417325db98de71a7f";
   if(!source.SequenceEqual(new[]{144,112})||!frame.SequenceEqual(new[]{240,128})||!offset.SequenceEqual(new[]{96,16})||!sprite.SequenceEqual(new[]{-48,8})||!anchor.SequenceEqual(new[]{168,128})||ground!=119||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_brood_toad source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=240||paddedImage.GetHeight()!=128)throw new InvalidOperationException("enemy_brood_toad rest grid mismatch");
   for(int y=0;y<128;y++)for(int x=0;x<240;x++){var expected=x>=96&&x<240&&y>=16&&y<128?sourceImage.GetPixel(x-96,y-16):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_brood_toad rest is not exact original RGBA registration");}
   canvas=new(240,128,168,128,-48,8,119);
  }else if(enemyId=="enemy_broodling"){
   const string originalHash="42ca562be9bc54204a3a291e0641b9b8468f6b4130755787520338a340b2bdc1";
   if(!source.SequenceEqual(new[]{72,64})||!frame.SequenceEqual(new[]{72,64})||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{36,64})||ground!=57||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_broodling source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=72||paddedImage.GetHeight()!=64)throw new InvalidOperationException("enemy_broodling rest grid mismatch");
   for(int y=0;y<64;y++)for(int x=0;x<72;x++){var expected=x>=0&&x<72&&y>=0&&y<64?sourceImage.GetPixel(x-0,y-0):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_broodling rest is not exact original RGBA registration");}
   canvas=new(72,64,36,64,0,0,57);
  }else if(enemyId=="enemy_raptor_chick"){
   const string originalHash="6b18fca389659e464660379fe3dedde2dbd90da54f2fa95a17c6dcc3aad61525";
   if(!source.SequenceEqual(new[]{88,64})||!frame.SequenceEqual(new[]{104,72})||!offset.SequenceEqual(new[]{16,8})||!sprite.SequenceEqual(new[]{-8,4})||!anchor.SequenceEqual(new[]{60,72})||ground!=65||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_raptor_chick source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=104||paddedImage.GetHeight()!=72)throw new InvalidOperationException("enemy_raptor_chick rest grid mismatch");
   for(int y=0;y<72;y++)for(int x=0;x<104;x++){var expected=x>=16&&x<104&&y>=8&&y<72?sourceImage.GetPixel(x-16,y-8):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_raptor_chick rest is not exact original RGBA registration");}
   canvas=new(104,72,60,72,-8,4,65);
  }else if(enemyId=="enemy_axe_automaton_reboot"){
   const string originalHash="97150152fd6f8ffd45e5d6b40bcc0102ada0132877a60c1bb43c517173ca5274";
   if(!source.SequenceEqual(new[]{136,88})||!frame.SequenceEqual(new[]{184,112})||!offset.SequenceEqual(new[]{32,24})||!sprite.SequenceEqual(new[]{-8,12})||!anchor.SequenceEqual(new[]{100,112})||ground!=103||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_axe_automaton_reboot source/canvas registration or changed exact original rest");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=184||paddedImage.GetHeight()!=112)throw new InvalidOperationException("enemy_axe_automaton_reboot rest grid mismatch");
   for(int y=0;y<112;y++)for(int x=0;x<184;x++){var expected=x>=32&&x<168&&y>=24&&y<112?sourceImage.GetPixel(x-32,y-24):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("enemy_axe_automaton_reboot rest is not exact original RGBA registration");}
   canvas=new(184,112,100,112,-8,12,103);
  }else if(enemyId=="enemy_big_slime"){
   const string originalHash="ab43bbdeae58ffaf700cd72b9ba7df54e8bc2021fedbd93086bcca5f619e43d1";
   if(!source.SequenceEqual(new[]{152,112})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{76,112})||ground!=103||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest_canvas.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_big_slime source/canvas registration or changed exact original rest");
   canvas=new(152,112,76,112,0,0,103);
  }else if(enemyId=="enemy_shield_squire"){
   const string originalHash="b48a5081f5d38813fecd2251b67fca54c313c944024a74e94a59ad864d0d9bf4";
   if(!source.SequenceEqual(new[]{104,88})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{52,88})||ground!=79||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest_canvas.png"))!=originalHash)throw new InvalidOperationException("Unapproved enemy_shield_squire source/canvas registration or changed exact original rest");
   canvas=new(104,88,52,88,0,0,79);
  }else if(enemyId=="enemy_wide_power"){
   const string originalHash="16d7839d17724518d11c6c0981b928868c4646a59e107cb058335ebd198741fc";
   if(!source.SequenceEqual(new[]{144,88})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{72,88})||ground!=81||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest_canvas.png"))!=originalHash)throw new InvalidOperationException("Unapproved wide-beast source/canvas registration or changed exact original rest");
   canvas=new(144,88,72,88,0,0,81);
  }else if(enemyId=="enemy_elite_owl"){
   const string originalHash="daeba4eba889010d4d43125542bf6b7ac544c60cba677da92149604d2da9dd03";
   if(!source.SequenceEqual(new[]{160,112})||!frame.SequenceEqual(new[]{192,128})||!offset.SequenceEqual(new[]{16,16})||!sprite.SequenceEqual(new[]{0,8})||!anchor.SequenceEqual(new[]{96,128})||ground!=119||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved owl source/canvas registration");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=192||paddedImage.GetHeight()!=128)throw new InvalidOperationException("Owl padded rest grid mismatch");
   for(int y=0;y<128;y++)for(int x=0;x<192;x++){var expected=x>=16&&x<176&&y>=16?sourceImage.GetPixel(x-16,y-16):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("Owl rest is not exact original RGBA padding");}
   canvas=new(192,128,96,128,0,8,119);
  }else if(enemyId=="enemy_elite_gold_raven"){
   const string originalHash="61b7f174373c52a751b5349d47d6ba831d28151ac97bcb0739345a190a780414";
   if(!source.SequenceEqual(new[]{176,112})||!frame.SequenceEqual(new[]{192,128})||!offset.SequenceEqual(new[]{16,16})||!sprite.SequenceEqual(new[]{-8,8})||!anchor.SequenceEqual(new[]{104,128})||ground!=119||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(root+"rest.png"))!=originalHash)throw new InvalidOperationException("Unapproved gold-raven source/canvas registration");
   using var sourceImage=EnemyResourceFile.LoadPng(original.ResourcePath);using var paddedImage=EnemyResourceFile.LoadPng(root+"rest_canvas.png");sourceImage.Convert(Image.Format.Rgba8);paddedImage.Convert(Image.Format.Rgba8);
   if(paddedImage.GetWidth()!=192||paddedImage.GetHeight()!=128)throw new InvalidOperationException("Gold-raven padded rest grid mismatch");
   for(int y=0;y<128;y++)for(int x=0;x<192;x++){var expected=x>=16&&y>=16?sourceImage.GetPixel(x-16,y-16):new Color(0,0,0,0);if(paddedImage.GetPixel(x,y)!=expected)throw new InvalidOperationException("Gold-raven rest is not exact original RGBA padding");}
   canvas=new(192,128,104,128,-8,8,119);
  }else if(enemyId=="enemy_moss_slime"){
   const string originalHash="bb16cfc6acd409a1d13b4ce5a7aa0b9e6b2dab0e23b2cb3896da9b0bd0251b52";
   if(!source.SequenceEqual(new[]{72,64})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{36,64})||ground!=57||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved moss-slime source/canvas registration");
   canvas=new(72,64,36,64,0,0,57);
  }else if(enemyId=="enemy_cultist"){
   const string originalHash="638183b8d53c15e23daa0348bb4f8449e0dedb5fac2dfb4e8ba0face509720d4";
   if(!source.SequenceEqual(new[]{96,88})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{48,88})||ground!=79||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved cultist source/canvas registration");
   canvas=new(96,88,48,88,0,0,79);
  }else if(enemyId=="enemy_apprentice_colossus"){
   const string originalHash="8a3dd3f996f4e30adf77a0dd999723a9f3c389d4ef080b5772291d212882597e";
   if(!source.SequenceEqual(new[]{128,112})||!frame.SequenceEqual(new[]{160,112})||!offset.SequenceEqual(new[]{32,0})||!sprite.SequenceEqual(new[]{-16,0})||!anchor.SequenceEqual(new[]{96,112})||ground!=103||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved apprentice-colossus source/canvas registration");
   canvas=new(160,112,96,112,-16,0,103);
  }else if(enemyId=="enemy_wolf"){
   const string originalHash="697ce544e63971f7e8a61b98b5695ddb2b78300c487f28387f59ba6ff8c402ee";
   if(!source.SequenceEqual(new[]{128,80})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{64,80})||ground!=73||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved wolf source/canvas registration");
   canvas=new(128,80,64,80,0,0,73);
  }else if(enemyId=="enemy_crossbow_archer"){
   const string originalHash="d940a95c0258df6f326507f90226b357b1f8bfb5d60102c13b3815b04eaa2f5d";
   if(!source.SequenceEqual(new[]{128,88})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{64,88})||ground!=79||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved crossbow source/canvas registration");
   canvas=new(128,88,64,88,0,0,79);
  }else if(enemyId=="enemy_set_wary"){
   const string originalHash="adf553d15e4e18d71726b877c4e3590eb4bbf6987683723571db3aa333add469";
   if(!source.SequenceEqual(new[]{96,96})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{48,96})||ground!=87||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved wary-shadow source/canvas registration");
   canvas=new(96,96,48,96,0,0,87);
  }else if(enemyId is "enemy_thorn_squirrel" or "enemy_snap_fruit" or "enemy_thief"){
   string originalHash=enemyId=="enemy_thief"?"936acf607bb937d5f366879e8ef088f32d4b27a0556dec873331b5494aefff25":enemyId=="enemy_thorn_squirrel"?"2cf0c15ac565b21d0f3c236799a7755dc04678543d71d6e6ab6e231e5c39c464":"11f343e0f2844b91aa06c33bda9902bd53a486a06fa45a8f6e1329e6a9b7d0ee";
   if(!source.SequenceEqual(new[]{80,64})||!frame.SequenceEqual(source)||!offset.SequenceEqual(new[]{0,0})||!sprite.SequenceEqual(new[]{0,0})||!anchor.SequenceEqual(new[]{40,64})||ground!=59||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved small-pair source/canvas registration");
   canvas=new(80,64,40,64,0,0,59);
  }else if(enemyId is "enemy_spore_cap" or "enemy_vine_walker" or "enemy_hexer"){
   var approved=enemyId switch{
    "enemy_spore_cap"=>(sw:64,sh:64,fw:64,pad:0,sx:0,ground:59,hash:"38980396b29d489233bc65f4a1a5cff3df483c1247b8b3ea8b2ee9ba4e7de6a3"),
    "enemy_vine_walker"=>(sw:120,sh:112,fw:144,pad:24,sx:-12,ground:103,hash:"d767ae128f5c17b3f3db7de97f1dcee9ae5c09a57126c536c3e8d708806741df"),
    _=>(sw:112,sh:96,fw:112,pad:0,sx:0,ground:87,hash:"7a9583416539a7107f9e8d256dc66188505ea2898eb865894ec74e259b4df680")};
   if(!source.SequenceEqual(new[]{approved.sw,approved.sh})||!frame.SequenceEqual(new[]{approved.fw,approved.sh})||!offset.SequenceEqual(new[]{approved.pad,0})||!sprite.SequenceEqual(new[]{approved.sx,0})||!anchor.SequenceEqual(new[]{approved.sw/2+approved.pad,approved.sh})||ground!=approved.ground||m["sourcePngSha256"]?.Value<string>()!=approved.hash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=approved.hash)throw new InvalidOperationException("Unapproved fungi/tree/caster source/canvas registration");
   canvas=new(approved.fw,approved.sh,approved.sw/2+approved.pad,approved.sh,approved.sx,0,approved.ground);
  }else{
   bool turtle=enemyId=="enemy_turtle";int sw=turtle?152:96,sh=turtle?112:96,fw=turtle?184:96,pad=turtle?32:0,offsetX=turtle?-16:0,groundY=turtle?103:87;
   string originalHash=enemyId switch{"enemy_probe"=>"330a4a97ab527026a7d129f4d60f9260438e4d13a738061c3dca37894b2fd47e","enemy_turtle"=>"4d629f745f705269de382a72f55784f1f7412a073aac5404e11b3d4738a86f1e","enemy_drummer"=>"52227ec83d80499a6425afee743e3ba9d955c528d3ec2afe7281f2a8638afa6a",_=>throw new InvalidOperationException("Unregistered authored canvas")};
   if(!source.SequenceEqual(new[]{sw,sh})||!frame.SequenceEqual(new[]{fw,sh})||!offset.SequenceEqual(new[]{pad,0})||!sprite.SequenceEqual(new[]{offsetX,0})||!anchor.SequenceEqual(new[]{sw/2+pad,sh})||ground!=groundY||m["sourcePngSha256"]?.Value<string>()!=originalHash||Hash(EnemyResourceFile.ReadAllBytes(original.ResourcePath))!=originalHash)throw new InvalidOperationException("Unapproved trio source/canvas registration");
   canvas=new(fw,sh,sw/2+pad,sh,offsetX,0,groundY);
  }
  var result=new EnemyAuthoredClipSet{Canvas=canvas,ManifestSha256=Hash(bytes)};var pending=new List<Texture2D>();
  try{
   if(m["clips"] is not JObject clips)throw new InvalidOperationException("Missing authored clips");
   foreach(var clip in clips.Properties()){
    if(clip.Value is not JArray rows||rows.Count==0)throw new InvalidOperationException("Empty authored clip");
    var textures=new List<Texture2D>();var durations=new List<float>();
    foreach(var row in rows){
     string file=row["file"]?.Value<string>()??"";float duration=row["duration"]?.Value<float>()??float.NaN;
     if(file!=Path.GetFileName(file)||!file.EndsWith(".png",StringComparison.Ordinal)||!float.IsFinite(duration)||duration<=0)throw new InvalidOperationException("Invalid authored frame record");
     string path=root+file;string frameSha=Hash(EnemyResourceFile.ReadAllBytes(path));if(frameSha!=row["sha256"]?.Value<string>())throw new InvalidOperationException("Authored PNG SHA mismatch");
     using var image=EnemyResourceFile.LoadPng(path);if(image.IsEmpty()||image.GetWidth()!=canvas.FrameWidth||image.GetHeight()!=canvas.FrameHeight)throw new InvalidOperationException("Authored PNG grid mismatch");
     if(strictM34||strictM35||strictM36||strictM37||strictM38||strictM39||strictM40||strictM41||strictM42||strictM43||strictM44||strictM45||strictM46||strictM47||strictM48||strictM49||strictM50||strictM51||strictM52||strictM53||strictM54||strictM57||strictM66||strictM65||strictM64||strictM63||strictM62||strictM61||strictM60||strictM59||strictM58||strictM56||strictM55){image.Convert(Image.Format.Rgba8);for(int y=0;y<image.GetHeight();y++)for(int x=0;x<image.GetWidth();x++){var c=image.GetPixel(x,y);if((c.A!=0&&c.A!=1)||(c.A==1&&!nativePalette!.Contains(c))||((strictM41||strictM42||strictM43||strictM44||strictM45||strictM46||strictM47||strictM48||strictM49||strictM50||strictM51||strictM52||strictM53||strictM54||strictM57||strictM66||strictM65||strictM64||strictM63||strictM62||strictM61||strictM60||strictM59||strictM58||strictM56||strictM55)&&c.A==0&&(c.R!=0||c.G!=0||c.B!=0)))throw new InvalidOperationException("Registered authored frame must retain binary alpha and native source palette");}// Only the approved first axe impact extends below the physical rear-boot ground.
      // Keep the complete opaque envelope for occlusion; never re-anchor to the blade.
      bool approvedAxeBlade=enemyId=="enemy_axe_automaton_reboot"&&clip.Name=="axecontact"&&file=="axecontact.png"&&canvas==new CanvasContract(184,112,100,112,-8,12,103)&&frameSha=="7f972b953ea9273b6086faf9243e8bbf1a7384658eba3e815693a381909d0bd7"&&Hash(image.GetData())=="4cf291a38c44b63d101d08d05a4917b367a65dbf03ec8196479b72e990570afd"&&image.GetUsedRect()==new Rect2I(42,43,104,63);
      bool approvedMudScoop=enemyId=="enemy_mud_lump"&&file=="slopprepare.png"&&canvas==new CanvasContract(96,80,48,80,0,0,73)&&frameSha=="2fd25203345a39f0d863e9cdf796d1ec445697aa77f90a535710a29975af37ad"&&image.GetUsedRect()==new Rect2I(11,28,69,47);
      if(enemyId=="enemy_mud_lump"){int rightFootY=-1;for(int y=0;y<80;y++)for(int x=58;x<83;x++)if(image.GetPixel(x,y).A>0)rightFootY=Math.Max(rightFootY,y);if(rightFootY!=73)throw new InvalidOperationException("M45 exact right-foot support drift");}
      bool floatingWraith=enemyId=="enemy_nemesis_wraith";
      if(floatingWraith&&!(file switch{
       "idle_00.png"=>frameSha=="bd21fe19ca579206ad5f5e689cbaaa2f6b729660f2ac99c17f63acdba1c52418"&&image.GetUsedRect()==new Rect2I(23,24,113,87),
       "idle_01.png"=>frameSha=="519a75d66fa92ea1ed041ace96bfb659efc097da109f84f60cc937df142ec6b4"&&image.GetUsedRect()==new Rect2I(23,23,113,87),
       "idle_02.png"=>frameSha=="9bf1dbb6e592b41ebe9ae6b41932bf3e557c7cb93d675fc32fc9757c8ab722cc"&&image.GetUsedRect()==new Rect2I(23,23,113,87),
       "idle_03.png"=>frameSha=="e8f9f7f8751b9b931c718033c2809d9410042ead7e045a873bbb273736978a93"&&image.GetUsedRect()==new Rect2I(23,22,113,87),
       "idle_04.png"=>frameSha=="fa208fd8eb72b313b14b1704750a607bb0dd74886a057895e3d50912d7d24a34"&&image.GetUsedRect()==new Rect2I(23,22,113,87),
       "idle_05.png"=>frameSha=="12594d22ab90274d539d065e558f7be3f2d7ff6a438693bde97f39e80a36f7fd"&&image.GetUsedRect()==new Rect2I(23,23,113,86),
       "idle_06.png"=>frameSha=="a63efffb5ac495d7f812a1a5aa37002b75c6ebc90cb6f28c85a07b8b5489be24"&&image.GetUsedRect()==new Rect2I(23,23,113,86),
       "idle_07.png"=>frameSha=="77d617012db6c5c860bfcd6d2f95a5975eff185bf07b0115706e4277a779d9ca"&&image.GetUsedRect()==new Rect2I(23,24,113,86),
       "idle.png"=>frameSha=="c82f8eb95f56d3d5656f461cd7bf73ec45465e0f1cf1d212024f384a35234ace"&&image.GetUsedRect()==new Rect2I(23,24,116,87),
       "hurt.png"=>frameSha=="cda0043c96e4cb1d31796e4709e45fedc8404f3ce0b2794fcebd0fb5e6ea7b32"&&image.GetUsedRect()==new Rect2I(24,22,109,89),
       "curseprepare.png"=>frameSha=="e9ba304c5ba367f205aec7e741ba4f9b9b3ce0942bcfd723529e32d8784f42f3"&&image.GetUsedRect()==new Rect2I(23,23,116,87),
       "cursecontact.png"=>frameSha=="a9a633b1308bb29084c41389705fdff52558fa541ce55babf494ea683f58963e"&&image.GetUsedRect()==new Rect2I(28,23,107,87),
       "scytheprepare.png"=>frameSha=="27d519701a84c7d374a3a06094bbb8ae290ae725295352de1774542eb53a0739"&&image.GetUsedRect()==new Rect2I(24,16,97,93),
       "scythecontact.png"=>frameSha=="f70f1246c37425e9bbaad20003aae82db9fc54fca55d3a8f0e2954e776a98dc4"&&image.GetUsedRect()==new Rect2I(12,34,126,69),
       "recovery.png"=>frameSha=="3937180aac95ff9c1118c91b0b85e14a97a527ea732ac60e2fd860ad56fa1502"&&image.GetUsedRect()==new Rect2I(20,22,118,86),
       "rest.png"=>frameSha=="bd21fe19ca579206ad5f5e689cbaaa2f6b729660f2ac99c17f63acdba1c52418"&&image.GetUsedRect()==new Rect2I(23,24,113,87),
       _=>false}))throw new InvalidOperationException("Unapproved M39 floating robe pixels or registration");
      if(strictM40&&!((enemyId,file) switch{
       ("enemy_axe_ogre","gasp.png")=>frameSha=="95709a270039a5cbc9ec2f466730a25bb10f728cdaaa0bc08a1c8219797c807b"&&image.GetUsedRect()==new Rect2I(16,42,97,70),
       ("enemy_axe_ogre","idle_00.png")=>frameSha=="257e05a08ce17c32b3ee5acc9a7ddd701744ad6998cf332cbbc438f8bac7f0c4"&&image.GetUsedRect()==new Rect2I(29,16,86,96),
       ("enemy_axe_ogre","idle_01.png")=>frameSha=="bd9b4cbcdb593c415ce91d5ee4838238e36caf2398573571ceb3336a86b8c2d3"&&image.GetUsedRect()==new Rect2I(28,16,87,96),
       ("enemy_axe_ogre","idle_02.png")=>frameSha=="0e3dc0db1a1c38defbdae11b18a67306635a839a7935e6940edd41cbb9946b99"&&image.GetUsedRect()==new Rect2I(28,15,87,97),
       ("enemy_axe_ogre","idle_03.png")=>frameSha=="61d7e0f5b7cf492fe7b9bb5cf6ce4f91af96db71d82595b27bdfaec205cfa7bd"&&image.GetUsedRect()==new Rect2I(28,14,87,98),
       ("enemy_axe_ogre","idle_04.png")=>frameSha=="5a0b5155a808c218a83ae410cb56e7fb0ac1c3d0069de771e757c5cdd4f91b25"&&image.GetUsedRect()==new Rect2I(29,14,86,98),
       ("enemy_axe_ogre","idle_05.png")=>frameSha=="f21dc3f2d890c52f9f57249734599764e02095d35c826b511570b794de952369"&&image.GetUsedRect()==new Rect2I(30,14,85,98),
       ("enemy_axe_ogre","idle_06.png")=>frameSha=="66afec9a0648e613746067dc39ce1cf32e01d16d44aeb6598751ec63b60fd224"&&image.GetUsedRect()==new Rect2I(30,15,85,97),
       ("enemy_axe_ogre","idle_07.png")=>frameSha=="4a34b246ccb961bc6cde180e42b99eae59436ecb8d10fd2f2496b59d8e230ef5"&&image.GetUsedRect()==new Rect2I(30,16,85,96),
       ("enemy_axe_ogre","hurt.png")=>frameSha=="cd8cd198b3a940615116a33979dacb6c8911612ce63fc55582016a05bae215b2"&&image.GetUsedRect()==new Rect2I(18,17,90,95),
       ("enemy_axe_ogre","idle.png")=>frameSha=="c1a12f4315fc0e137fd367ea486cc0619728355f2d6ae6f69a3bcab3d96687e0"&&image.GetUsedRect()==new Rect2I(19,16,99,96),
       ("enemy_axe_ogre","recovery.png")=>frameSha=="cab743ac9bb76e9048d83a3bae6f7a9361b800888ea738754a8f7f73ac1eafd7"&&image.GetUsedRect()==new Rect2I(6,37,108,75),
       ("enemy_axe_ogre","rest.png")=>frameSha=="257e05a08ce17c32b3ee5acc9a7ddd701744ad6998cf332cbbc438f8bac7f0c4"&&image.GetUsedRect()==new Rect2I(29,16,86,96),
       ("enemy_axe_ogre","swingcontact.png")=>frameSha=="7208a650ee6f0905b8d5e2e3dbe5f8962a3564cae5ff505dfbcdebc415df94dc"&&image.GetUsedRect()==new Rect2I(0,36,119,76),
       ("enemy_axe_ogre","swingprepare.png")=>frameSha=="02ba31cbff49a67baada9d6c2f3cb5c618828fbb6cc1ec4009a56a20fc2cbb88"&&image.GetUsedRect()==new Rect2I(28,17,92,95),
       ("enemy_axe_ogre","winduphold.png")=>frameSha=="78416b49ca18f76a0daa74f81360cd4faec2f5112fe12502c22866dd1040e69e"&&image.GetUsedRect()==new Rect2I(24,26,93,86),
       ("enemy_axe_ogre","windupprepare.png")=>frameSha=="e3a2e8059ad937a5f957f5c381101ed002e14f3c6a6cdfe3c8fc2be612d2b71b"&&image.GetUsedRect()==new Rect2I(26,23,96,89),
       ("enemy_forgotten_soul","dreadcontact.png")=>frameSha=="595fe66d9a449f5e94ba328fbaf10cf16d0dbe0ab2bf69358fe456531d1c2714"&&image.GetUsedRect()==new Rect2I(10,17,98,61),
       ("enemy_forgotten_soul","dreadprepare.png")=>frameSha=="3b506eae97c0b9a716b119a2a629dc8707df40ff76f87c76b9365af30d6e49e8"&&image.GetUsedRect()==new Rect2I(39,14,59,64),
       ("enemy_forgotten_soul","guardhold.png")=>frameSha=="e311bafbad2aad21b75391ea14c45dc289ec762bb0f33842db40d724dd9d1da7"&&image.GetUsedRect()==new Rect2I(42,13,58,64),
       ("enemy_forgotten_soul","guardprepare.png")=>frameSha=="df83eff477666a081f020f46b197bee85c581f526dd84350cefc099a68ffee60"&&image.GetUsedRect()==new Rect2I(38,12,62,65),
       ("enemy_forgotten_soul","idle_00.png")=>frameSha=="93fd1ee895aaf08f95b7370caef7e803dc855b616fea92589870a3839d648d2e"&&image.GetUsedRect()==new Rect2I(29,16,69,64),
       ("enemy_forgotten_soul","idle_01.png")=>frameSha=="5f2f155ca1bd24c2166b7aebf43c8dd88f9655288ef264a39d9b11dd8d244530"&&image.GetUsedRect()==new Rect2I(29,15,69,65),
       ("enemy_forgotten_soul","idle_02.png")=>frameSha=="7f513df2ea5b7a33b3aa1aff0b2fa60f1e7ee5dff5c18ae76881d05ad6d20b83"&&image.GetUsedRect()==new Rect2I(28,15,70,65),
       ("enemy_forgotten_soul","idle_03.png")=>frameSha=="33043e40813ea67cabfa74a2a17dc89f60e2f25c66e0aa674490d957a3edd738"&&image.GetUsedRect()==new Rect2I(28,14,70,65),
       ("enemy_forgotten_soul","idle_04.png")=>frameSha=="7392348eb90653910bc450276696932634052b1b648163e2f0780b5f4d92d176"&&image.GetUsedRect()==new Rect2I(28,14,70,65),
       ("enemy_forgotten_soul","idle_05.png")=>frameSha=="b5b9a10a035d1d5a578de5c6c65dded5598e4a740aa3b1ceccdd883cdbeeccaf"&&image.GetUsedRect()==new Rect2I(29,15,69,63),
       ("enemy_forgotten_soul","idle_06.png")=>frameSha=="e27c0e453f5e2f5f409dab859a48ab01aee1365011c776f82569b27830eb7794"&&image.GetUsedRect()==new Rect2I(29,15,69,64),
       ("enemy_forgotten_soul","idle_07.png")=>frameSha=="357d555861524d0d0ad9fe2659919f77455eaf1da1cb1b269d50c9ce202c1361"&&image.GetUsedRect()==new Rect2I(29,16,69,64),
       ("enemy_forgotten_soul","hurt.png")=>frameSha=="dd99a20cf2b748102d6d439449b0c24cdbc788114f313be229ff325778497442"&&image.GetUsedRect()==new Rect2I(32,18,61,63),
       ("enemy_forgotten_soul","idle.png")=>frameSha=="74b8620c913242bdefaa8cf2a0d273da3713e05672927e253c924e428e4dfb98"&&image.GetUsedRect()==new Rect2I(27,16,73,64),
       ("enemy_forgotten_soul","miasmacontact.png")=>frameSha=="25ac0db8c41c02bda84b7808f5aa523a12bbe1d2dfb2511512a51b33fe17484a"&&image.GetUsedRect()==new Rect2I(17,13,100,65),
       ("enemy_forgotten_soul","miasmaprepare.png")=>frameSha=="0b7173b75484c5c6711b243ba134147bbbe0f4274ebb33500456c791068cd612"&&image.GetUsedRect()==new Rect2I(39,19,60,62),
       ("enemy_forgotten_soul","recovery.png")=>frameSha=="6b32fa93cea2442cb4b5e561819a4dcccb7ef6e17869d8bea4d416ffefdfcb51"&&image.GetUsedRect()==new Rect2I(29,14,73,63),
       ("enemy_forgotten_soul","rest.png")=>frameSha=="07f495e9664efd41c7e358cc38b8983f60223097bbaaffa06ae0af4c1b184c0e"&&image.GetUsedRect()==new Rect2I(29,16,69,64),
       _=>false}))throw new InvalidOperationException("Unapproved M40 exact authored pixels or native bounds");
      if(strictM43&&!((enemyId,file) switch{
       ("enemy_mudling","bumpcontact.png")=>frameSha=="33aba464e9a2487c5a12ca2f7bc2e742de6004aa79d4a4384f2b7fd8a8703701"&&image.GetUsedRect()==new Rect2I(4,24,65,36),
       ("enemy_mudling","bumpprepare.png")=>frameSha=="0c363201956bcb35e7e1e18ccdf60dee3b086fb59bfffb6fd6f68e09ebf1f2ac"&&image.GetUsedRect()==new Rect2I(12,23,57,37),
       ("enemy_mudling","guardhold.png")=>frameSha=="2dd5afa6d972e76fec4bbd12824aa92089b7aa2ab464a6286ead0f632ae3266f"&&image.GetUsedRect()==new Rect2I(5,27,60,33),
       ("enemy_mudling","guardprepare.png")=>frameSha=="268f109c3e988685eba881f53ac35efa3ecbd34618f8a85fdad3715e2df943d2"&&image.GetUsedRect()==new Rect2I(8,29,59,31),
       ("enemy_mudling","idle_00.png")=>frameSha=="1934ecf70edd3a01036d6cd5baa5601d1f01b0e59dd3ee778134f0e7f76a454d"&&image.GetUsedRect()==new Rect2I(9,20,54,40),
       ("enemy_mudling","idle_01.png")=>frameSha=="747f06788e5b01924533ec8e2d21830ad079d8ac77f89ca80c98f6b823d36da4"&&image.GetUsedRect()==new Rect2I(9,19,54,41),
       ("enemy_mudling","idle_02.png")=>frameSha=="a08008b3af77c1b44ab1d22da6a0132a369bd7f92b89f01a90373bd6f277648c"&&image.GetUsedRect()==new Rect2I(9,19,54,41),
       ("enemy_mudling","idle_03.png")=>frameSha=="4054615879035697b620da11405d86d5c3331754485e4713490d42da8dd18357"&&image.GetUsedRect()==new Rect2I(9,18,54,42),
       ("enemy_mudling","idle_04.png")=>frameSha=="4a32bdf520715b32f2296f2bfe45b3acca964a10962670f5447fea46ed20cd36"&&image.GetUsedRect()==new Rect2I(9,18,54,42),
       ("enemy_mudling","idle_05.png")=>frameSha=="072f9d137a48a74f17ec6d49bc3e5783b0dfdcacc5bd5cdd4b870858666624aa"&&image.GetUsedRect()==new Rect2I(9,19,54,41),
       ("enemy_mudling","idle_06.png")=>frameSha=="4edd940a5bd99e7921aa5a1132b3a7c7ef4e4bf2ad34ca22472fd61414107a0a"&&image.GetUsedRect()==new Rect2I(9,19,54,41),
       ("enemy_mudling","idle_07.png")=>frameSha=="3b7acd0afc0f8bd22784da6db55cbb02b80b531e3e5004e841419f1720018950"&&image.GetUsedRect()==new Rect2I(9,20,54,40),
       ("enemy_mudling","hurt.png")=>frameSha=="da4fd3ae9adb6403c06ca0784ffc62efa145337329a8326f9225202d9b3b8d64"&&image.GetUsedRect()==new Rect2I(7,29,49,31),
       ("enemy_mudling","idle.png")=>frameSha=="89592bf8ab526d69d37416371c0ed206e2b3b2c00d7dc6d9d67cc9454a3e7ec3"&&image.GetUsedRect()==new Rect2I(7,20,57,40),
       ("enemy_mudling","recovery.png")=>frameSha=="16cb23d0fda5871054d131c5bf36c6e9efb1c1aa26b3f2c678116acb1e534d4c"&&image.GetUsedRect()==new Rect2I(5,19,59,41),
       ("enemy_mudling","rest_canvas.png")=>frameSha=="c28f218828b6bf1555118c89f6afae023eb4ee749f927bdb39ded12794693a1d"&&image.GetUsedRect()==new Rect2I(9,20,54,40),
       ("enemy_mudling","slopapplication.png")=>frameSha=="daa57f21a496f9fb044cfb3d0af11eba233c41e6bf9dd3377e735b17657e4275"&&image.GetUsedRect()==new Rect2I(1,23,60,37),
       ("enemy_mudling","slopprepare.png")=>frameSha=="491af27cabed83c5310dc713eee621b5cbd68842ca08c0b58263cdaa40cdd9ff"&&image.GetUsedRect()==new Rect2I(11,19,49,41),
       ("enemy_lost_soul","beamprepare.png")=>frameSha=="94dc5c6c4a60584ada54cc0f13a042538360990e58412128d3b98b04e65b7f05"&&image.GetUsedRect()==new Rect2I(51,18,70,62),
       ("enemy_lost_soul","beamrelease.png")=>frameSha=="cb06196d7fb39c58b484295d135dbaf01e327210db04d55632baf83fbff8722d"&&image.GetUsedRect()==new Rect2I(52,16,67,64),
       ("enemy_lost_soul","guardhold.png")=>frameSha=="13a755a49efcc7d8ac07b7064e5630ac3052a9f737d5f9d0319882165e96821c"&&image.GetUsedRect()==new Rect2I(59,21,53,59),
       ("enemy_lost_soul","guardprepare.png")=>frameSha=="6aba6b44349813e710888ea552802b50feeabd2229b003a028c9a8f5d1e76efb"&&image.GetUsedRect()==new Rect2I(49,21,64,58),
       ("enemy_lost_soul","idle_00.png")=>frameSha=="188d0b3e628278790beb08e6f015aa57274e18497082faf4e3a85faecb0dbef8"&&image.GetUsedRect()==new Rect2I(36,16,71,64),
       ("enemy_lost_soul","idle_01.png")=>frameSha=="1588a95cdfb80973efa09d49b4ae33629d0d56020d249a54e38bab9726049993"&&image.GetUsedRect()==new Rect2I(36,15,71,65),
       ("enemy_lost_soul","idle_02.png")=>frameSha=="cd7bf632b8bb55a758a735ec4fba749bf51b7be693cb3eede0b828f1345067d5"&&image.GetUsedRect()==new Rect2I(36,15,71,64),
       ("enemy_lost_soul","idle_03.png")=>frameSha=="805dd2795cf24413b6c77a1b22aff87f6d65e1dbc8df9815046fb6f88926c64d"&&image.GetUsedRect()==new Rect2I(36,15,71,64),
       ("enemy_lost_soul","idle_04.png")=>frameSha=="0d7dbd5a0396dbc76131d3f104de81ce1bd0c98679172d8a88e83561ec65feb9"&&image.GetUsedRect()==new Rect2I(36,16,71,63),
       ("enemy_lost_soul","idle_05.png")=>frameSha=="d169a51a3b8582e3952de2ab451a426b7d9b0c7d8237110ce8b74bca2d3252c9"&&image.GetUsedRect()==new Rect2I(36,16,71,64),
       ("enemy_lost_soul","idle_06.png")=>frameSha=="d4c1140914338995381d5e7561f8b6a3387ada1554236d4f446b5f402cc6c445"&&image.GetUsedRect()==new Rect2I(36,16,71,64),
       ("enemy_lost_soul","idle_07.png")=>frameSha=="23389a688d1541dd26e865569be5080c41a29804fb11bed1911f9a8627cabe92"&&image.GetUsedRect()==new Rect2I(36,16,71,64),
       ("enemy_lost_soul","hurt.png")=>frameSha=="2729fdfb65e9ab3a9c452fbc5566db8427acbbdd333adbf8a9f55af32752cbac"&&image.GetUsedRect()==new Rect2I(46,13,70,67),
       ("enemy_lost_soul","idle.png")=>frameSha=="3ef91f9c603dde60d9b3381d26b46877127de70c24016920e5b38f78ed08a169"&&image.GetUsedRect()==new Rect2I(39,16,74,63),
       ("enemy_lost_soul","recovery.png")=>frameSha=="53b5e1dc524527e46434b6792462a99e6d4cc1855a7134525bd3cc3f22e374ca"&&image.GetUsedRect()==new Rect2I(52,17,63,63),
       ("enemy_lost_soul","rest.png")=>frameSha=="188d0b3e628278790beb08e6f015aa57274e18497082faf4e3a85faecb0dbef8"&&image.GetUsedRect()==new Rect2I(36,16,71,64),
       ("enemy_lost_soul","smogprepare.png")=>frameSha=="13b90526fa72d1c5a450ba148bd548f24d36128c1d2dd4ba6ebd9cae2d61be64"&&image.GetUsedRect()==new Rect2I(50,20,57,60),
       ("enemy_lost_soul","smogrelease.png")=>frameSha=="c19089db382f0bf9fc3bf6148c023b260dacfb613edbf725e691ac0eb9f34224"&&image.GetUsedRect()==new Rect2I(13,18,107,62),
       _=>false}))throw new InvalidOperationException("Unapproved M43 exact frame bytes or bounds");
      if(strictM44&&!((enemyId,file) switch{
       ("enemy_strangler_serpent","constrictcontact.png")=>frameSha=="b58390bc40ecad665710f1f9a33d2bfa4ba3e25774915264a5d1c14d3e21d6b8"&&image.GetUsedRect()==new Rect2I(37,20,61,60),
       ("enemy_strangler_serpent","constrictprepare.png")=>frameSha=="3e42cc054411f92ff501aa770f62bf4b08c5ce832e13efbded4afe0d56ef1bba"&&image.GetUsedRect()==new Rect2I(31,13,71,67),
       ("enemy_strangler_serpent","guardhold.png")=>frameSha=="e3cbcde94997fab040becea82e269ccfab3f735920a4845daf479c28c9a04c10"&&image.GetUsedRect()==new Rect2I(33,22,71,58),
       ("enemy_strangler_serpent","guardprepare.png")=>frameSha=="994d10fc7be46cb4ded4eceb9709b6543396deaa71f2f15c42e09a79605b6c7b"&&image.GetUsedRect()==new Rect2I(30,24,74,56),
       ("enemy_strangler_serpent","idle_00.png")=>frameSha=="2b077a4242b1c0d2487b182496245ecf512e31e5a9043e60e4cbef5d851fbda3"&&image.GetUsedRect()==new Rect2I(32,16,69,64),
       ("enemy_strangler_serpent","idle_01.png")=>frameSha=="9b6abe2c59ba0836fa35ab6ea94ccf65593be9cd900e9e957625ad12a790b304"&&image.GetUsedRect()==new Rect2I(31,16,70,64),
       ("enemy_strangler_serpent","idle_02.png")=>frameSha=="6bf8a5cc308355f84fbf2b29da70dedddf8421b3e99ef3f9f7912168f44fe0da"&&image.GetUsedRect()==new Rect2I(30,16,71,64),
       ("enemy_strangler_serpent","idle_03.png")=>frameSha=="b8efc1edf1e775d2a6997e6d11fee4749ebebe8d3fa9e6e725264e9fbeb6fadd"&&image.GetUsedRect()==new Rect2I(30,16,72,64),
       ("enemy_strangler_serpent","idle_04.png")=>frameSha=="f9c64482afe1dc98917de2b67d3d5c1b71d9155304f693ff3ca3c3163a3e32c3"&&image.GetUsedRect()==new Rect2I(31,16,71,64),
       ("enemy_strangler_serpent","idle_05.png")=>frameSha=="7978a90c537ea632cd907849342ce9e3491847aa0cf064c16ac8f2db5e6d3993"&&image.GetUsedRect()==new Rect2I(32,16,70,64),
       ("enemy_strangler_serpent","idle_06.png")=>frameSha=="88e40f9df9bc2734d62ce4d5a0d8ef921d975af43c04fb146ad68071c67f2710"&&image.GetUsedRect()==new Rect2I(32,16,69,64),
       ("enemy_strangler_serpent","idle_07.png")=>frameSha=="05ba4a1dc437260844e4a98e750909228c4d8920d15ea68bef431b06bd58d1f2"&&image.GetUsedRect()==new Rect2I(32,16,69,64),
       ("enemy_strangler_serpent","hurt.png")=>frameSha=="77c6e8b744c98525a8fcd3649c6133c5ba5b593eec07293cb03cfc985ca9627c"&&image.GetUsedRect()==new Rect2I(30,20,75,60),
       ("enemy_strangler_serpent","idle.png")=>frameSha=="14fb4e6678e72470a24256b6c3770104affc479658e667ec72dc0370c5b5b488"&&image.GetUsedRect()==new Rect2I(31,16,69,64),
       ("enemy_strangler_serpent","lashcontact.png")=>frameSha=="e458f53998abe04225ac68449930886870ab756df2a6fec2b493aa103fe35c33"&&image.GetUsedRect()==new Rect2I(2,29,101,51),
       ("enemy_strangler_serpent","lashprepare.png")=>frameSha=="ac9fea0f0eb0979a5695a9b11e06e704475657354c48293bf59012cc39fc337e"&&image.GetUsedRect()==new Rect2I(31,17,72,63),
       ("enemy_strangler_serpent","recovery.png")=>frameSha=="5c07bb30fa57f5b3e41b6792e071d5c61626611180f8dfcadb052a92662d1f43"&&image.GetUsedRect()==new Rect2I(28,13,80,67),
       ("enemy_strangler_serpent","rest_canvas.png")=>frameSha=="2b077a4242b1c0d2487b182496245ecf512e31e5a9043e60e4cbef5d851fbda3"&&image.GetUsedRect()==new Rect2I(32,16,69,64),
       ("enemy_strangler_serpent","thwackcontact.png")=>frameSha=="64a2fbd49f08c7f1573b13f9b0e182ddb09ce2df7f975f34355002f592d86600"&&image.GetUsedRect()==new Rect2I(21,17,75,63),
       ("enemy_strangler_serpent","thwackprepare.png")=>frameSha=="a6da932a9fc17d6973c27d187fac90cfa5f032801a67e42eb8a8debe5d23f8cd"&&image.GetUsedRect()==new Rect2I(28,14,80,66),
       ("enemy_elite_stab_book","contactA.png")=>frameSha=="6666b1d8680ac13455e082cdb3b577f6ac71fe027ee24588be1d8d16a5408da9"&&image.GetUsedRect()==new Rect2I(26,16,123,88),
       ("enemy_elite_stab_book","contactB.png")=>frameSha=="dca1f7d76105561031b0cefbdea60a8b719f93a29e365480f7d53cc7a23b8b01"&&image.GetUsedRect()==new Rect2I(29,15,124,89),
       ("enemy_elite_stab_book","guardhold.png")=>frameSha=="5f18256757d2129140266a6f6cd710a302eab0823777c597e1010fc3b5e82d4c"&&image.GetUsedRect()==new Rect2I(89,11,70,95),
       ("enemy_elite_stab_book","guardprepare.png")=>frameSha=="0eb10b9a3907b77dd1bd1f8ec1a20bb4223ef2686f3322f90a4a3e48e8bf80f2"&&image.GetUsedRect()==new Rect2I(69,15,87,90),
       ("enemy_elite_stab_book","idle_00.png")=>frameSha=="5c44f7632f3a09e7b6a8d0b9930d0ee5a5759202fc28d2241e6c3c47fc4f2769"&&image.GetUsedRect()==new Rect2I(44,16,105,88),
       ("enemy_elite_stab_book","idle_01.png")=>frameSha=="e6bef063a300708f80a144d5847d1bd80189d012e39179f291a9be1116035b0e"&&image.GetUsedRect()==new Rect2I(44,16,105,88),
       ("enemy_elite_stab_book","idle_02.png")=>frameSha=="0e705ca2da7081fa06c48a586d822bc48e958335220d3c12fad136918d524bcd"&&image.GetUsedRect()==new Rect2I(44,16,106,88),
       ("enemy_elite_stab_book","idle_03.png")=>frameSha=="2e57c76b8e8354691b0ef2df41d77d10285b31ced3ab41f7b58f207ba09c7a13"&&image.GetUsedRect()==new Rect2I(44,16,106,88),
       ("enemy_elite_stab_book","idle_04.png")=>frameSha=="d8017e9066490b68e8f3abe430ed1ee13b683b2062a1c273c212f3a551b8519e"&&image.GetUsedRect()==new Rect2I(44,16,106,88),
       ("enemy_elite_stab_book","idle_05.png")=>frameSha=="a69cc87dacf10c3bfa90d22a3d2aef9f5b3605e296ec3cd4d0d3d0e4d581b380"&&image.GetUsedRect()==new Rect2I(44,16,105,88),
       ("enemy_elite_stab_book","idle_06.png")=>frameSha=="a8eeca1dbbd7f2529f5aee3c3f66622e487c04654499923a829522630e84b7c5"&&image.GetUsedRect()==new Rect2I(44,16,105,88),
       ("enemy_elite_stab_book","idle_07.png")=>frameSha=="3e35afd993a4251d6f920d64274869ce9ad30df91048cb46619cb22da386dce4"&&image.GetUsedRect()==new Rect2I(44,16,105,88),
       ("enemy_elite_stab_book","hurt.png")=>frameSha=="0ea2f17978a2c4d671c266bece91bc65027bdc1c44c07341172a183266d6b7ed"&&image.GetUsedRect()==new Rect2I(60,20,91,78),
       ("enemy_elite_stab_book","idle.png")=>frameSha=="b265d5bd1c51ba71c1f794c3027409aaa6f442df06fe8028f8f7a5303bc6628e"&&image.GetUsedRect()==new Rect2I(44,16,105,86),
       ("enemy_elite_stab_book","prepare.png")=>frameSha=="ea535ae5f8cf01ed4123303570c015b7fab46559cc5e8fe5a3f73e056ef949b5"&&image.GetUsedRect()==new Rect2I(63,17,84,86),
       ("enemy_elite_stab_book","rebound.png")=>frameSha=="b4e86704c13680487990c5dfdc1cf2f9fc95d6f8b3d3cd0b1cbc3a99a2582f10"&&image.GetUsedRect()==new Rect2I(55,19,95,84),
       ("enemy_elite_stab_book","recovery.png")=>frameSha=="ff00b83488fb0336775fc1b7bf5f0e83791842961229313b32104bd91c89da39"&&image.GetUsedRect()==new Rect2I(46,17,105,87),
       ("enemy_elite_stab_book","rest.png")=>frameSha=="5c44f7632f3a09e7b6a8d0b9930d0ee5a5759202fc28d2241e6c3c47fc4f2769"&&image.GetUsedRect()==new Rect2I(44,16,105,88),
       _=>false}))throw new InvalidOperationException("Unapproved M44 exact frame bytes or bounds");
      if(strictM45&&!((enemyId,file) switch{
       ("enemy_mud_lump","guardhold.png")=>frameSha=="ba7d470b8972d3280cc18649bb18525c80c2dd51b3dcde13bde19f59c6a0d1af"&&image.GetUsedRect()==new Rect2I(25,21,56,53),
       ("enemy_mud_lump","guardprepare.png")=>frameSha=="9c62d1b9e2fb55370eb1aef5d85400dd4d53eee34ba1f4f9737f3d49dd11f6e1"&&image.GetUsedRect()==new Rect2I(27,13,55,61),
       ("enemy_mud_lump","idle_00.png")=>frameSha=="21741ab1a4a1d7e981fc68a4d042c64e21911cb58d2b0b24133579a9d0e72efa"&&image.GetUsedRect()==new Rect2I(15,14,66,60),
       ("enemy_mud_lump","idle_01.png")=>frameSha=="9513c1c1410c8f3d414432adfd88a0319078665e4dd3ea803dbc00ace0383a82"&&image.GetUsedRect()==new Rect2I(15,13,66,61),
       ("enemy_mud_lump","idle_02.png")=>frameSha=="cdd15723730759175ae0e50ce4133e96575178e39853ad4310eb68b1a48c5c16"&&image.GetUsedRect()==new Rect2I(16,13,65,61),
       ("enemy_mud_lump","idle_03.png")=>frameSha=="ff8c74614340a585e2a340d1b00b89712b39c27ff2fe1488806f04fbe2842f32"&&image.GetUsedRect()==new Rect2I(16,13,65,61),
       ("enemy_mud_lump","idle_04.png")=>frameSha=="17bc65590961be4b8774f9284719674082887dc81c096148f8cc4856b41f19dc"&&image.GetUsedRect()==new Rect2I(16,14,66,60),
       ("enemy_mud_lump","idle_05.png")=>frameSha=="4d4ad9f160676fcc07b1b8a6f01f3ae4e2ff6272f3a3f1a110b08eb13d3075f6"&&image.GetUsedRect()==new Rect2I(15,14,67,60),
       ("enemy_mud_lump","idle_06.png")=>frameSha=="266cfef64b6b8297cacc8595f6bf3deb84a53efa0f674a2c883e9d4921bd2b87"&&image.GetUsedRect()==new Rect2I(15,14,66,60),
       ("enemy_mud_lump","idle_07.png")=>frameSha=="c2a2dc4ecc59918d2ca0c0fb02a0b73e398537ac6b9bff641f162aebd13cb640"&&image.GetUsedRect()==new Rect2I(14,14,67,60),
       ("enemy_mud_lump","hurt.png")=>frameSha=="765f28a4a90cb03be9125a0dea36c5bff77781cb101b463ccb4ea37e18ba6997"&&image.GetUsedRect()==new Rect2I(24,12,56,62),
       ("enemy_mud_lump","idle.png")=>frameSha=="60bcd11af91a83e7c665ca25fb32e3606b0a40ef2da6528e05f8075e9b51fd91"&&image.GetUsedRect()==new Rect2I(17,13,64,61),
       ("enemy_mud_lump","mudtosscontact.png")=>frameSha=="8d676b6719599af132bb2fa3e9479c8949fe26a59f74fb9e3daf8553c8022b24"&&image.GetUsedRect()==new Rect2I(4,21,76,53),
       ("enemy_mud_lump","mudtossprepare.png")=>frameSha=="72c6ca12d1363290eaebbd9b9929c3d4d971a67797427a787f28adc574c33698"&&image.GetUsedRect()==new Rect2I(12,13,68,61),
       ("enemy_mud_lump","recovery.png")=>frameSha=="24a5cc631b4bd4e74ff73bd169e9d6d8afdeb48b332b4989e89fbb3ab4aa7754"&&image.GetUsedRect()==new Rect2I(16,12,65,62),
       ("enemy_mud_lump","rest_canvas.png")=>frameSha=="21741ab1a4a1d7e981fc68a4d042c64e21911cb58d2b0b24133579a9d0e72efa"&&image.GetUsedRect()==new Rect2I(15,14,66,60),
       ("enemy_mud_lump","slamcontact.png")=>frameSha=="16e11533b672fa9667801b0c5aefaaf206d7f458579d9d4b849d958531db6c4c"&&image.GetUsedRect()==new Rect2I(7,27,73,47),
       ("enemy_mud_lump","slamprepare.png")=>frameSha=="1d817e5467a4785cac7002ddf09cfa9daaab400187c0c75b5fb52ce9ff61a637"&&image.GetUsedRect()==new Rect2I(24,5,58,69),
       ("enemy_mud_lump","slopcontact.png")=>frameSha=="24c5f939fb2777194af76ff89b40caa18327824708017e3614a36a2824e0456c"&&image.GetUsedRect()==new Rect2I(9,23,71,51),
       ("enemy_mud_lump","slopprepare.png")=>frameSha=="2fd25203345a39f0d863e9cdf796d1ec445697aa77f90a535710a29975af37ad"&&image.GetUsedRect()==new Rect2I(11,28,69,47),
       ("enemy_sludge_spider","guardhold.png")=>frameSha=="8eac886e3be847583b61fa9ff55ad19bafefb7c96d98f40e6232661f1dd89d9a"&&image.GetUsedRect()==new Rect2I(34,28,76,45),
       ("enemy_sludge_spider","guardprepare.png")=>frameSha=="62cda5df793fdf52e6dd5d01d1ce747be714f91640f916bd031959844c366110"&&image.GetUsedRect()==new Rect2I(27,23,84,50),
       ("enemy_sludge_spider","idle_00.png")=>frameSha=="b871902ce0fb6a23edc68dd117c3b7105b479a9e3401aadff4c3f35eb7dae062"&&image.GetUsedRect()==new Rect2I(20,18,87,55),
       ("enemy_sludge_spider","idle_01.png")=>frameSha=="7ff99a481628652d69dbcf9c13bc33d8de6688befc25f151e17973563fffe882"&&image.GetUsedRect()==new Rect2I(20,19,87,54),
       ("enemy_sludge_spider","idle_02.png")=>frameSha=="74e351b82594516e442c11b49272cf1ccbdf6bd0fecba997c3ee177cc94001d2"&&image.GetUsedRect()==new Rect2I(20,19,87,54),
       ("enemy_sludge_spider","idle_03.png")=>frameSha=="d541b3982613b719e6a2e941b23e9ba78c4f3dac444ef9326c7a79d865918ee1"&&image.GetUsedRect()==new Rect2I(20,19,87,54),
       ("enemy_sludge_spider","idle_04.png")=>frameSha=="be6b77e43dccf2623126f2abeb9a8ea30b3ac86434f1e22355f71a5f82a6090c"&&image.GetUsedRect()==new Rect2I(20,19,87,54),
       ("enemy_sludge_spider","idle_05.png")=>frameSha=="61e509c07ea65362de96d169b92577cf295dc712ab7b853732ec21665b1a1c86"&&image.GetUsedRect()==new Rect2I(20,18,87,55),
       ("enemy_sludge_spider","idle_06.png")=>frameSha=="d1a149359fe0d562c3ba2749d405deb5b77c116940981b98d7afa1fd09e66149"&&image.GetUsedRect()==new Rect2I(20,18,87,55),
       ("enemy_sludge_spider","idle_07.png")=>frameSha=="fe3ec0e03e5f79f6a7e689b07df7ac7e526eb9ee32bb2daeba325b4bbcafbcc3"&&image.GetUsedRect()==new Rect2I(20,18,87,55),
       ("enemy_sludge_spider","hurt.png")=>frameSha=="2bb0912ffd145166ae40d2c64bcf30222f162b7473ee06ad65e6fe806f33bf7b"&&image.GetUsedRect()==new Rect2I(33,24,79,49),
       ("enemy_sludge_spider","idle.png")=>frameSha=="6cba302334fe943a5c2592aeef38adb86657b9b9122fa10ac876ca6219fc460a"&&image.GetUsedRect()==new Rect2I(22,16,89,57),
       ("enemy_sludge_spider","oil_sprayprepare.png")=>frameSha=="21e51f94c5e8a8ab6a2bc3c81b406338a70bd1038ad0d70d297a5475a47374d7"&&image.GetUsedRect()==new Rect2I(19,15,93,58),
       ("enemy_sludge_spider","oil_sprayrelease.png")=>frameSha=="6b0db363c1816910bd4669bac4c3eba6970a7e28e83f7001b67593e24af70731"&&image.GetUsedRect()==new Rect2I(15,17,100,56),
       ("enemy_sludge_spider","ragecontact.png")=>frameSha=="a6736abe6c3c156753026d39f09eb5d176a484023e7bac9776f0c1cc0ace24cb"&&image.GetUsedRect()==new Rect2I(5,23,108,50),
       ("enemy_sludge_spider","rageprepare.png")=>frameSha=="d983e1e42efa021a310dbd94f29557165a2b5bfd36a47be8b6f3b930f8b3d1ff"&&image.GetUsedRect()==new Rect2I(13,22,95,51),
       ("enemy_sludge_spider","recovery.png")=>frameSha=="bd43af59b852ba555c1fe67b5e3e8a2b5afe507e852f926cc379fc250c678ef3"&&image.GetUsedRect()==new Rect2I(21,18,91,55),
       ("enemy_sludge_spider","rest.png")=>frameSha=="b871902ce0fb6a23edc68dd117c3b7105b479a9e3401aadff4c3f35eb7dae062"&&image.GetUsedRect()==new Rect2I(20,18,87,55),
       ("enemy_sludge_spider","slamcontact.png")=>frameSha=="af8364b49e4ce7cba768ff5797f2b1432cd058db7cb0bbc58b0891cfd3b8bf55"&&image.GetUsedRect()==new Rect2I(12,22,100,51),
       ("enemy_sludge_spider","slamprepare.png")=>frameSha=="c92dba99d67acd04c26b48d603844604435c45a08c358903bcfcfa2cff6c58e2"&&image.GetUsedRect()==new Rect2I(19,6,93,67),
       _=>false}))throw new InvalidOperationException("Unapproved M45 exact frame bytes or bounds");
      if(strictM46&&!((enemyId,file) switch{
       ("enemy_whetstone_colossus","bracehold.png")=>frameSha=="243ac69eb00f3538966222663de92e6e0018ac78fdde49d6949b4f2b778cb153"&&image.GetUsedRect()==new Rect2I(52,18,56,104),
       ("enemy_whetstone_colossus","braceprepare.png")=>frameSha=="695cdc749c61f5e6dea7452cfa95e602353eb9b6063ca525b31c63946bc455ec"&&image.GetUsedRect()==new Rect2I(52,15,56,107),
       ("enemy_whetstone_colossus","guillotinecontact.png")=>frameSha=="29a54d550aa04b028311cc6a1e51a0a23891ef9101320a7949ab06d053acb2b5"&&image.GetUsedRect()==new Rect2I(4,33,105,89),
       ("enemy_whetstone_colossus","guillotineprepare.png")=>frameSha=="0b3d77d6ff7af4b98eeb7de7276406fb012674b83f0b321fff621a1ec6412594"&&image.GetUsedRect()==new Rect2I(52,4,64,118),
       ("enemy_whetstone_colossus","guillotinerecovery.png")=>frameSha=="7e031bec05b55732950b00dc25cf9dfeb357c49f70c7e7348729467c116888a8"&&image.GetUsedRect()==new Rect2I(31,29,77,93),
       ("enemy_whetstone_colossus","honehold.png")=>frameSha=="f02f6f8a15dbc9540df7dc81ff6755bdfc64393d15097b9d4e6e2f554f13f3e6"&&image.GetUsedRect()==new Rect2I(25,32,90,90),
       ("enemy_whetstone_colossus","honeprepare.png")=>frameSha=="c0dbe877a566836712c260cfc34928702bb72a8bc454d51f95d6f23903f884e9"&&image.GetUsedRect()==new Rect2I(42,30,69,92),
       ("enemy_whetstone_colossus","idle_00.png")=>frameSha=="9c6ae4526d8084198eed9fb0974bfeb4779557181a4748aa95af199c2f7a27dc"&&image.GetUsedRect()==new Rect2I(38,26,68,96),
       ("enemy_whetstone_colossus","idle_01.png")=>frameSha=="19c80d629ab812c61f3a77b7022d6f00bbd06cbb6c7f8d0061e721f8da43a362"&&image.GetUsedRect()==new Rect2I(38,27,68,95),
       ("enemy_whetstone_colossus","idle_02.png")=>frameSha=="e6607a7a342eaa35fd6ef0d0073365895ce48f40c7e060322d58b815f18bb292"&&image.GetUsedRect()==new Rect2I(38,27,68,95),
       ("enemy_whetstone_colossus","idle_03.png")=>frameSha=="6e9596a83363c895f1cc496a0f71d9344f675224a36c7b82a9bc0804dde2e4d9"&&image.GetUsedRect()==new Rect2I(38,27,68,95),
       ("enemy_whetstone_colossus","idle_04.png")=>frameSha=="5a0770f044cf73535e56e33e8352f80132bd040fc95d0509a43aa019ef4cc7b9"&&image.GetUsedRect()==new Rect2I(38,27,68,95),
       ("enemy_whetstone_colossus","idle_05.png")=>frameSha=="9ae1ab8f7f20785996cf5b20f4111c83db5c0cc26c28d582f38360fb869ceca2"&&image.GetUsedRect()==new Rect2I(38,26,68,96),
       ("enemy_whetstone_colossus","idle_06.png")=>frameSha=="95af894d8d9b2daa161428cf109c41d3650971b3619497e5ef656b3b66292df7"&&image.GetUsedRect()==new Rect2I(38,26,68,96),
       ("enemy_whetstone_colossus","idle_07.png")=>frameSha=="f30690a7334a4c12a88bba8e83bf24911554f1447b7bc6337ce21c54ed27193f"&&image.GetUsedRect()==new Rect2I(38,26,68,96),
       ("enemy_whetstone_colossus","hurt.png")=>frameSha=="c9c1a1cde74787164af6b5a2407ef408eea0610785e9d22739d3ea1c01d4d206"&&image.GetUsedRect()==new Rect2I(39,32,69,90),
       ("enemy_whetstone_colossus","idle.png")=>frameSha=="8312ac4ee3aaf71f6621046251e0d0ca068b5cc7ddb3fca8fcd6feff01837d91"&&image.GetUsedRect()==new Rect2I(37,26,70,96),
       ("enemy_whetstone_colossus","rest_canvas.png")=>frameSha=="9c6ae4526d8084198eed9fb0974bfeb4779557181a4748aa95af199c2f7a27dc"&&image.GetUsedRect()==new Rect2I(38,26,68,96),
       ("enemy_iron_clam","guardhold.png")=>frameSha=="2f3a994f773275d57233e9c376c6b72927dfd1c8612cc0e4fe641116e776b8e6"&&image.GetUsedRect()==new Rect2I(21,33,73,41),
       ("enemy_iron_clam","guardprepare.png")=>frameSha=="ce1e4cc12c2933b8b3febc03772d952516a9eaf841aba589c6cf8563475acde2"&&image.GetUsedRect()==new Rect2I(26,31,68,43),
       ("enemy_iron_clam","idle_00.png")=>frameSha=="fab9f40bfab25c48c7b788f006f0f0a1aad9a9436cc1570ee11f25f3650e91f2"&&image.GetUsedRect()==new Rect2I(17,26,77,48),
       ("enemy_iron_clam","idle_01.png")=>frameSha=="5d3ba5d13fbc8d54f330f51eeb2b1580b5a3e8d6626140d8bfd0dd4db9142d1e"&&image.GetUsedRect()==new Rect2I(17,25,77,49),
       ("enemy_iron_clam","idle_02.png")=>frameSha=="2311d5394e0d1d05d7ee65a75551046870137b3e285c02b4bf71ba3f52b54a07"&&image.GetUsedRect()==new Rect2I(16,24,78,50),
       ("enemy_iron_clam","idle_03.png")=>frameSha=="5e2990a48629660e14cac631482ba178cf92801480e7b33692d1935e9317c1ba"&&image.GetUsedRect()==new Rect2I(16,23,78,51),
       ("enemy_iron_clam","idle_04.png")=>frameSha=="e991d25d7c1c86d1cb589246aa636dfe9af7abbd53916b4adb81cc67110e1661"&&image.GetUsedRect()==new Rect2I(17,23,77,51),
       ("enemy_iron_clam","idle_05.png")=>frameSha=="c85e27b66cef3c8f3aee300593ae2f1355329d581f67a009eab80e2b85e671e6"&&image.GetUsedRect()==new Rect2I(18,24,76,50),
       ("enemy_iron_clam","idle_06.png")=>frameSha=="3a7f5222cc78c34b8c783e37ebafed2fbace832cf0ef0f2e5f2ad33d63ef176c"&&image.GetUsedRect()==new Rect2I(18,25,76,49),
       ("enemy_iron_clam","idle_07.png")=>frameSha=="e20175140a0f9ebcc8b25172559b00d5a05a93bd0e7ba3452f0806e4daa1c1b1"&&image.GetUsedRect()==new Rect2I(17,26,77,48),
       ("enemy_iron_clam","hurt.png")=>frameSha=="7c6d92584d6f64f8c559c84adb76a07f76b7affd7ae1ec9f9676f5b7f0fc41ba"&&image.GetUsedRect()==new Rect2I(21,32,73,42),
       ("enemy_iron_clam","idle.png")=>frameSha=="e011553554912bfe0522c710389082ad9251be4bdfe64ea367607c999ef15ecf"&&image.GetUsedRect()==new Rect2I(16,23,78,51),
       ("enemy_iron_clam","jetprepare.png")=>frameSha=="6834743a05c0df36a3360a199067a712c1d2c1453dd9e77a14a9fe0398cccf45"&&image.GetUsedRect()==new Rect2I(22,31,72,43),
       ("enemy_iron_clam","jetrecovery.png")=>frameSha=="351ad50b2fe2e54746d05d467b013c5a503c2c76a50780440b141411b081f6bc"&&image.GetUsedRect()==new Rect2I(17,24,77,50),
       ("enemy_iron_clam","jetrelease.png")=>frameSha=="ed8fbfc969e3b959a3c6d0c685dbf3be40527769c022259b6b943660e84cf8e6"&&image.GetUsedRect()==new Rect2I(10,15,84,59),
       ("enemy_iron_clam","pressurizehold.png")=>frameSha=="b7387989d54e1a56e9fefd9d904b0abdf0a1f3dd47065cb5552e72b5120e74eb"&&image.GetUsedRect()==new Rect2I(24,33,70,41),
       ("enemy_iron_clam","pressurizeprepare.png")=>frameSha=="031519f8442dfe4d59d1cccfd939bf962fe9018f40dfb1f0f46fca274a9bc9a8"&&image.GetUsedRect()==new Rect2I(24,31,70,43),
       ("enemy_iron_clam","rest.png")=>frameSha=="fab9f40bfab25c48c7b788f006f0f0a1aad9a9436cc1570ee11f25f3650e91f2"&&image.GetUsedRect()==new Rect2I(17,26,77,48),
       _=>false}))throw new InvalidOperationException("Unapproved M46 exact frame bytes or bounds");
      if(strictM47&&!((enemyId,file) switch{
       ("enemy_elite_giant_face","crushcontact.png")=>frameSha=="533382cefc1d60ca1a96f5e246032f099e879a2f2f679e40f5f07b885ebd0703"&&image.GetUsedRect()==new Rect2I(31,18,77,86),
       ("enemy_elite_giant_face","crushprepare.png")=>frameSha=="5ce7c896c48e2be55d5f99fcf9130fc639a2144af01fd1df76d6585f2870024e"&&image.GetUsedRect()==new Rect2I(33,14,75,90),
       ("enemy_elite_giant_face","crushrecovery.png")=>frameSha=="6dbea5b327845b430eeb2ec33f0d513a901941018b4275420c444fdb2f56849c"&&image.GetUsedRect()==new Rect2I(32,17,76,87),
       ("enemy_elite_giant_face","glarehold.png")=>frameSha=="249eabbb1948421ed905cc5a626545909db9cfc1c28e7a8a3b1a0c121014f754"&&image.GetUsedRect()==new Rect2I(32,16,76,88),
       ("enemy_elite_giant_face","glareprepare.png")=>frameSha=="07e2e8a05981b20e6c99c6fd92312052fe229815c7355aad5d5bddb94fa7469d"&&image.GetUsedRect()==new Rect2I(31,17,77,87),
       ("enemy_elite_giant_face","guardhold.png")=>frameSha=="d4f0526f5380adb33e0ed1ee5f1857ac9df9ab5b4714fe467af0c8b371398c8b"&&image.GetUsedRect()==new Rect2I(31,17,77,87),
       ("enemy_elite_giant_face","guardprepare.png")=>frameSha=="5485e51fa0c33627ffd683d8b7cacae7624c89676e33a98eae0b5fe3a5237fa3"&&image.GetUsedRect()==new Rect2I(32,17,76,87),
       ("enemy_elite_giant_face","idle_00.png")=>frameSha=="324fb32f363795ee58f10c71730348d94f6d9c61c242d6f2c88c2848387ef94e"&&image.GetUsedRect()==new Rect2I(35,16,73,88),
       ("enemy_elite_giant_face","idle_01.png")=>frameSha=="28f59ce242451ed3e5368662d636c7d13706a470d404e99d2897f085048354c0"&&image.GetUsedRect()==new Rect2I(34,15,74,88),
       ("enemy_elite_giant_face","idle_02.png")=>frameSha=="8cad1a5720b6ced6e039278c5ff831b3ff41ea34433c4550904aaef2e7b99bb7"&&image.GetUsedRect()==new Rect2I(34,14,74,88),
       ("enemy_elite_giant_face","idle_03.png")=>frameSha=="7587b54a728605213a4d904d7782f1f19c670cd6e9b248ef5f3ff597fb6c5b8f"&&image.GetUsedRect()==new Rect2I(33,14,75,88),
       ("enemy_elite_giant_face","idle_04.png")=>frameSha=="823be85259c430078504ca38a1aadf0ddc603686bf59836a39791ec11a846d6d"&&image.GetUsedRect()==new Rect2I(33,15,74,88),
       ("enemy_elite_giant_face","idle_05.png")=>frameSha=="edb67d0fb2f373c95ec4105acc6f80b7d69209d06ad88148a1cd13776fcd3f94"&&image.GetUsedRect()==new Rect2I(34,16,73,88),
       ("enemy_elite_giant_face","idle_06.png")=>frameSha=="b90c71e9f99e5c07edf38f5ed807d980f7a3127b7063de6114ea96266884e68b"&&image.GetUsedRect()==new Rect2I(35,17,73,88),
       ("enemy_elite_giant_face","idle_07.png")=>frameSha=="04723ffd395f260e6b7485c3bf9b5650186c23711fbae5c4b018ab3a885a6070"&&image.GetUsedRect()==new Rect2I(35,16,73,88),
       ("enemy_elite_giant_face","hurt.png")=>frameSha=="7cd1e9ed09cdb75971ee5d33d40c464607718dd9420a552f77668e64689d89c5"&&image.GetUsedRect()==new Rect2I(32,19,76,85),
       ("enemy_elite_giant_face","idle.png")=>frameSha=="5dc2aa3df3de1ac387119c512316bc161ce1345de910c404b8c5c0e8c3d0bc8e"&&image.GetUsedRect()==new Rect2I(32,18,76,86),
       ("enemy_elite_giant_face","rest.png")=>frameSha=="324fb32f363795ee58f10c71730348d94f6d9c61c242d6f2c88c2848387ef94e"&&image.GetUsedRect()==new Rect2I(35,16,73,88),
       ("enemy_elite_doom_chanter","chantcontact1.png")=>frameSha=="0eb6aa4b57b6e215c6c7346bbbc86bc9f9f50ce1a84112f4262b901569e04c8b"&&image.GetUsedRect()==new Rect2I(32,15,80,89),
       ("enemy_elite_doom_chanter","chantcontact2.png")=>frameSha=="bd42624b2c7f9f2d9b6751185502fbab8832f85770a0b9da3fca5e31244b9f8a"&&image.GetUsedRect()==new Rect2I(30,26,81,78),
       ("enemy_elite_doom_chanter","chantprepare.png")=>frameSha=="3720a4e2214a949e9da55c9bc1fa63b2cc8534e7c836f4cded06accad4fc2826"&&image.GetUsedRect()==new Rect2I(36,22,71,82),
       ("enemy_elite_doom_chanter","chantrebound.png")=>frameSha=="8917e1bff5229328da2529e8cf10f5859d3a3de37a6af7235db65a58bb1406ac"&&image.GetUsedRect()==new Rect2I(32,16,78,88),
       ("enemy_elite_doom_chanter","crescendoprepare.png")=>frameSha=="7ac74c4a7aae211bef1869d055f864cbd24e4efbce0da6a92053cb02c64ed075"&&image.GetUsedRect()==new Rect2I(34,23,76,81),
       ("enemy_elite_doom_chanter","crescendopulse.png")=>frameSha=="e21527df5364dc66ce03b90ff7c53ce5f56b1f5450cd0f440457a8f462a01940"&&image.GetUsedRect()==new Rect2I(33,16,77,88),
       ("enemy_elite_doom_chanter","guardhold.png")=>frameSha=="fdaa065b52c0b0c27d3f393faff6cdfaa0b8b51721b7de9f5bcd94cbd98228bc"&&image.GetUsedRect()==new Rect2I(33,29,77,75),
       ("enemy_elite_doom_chanter","guardprepare.png")=>frameSha=="21efaa8e46ca3ea5299b8f42b765a9ea175b5d7114d23745958098b66da655ed"&&image.GetUsedRect()==new Rect2I(34,27,76,77),
       ("enemy_elite_doom_chanter","idle_00.png")=>frameSha=="d1e86e2f39b4b1df4016c50a6e5bd97cf1f402b423e9b0c863351e6fd881695e"&&image.GetUsedRect()==new Rect2I(35,16,73,88),
       ("enemy_elite_doom_chanter","idle_01.png")=>frameSha=="0809c82b91932e35fd58ef0e52e0c0c96646000c14c82f464b9e4120033398a3"&&image.GetUsedRect()==new Rect2I(35,15,73,89),
       ("enemy_elite_doom_chanter","idle_02.png")=>frameSha=="fa45f03d34f1f32365ae8fa5a2f846eb53e450e484fcb11c352c22e7ea6bc07a"&&image.GetUsedRect()==new Rect2I(35,15,73,89),
       ("enemy_elite_doom_chanter","idle_03.png")=>frameSha=="79497e20ffc7f48342ed88e9680bda4a9c9f5550fd140da7e7c4171ce0f0e14f"&&image.GetUsedRect()==new Rect2I(35,15,73,89),
       ("enemy_elite_doom_chanter","idle_04.png")=>frameSha=="a537e92ba710a4fbcdede5e557fa6e55cc284b4cf722cb35b55a5b3b79789fb6"&&image.GetUsedRect()==new Rect2I(35,16,73,88),
       ("enemy_elite_doom_chanter","idle_05.png")=>frameSha=="bd832497bf9a72d57728de7dd4ae823b9c1575e6a66f565f359de9919e66b0d9"&&image.GetUsedRect()==new Rect2I(34,16,75,88),
       ("enemy_elite_doom_chanter","idle_06.png")=>frameSha=="1e7a7641526f349aa5110c0d99ab74895177f3c4b5a9241a1a5dd942c2c9f331"&&image.GetUsedRect()==new Rect2I(34,16,75,88),
       ("enemy_elite_doom_chanter","idle_07.png")=>frameSha=="9fba4dbd95e5e0b9228e9d24916b73f84daaa9ef24ff3eb59f11e8732e14112b"&&image.GetUsedRect()==new Rect2I(35,16,74,88),
       ("enemy_elite_doom_chanter","hurt.png")=>frameSha=="108bc06eaeaf1909c31d3198dac72739286d5c86353d4c7e11d3470ac17ba899"&&image.GetUsedRect()==new Rect2I(34,22,74,82),
       ("enemy_elite_doom_chanter","idle.png")=>frameSha=="af690a403d0571c5fd6f6c5e71e9ee67a7718967ed4c3330cd6d93374d266930"&&image.GetUsedRect()==new Rect2I(34,19,75,85),
       ("enemy_elite_doom_chanter","rest.png")=>frameSha=="d1e86e2f39b4b1df4016c50a6e5bd97cf1f402b423e9b0c863351e6fd881695e"&&image.GetUsedRect()==new Rect2I(35,16,73,88),
       _=>false}))throw new InvalidOperationException("Unapproved M47 exact frame bytes or floating bounds");
      if(strictM48&&!((enemyId,file) switch{
       ("enemy_axe_automaton","axecontact.png")=>frameSha=="0470055df599a9a3f5ca25373d532f641323400aeec8c2d6572eb6ef504d9753"&&image.GetUsedRect()==new Rect2I(8,35,110,61),
       ("enemy_axe_automaton","fistcontact.png")=>frameSha=="15342926cf1812425e2376439712c53b23c9e9ba487cd7537b1ada534b09bb58"&&image.GetUsedRect()==new Rect2I(19,35,108,61),
       ("enemy_axe_automaton","guardhold.png")=>frameSha=="b869087a508d082d9d2a5efd351d62758069c4605ad39965dbedc19981942311"&&image.GetUsedRect()==new Rect2I(52,29,66,67),
       ("enemy_axe_automaton","guardprepare.png")=>frameSha=="5ba611dd98f4402f13efed87c359acc5bc19b1f42501da5df6e84f3a91c7f999"&&image.GetUsedRect()==new Rect2I(45,29,75,67),
       ("enemy_axe_automaton","idle_00.png")=>frameSha=="24a64457b9117aa8c256c7267806f90c86f5a7a9ecc880e3d1da2eabe756f8cd"&&image.GetUsedRect()==new Rect2I(15,32,106,64),
       ("enemy_axe_automaton","idle_01.png")=>frameSha=="6632169b2e08f4133c5240e9cbc73e6bc8f0bedf580c586f69c880a1197a43f0"&&image.GetUsedRect()==new Rect2I(15,31,106,65),
       ("enemy_axe_automaton","idle_02.png")=>frameSha=="bf3e1a2db183654d384b421d71ad6794c353476e8c16c5042447fc646f16973d"&&image.GetUsedRect()==new Rect2I(15,31,106,65),
       ("enemy_axe_automaton","idle_03.png")=>frameSha=="a4718d6547eed25348fe178395e8c7c10b2512126cf200d04f0e1fd5aaa70cb5"&&image.GetUsedRect()==new Rect2I(15,30,106,66),
       ("enemy_axe_automaton","idle_04.png")=>frameSha=="f4163f66df66c9879d10533219c50259b48864e3531d1770e013c91eaabd31e1"&&image.GetUsedRect()==new Rect2I(15,30,106,66),
       ("enemy_axe_automaton","idle_05.png")=>frameSha=="39b86669e8e27b1f471d2bfb40af94c90df2a90da229b883655a5e91563563e5"&&image.GetUsedRect()==new Rect2I(15,31,106,65),
       ("enemy_axe_automaton","idle_06.png")=>frameSha=="ac0f08554d6d23154eb0b5cb650c7911466fd69370bb4487a4a77cb308e1171b"&&image.GetUsedRect()==new Rect2I(15,31,106,65),
       ("enemy_axe_automaton","idle_07.png")=>frameSha=="dbe0ffad2ae99a71080123a06e14f3fc14c97fbb6487be1871cd4a1c082544f7"&&image.GetUsedRect()==new Rect2I(15,32,106,64),
       ("enemy_axe_automaton","hurt.png")=>frameSha=="13e6d0274b0da62e852aad77b88ee269ea6153a813f56e3a407c30d72ef87d4a"&&image.GetUsedRect()==new Rect2I(37,34,80,62),
       ("enemy_axe_automaton","idle.png")=>frameSha=="37420b90ed3c9a1ffe22a1ae31848107484262f5bb1d66d0f6a6b9d854320e94"&&image.GetUsedRect()==new Rect2I(17,29,102,67),
       ("enemy_axe_automaton","recovery.png")=>frameSha=="d4880c4fa0caf0f6df531e792799636bf313af01d191207b7b427479ce7214d0"&&image.GetUsedRect()==new Rect2I(17,31,104,65),
       ("enemy_axe_automaton","rest.png")=>frameSha=="c54ef58c38e9e0a68f52d9807d234f7c02bf460c7df8e1f8e247d7cbd5b493d0"&&image.GetUsedRect()==new Rect2I(15,16,106,64),
       ("enemy_axe_automaton","rest_canvas.png")=>frameSha=="24a64457b9117aa8c256c7267806f90c86f5a7a9ecc880e3d1da2eabe756f8cd"&&image.GetUsedRect()==new Rect2I(15,32,106,64),
       ("enemy_axe_automaton","sharpenprepare.png")=>frameSha=="aa4ce9e117ff06da14d7be72ea8db61d0007d4e1f555df45abd95dc519927de8"&&image.GetUsedRect()==new Rect2I(49,27,71,69),
       ("enemy_axe_automaton","sharpenpulse.png")=>frameSha=="32644b26a8576a9ca0c330ebc449bd4be86b210cabdc1839791be7aeeb1c8219"&&image.GetUsedRect()==new Rect2I(42,28,77,68),
       ("enemy_axe_automaton","twoprepare.png")=>frameSha=="9480c2ae3b32303d08fc7969c9a95ab3a44acb16af34a33f0f0b17f3a973d711"&&image.GetUsedRect()==new Rect2I(50,4,69,92),
       ("enemy_axe_automaton","tworebound.png")=>frameSha=="f5b036caa5dec702763c2bef9656434849d814da1ceb322bff9159ae85ca9ece"&&image.GetUsedRect()==new Rect2I(27,33,93,63),
       ("enemy_axe_automaton","uppercontact.png")=>frameSha=="4ee8823c15817b3eeacb4916355bb4bd632082895ea0a025cbd6b1e21ac919b2"&&image.GetUsedRect()==new Rect2I(37,15,91,81),
       ("enemy_axe_automaton","upperprepare.png")=>frameSha=="1e735d7a1f75eaa4268d2a75a799ca78efe1ca413ba728e9ff5630bf2e18d13f"&&image.GetUsedRect()==new Rect2I(46,35,84,61),
       ("enemy_biting_scroll","chewcontact1.png")=>frameSha=="2c2cfa3e82c33fa8e5db487260a7341548a3b8c5b200e03df2aa4b0b3e07f4f4"&&image.GetUsedRect()==new Rect2I(30,22,48,36),
       ("enemy_biting_scroll","chewcontact2.png")=>frameSha=="0402caef0cb55b00801f97fa9b717c202432d4eb0b5da2100244fa6121452d4b"&&image.GetUsedRect()==new Rect2I(29,24,49,34),
       ("enemy_biting_scroll","chewprepare.png")=>frameSha=="f7253b2fe8707577475d80b6ddac3e0ddcf08991d8806bd38b4100b893a1a52f"&&image.GetUsedRect()==new Rect2I(28,12,50,46),
       ("enemy_biting_scroll","chewrebound.png")=>frameSha=="08407244ceaed2282eb1305a43de6b3bde6f59d9662152dbf9f75d7608a608a5"&&image.GetUsedRect()==new Rect2I(28,14,50,44),
       ("enemy_biting_scroll","chompcontact.png")=>frameSha=="d474a05aca810936a5684fc54ff19c074f9538ac7ef79e7168605b6a3d930365"&&image.GetUsedRect()==new Rect2I(28,20,50,38),
       ("enemy_biting_scroll","chompprepare.png")=>frameSha=="8859d6ed438a2d1f6300637379b3c782856cbf26009c4b40aaaf4310bf695bb1"&&image.GetUsedRect()==new Rect2I(25,4,53,54),
       ("enemy_biting_scroll","guardhold.png")=>frameSha=="06fba3ee35529f4d6cc50aa79e2da1f7f45bd1746922f0e4952439237daeb45a"&&image.GetUsedRect()==new Rect2I(31,18,47,40),
       ("enemy_biting_scroll","guardprepare.png")=>frameSha=="900f693eb42c0036a8bf9a779152a33948280d4051da890998645c569c7ebba3"&&image.GetUsedRect()==new Rect2I(30,16,48,42),
       ("enemy_biting_scroll","idle_00.png")=>frameSha=="d653a1ae11dec1696fe5360c7de869cd7f34b19bb4111f9fe7101511cffd99c8"&&image.GetUsedRect()==new Rect2I(27,14,51,44),
       ("enemy_biting_scroll","idle_01.png")=>frameSha=="011bbdc5f681597d7f72efba14fdecfb66bc19d05b2bd77fa00959733c32a309"&&image.GetUsedRect()==new Rect2I(27,13,51,45),
       ("enemy_biting_scroll","idle_02.png")=>frameSha=="661747995a406354afa199685b5859ebbfcba1d0a9a9eaa3b0f4b89558b1f2f0"&&image.GetUsedRect()==new Rect2I(27,13,51,46),
       ("enemy_biting_scroll","idle_03.png")=>frameSha=="c40f16535d7b81fa0acd17c82dbb6f3dc886da137fedd8ab22e103ce0965b287"&&image.GetUsedRect()==new Rect2I(27,12,51,47),
       ("enemy_biting_scroll","idle_04.png")=>frameSha=="e6ed8ba913f900dec695e67f09b26c976963dba3270824bd39aa70c5b71828db"&&image.GetUsedRect()==new Rect2I(27,12,51,46),
       ("enemy_biting_scroll","idle_05.png")=>frameSha=="ee101a7ecb4c09c0006ed1ea86c28f605dc262c6dc338eb96651bdedb280f951"&&image.GetUsedRect()==new Rect2I(27,13,51,44),
       ("enemy_biting_scroll","idle_06.png")=>frameSha=="ec1f5abb30cfcc627ef1da99de4d2056b9137dc7d8579fafdae56ef23e660b8a"&&image.GetUsedRect()==new Rect2I(27,14,51,43),
       ("enemy_biting_scroll","idle_07.png")=>frameSha=="f5e90b0aa710cfc40c4c143d4e613edad088765dcc893497a66952fd20796ecf"&&image.GetUsedRect()==new Rect2I(27,14,51,44),
       ("enemy_biting_scroll","hurt.png")=>frameSha=="008089006ccab7449ada308934c47723ad5597e04892cf998c9bf971294e6848"&&image.GetUsedRect()==new Rect2I(29,18,49,40),
       ("enemy_biting_scroll","idle.png")=>frameSha=="3c1f5064f616491d8240fcb011a76fffe69d445058e51997e5e3b47f28b0cfd6"&&image.GetUsedRect()==new Rect2I(26,13,52,45),
       ("enemy_biting_scroll","recovery.png")=>frameSha=="21a2a660809378bda61596e7be4d749ca01572e39df8f84c07787f4087c95988"&&image.GetUsedRect()==new Rect2I(26,13,52,45),
       ("enemy_biting_scroll","rest.png")=>frameSha=="d653a1ae11dec1696fe5360c7de869cd7f34b19bb4111f9fe7101511cffd99c8"&&image.GetUsedRect()==new Rect2I(27,14,51,44),
       ("enemy_biting_scroll","teethprepare.png")=>frameSha=="fe4dd3c78f644d4d97d3b1643afd534963d0f4aad863dc815136dc8cd2d480a1"&&image.GetUsedRect()==new Rect2I(24,13,54,45),
       ("enemy_biting_scroll","teethpulse.png")=>frameSha=="2dcdab1d71e04011e121ecf2e63941e8a1895e0cf61d2a12a24e55c809f6c1a3"&&image.GetUsedRect()==new Rect2I(25,11,53,47),
       _=>false}))throw new InvalidOperationException("Unapproved M48 exact frame bytes or floating bounds");
      if(strictM49&&!((enemyId,file) switch{
       ("enemy_slug","guardhold.png")=>frameSha=="2e96794a95b3764e942a04487547fb7796eddeb65e59c59cfb613f6a78f5dda1"&&image.GetUsedRect()==new Rect2I(29,33,147,41),
       ("enemy_slug","guardprepare.png")=>frameSha=="7c30e6a22aabd00c850a4dce4dfec0de65066363e429df7cc3d25ac79c08609c"&&image.GetUsedRect()==new Rect2I(39,32,137,42),
       ("enemy_slug","idle_00.png")=>frameSha=="463e07d84c97146079283e533c58bbe3bddf3296e39f5f29fb75b1ca54a9cb03"&&image.GetUsedRect()==new Rect2I(36,26,140,48),
       ("enemy_slug","idle_01.png")=>frameSha=="d07dd6390bed7f6f9fa522a2f0f72547a7a98db7a6a050231e57bdd61cae9eb1"&&image.GetUsedRect()==new Rect2I(36,25,140,49),
       ("enemy_slug","idle_02.png")=>frameSha=="a914909b300ba88c8a9927e1993d56316c75a543f201638ecd07d7ae60d27f00"&&image.GetUsedRect()==new Rect2I(36,25,140,49),
       ("enemy_slug","idle_03.png")=>frameSha=="4fa38cf9b57c5c0775d0c83c28de45535d45b813e7fd8487d6d35779aa8502be"&&image.GetUsedRect()==new Rect2I(36,25,140,49),
       ("enemy_slug","idle_04.png")=>frameSha=="24b7862954ef85d73612cf43a1d930515002fa615d8089cf65ee7d8da102ee0a"&&image.GetUsedRect()==new Rect2I(36,25,140,49),
       ("enemy_slug","idle_05.png")=>frameSha=="29f4c0de73eba2649542b34ea36ec01111337b55e6976a4a9d2cf40a0781f303"&&image.GetUsedRect()==new Rect2I(36,26,140,48),
       ("enemy_slug","idle_06.png")=>frameSha=="83e93886158f36fff10b837648979d38185311b351a89a43ba96c20608685cf4"&&image.GetUsedRect()==new Rect2I(36,26,140,48),
       ("enemy_slug","idle_07.png")=>frameSha=="72285822514d3830c9041030c63f870d82d228052ce639c0c0f3a98b96b3f751"&&image.GetUsedRect()==new Rect2I(36,26,140,48),
       ("enemy_slug","hurt.png")=>frameSha=="f5b2f5219ccd16be568b665d49c8026e76a9ca06dd3a14cf81147406ab7f6232"&&image.GetUsedRect()==new Rect2I(37,30,139,44),
       ("enemy_slug","idle.png")=>frameSha=="f1552ad63928754764245fe2421ad1fe13d9559b2b33d1c4188442138332724d"&&image.GetUsedRect()==new Rect2I(36,22,140,52),
       ("enemy_slug","lickcontact.png")=>frameSha=="2b69240c23fae78d5b83ca69829a166026416b2dbf40bcb799dafd02113c4912"&&image.GetUsedRect()==new Rect2I(10,27,166,47),
       ("enemy_slug","lickprepare.png")=>frameSha=="3174a3ed4d8d647a55d1a60eef7f3a5ba4c63281eb8c5617c2da172db2eeb2d3"&&image.GetUsedRect()==new Rect2I(37,27,139,47),
       ("enemy_slug","recovery.png")=>frameSha=="8ae2dc831d5cabc13bfa74efa0e15dd69d85c13113b307ebfcf3a3e89a33ffa2"&&image.GetUsedRect()==new Rect2I(34,23,142,51),
       ("enemy_slug","rest.png")=>frameSha=="e1bfcb9620f6cd954d824e7344f0b2cd6b28f56c3520348a8ca9d94a837a48bb"&&image.GetUsedRect()==new Rect2I(10,26,140,48),
       ("enemy_slug","rest_canvas.png")=>frameSha=="463e07d84c97146079283e533c58bbe3bddf3296e39f5f29fb75b1ca54a9cb03"&&image.GetUsedRect()==new Rect2I(36,26,140,48),
       ("enemy_slug","spitprepare.png")=>frameSha=="177b2b6f946686c836791ea6bad43de04465d92734400172e379afa37445aecd"&&image.GetUsedRect()==new Rect2I(39,16,137,58),
       ("enemy_slug","spitrelease.png")=>frameSha=="a78e8c61df68e59003d386bbca4efae326567b3e38fcd5fc86c05b73dd0baeba"&&image.GetUsedRect()==new Rect2I(33,21,143,53),
       ("enemy_slug","tacklecontact.png")=>frameSha=="80633d7ece339e08ef624b7721bc8dd691407f760d78a2aa1b49c788f2e7c495"&&image.GetUsedRect()==new Rect2I(22,32,154,42),
       ("enemy_slug","tackleprepare.png")=>frameSha=="760b327ea84bcb7aa7593b5ef5187af357c6de5ce7bc44e5a4be612f83e99dcc"&&image.GetUsedRect()==new Rect2I(42,33,134,41),
       ("enemy_haze_stag","chargecontact.png")=>frameSha=="4e353959ef234957eec369b3a3cb6a27f3b717ff76e269850a8fc8560b96b2cc"&&image.GetUsedRect()==new Rect2I(12,46,189,90),
       ("enemy_haze_stag","chargeprepare.png")=>frameSha=="fb23a247e18c8abde2a98903dbb517a86fa63f05a8d403e0893e56d87224f644"&&image.GetUsedRect()==new Rect2I(14,45,161,91),
       ("enemy_haze_stag","fadehold.png")=>frameSha=="a53594ba2ca188c33ea7fd5a49b706787ded5f099ecfd0f05468adc32b467461"&&image.GetUsedRect()==new Rect2I(51,27,134,109),
       ("enemy_haze_stag","fadeprepare.png")=>frameSha=="bb9c885d67dafb93b80da8f93781e000c354648f0976b3946087b90e6c4a952e"&&image.GetUsedRect()==new Rect2I(56,28,132,108),
       ("enemy_haze_stag","flurrycontact1.png")=>frameSha=="a870378f1ad71a2c966704e7650d70144ca04a59f3ee7c24eeadea27cd080f1c"&&image.GetUsedRect()==new Rect2I(23,10,135,126),
       ("enemy_haze_stag","flurrycontact2.png")=>frameSha=="7143cb051adb154d6f38be171923d128dfb30897bfd2e0c264795c75f60ee68c"&&image.GetUsedRect()==new Rect2I(22,15,136,121),
       ("enemy_haze_stag","flurryprepare.png")=>frameSha=="932bc138e041d21763902a613555308211473dad546b320c87b283fe73eb740a"&&image.GetUsedRect()==new Rect2I(27,6,130,130),
       ("enemy_haze_stag","flurryrebound.png")=>frameSha=="aa58c71d24c7a03559a2148b3df30f38330f467a40a7f948f6a715dc651695b8"&&image.GetUsedRect()==new Rect2I(20,12,139,124),
       ("enemy_haze_stag","idle_00.png")=>frameSha=="97a66f0468d8c88c2791ebf8dc9a3296dca5a9b5c0dbb805221d1a75c1d6279d"&&image.GetUsedRect()==new Rect2I(20,24,167,112),
       ("enemy_haze_stag","idle_01.png")=>frameSha=="521fa738de5a0c9acab5d66c2c29cf8ac287ccef479b6e7c29696b381012bc80"&&image.GetUsedRect()==new Rect2I(20,23,167,113),
       ("enemy_haze_stag","idle_02.png")=>frameSha=="28248611e0fb7f2575508c62e10fa37f0b4c17859f85be058b9568b1d71453ab"&&image.GetUsedRect()==new Rect2I(20,23,167,113),
       ("enemy_haze_stag","idle_03.png")=>frameSha=="1b004befe1a39eed226dbeeeaff1d093b9e16e4027fefb183f7868575683630b"&&image.GetUsedRect()==new Rect2I(20,22,167,114),
       ("enemy_haze_stag","idle_04.png")=>frameSha=="b1f627fdd28d4587fffc335bfeb5c5d0165e30895b1d45fc75b3a6f30f1c2c13"&&image.GetUsedRect()==new Rect2I(20,22,167,114),
       ("enemy_haze_stag","idle_05.png")=>frameSha=="092ae08b8c4f946d274c45f33e278be464092a9fd83bd5004810d31e504ffe6e"&&image.GetUsedRect()==new Rect2I(20,23,167,113),
       ("enemy_haze_stag","idle_06.png")=>frameSha=="69db0de7997730d26e5ada6dec3e0b974e5327785546ad701365ba89e7b07005"&&image.GetUsedRect()==new Rect2I(20,23,167,113),
       ("enemy_haze_stag","idle_07.png")=>frameSha=="adc0dbdb3e7875b581fcd0b22eb05c6b0d0876619dfa704058aaaca64f1bfbee"&&image.GetUsedRect()==new Rect2I(20,24,167,112),
       ("enemy_haze_stag","hurt.png")=>frameSha=="c39c1cf0dc741ee005d43d4fd929bfef231137382edfa773a1b2c50f2684e118"&&image.GetUsedRect()==new Rect2I(31,12,146,124),
       ("enemy_haze_stag","idle.png")=>frameSha=="01aeeb96e842164cac54c4c668d86ff5eeac08af12e8de8a4ac6531f1199eb5f"&&image.GetUsedRect()==new Rect2I(31,22,154,114),
       ("enemy_haze_stag","jabcontact.png")=>frameSha=="13949219c30a61ec3782c39ea3bc9efe1efd6994a8630539fbdcd2cab6ec95b2"&&image.GetUsedRect()==new Rect2I(23,31,165,105),
       ("enemy_haze_stag","jabprepare.png")=>frameSha=="1ead1aabf6325ae5b8651867d0fdf3b3b1ebe5d77cf4438835ce1ac66a7a2537"&&image.GetUsedRect()==new Rect2I(23,29,161,107),
       ("enemy_haze_stag","recovery.png")=>frameSha=="a32821f79d2a27cf029a6673c1a9a8f82135b9ebc265a80794946af5f639c468"&&image.GetUsedRect()==new Rect2I(35,25,149,111),
       ("enemy_haze_stag","rest.png")=>frameSha=="828908dac7f5f1a574b6d1cd6f58e1b5e9a1433ef5a781013f9520d41b4f47c5"&&image.GetUsedRect()==new Rect2I(12,24,167,112),
       ("enemy_haze_stag","rest_canvas.png")=>frameSha=="97a66f0468d8c88c2791ebf8dc9a3296dca5a9b5c0dbb805221d1a75c1d6279d"&&image.GetUsedRect()==new Rect2I(20,24,167,112),
       _=>false}))throw new InvalidOperationException("Unapproved M49 exact frame bytes or bounds");
      if(strictM50&&!((enemyId,file) switch{
       ("enemy_cinder_imp","bitecontact.png")=>frameSha=="24f7ca4919e28ac9fb19a0517ff129fca0151aa19dc3eeb83851e014bcf15db0"&&image.GetUsedRect()==new Rect2I(14,25,57,41),
       ("enemy_cinder_imp","biteprepare.png")=>frameSha=="bfe651bc887706a370cbe476f99306948324e587c8d7af6cbebee9a818d29427"&&image.GetUsedRect()==new Rect2I(22,20,49,46),
       ("enemy_cinder_imp","fanapply.png")=>frameSha=="abe90003ed1e2a8634f6a941557c1823e5584bd233f1209c48c9ed9aab965af5"&&image.GetUsedRect()==new Rect2I(5,20,66,46),
       ("enemy_cinder_imp","fanprepare.png")=>frameSha=="41d8c134bb76804e5deb91cd947936520860cb2db80204a0f758fd7af8bd61a7"&&image.GetUsedRect()==new Rect2I(19,18,61,48),
       ("enemy_cinder_imp","guardhold.png")=>frameSha=="b0691949b0cd825d422c62bd8eeb8d46b9168faf71b357b28e3dad581e53d5e2"&&image.GetUsedRect()==new Rect2I(28,19,45,47),
       ("enemy_cinder_imp","guardprepare.png")=>frameSha=="4ff448b93ca42564ea4e8f8a5f56f173271463595d31f94153d6cae1391f3906"&&image.GetUsedRect()==new Rect2I(22,18,50,48),
       ("enemy_cinder_imp","idle_00.png")=>frameSha=="a681b77d86875488c3634fa33ed08d4c2941211158178d1c5ad1b6dd7bd91e90"&&image.GetUsedRect()==new Rect2I(17,18,54,48),
       ("enemy_cinder_imp","idle_01.png")=>frameSha=="0646006c20db39423711a87cb596ad509d71e1e86c6cab1f88664e437d76d5c3"&&image.GetUsedRect()==new Rect2I(17,17,54,49),
       ("enemy_cinder_imp","idle_02.png")=>frameSha=="f002f455aa79e6869f17af6b7112874f6dea45cbfa04cb42544516ed4ca71274"&&image.GetUsedRect()==new Rect2I(17,17,54,49),
       ("enemy_cinder_imp","idle_03.png")=>frameSha=="6db4b67d63516320b11f8ccad3f0de2e1d92d2a46534899a8f9b16b1d3795e08"&&image.GetUsedRect()==new Rect2I(17,16,54,50),
       ("enemy_cinder_imp","idle_04.png")=>frameSha=="fb5e71761c6fda1752d18a3dfc7681a40d81d27661491d34f2f8a9eb4ada2de8"&&image.GetUsedRect()==new Rect2I(17,16,54,50),
       ("enemy_cinder_imp","idle_05.png")=>frameSha=="a272fadac4b644ad957455a4145bb28b5c6dec55368197adff248299fcde070d"&&image.GetUsedRect()==new Rect2I(17,17,54,49),
       ("enemy_cinder_imp","idle_06.png")=>frameSha=="8813cead5375b4e431a19ba4a3e1fcb02011ec90c7c0742d7e6139e6649caafe"&&image.GetUsedRect()==new Rect2I(17,18,54,48),
       ("enemy_cinder_imp","idle_07.png")=>frameSha=="51d53ce8cf4ac7c443356853ecbb49cc45d9c1b61ca0cb48e95b9b86b38d8495"&&image.GetUsedRect()==new Rect2I(17,18,54,48),
       ("enemy_cinder_imp","hurt.png")=>frameSha=="b0d3ec135cbf665dc9ba4a76b8e1ed8e8d15614e2d4e0b2042e3c5dabbf3472a"&&image.GetUsedRect()==new Rect2I(24,17,50,49),
       ("enemy_cinder_imp","idle.png")=>frameSha=="9ddaeb354bd8d427cbf5b7501bd4205942d012df483aeaead87e3e8b0dee6702"&&image.GetUsedRect()==new Rect2I(18,18,54,48),
       ("enemy_cinder_imp","recovery.png")=>frameSha=="4775ae43d77d93fc68a62a575f9f54be7ce281350ab9a52c644478c4ff775327"&&image.GetUsedRect()==new Rect2I(15,18,56,48),
       ("enemy_cinder_imp","rest.png")=>frameSha=="e3083ec6e8099bccd91e79a8647f4c15cd2c6323fd85110cfcb8fa80e23ed2c9"&&image.GetUsedRect()==new Rect2I(13,18,54,48),
       ("enemy_cinder_imp","rest_canvas.png")=>frameSha=="a681b77d86875488c3634fa33ed08d4c2941211158178d1c5ad1b6dd7bd91e90"&&image.GetUsedRect()==new Rect2I(17,18,54,48),
       ("enemy_cinder_imp","tossprepare.png")=>frameSha=="ae67e03d3f0696396d98bab7fdec925f082588e10587a1bc39114ac09ecea6c1"&&image.GetUsedRect()==new Rect2I(20,18,51,48),
       ("enemy_cinder_imp","tossrelease.png")=>frameSha=="26fd7448895063d729f8d058952a237f1d882dd7771bcf4b462acb7904cf200c"&&image.GetUsedRect()==new Rect2I(4,19,67,47),
       ("enemy_scald_gnat","bitecontact.png")=>frameSha=="6b59e735854c90f17c483d331b860ae8b0d80a803cee50aaadf1ea654cd4a3d7"&&image.GetUsedRect()==new Rect2I(32,16,70,37),
       ("enemy_scald_gnat","biteprepare.png")=>frameSha=="5e3c9e5381ccca1a9b5081b19f3a5767606eaedb3fd05032e6bc96581548e4a8"&&image.GetUsedRect()==new Rect2I(35,18,59,40),
       ("enemy_scald_gnat","guardhold.png")=>frameSha=="e9a7f9c06cf4488a3842fdd1ef5ec8183e32dc79290b0e0a06c6be630f8725bb"&&image.GetUsedRect()==new Rect2I(38,15,48,42),
       ("enemy_scald_gnat","guardprepare.png")=>frameSha=="9b3be101f99a7624f20a56f9b88e247db93af7dcbe09a1f67b66e47a030ed5e9"&&image.GetUsedRect()==new Rect2I(32,15,56,41),
       ("enemy_scald_gnat","idle_00.png")=>frameSha=="798866456b72a4b5e34c45e7d8b2836f7f1aa5c1c0fe12b3b06bbbb7bd254e64"&&image.GetUsedRect()==new Rect2I(26,14,70,44),
       ("enemy_scald_gnat","idle_01.png")=>frameSha=="cfdade5b1d97d77476312ee46c3daaf691f8a6a5dcdde4d4929a550c0e2c4a4f"&&image.GetUsedRect()==new Rect2I(26,13,70,45),
       ("enemy_scald_gnat","idle_02.png")=>frameSha=="9068bc6fedb8bcbd851e4438293dff59383c3758ded04ef87d870e69002f297a"&&image.GetUsedRect()==new Rect2I(26,12,70,45),
       ("enemy_scald_gnat","idle_03.png")=>frameSha=="7dc985e719bdec43fabd84ba9181c1be7d19acc8aeb6ecbf047966214994e763"&&image.GetUsedRect()==new Rect2I(26,12,71,45),
       ("enemy_scald_gnat","idle_04.png")=>frameSha=="60459045a7d1b7bd3994d2b457c2c338ec40031e9034ab0115e95af9176ea292"&&image.GetUsedRect()==new Rect2I(26,13,71,44),
       ("enemy_scald_gnat","idle_05.png")=>frameSha=="0776cbec6b83b45134282dbff6a5a4ee93ca6ca21a7944e903e72857569aeab9"&&image.GetUsedRect()==new Rect2I(26,14,71,44),
       ("enemy_scald_gnat","idle_06.png")=>frameSha=="82d7dd712435b27893feed3a898da883ad3e36fcf753a1f04b3d9f28d338ac73"&&image.GetUsedRect()==new Rect2I(26,14,70,44),
       ("enemy_scald_gnat","idle_07.png")=>frameSha=="4f3ed9a6dc4dfde49b59e15b1695c942ca0e331c93d3c04590186bdb269bd062"&&image.GetUsedRect()==new Rect2I(26,14,69,44),
       ("enemy_scald_gnat","hurt.png")=>frameSha=="12f5865a5ce97117320f3744f0e79a673d2f4dee62ec345ef0e1d7e6194ea079"&&image.GetUsedRect()==new Rect2I(30,16,59,38),
       ("enemy_scald_gnat","idle.png")=>frameSha=="0bc5056bdf8d11e79a5d293c23de4f0ad852ec548702a35727bc273ffac4ed51"&&image.GetUsedRect()==new Rect2I(24,14,73,45),
       ("enemy_scald_gnat","recovery.png")=>frameSha=="e97aa3e3508913e0a77da0652e89fe65c6c1e17fb0dbb41f32ddf1ec3d0d1359"&&image.GetUsedRect()==new Rect2I(26,15,70,41),
       ("enemy_scald_gnat","rest.png")=>frameSha=="798866456b72a4b5e34c45e7d8b2836f7f1aa5c1c0fe12b3b06bbbb7bd254e64"&&image.GetUsedRect()==new Rect2I(26,14,70,44),
       ("enemy_scald_gnat","scaldapply.png")=>frameSha=="6e7f13694f85cdce3cde9abce7d04955eac47754d429c6d62ba380520aa9c9a9"&&image.GetUsedRect()==new Rect2I(22,14,74,41),
       ("enemy_scald_gnat","scaldprepare.png")=>frameSha=="36f062d4cddd0168bca5bcb91aa16d17ae6720e637ba7dbee0416c1eae77b859"&&image.GetUsedRect()==new Rect2I(29,14,68,44),
       ("enemy_scald_gnat","suckcontact.png")=>frameSha=="dc60143e06aefb06ae0afcc6c36d5c3a608745023944c6bbc22053e2599062bd"&&image.GetUsedRect()==new Rect2I(22,13,76,39),
       ("enemy_scald_gnat","suckprepare.png")=>frameSha=="e6d13c58e7c044d55880f1617f50684063fa7b2988ac8e6b822f95b626549b42"&&image.GetUsedRect()==new Rect2I(19,12,80,44),
       _=>false}))throw new InvalidOperationException("Unapproved M50 exact frame bytes or bounds");
      if(strictM51&&!((enemyId,file) switch{
       ("enemy_bomber","boomprepare.png")=>frameSha=="0d45035058aa34e752258bfacc10db82fcaca1fcb2127269e30d70f95cede8d3"&&image.GetUsedRect()==new Rect2I(14,20,73,60),
       ("enemy_bomber","boomrelease.png")=>frameSha=="c98602ca47e37fd3c15960da8a05038cf375a385645fea98da3e5fbc8fc4f65b"&&image.GetUsedRect()==new Rect2I(20,12,72,68),
       ("enemy_bomber","guardhold.png")=>frameSha=="89a68ce704a907bcf2678a5fdfad22d02bf4bdf3f329429af519999ec480619d"&&image.GetUsedRect()==new Rect2I(23,18,64,62),
       ("enemy_bomber","guardprepare.png")=>frameSha=="93c2dbbb4d42175997ffb9c8e64ad67d469745b2f1dbda0bed0bc8fdfc5d3e0e"&&image.GetUsedRect()==new Rect2I(19,11,68,69),
       ("enemy_bomber","idle_00.png")=>frameSha=="567a3c022ab69b55f6ea37caf2651e816a4a1b9c892c8c8db1b8b782608dd5be"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_01.png")=>frameSha=="f2f7d9f42b276e93513848e98ed46aa7adb6f85446a058d6f22411bb2bc0aac4"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_02.png")=>frameSha=="880e3fbf2ac3db8f7600b7e0f0d510aec790239b0b0e2567aca5e1c03233e66c"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_03.png")=>frameSha=="731d24d54b0cae16f40b6a879289509d2353cafcf80cbe593c3543bd5c3b068a"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_04.png")=>frameSha=="2e3f1989e4be91ee9edc10cc67e79d84d9d02207fe5b127b17d65672c477a124"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_05.png")=>frameSha=="ed3c75ac6f65b90340b398e05bd70aea189c69812974910ee0145417ecfd3d1a"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_06.png")=>frameSha=="0c04673a00c40c9c6b239104b2aea58d77e0e44f0bcea33a0775525fcc242ddd"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","idle_07.png")=>frameSha=="98f8521f196c0ee8ed011258f1327a5cd0ed1067a265baecac4985d74b8bb7af"&&image.GetUsedRect()==new Rect2I(25,16,62,64),
       ("enemy_bomber","hurt.png")=>frameSha=="e1349a56162d56ea68675485eb6a054e03bc2b975d448657a51be29502068a40"&&image.GetUsedRect()==new Rect2I(21,10,66,70),
       ("enemy_bomber","idle.png")=>frameSha=="01eed201fadb3db65799780c9d54e609b80da0489be50782227696900894b4ca"&&image.GetUsedRect()==new Rect2I(24,15,63,65),
       ("enemy_bomber","recovery.png")=>frameSha=="62c9e3f1f3c9c4d9d5647114abab4eb5afe1cf928f1b2949d12e43182d8a78d2"&&image.GetUsedRect()==new Rect2I(19,16,68,64),
       ("enemy_bomber","rest.png")=>frameSha=="3564783960bb54e046f927cf7248b2fccedc7681fe0fbd1f90f5b05fb624ee9f"&&image.GetUsedRect()==new Rect2I(21,17,62,63),
       ("enemy_bomber","rest_canvas.png")=>frameSha=="567a3c022ab69b55f6ea37caf2651e816a4a1b9c892c8c8db1b8b782608dd5be"&&image.GetUsedRect()==new Rect2I(25,17,62,63),
       ("enemy_bomber","tossprepare.png")=>frameSha=="3b3c8fd475b78196464d17596e597d4d4d16d88f92eeae5eeb8efb19975a4988"&&image.GetUsedRect()==new Rect2I(15,12,72,68),
       ("enemy_bomber","tossrelease.png")=>frameSha=="e3fe1a28cc9ed41a75703404230633bb8ec6a4b86080ee106d63da263891ff77"&&image.GetUsedRect()==new Rect2I(3,18,84,62),
       ("enemy_chomper","clampcontact1.png")=>frameSha=="e88bba35eda7ffb275856df3cac81c5d9c9eb88be1a0fc503e29147782defd63"&&image.GetUsedRect()==new Rect2I(17,43,105,45),
       ("enemy_chomper","clampcontact2.png")=>frameSha=="1344e0fd0a122d5305f36467325ba5a20ffe6958b2fa73aebe2f931517d48722"&&image.GetUsedRect()==new Rect2I(15,40,108,48),
       ("enemy_chomper","clampprepare.png")=>frameSha=="b87d68a1a569a326d6d97f31ee8f360d51d72d4d1aa32be23ba614131ed2cb0d"&&image.GetUsedRect()==new Rect2I(14,18,107,70),
       ("enemy_chomper","clamprebound.png")=>frameSha=="56a2a0a66dcf70c1caeca1d804e72b14a45b623033a983f3c5884feae61a34dc"&&image.GetUsedRect()==new Rect2I(14,26,108,62),
       ("enemy_chomper","guardhold.png")=>frameSha=="b33be06f0b5ee7b08fc877d266d52e40aa78ef9ee0a998c8494ab1879bb9a10e"&&image.GetUsedRect()==new Rect2I(21,48,102,40),
       ("enemy_chomper","guardprepare.png")=>frameSha=="a95f1b5e339388812e8bf0731413589893cb926d595876460dd73fe0a0909723"&&image.GetUsedRect()==new Rect2I(20,38,103,50),
       ("enemy_chomper","idle_00.png")=>frameSha=="6a8701c661a02a931fcad3ed7c7b94693da9a3ffbe4b4fe4ed607b61534a4811"&&image.GetUsedRect()==new Rect2I(22,24,100,64),
       ("enemy_chomper","idle_01.png")=>frameSha=="facac3071fdfcf1bf3d93d02686985cb64096e53c8ee279b7008654536b36dfb"&&image.GetUsedRect()==new Rect2I(22,25,100,63),
       ("enemy_chomper","idle_02.png")=>frameSha=="dbbfdd982b3df3abafb9f7efcf601bc80b0c1637ff70a3b15b331bd5dcf4c582"&&image.GetUsedRect()==new Rect2I(22,26,100,62),
       ("enemy_chomper","idle_03.png")=>frameSha=="b18959545d730cd5ed9a52e9903badd213ae124cd11cad5332e22110b4f32c2c"&&image.GetUsedRect()==new Rect2I(22,27,100,61),
       ("enemy_chomper","idle_04.png")=>frameSha=="47745c3e70e3ba11122b56bf93d04557aa799cf0bd1405186b3802d0591fbf08"&&image.GetUsedRect()==new Rect2I(22,27,100,61),
       ("enemy_chomper","idle_05.png")=>frameSha=="8bef1589bc1a9d2f6005805487b32962a7cd1059004760a4ec067579f21f9632"&&image.GetUsedRect()==new Rect2I(22,26,100,62),
       ("enemy_chomper","idle_06.png")=>frameSha=="e2b26b962053b96e29e2d46a639c4439f20a8af2641a0f222dd6c4971567c7c9"&&image.GetUsedRect()==new Rect2I(22,25,100,63),
       ("enemy_chomper","idle_07.png")=>frameSha=="b00c50c7bfefd3eed02b0c69b4dbabdf2c262a79d695ea61152dc78a683be934"&&image.GetUsedRect()==new Rect2I(22,24,100,64),
       ("enemy_chomper","hurt.png")=>frameSha=="5133627f7ab293ceae5afce863e32ca875c41b54cf75dccee59f34b5abbfa505"&&image.GetUsedRect()==new Rect2I(24,32,97,56),
       ("enemy_chomper","idle.png")=>frameSha=="97853fa223d016bffebfefd49e7d4c0584c08a3cd6b251fdfedb6cb23088389c"&&image.GetUsedRect()==new Rect2I(20,34,100,54),
       ("enemy_chomper","recovery.png")=>frameSha=="8f61daee54e08ce0746c6e01ab32c73393d6e48fad68ed2251d94c16942906ad"&&image.GetUsedRect()==new Rect2I(16,31,105,57),
       ("enemy_chomper","rest-registered.png")=>frameSha=="6a8701c661a02a931fcad3ed7c7b94693da9a3ffbe4b4fe4ed607b61534a4811"&&image.GetUsedRect()==new Rect2I(22,24,100,64),
       ("enemy_chomper","rest.png")=>frameSha=="dfba9e1beac0a033de53d962c5efbd4aa87cbcd970a2ea2406eef2e33887436b"&&image.GetUsedRect()==new Rect2I(22,16,100,64),
       ("enemy_chomper","screechapply.png")=>frameSha=="7166174241e267864ee04813e307d53d526be4dd3366e0db2659a07bdc02c8c8"&&image.GetUsedRect()==new Rect2I(18,6,105,82),
       ("enemy_chomper","screechprepare.png")=>frameSha=="f132d6c48415cecb097fe619d1edd72b62bc43902a46f39c6a70f6a4c7a7b6c1"&&image.GetUsedRect()==new Rect2I(20,17,100,71),
       _=>false}))throw new InvalidOperationException("Unapproved M51 exact frame bytes or bounds");
      if(strictM52&&!((enemyId,file) switch{
       ("enemy_maw_hunter","bitecontact.png")=>frameSha=="8c85327f8fcc3ed09aae0e14af750004d8c13acb41f473205ae3ff7be6a59eae"&&image.GetUsedRect()==new Rect2I(15,19,101,59),
       ("enemy_maw_hunter","biteprepare.png")=>frameSha=="4e6b35bee3d0f2dabfb6c1a2d8efb15cffd725c6819658cf4b91162ed218a9ad"&&image.GetUsedRect()==new Rect2I(18,20,97,58),
       ("enemy_maw_hunter","clawscontacta.png")=>frameSha=="67985fe520eda1d0bde311d6c96c9ad7599948917b11f28d439b409c6029085e"&&image.GetUsedRect()==new Rect2I(8,25,107,53),
       ("enemy_maw_hunter","clawscontactb.png")=>frameSha=="dea46d2fa05e61b1b9ede1b785caab393481873b6724174a12579932f0e5333b"&&image.GetUsedRect()==new Rect2I(11,22,104,56),
       ("enemy_maw_hunter","clawsprepare.png")=>frameSha=="8084c5da49000a3e8f2a5e47ae81e0d5930062e13e7e54763ae148339e4cbb37"&&image.GetUsedRect()==new Rect2I(16,17,99,62),
       ("enemy_maw_hunter","clawsrebound.png")=>frameSha=="98c8f230a8eb9dabd00839d2e570a8ec3fd078759c5dd9b369b45f0ac96b3615"&&image.GetUsedRect()==new Rect2I(14,17,101,61),
       ("enemy_maw_hunter","guardhold.png")=>frameSha=="98bef8728bc0edb976575da286f4877957b52958e0315cf85ac6640ee34f2f52"&&image.GetUsedRect()==new Rect2I(21,31,94,47),
       ("enemy_maw_hunter","guardprepare.png")=>frameSha=="e8ca2348ba790c28e1edfb88d1f94cbbed5b696486d3856908a38b86a51a7653"&&image.GetUsedRect()==new Rect2I(20,30,96,48),
       ("enemy_maw_hunter","idle_00.png")=>frameSha=="feba38a02962d100dbfb2649a01c7bac28bfd75de185bac714c6cd926b5d12e6"&&image.GetUsedRect()==new Rect2I(18,16,100,64),
       ("enemy_maw_hunter","idle_01.png")=>frameSha=="9d08e06d8377b25a670935bba62e1939b3935dd7080036859dc553b4ca86846a"&&image.GetUsedRect()==new Rect2I(18,16,100,64),
       ("enemy_maw_hunter","idle_02.png")=>frameSha=="2174dbfca9810ea9b829ebd138662d8b36082822eff72eac710af1594eca4c18"&&image.GetUsedRect()==new Rect2I(17,15,101,65),
       ("enemy_maw_hunter","idle_03.png")=>frameSha=="f079312ff53d429f2569b6d75bbe2d48b106209c02096338b98387905b12c417"&&image.GetUsedRect()==new Rect2I(17,15,101,65),
       ("enemy_maw_hunter","idle_04.png")=>frameSha=="6ba288546f2233c64f30abe70c235da5044f827121f4a7abef047a21f32e536c"&&image.GetUsedRect()==new Rect2I(17,16,101,64),
       ("enemy_maw_hunter","idle_05.png")=>frameSha=="771d35c1c959b1bb74c7c37cb002a91fdda06789c145352d26d2f1175eaf7aa6"&&image.GetUsedRect()==new Rect2I(18,16,100,64),
       ("enemy_maw_hunter","idle_06.png")=>frameSha=="b1f9a68634d04eb9bf6234e5d3c1a3b46cfe6b4b381d9e0dcfc468719eb7c54d"&&image.GetUsedRect()==new Rect2I(18,17,100,63),
       ("enemy_maw_hunter","idle_07.png")=>frameSha=="479c1b0420f5df88fa59b48f837d8bfdb7ffe6721f448bd34e2457ba424b5ee5"&&image.GetUsedRect()==new Rect2I(18,16,100,64),
       ("enemy_maw_hunter","hurt.png")=>frameSha=="13eb3772d00f6d0dc24df416e055c87d1edd4cec877d775b1953409c7f059326"&&image.GetUsedRect()==new Rect2I(13,19,102,59),
       ("enemy_maw_hunter","idle.png")=>frameSha=="4d958907e4f3058f6f483281c457dcd68d8a15b8dc67a65b3784090b8a37a7f1"&&image.GetUsedRect()==new Rect2I(15,17,100,61),
       ("enemy_maw_hunter","recovery.png")=>frameSha=="2f8373ee08f02fc13357986f6dab018e2d3551c09c4b2e129a675568bf3b659e"&&image.GetUsedRect()==new Rect2I(17,22,98,56),
       ("enemy_maw_hunter","rest.png")=>frameSha=="feba38a02962d100dbfb2649a01c7bac28bfd75de185bac714c6cd926b5d12e6"&&image.GetUsedRect()==new Rect2I(18,16,100,64),
       ("enemy_maw_hunter","webapplied.png")=>frameSha=="d4029b0f20f9aad57a623a0d6c3fabeadaf2ec4a14586f243dfd198cc580e1b7"&&image.GetUsedRect()==new Rect2I(9,25,106,53),
       ("enemy_maw_hunter","webprepare.png")=>frameSha=="84e97f7aa4c5e6347f8f3b3b7c4f4726f9e13c7dd6f1ac9a33070db52ec7979b"&&image.GetUsedRect()==new Rect2I(14,14,101,65),
       ("enemy_cog_construct","chargehold.png")=>frameSha=="038ae7d28d53696fc7e03279c7f947bda80fdaf4d236b6c5ecd0f24a6d089c38"&&image.GetUsedRect()==new Rect2I(17,6,70,68),
       ("enemy_cog_construct","chargeprepare.png")=>frameSha=="32f28d8825d1ab59c4ba5192f7882ca02058ab6fdfaca148034343b91efbc7a3"&&image.GetUsedRect()==new Rect2I(17,8,70,66),
       ("enemy_cog_construct","expelcontact1.png")=>frameSha=="65dbcb13008a08cba71d7143bab0ddb24b43a4ae32ac485750507f6af2502d84"&&image.GetUsedRect()==new Rect2I(29,21,60,53),
       ("enemy_cog_construct","expelcontact2.png")=>frameSha=="43766f28eb832d12fdc5a8375d7fb3ac1f88d657a65ddc7cc136a3a3f4d438e6"&&image.GetUsedRect()==new Rect2I(25,20,69,54),
       ("enemy_cog_construct","expelprepare.png")=>frameSha=="dcd6919a5ddc95509cec5b0a38b40e88dd54f0de886b16e1b3d24483de34650b"&&image.GetUsedRect()==new Rect2I(20,22,69,52),
       ("enemy_cog_construct","expelrebound.png")=>frameSha=="b122edd15c0323cb389da6dca79ae1bccfb866f788b203f425cd45f94cb3b4a2"&&image.GetUsedRect()==new Rect2I(18,16,72,58),
       ("enemy_cog_construct","guardhold.png")=>frameSha=="72bbc07b8b6ec749882502f0c2da05449139e42f8567699112ef3a6b15f08a90"&&image.GetUsedRect()==new Rect2I(26,26,65,48),
       ("enemy_cog_construct","guardprepare.png")=>frameSha=="c442647a704b2a532c06f370564f8995184d52b63bf2bef4cfac6da93661e540"&&image.GetUsedRect()==new Rect2I(24,18,66,56),
       ("enemy_cog_construct","idle_00.png")=>frameSha=="cded57396501b37447b692d1fd81779ef4c05986b75e5db53d0449ef6474b32d"&&image.GetUsedRect()==new Rect2I(16,14,71,60),
       ("enemy_cog_construct","idle_01.png")=>frameSha=="5d01cbc426eb2b22507d9e53a5b1460bba77b5402e50267dad1882aee73c6ad9"&&image.GetUsedRect()==new Rect2I(16,13,71,61),
       ("enemy_cog_construct","idle_02.png")=>frameSha=="e92e86034e5258f32526f6ba340fbbe6077e1fa229636aab70b878e012416259"&&image.GetUsedRect()==new Rect2I(16,12,71,62),
       ("enemy_cog_construct","idle_03.png")=>frameSha=="e10702a4883c4e5c55c48d652f053577796370b6beefd972efe6b813e29f8bcd"&&image.GetUsedRect()==new Rect2I(16,12,71,62),
       ("enemy_cog_construct","idle_04.png")=>frameSha=="10e77a3f761f93e4e2dc2d91433201fdbc260b27072ff578a1f1ce00728ebce7"&&image.GetUsedRect()==new Rect2I(16,12,71,62),
       ("enemy_cog_construct","idle_05.png")=>frameSha=="2c7b4b075410a093546b8a9a222355710f4637a8803f49ccc4e4ef5462834ae2"&&image.GetUsedRect()==new Rect2I(16,13,71,61),
       ("enemy_cog_construct","idle_06.png")=>frameSha=="d5580cee4f6d907ffcb7f6054d6efc4eb697cae9fc0497f71323fbe9e54e40ca"&&image.GetUsedRect()==new Rect2I(16,14,71,60),
       ("enemy_cog_construct","idle_07.png")=>frameSha=="3bd07576569cc0296847f0a3116381e6a8e9174f86e7c13f48b8c6b917a67a8d"&&image.GetUsedRect()==new Rect2I(16,14,71,60),
       ("enemy_cog_construct","hurt.png")=>frameSha=="583d3dfacb68fe9a146242699c5fe1242c1671806f3babb404deafa78cbdd820"&&image.GetUsedRect()==new Rect2I(19,17,69,57),
       ("enemy_cog_construct","idle.png")=>frameSha=="327abd34a1dac4979438640579014d70e448abbb2ae77748414f22f76e198506"&&image.GetUsedRect()==new Rect2I(16,15,70,59),
       ("enemy_cog_construct","recovery.png")=>frameSha=="5cd963233588add57e33da5356a21732c285f74fbde53d914739a45c7cf3ccd4"&&image.GetUsedRect()==new Rect2I(17,16,69,58),
       ("enemy_cog_construct","repeatercontact.png")=>frameSha=="3a71028190339907688b539d1f3aa7d473b3450d3815d9711a6b7fb3434caa83"&&image.GetUsedRect()==new Rect2I(25,16,67,58),
       ("enemy_cog_construct","repeaterprepare.png")=>frameSha=="0006a2090dcfa02e75e78d9c138e08899699316f2f8cdae568ae056e56ecaa3c"&&image.GetUsedRect()==new Rect2I(21,13,70,61),
       ("enemy_cog_construct","rest.png")=>frameSha=="cded57396501b37447b692d1fd81779ef4c05986b75e5db53d0449ef6474b32d"&&image.GetUsedRect()==new Rect2I(16,14,71,60),
       _=>false}))throw new InvalidOperationException("Unapproved M52 exact frame bytes or bounds");
      if(strictM53&&!((enemyId,file) switch{
       ("enemy_gaping_maw","clawcontact1.png")=>frameSha=="96f3c536968fa647b03edbf550616759d5c5f463c9f2812cb33aae5a1d27944f"&&image.GetUsedRect()==new Rect2I(15,11,81,68),
       ("enemy_gaping_maw","clawcontact2.png")=>frameSha=="3f46c3df4ef06c7c48f09f5e9ffdd5d7b5a4c6557a3ca5a8bb3ac7bef1721e2e"&&image.GetUsedRect()==new Rect2I(19,13,77,66),
       ("enemy_gaping_maw","clawprepare.png")=>frameSha=="f81fc29adf69cfd816c845356d273a406650677f77af74e9dd6c579907f82532"&&image.GetUsedRect()==new Rect2I(15,10,81,69),
       ("enemy_gaping_maw","clawrebound.png")=>frameSha=="de44211d3fb308e301f6d78d7ffe14914a4ceb9d5c06e2bb7fe80394115132c4"&&image.GetUsedRect()==new Rect2I(14,10,82,69),
       ("enemy_gaping_maw","guardhold.png")=>frameSha=="659ca42692e1429bdc25bcd55d9a053543bb54d9c69177920b136381b60b1da3"&&image.GetUsedRect()==new Rect2I(18,26,78,50),
       ("enemy_gaping_maw","guardprepare.png")=>frameSha=="0ef53025b07d4d74370469e70d37acac579e21ceda5ad24011f682c6f020543c"&&image.GetUsedRect()==new Rect2I(22,25,74,50),
       ("enemy_gaping_maw","idle_00.png")=>frameSha=="14ac9fe54fa8f98fa705dd38b2798fc6af9a87ba15902b377df6323005d5ecda"&&image.GetUsedRect()==new Rect2I(16,16,80,64),
       ("enemy_gaping_maw","idle_01.png")=>frameSha=="b874508f42fd542a3305b782a404a49f2007bc0c1cf451856748f520e6fe029e"&&image.GetUsedRect()==new Rect2I(16,15,80,65),
       ("enemy_gaping_maw","idle_02.png")=>frameSha=="c9b077017db28b272949c1cc8d1802a46f5e9deae1ebe2331718ccfd9542be07"&&image.GetUsedRect()==new Rect2I(16,15,80,65),
       ("enemy_gaping_maw","idle_03.png")=>frameSha=="bbd8c7bf0bd83e6a058b5eee10b6e25d665e9511304d2b42041fa13c5df6ca98"&&image.GetUsedRect()==new Rect2I(15,14,81,66),
       ("enemy_gaping_maw","idle_04.png")=>frameSha=="6acf783efc11759ff84eec81b0e7d72a11f8ccbacb455d5f794835373ce39500"&&image.GetUsedRect()==new Rect2I(16,14,80,66),
       ("enemy_gaping_maw","idle_05.png")=>frameSha=="dfd9e480f0d1844bebb97f23df5a7f0b094ae2cf5ab190000939719ad5a36e5a"&&image.GetUsedRect()==new Rect2I(16,15,80,65),
       ("enemy_gaping_maw","idle_06.png")=>frameSha=="d8043b2072b58185af0cb9edbf1b4dc09b5f11cfa46f35fe5e1fe24542900ebe"&&image.GetUsedRect()==new Rect2I(16,16,81,64),
       ("enemy_gaping_maw","idle_07.png")=>frameSha=="d5db9691cc8276db401032cecead4adf84c94830e1ac02b8779380a3bf48aff4"&&image.GetUsedRect()==new Rect2I(16,16,81,64),
       ("enemy_gaping_maw","hurt.png")=>frameSha=="5a8c78b499ffa321a601d7ef40a49ae66a42f145c99cf5b59d559ab3935d7c6a"&&image.GetUsedRect()==new Rect2I(21,24,75,51),
       ("enemy_gaping_maw","idle.png")=>frameSha=="6e4fd613660a785fa0ab9d38c5d8542ab196cab86fec8fe9b0048cb56cbac6c0"&&image.GetUsedRect()==new Rect2I(19,14,76,64),
       ("enemy_gaping_maw","recovery.png")=>frameSha=="65ebf56b6c2b8fcfab5f4961ff464dc520f4b44bdc5c02b111a99dfa8eee3a18"&&image.GetUsedRect()==new Rect2I(17,13,79,65),
       ("enemy_gaping_maw","rendcontact.png")=>frameSha=="11b99f67a1438c47e3bccf83e2237619d5073803a5e1beb395ecf0f26117d0bd"&&image.GetUsedRect()==new Rect2I(15,11,81,66),
       ("enemy_gaping_maw","rendprepare.png")=>frameSha=="a60bc6676eeaf247c98b95f64267331cd10b8fb48728d3f6a429325083c60933"&&image.GetUsedRect()==new Rect2I(18,10,78,69),
       ("enemy_gaping_maw","rest.png")=>frameSha=="14ac9fe54fa8f98fa705dd38b2798fc6af9a87ba15902b377df6323005d5ecda"&&image.GetUsedRect()==new Rect2I(16,16,80,64),
       ("enemy_gaping_maw","roarapplied.png")=>frameSha=="ca335903579c091b47a8aa966b9e689edc4f0f923d7da41e5329c0b2ddc29bdd"&&image.GetUsedRect()==new Rect2I(16,3,80,74),
       ("enemy_gaping_maw","roarprepare.png")=>frameSha=="79b2ad4f55c7bb1ffd7615f28792bb81f4eca46565990c6cb9a9075308f207e8"&&image.GetUsedRect()==new Rect2I(20,22,76,54),
       ("enemy_joker","bluffcontact.png")=>frameSha=="ef8b21e5e66bf0a9b2a90be242fbd3a8a22a2a5cfdd3552d362c5fce4634095d"&&image.GetUsedRect()==new Rect2I(18,19,64,61),
       ("enemy_joker","bluffprepare.png")=>frameSha=="7cc4ccef2cbf2f1d3c1f634a7bae64a3cc7444f7be62d90a2e61f032600602ef"&&image.GetUsedRect()==new Rect2I(31,17,51,63),
       ("enemy_joker","guardhold.png")=>frameSha=="dac9346181277ab37456ccc617e34e18a5d1b51e3c8cc0cc062a8347a8dc6f5c"&&image.GetUsedRect()==new Rect2I(31,17,51,63),
       ("enemy_joker","guardprepare.png")=>frameSha=="8f4fbd8307812d9d09c600c6ed7fbd5a7180cccd26040a89f962b6841cc7e2b3"&&image.GetUsedRect()==new Rect2I(48,17,37,63),
       ("enemy_joker","idle_00.png")=>frameSha=="8c54cc23fdd5d379517b431fe2f4a17135d046a02c8f36b63c66f1b309934f33"&&image.GetUsedRect()==new Rect2I(14,16,68,64),
       ("enemy_joker","idle_01.png")=>frameSha=="015d551d6534451894b8c4a5897e7bf933ebd518a40e371cc4d28f94ebaf0fa7"&&image.GetUsedRect()==new Rect2I(14,15,68,65),
       ("enemy_joker","idle_02.png")=>frameSha=="4f2afe9e87752fe8e8ed4b66d2fd2996aecfa0e471538f1d2668ed85fafbd54f"&&image.GetUsedRect()==new Rect2I(14,15,68,65),
       ("enemy_joker","idle_03.png")=>frameSha=="e170a52346069e0dd8e5d412a10a7f090130831f4e617f5f34b9769d8fa293fe"&&image.GetUsedRect()==new Rect2I(14,15,68,65),
       ("enemy_joker","idle_04.png")=>frameSha=="89b3ed91ecb08ea57528142922a8a4c34d71c4e7a4f672af8f772c61db62d727"&&image.GetUsedRect()==new Rect2I(14,16,68,64),
       ("enemy_joker","idle_05.png")=>frameSha=="b9668979be85280d36adf88f7ad42114ec8898d1a166ad015b90bc3e97ea6d82"&&image.GetUsedRect()==new Rect2I(14,16,68,64),
       ("enemy_joker","idle_06.png")=>frameSha=="eb12e251e860dc4b32615d4d16cfa92a8b1e35eb6d5782b0f120c4de8727c055"&&image.GetUsedRect()==new Rect2I(14,17,68,63),
       ("enemy_joker","idle_07.png")=>frameSha=="9bd977ee9efbcf3701e0c5e5464a2ee941cbbcb9bf85b6a72c47b56f71472b14"&&image.GetUsedRect()==new Rect2I(14,16,68,64),
       ("enemy_joker","hurt.png")=>frameSha=="e4969d1793de51b7fba2155bfdeb83bd99ed9e9bd4dba15c923380cef1420dbe"&&image.GetUsedRect()==new Rect2I(44,17,41,63),
       ("enemy_joker","idle.png")=>frameSha=="de886f20927abf8a4f7bca4e6eaf5fabf84ec5b9e652edb20e74b635d149a368"&&image.GetUsedRect()==new Rect2I(32,16,51,64),
       ("enemy_joker","jabcontact.png")=>frameSha=="36cb47c49306506cbb8ddb210e4bf1f8a8c5718994341f86ffd80f1cbacac753"&&image.GetUsedRect()==new Rect2I(16,19,67,61),
       ("enemy_joker","jabprepare.png")=>frameSha=="d69a0fded8812dac36233fbf0fd643680a17d74c238ea3eb8192a475f1f7c290"&&image.GetUsedRect()==new Rect2I(34,19,49,61),
       ("enemy_joker","recovery.png")=>frameSha=="0af08e4443f33d60b91510f4b64548919fbae4c7baec18f83b07cccc0c5fd7a4"&&image.GetUsedRect()==new Rect2I(27,16,55,64),
       ("enemy_joker","rest.png")=>frameSha=="8c54cc23fdd5d379517b431fe2f4a17135d046a02c8f36b63c66f1b309934f33"&&image.GetUsedRect()==new Rect2I(14,16,68,64),
       ("enemy_joker","wildcontact.png")=>frameSha=="582ecab6e532e4a1bb908bd1fdac5b5c712b60d4f56cd998f0a16050c4476d88"&&image.GetUsedRect()==new Rect2I(5,19,78,61),
       ("enemy_joker","wildprepare.png")=>frameSha=="c8d25ac45f39c5b6146a104e500aa7b5e2d23bf1a66271793ac00f7ccf424163"&&image.GetUsedRect()==new Rect2I(15,13,68,67),
       _=>false}))throw new InvalidOperationException("Unapproved M53 exact frame bytes or bounds");
      if(strictM55&&!((enemyId,file) switch{
       ("enemy_mimic_imp","contacta.png")=>frameSha=="81974b3bab46584f2f03dcc1fbab5cbd19444e3761ac51dd028971c3089b3e8a"&&image.GetUsedRect()==new Rect2I(1,27,70,39),
       ("enemy_mimic_imp","contactb.png")=>frameSha=="cedb08c41bc4218f3b02074bff13c942383c6b1b6b597dee6151eeca475e59f9"&&image.GetUsedRect()==new Rect2I(2,31,69,35),
       ("enemy_mimic_imp","idle_00.png")=>frameSha=="51423b102e1170492c840cc895a90781bffe35e7024053b2963b221985895049"&&image.GetUsedRect()==new Rect2I(16,18,55,48),
       ("enemy_mimic_imp","idle_01.png")=>frameSha=="31b3aa278c83807d9d767f0737755fe7a59374be3fe0360902ce78eda26d2d6a"&&image.GetUsedRect()==new Rect2I(16,19,56,47),
       ("enemy_mimic_imp","idle_02.png")=>frameSha=="4b642e38c4938ea685b60d5e3f2bdf0dc7c3b04683908a66e1951eb657de41a5"&&image.GetUsedRect()==new Rect2I(16,19,56,47),
       ("enemy_mimic_imp","idle_03.png")=>frameSha=="f669dfed31d96ac6da1de7bf4bc397a18e5c8b4c8a04588f9dbac7d096a842ed"&&image.GetUsedRect()==new Rect2I(15,20,56,46),
       ("enemy_mimic_imp","idle_04.png")=>frameSha=="1c349deeb34c40801afc6fa9eb9f04757abdef9daa735e2026b0c59b0dd2d02c"&&image.GetUsedRect()==new Rect2I(15,19,55,47),
       ("enemy_mimic_imp","idle_05.png")=>frameSha=="04f5c0de0ae929ecd9b745c17879b8515d7f090c481e32f9ff5d8fbe1f86facb"&&image.GetUsedRect()==new Rect2I(16,19,54,47),
       ("enemy_mimic_imp","idle_06.png")=>frameSha=="ba6b1a339ae260d8fba3655c69e56df0ad4755338c5bd90f6a50ab1fe4976b03"&&image.GetUsedRect()==new Rect2I(16,18,55,48),
       ("enemy_mimic_imp","idle_07.png")=>frameSha=="55ed38f6864be23e740d439e71f34292301d34ffdccd4947ed1687b7a6220c87"&&image.GetUsedRect()==new Rect2I(17,18,55,48),
       ("enemy_mimic_imp","hurt.png")=>frameSha=="8c5808f316f8f7a5a00a25d6313aa76ab5508d5f870f4b37ca017ea6b2f9ffb0"&&image.GetUsedRect()==new Rect2I(22,21,51,45),
       ("enemy_mimic_imp","idle.png")=>frameSha=="a11c39131755d77031be9077c85ac573319aa85e4e53a18e83306da5655984b0"&&image.GetUsedRect()==new Rect2I(16,19,55,47),
       ("enemy_mimic_imp","prepare.png")=>frameSha=="84166101b264dc8cb5859ae5df1bfadc4dbf971fc3ad54e0cc653665d1448a11"&&image.GetUsedRect()==new Rect2I(21,24,52,42),
       ("enemy_mimic_imp","rebound.png")=>frameSha=="8626fb8746a662448e3bfa734176e94fce7124090817dfbd6a917e56f6c41d92"&&image.GetUsedRect()==new Rect2I(25,21,46,45),
       ("enemy_mimic_imp","recovery.png")=>frameSha=="46774e73f0a48e1b45ec189c88a39f9ac4ca4cc791a03a5fb053ad5ac76adb2e"&&image.GetUsedRect()==new Rect2I(19,21,51,45),
       ("enemy_mimic_imp","rest.png")=>frameSha=="51423b102e1170492c840cc895a90781bffe35e7024053b2963b221985895049"&&image.GetUsedRect()==new Rect2I(16,18,55,48),
       ("enemy_mimic_jester","contacta.png")=>frameSha=="f67a6de33a18f43f6337499dc9e0ee4b4907938a4afc5c82556a5de3f366f59c"&&image.GetUsedRect()==new Rect2I(5,19,87,61),
       ("enemy_mimic_jester","contactb.png")=>frameSha=="18b7c6e8ffdeee1b1fd028af2df5139284057f327e60606d98d5cb9f2f86ab55"&&image.GetUsedRect()==new Rect2I(4,26,88,54),
       ("enemy_mimic_jester","idle_00.png")=>frameSha=="7edb4d0576286e8df14386d7021e840e6415b9b5f8d9c6c7b5206242e188430d"&&image.GetUsedRect()==new Rect2I(13,17,78,63),
       ("enemy_mimic_jester","idle_01.png")=>frameSha=="79466ef7e7ca770562c62e3dfe8d3d866bc4ee5c88722991e951f1a2e0ef82d6"&&image.GetUsedRect()==new Rect2I(13,16,78,64),
       ("enemy_mimic_jester","idle_02.png")=>frameSha=="1a6e8c6dc86082c8d5e13a495bb64230fc00ac99d783119451d4662e1cebacd5"&&image.GetUsedRect()==new Rect2I(13,15,78,65),
       ("enemy_mimic_jester","idle_03.png")=>frameSha=="99c385d624afb303f3f2752eb6b3f332e8d00bd58ab5aae57e132298bedaf2d6"&&image.GetUsedRect()==new Rect2I(13,15,78,65),
       ("enemy_mimic_jester","idle_04.png")=>frameSha=="d298654329dded35cde9195dc45016d25ec221f1ff9b488900c1d52f7adadc38"&&image.GetUsedRect()==new Rect2I(13,16,78,64),
       ("enemy_mimic_jester","idle_05.png")=>frameSha=="6a51f6df4d9d0a6b86eecb406eb9d4bd9b60c6f5087bff02cf2e1e259ee3f742"&&image.GetUsedRect()==new Rect2I(13,17,78,63),
       ("enemy_mimic_jester","idle_06.png")=>frameSha=="8e9bfdadfbdeab404405c57edcecc97bbb02ae0e67e0574d4ed463d3ba175073"&&image.GetUsedRect()==new Rect2I(13,18,78,62),
       ("enemy_mimic_jester","idle_07.png")=>frameSha=="b1f821eb20e7d5fbe295b41dbd4c3464323c60795f9be693979a2fa9e5a028ac"&&image.GetUsedRect()==new Rect2I(13,17,78,63),
       ("enemy_mimic_jester","hurt.png")=>frameSha=="0486731c764d15f5e1d001e7cac76f2497b0b5dd0597728c16bec6463a4afd7c"&&image.GetUsedRect()==new Rect2I(19,19,78,61),
       ("enemy_mimic_jester","idle.png")=>frameSha=="8a10b4ac424ae83217db7a00ca1190c1d117edf6eac03573e78a8c95165ef2d4"&&image.GetUsedRect()==new Rect2I(15,17,76,63),
       ("enemy_mimic_jester","prepare.png")=>frameSha=="afcc6a8863095347edbcb9c25a581b2275c0fd9067d49701dfca9847c476fdb0"&&image.GetUsedRect()==new Rect2I(25,15,67,65),
       ("enemy_mimic_jester","rebound.png")=>frameSha=="67444a12866f0e868d5ccc0e896a1129f79df36dd9ac77975a9c4a1c540c8e3c"&&image.GetUsedRect()==new Rect2I(16,21,76,59),
       ("enemy_mimic_jester","recovery.png")=>frameSha=="bc565ccd36311a6a87029a01bf294a9f8a0d99ea48aae799bd1c4ea145b4fd45"&&image.GetUsedRect()==new Rect2I(14,18,77,62),
       ("enemy_mimic_jester","rest.png")=>frameSha=="7edb4d0576286e8df14386d7021e840e6415b9b5f8d9c6c7b5206242e188430d"&&image.GetUsedRect()==new Rect2I(13,17,78,63),
       _=>false}))throw new InvalidOperationException("Unapproved M55 exact frame/bounds");
      if(strictM54&&!((enemyId,file) switch{
       ("enemy_frog_knight","chargecontact.png")=>frameSha=="43975036d0d5d93e6263795c9e5ebade88c8a6e25136ba5fe5b8e13bbb387d33"&&image.GetUsedRect()==new Rect2I(6,31,133,49),
       ("enemy_frog_knight","chargeprepare.png")=>frameSha=="08385ced88646e017099d9eb860df8c10218c9e9f656c0e3b77bf7a2343afe67"&&image.GetUsedRect()==new Rect2I(10,28,129,52),
       ("enemy_frog_knight","guardhold.png")=>frameSha=="7a7158278416f66af2a928c1c292e2c44d1b2e58d204eda3f229dcef83136dca"&&image.GetUsedRect()==new Rect2I(14,22,124,58),
       ("enemy_frog_knight","guardprepare.png")=>frameSha=="388432aa76ae0038a1b497bfb27d0529ac40f1ebb43cb08122828b4708b01371"&&image.GetUsedRect()==new Rect2I(17,20,122,60),
       ("enemy_frog_knight","idle_00.png")=>frameSha=="0daf426a24e5ef425a42ec9b77dc3158a3311a21018a249d11f72dd9c4b962b2"&&image.GetUsedRect()==new Rect2I(21,16,117,64),
       ("enemy_frog_knight","idle_01.png")=>frameSha=="a414aeead3db292af0e1a56cb573d6ac60a5f446b2557d4c091cec681d95a21b"&&image.GetUsedRect()==new Rect2I(21,15,117,65),
       ("enemy_frog_knight","idle_02.png")=>frameSha=="fff525b462121801deb066b85751852764f5e5e299efc0781729d293a0bef41c"&&image.GetUsedRect()==new Rect2I(21,15,117,65),
       ("enemy_frog_knight","idle_03.png")=>frameSha=="0ed7b9118c7e559cd2cd9d4ea5ecf7f0d9de8ca6c50de35404f1bc8b3c296abf"&&image.GetUsedRect()==new Rect2I(21,14,117,66),
       ("enemy_frog_knight","idle_04.png")=>frameSha=="0dae5031859b549d5c327a4c663dee59fdfdd65b63104e725559fc9b3dac3125"&&image.GetUsedRect()==new Rect2I(21,14,117,66),
       ("enemy_frog_knight","idle_05.png")=>frameSha=="2108e27fee4c3a7afbf97ac09b9a82fdbc11a00cfc05b6fa297306d5f90f1397"&&image.GetUsedRect()==new Rect2I(21,15,117,65),
       ("enemy_frog_knight","idle_06.png")=>frameSha=="67d35ff5eaf3481bb59ce42af32c0432069885114abde1576690ac43069806cd"&&image.GetUsedRect()==new Rect2I(21,15,117,65),
       ("enemy_frog_knight","idle_07.png")=>frameSha=="5794b581026a05b91f32e980c9963af95018acef142360c73cd70bda0bda0b85"&&image.GetUsedRect()==new Rect2I(21,16,117,64),
       ("enemy_frog_knight","hurt.png")=>frameSha=="dc7259903d212fe10874a3b47dbf49196a26c8a90bdee10d8546ca3e49c4bc71"&&image.GetUsedRect()==new Rect2I(36,14,113,66),
       ("enemy_frog_knight","idle.png")=>frameSha=="ab18542d35bc82ce39cacd2b97db639a5109973206f77f6d56efd444e41f789b"&&image.GetUsedRect()==new Rect2I(14,16,125,64),
       ("enemy_frog_knight","queenhold.png")=>frameSha=="201ac389f0a81c6e94d971415417eca79776378e5e2e2ac4d40b2d537081d8f7"&&image.GetUsedRect()==new Rect2I(19,12,120,68),
       ("enemy_frog_knight","queenprepare.png")=>frameSha=="eb83580077087790d20900c5839a74ce3165caad4fb895779d706c492290dbfa"&&image.GetUsedRect()==new Rect2I(20,13,119,67),
       ("enemy_frog_knight","recovery.png")=>frameSha=="0584b80395c15b8ad7e3e03e2a0208338beeac24ce024a0084d5455d0bdd0b28"&&image.GetUsedRect()==new Rect2I(17,16,122,64),
       ("enemy_frog_knight","rest.png")=>frameSha=="0daf426a24e5ef425a42ec9b77dc3158a3311a21018a249d11f72dd9c4b962b2"&&image.GetUsedRect()==new Rect2I(21,16,117,64),
       ("enemy_frog_knight","strikecontact.png")=>frameSha=="035a6b48b87e3e5eb623b93b8a5004080720bb093a7214faa0e0db96aca6a7cc"&&image.GetUsedRect()==new Rect2I(12,23,127,57),
       ("enemy_frog_knight","strikeprepare.png")=>frameSha=="8a1c484e696639ad21468ac7557dc012870b21a2d38e32af2b8584d78d29ab07"&&image.GetUsedRect()==new Rect2I(44,5,96,75),
       ("enemy_frog_knight","tonguecontact.png")=>frameSha=="7cd2eb711a6de58d9690e233f6d83702e30171d26c7e069d72f528ff2cee87a6"&&image.GetUsedRect()==new Rect2I(10,19,129,61),
       ("enemy_frog_knight","tongueprepare.png")=>frameSha=="d649fd27d2c030b266ed500018e71268ddeb2baf2ee8ab18d70586d0e20d2ddc"&&image.GetUsedRect()==new Rect2I(11,16,128,64),
       ("enemy_elite_slaver","chaincontact.png")=>frameSha=="014995c95c161a288cd92f86a26f3d0d87758b2840e84047ecc8682c304f6394"&&image.GetUsedRect()==new Rect2I(11,20,169,83),
       ("enemy_elite_slaver","chainprepare.png")=>frameSha=="d7ad894648d3cedfb739236b7bec49719cba2bbe156e75c52dd595a52d207a71"&&image.GetUsedRect()==new Rect2I(55,20,126,83),
       ("enemy_elite_slaver","guardhold.png")=>frameSha=="8058c8795ee933fb8c8205b91e1eb8bf371846ee0dcd5801cdc6e49bf99faeb1"&&image.GetUsedRect()==new Rect2I(74,17,105,86),
       ("enemy_elite_slaver","guardprepare.png")=>frameSha=="825b5679a3be4e017fc71f1f0c16adb33292e49ddef462af8521168f6e4994cb"&&image.GetUsedRect()==new Rect2I(76,20,102,83),
       ("enemy_elite_slaver","idle_00.png")=>frameSha=="5d609b796a57a8d6a532f129e443cc75f9954f23e95d36a18249cc3530c450e0"&&image.GetUsedRect()==new Rect2I(45,16,134,88),
       ("enemy_elite_slaver","idle_01.png")=>frameSha=="0b74cd7ff65e328fbe7efdfb2be30f14fb621f7b6e1531d95b93b206fef28eb7"&&image.GetUsedRect()==new Rect2I(45,17,134,87),
       ("enemy_elite_slaver","idle_02.png")=>frameSha=="bdc28a7063220800ccae5a187c77176ebb9781ca9e63eda2e10d99e36447e827"&&image.GetUsedRect()==new Rect2I(45,17,134,87),
       ("enemy_elite_slaver","idle_03.png")=>frameSha=="8a0e5567013b3be2b896450fd7f3b7a5439b45831e1916b031d4fc834912dc7d"&&image.GetUsedRect()==new Rect2I(46,16,134,88),
       ("enemy_elite_slaver","idle_04.png")=>frameSha=="4b3c6e6b15ed7681a435ce46c4797eb4d190f0a320974383764f4295d2336a22"&&image.GetUsedRect()==new Rect2I(46,15,134,89),
       ("enemy_elite_slaver","idle_05.png")=>frameSha=="d7b0110d7fd4367db7f97884db83eea75d7e1c8a3549e30ede3f6a7cceabd34d"&&image.GetUsedRect()==new Rect2I(46,15,133,89),
       ("enemy_elite_slaver","idle_06.png")=>frameSha=="ba7c064a0efedfc08b905aa9846bed8d5f2aad9b5734e70e40d5f0115195cf72"&&image.GetUsedRect()==new Rect2I(45,16,133,88),
       ("enemy_elite_slaver","idle_07.png")=>frameSha=="b4e1a3b814562e02bc2ada8fa4c00357eef0702e85fb9b09b74a9f972f47b37a"&&image.GetUsedRect()==new Rect2I(45,16,134,88),
       ("enemy_elite_slaver","hurt.png")=>frameSha=="e03177307e14d6879a1099bae3d4bcfdff41c935f25c47f2c1b77c69ee09edeb"&&image.GetUsedRect()==new Rect2I(58,20,125,83),
       ("enemy_elite_slaver","idle.png")=>frameSha=="7704e0a2de69f77a90ad96b90096252103bd6adcf1ef0834b8db5d931ba13510"&&image.GetUsedRect()==new Rect2I(58,16,121,87),
       ("enemy_elite_slaver","lashcontact.png")=>frameSha=="4b05b0d638089ab19734048da48ddfd4c3bf2ba02bb007577d40f6851d2960a4"&&image.GetUsedRect()==new Rect2I(7,15,176,88),
       ("enemy_elite_slaver","lashprepare.png")=>frameSha=="53ee6c71d904bf297f3c6e5c1ab284f63c123cd567a3c0342073f749456371e5"&&image.GetUsedRect()==new Rect2I(48,20,146,83),
       ("enemy_elite_slaver","netapplied.png")=>frameSha=="4fca3c3cb776e3d0ebdc4ed202be2db28691026294d01e0a6dc2ba07b0b0d19e"&&image.GetUsedRect()==new Rect2I(61,10,118,93),
       ("enemy_elite_slaver","netprepare.png")=>frameSha=="4d8e33d20ac470c618355059f7b9c0c1a2360f482c8d44cae2f20f76a93c0b39"&&image.GetUsedRect()==new Rect2I(66,13,113,90),
       ("enemy_elite_slaver","recovery.png")=>frameSha=="210ca29a9b44cb24272708cb86225443dc53c121fb0f8cd7c2998ccf4a6fbbcc"&&image.GetUsedRect()==new Rect2I(59,16,120,87),
       ("enemy_elite_slaver","rest.png")=>frameSha=="d436587bdb1e596670d1b54b162340b66549e0fb2fbbb17c44c4575a0f9d4105"&&image.GetUsedRect()==new Rect2I(29,16,134,88),
       ("enemy_elite_slaver","rest_canvas.png")=>frameSha=="5d609b796a57a8d6a532f129e443cc75f9954f23e95d36a18249cc3530c450e0"&&image.GetUsedRect()==new Rect2I(45,16,134,88),
       ("enemy_elite_slaver","weightcontact.png")=>frameSha=="fec4ec12f915ab315ad09fa641aa546797af0b53b46075a76de3212133d063e7"&&image.GetUsedRect()==new Rect2I(58,33,120,70),
       ("enemy_elite_slaver","weightprepare.png")=>frameSha=="10ba9ceb7da5e8782c4b3ecff7cb0b5f44b9201dbcf7f43bddb310f17fe65764"&&image.GetUsedRect()==new Rect2I(44,9,136,94),
       _=>false}))throw new InvalidOperationException("Unapproved M54 exact frame/bounds");
      if(strictM56&&!((enemyId,file) switch{
       ("enemy_elite_iron_egg","awakenhold.png")=>frameSha=="0f4545edec763133fdb9211f4fe6ee1b1fee99053c25e7f1149b031046f73303"&&image.GetUsedRect()==new Rect2I(13,12,128,92),
       ("enemy_elite_iron_egg","awakenprepare.png")=>frameSha=="7b777e17da634f3717428a796315a1d0c0d3fbb2af058514bbf8a182eaeeeae2"&&image.GetUsedRect()==new Rect2I(9,11,132,93),
       ("enemy_elite_iron_egg","idle_00.png")=>frameSha=="c094eedb47486c5c5eae39752ea55578ab64b25adaafc0f4dbaab418b43469f9"&&image.GetUsedRect()==new Rect2I(12,16,143,88),
       ("enemy_elite_iron_egg","idle_01.png")=>frameSha=="c2acc1af2018d5b96221ce5cc4fbcfb9123acbf89462ecea1189f429db7989c5"&&image.GetUsedRect()==new Rect2I(12,17,143,87),
       ("enemy_elite_iron_egg","idle_02.png")=>frameSha=="3740b371f364b3735065e1bd8cfaa6f1c44d8343c304c19111bbf379930b2564"&&image.GetUsedRect()==new Rect2I(12,17,143,87),
       ("enemy_elite_iron_egg","idle_03.png")=>frameSha=="87b6bc214e63a659542a12d0af49015a183c258aea86f555e6a212f6b066835f"&&image.GetUsedRect()==new Rect2I(12,17,143,87),
       ("enemy_elite_iron_egg","idle_04.png")=>frameSha=="732a68f32af24c0391383b3c39bbe0c5a89c01ad586d98ac8e0fecb334d5defd"&&image.GetUsedRect()==new Rect2I(12,17,142,87),
       ("enemy_elite_iron_egg","idle_05.png")=>frameSha=="afe4938a6b7ef732d68cce05cda7bbf53800a9a8811b1bcaddb7130be4e1bc25"&&image.GetUsedRect()==new Rect2I(12,16,142,88),
       ("enemy_elite_iron_egg","idle_06.png")=>frameSha=="eb21e0fdc5e359ec39da6dff0c501084b21260b9f183ee39357bc3a3df821efe"&&image.GetUsedRect()==new Rect2I(12,16,142,88),
       ("enemy_elite_iron_egg","idle_07.png")=>frameSha=="ca0c8672c07df31205ea8ea1d83bdebf1ae275dba551af93bbfbdbf59c6204d6"&&image.GetUsedRect()==new Rect2I(12,16,142,88),
       ("enemy_elite_iron_egg","hurt.png")=>frameSha=="2efe9fb0ea1c94b3d3281ae2f3c4a9470d39e1a5ff779b0f27cb319a456e220f"&&image.GetUsedRect()==new Rect2I(9,20,127,84),
       ("enemy_elite_iron_egg","idle.png")=>frameSha=="4f320c9ff8e772ce5d13a8056d0e48bee35f6c81aa21f38059b6ab9214f8f29b"&&image.GetUsedRect()==new Rect2I(12,16,133,88),
       ("enemy_elite_iron_egg","recovery.png")=>frameSha=="5d0f632a3627b4318269b9b33c2ee2733965fdbe1ee89d9ab82abfd3dcfe7c2b"&&image.GetUsedRect()==new Rect2I(12,15,136,89),
       ("enemy_elite_iron_egg","rest.png")=>frameSha=="c094eedb47486c5c5eae39752ea55578ab64b25adaafc0f4dbaab418b43469f9"&&image.GetUsedRect()==new Rect2I(12,16,143,88),
       ("enemy_elite_iron_egg","sleephold.png")=>frameSha=="0c050d3bd7f1c8bceb30b3c292cb7c1142beb7877e198bcd280ef6236bb5946d"&&image.GetUsedRect()==new Rect2I(5,32,119,72),
       ("enemy_elite_iron_egg","sleepprepare.png")=>frameSha=="af1e4a25e59591f4d9b4da72cabffdfa7d3cef6cd231f5bb9ed00de2184613f8"&&image.GetUsedRect()==new Rect2I(7,28,126,76),
       ("enemy_elite_iron_egg","source-rest.png")=>frameSha=="5a96fe0c356b06605d3707f619f796643f48b7aefd1e8edd0d5fb350ecb24ca1"&&image.GetUsedRect()==new Rect2I(4,16,143,88),
       ("enemy_elite_iron_egg","tailcontact.png")=>frameSha=="8608ed49cd70f0b90866d2a16c4f6a948c8aaeea057aef1e1943e27aa15b6e91"&&image.GetUsedRect()==new Rect2I(12,15,108,89),
       ("enemy_elite_iron_egg","tailprepare.png")=>frameSha=="fb6e0e056b25287fd82fc326c7e19e8e8fdbee3675f38ff98e92e7b8f8d522db"&&image.GetUsedRect()==new Rect2I(12,10,119,94),
       ("enemy_elite_sentry","beamprepare.png")=>frameSha=="01dd78f6191b1bc142889ea67c0fb5faea8bbf09154ac59de1da6c5d90c93d9e"&&image.GetUsedRect()==new Rect2I(25,29,95,75),
       ("enemy_elite_sentry","beamrelease.png")=>frameSha=="845683a25f1f799d5b2fd81b9b9daa4d2af27f1e52f780ecfa74a57cbb991ccf"&&image.GetUsedRect()==new Rect2I(41,26,88,78),
       ("enemy_elite_sentry","boltprepare.png")=>frameSha=="5ea7400bfcf841c19ca1ae767d4c3446e7e1a53cd90440e8c5c6f0350f4e3bf3"&&image.GetUsedRect()==new Rect2I(25,15,90,89),
       ("enemy_elite_sentry","boltrelease.png")=>frameSha=="ec993d300560225a0dd7ef21b4f95007139dba4315eb4239e48e70952c4e637f"&&image.GetUsedRect()==new Rect2I(42,19,89,85),
       ("enemy_elite_sentry","guardhold.png")=>frameSha=="b55304482ed6462c59621cdb4f665f50ea394d300f3d1fadcc720e69e0fa82e1"&&image.GetUsedRect()==new Rect2I(43,21,66,83),
       ("enemy_elite_sentry","guardprepare.png")=>frameSha=="01ec6296651f8968b8588f3e7b967e2e39b0cbe6e70079a09651cc5a02abf9a5"&&image.GetUsedRect()==new Rect2I(43,19,69,85),
       ("enemy_elite_sentry","idle_00.png")=>frameSha=="26f478cd3bb4acc312027322d99725573898f8c7c68f3aacc690e5d063a29ac6"&&image.GetUsedRect()==new Rect2I(26,16,92,88),
       ("enemy_elite_sentry","idle_01.png")=>frameSha=="fb6b91b519537bcd2172b8b6406780f5e2220655b37615520f2b46235d9e8a40"&&image.GetUsedRect()==new Rect2I(26,17,92,87),
       ("enemy_elite_sentry","idle_02.png")=>frameSha=="23cccdcbd3849721a928cf1c3443178da9687550f08f47c4cda36f0e3755d4b2"&&image.GetUsedRect()==new Rect2I(26,17,92,87),
       ("enemy_elite_sentry","idle_03.png")=>frameSha=="6a1b85ac6c0a624f98548f4e9f2a9be40f4997979a68fd52813270708c462f22"&&image.GetUsedRect()==new Rect2I(26,17,91,87),
       ("enemy_elite_sentry","idle_04.png")=>frameSha=="9ea07a7bd4d8de677476960985f8c0dc97f6bc5625291a314c162abe29d7b26b"&&image.GetUsedRect()==new Rect2I(26,17,91,87),
       ("enemy_elite_sentry","idle_05.png")=>frameSha=="afc938661fbd65f9e7bb355d857166d957b04790d4a70bd65210f203bb3e5951"&&image.GetUsedRect()==new Rect2I(26,16,91,88),
       ("enemy_elite_sentry","idle_06.png")=>frameSha=="7272b99298e1528e51d8c7c4a0e8bc23d90297a9ced6f85b00baf74e75a220ab"&&image.GetUsedRect()==new Rect2I(26,16,91,88),
       ("enemy_elite_sentry","idle_07.png")=>frameSha=="3afba1c8529d680f992ddf8dcddd438d2e9c7b016420448c5a6eca2fc36bf55f"&&image.GetUsedRect()==new Rect2I(26,16,91,88),
       ("enemy_elite_sentry","hurt.png")=>frameSha=="2efa0a9ffd60a30ed733539ad1fbcf86a2c1fcf9929783247917faa67d4594a0"&&image.GetUsedRect()==new Rect2I(40,15,88,89),
       ("enemy_elite_sentry","idle.png")=>frameSha=="089f0f095264762c78b62f3df6b6e3a461803977bc0af8ee5a23bf9c7933a680"&&image.GetUsedRect()==new Rect2I(25,15,92,89),
       ("enemy_elite_sentry","recovery.png")=>frameSha=="b09a183521e90dee43871f097098f24c3f55b2c6151aef613d3108417372a82b"&&image.GetUsedRect()==new Rect2I(25,15,92,89),
       ("enemy_elite_sentry","rest.png")=>frameSha=="26f478cd3bb4acc312027322d99725573898f8c7c68f3aacc690e5d063a29ac6"&&image.GetUsedRect()==new Rect2I(26,16,92,88),
       _=>false}))throw new InvalidOperationException("Unapproved M56 exact frame/bounds");
      if(strictM57&&!((enemyId,file) switch{
       ("enemy_elite_mirror_djinn","contacta.png")=>frameSha=="ec2086f94e09b995fe9e1b9291fa828abf5d5f06cdd02cc394f61609499f2eab"&&image.GetUsedRect()==new Rect2I(30,18,109,85),
       ("enemy_elite_mirror_djinn","contactb.png")=>frameSha=="22212dfdbb8b50eec45502ba8796ffd49dc59a3c6296bb957edbb816b5e2453f"&&image.GetUsedRect()==new Rect2I(27,21,112,77),
       ("enemy_elite_mirror_djinn","gazeapplied.png")=>frameSha=="b93b4eb9417312f96aa1633e5b71c93ec950078871d1613a6fb0c214d4041310"&&image.GetUsedRect()==new Rect2I(44,16,97,86),
       ("enemy_elite_mirror_djinn","gazeprepare.png")=>frameSha=="6fe093237d55d614b9ea4656f08ff551275906ed856dc4890d54d32e9fb090d9"&&image.GetUsedRect()==new Rect2I(49,20,81,83),
       ("enemy_elite_mirror_djinn","guardhold.png")=>frameSha=="5433f5bf220331ae1dd83f78b7606e78944b88a093fe5566d69b3c091cbc7b70"&&image.GetUsedRect()==new Rect2I(65,18,57,83),
       ("enemy_elite_mirror_djinn","guardprepare.png")=>frameSha=="1724ad938bd6c01218bbc0c3623b5b4f27aae824aec79fdc4d75f8d675e41f4a"&&image.GetUsedRect()==new Rect2I(61,18,63,82),
       ("enemy_elite_mirror_djinn","idle_00.png")=>frameSha=="64a698cc41f1933c29d0f7e7f73c0d2545c59f019b61a459292d94071bdbf39b"&&image.GetUsedRect()==new Rect2I(44,16,87,88),
       ("enemy_elite_mirror_djinn","idle_01.png")=>frameSha=="9a23f534738a8ecfc2d566160721b3938e6f01e7ed09468d17bbab04361b4296"&&image.GetUsedRect()==new Rect2I(44,16,87,87),
       ("enemy_elite_mirror_djinn","idle_02.png")=>frameSha=="32dc0a455199c6076997b4678c52e18977e1ccc940f352ffcc7d1eff8460f4a9"&&image.GetUsedRect()==new Rect2I(45,15,85,88),
       ("enemy_elite_mirror_djinn","idle_03.png")=>frameSha=="2bdb4c95c90519b93e48947ec34593a8951511b85cea8da25a7d99e2904bba74"&&image.GetUsedRect()==new Rect2I(45,15,85,88),
       ("enemy_elite_mirror_djinn","idle_04.png")=>frameSha=="ef4dee67d3bbd6584cc566bc4ba0a4961303cd7d4f2522518c5a9c4612c15c94"&&image.GetUsedRect()==new Rect2I(45,15,86,88),
       ("enemy_elite_mirror_djinn","idle_05.png")=>frameSha=="ebb427a211e51af2a581c6826174027487fdc84ba993392d5cab2a8f67b2d0d8"&&image.GetUsedRect()==new Rect2I(44,16,88,87),
       ("enemy_elite_mirror_djinn","idle_06.png")=>frameSha=="d99f2362ef25c968ff788acde208a691845bc90e3228b2c73914559b89e99cc6"&&image.GetUsedRect()==new Rect2I(43,16,89,88),
       ("enemy_elite_mirror_djinn","idle_07.png")=>frameSha=="cbbd8987010d434c7ec00e800335ba9c0e63f107c3f119ccc30f95ecc9d917ed"&&image.GetUsedRect()==new Rect2I(43,16,88,88),
       ("enemy_elite_mirror_djinn","hurt.png")=>frameSha=="1937181fda251f28c168ebf1865571eeef92db74b92c8604c64053d70bd98c44"&&image.GetUsedRect()==new Rect2I(52,20,91,80),
       ("enemy_elite_mirror_djinn","idle.png")=>frameSha=="6ea4edc36689b3ceacfcaf9715b520134ba84182c0397ec8a0056ee1f7eece05"&&image.GetUsedRect()==new Rect2I(43,16,91,86),
       ("enemy_elite_mirror_djinn","mirrorprepare.png")=>frameSha=="0809e2283ab02a4d31a22b3a7d333685f73341caa34b49ad3a33a1f9bd6bb67d"&&image.GetUsedRect()==new Rect2I(50,16,84,88),
       ("enemy_elite_mirror_djinn","rebound.png")=>frameSha=="f3ccc590b92564ff3890c8d8d726aa2efe8244ec5ed10857997375f86f5247d1"&&image.GetUsedRect()==new Rect2I(59,18,78,83),
       ("enemy_elite_mirror_djinn","recovery.png")=>frameSha=="fedce946998fb09ef04f0f0ec01ef87e3d688b5bc19c4f4cb0f4a51dbfaf8921"&&image.GetUsedRect()==new Rect2I(43,19,96,83),
       ("enemy_elite_mirror_djinn","rest.png")=>frameSha=="64a698cc41f1933c29d0f7e7f73c0d2545c59f019b61a459292d94071bdbf39b"&&image.GetUsedRect()==new Rect2I(44,16,87,88),
       ("enemy_elite_devourer","bitecontact.png")=>frameSha=="6fc5b1e943e45b94e1a983aae787cd3c4427cac56d6529fd4942a2ea173f8a29"&&image.GetUsedRect()==new Rect2I(9,36,178,84),
       ("enemy_elite_devourer","biteprepare.png")=>frameSha=="8bb66e199fc19477f5cff85ff7138c53cc17dc3c9397a8cfe8f6a634d0c404cd"&&image.GetUsedRect()==new Rect2I(38,6,147,114),
       ("enemy_elite_devourer","devourapplied.png")=>frameSha=="e1f9d4e16ffc951cd5bb541a4e8e7e47d5a4e5831439142282fb2f5eba2a660d"&&image.GetUsedRect()==new Rect2I(38,34,138,86),
       ("enemy_elite_devourer","devourprepare.png")=>frameSha=="eb15017297f13bef3974bc88f698616086146fa400312013062ad40607a21f17"&&image.GetUsedRect()==new Rect2I(20,9,164,111),
       ("enemy_elite_devourer","idle_00.png")=>frameSha=="9b24e309e31755b17bfbbcc5a643b3d1aa3045755f764f414e0aead7662dd80f"&&image.GetUsedRect()==new Rect2I(27,32,154,88),
       ("enemy_elite_devourer","idle_01.png")=>frameSha=="04ea2e5fbdbebddcfac3df9c07fe9cb5b733eb63f1541fdd0b8cbcbc0fc50f13"&&image.GetUsedRect()==new Rect2I(27,31,154,89),
       ("enemy_elite_devourer","idle_02.png")=>frameSha=="89d45a82141dbca51ec0966c5dd0b3764ee6979a9633428caf3b0bbbb2cd25c8"&&image.GetUsedRect()==new Rect2I(26,31,155,89),
       ("enemy_elite_devourer","idle_03.png")=>frameSha=="dccb7272eb4256f6db730e7c30d43596461ce03a8e2c2b26794a92d18f7987cf"&&image.GetUsedRect()==new Rect2I(26,30,155,90),
       ("enemy_elite_devourer","idle_04.png")=>frameSha=="077d3a8f2538892169411cae74b3aed445b3f2781289fab0a1dfeacfea4ebb28"&&image.GetUsedRect()==new Rect2I(27,30,154,90),
       ("enemy_elite_devourer","idle_05.png")=>frameSha=="d7d915adcda9710b4ca02d570edc88326963c9d185ca5323b5c7a9350af0df7e"&&image.GetUsedRect()==new Rect2I(28,31,153,89),
       ("enemy_elite_devourer","idle_06.png")=>frameSha=="bec763720b2360ae32c45ebe880ae91715d88b3f1ae51b03300aac8c12f1d791"&&image.GetUsedRect()==new Rect2I(28,32,153,88),
       ("enemy_elite_devourer","idle_07.png")=>frameSha=="89433730a09878c1f81ab6834b86a7e8985b33723b99ab3d5f013caa8445a9a4"&&image.GetUsedRect()==new Rect2I(27,32,154,88),
       ("enemy_elite_devourer","hurt.png")=>frameSha=="cc4fc6b8e206cdd410b9a06c2eab136ddc66b44b9104584f5b2dc5077f0338c4"&&image.GetUsedRect()==new Rect2I(55,28,134,92),
       ("enemy_elite_devourer","idle.png")=>frameSha=="51ce13b80a91461700f48f256621eff8a9a086fc3fbf5c9a2fa3c2ef15e38b88"&&image.GetUsedRect()==new Rect2I(25,27,161,93),
       ("enemy_elite_devourer","recovery.png")=>frameSha=="504521a419514b7b696a711818025ac205d3fc3a8856a8eac5eb946d5a3a01bb"&&image.GetUsedRect()==new Rect2I(26,32,160,88),
       ("enemy_elite_devourer","rest.png")=>frameSha=="9b24e309e31755b17bfbbcc5a643b3d1aa3045755f764f414e0aead7662dd80f"&&image.GetUsedRect()==new Rect2I(27,32,154,88),
       ("enemy_elite_devourer","source-rest.png")=>frameSha=="333e5b9daa417e8442939e173ff88f6bc89e3940da24d9d7fe2f55e34296ea34"&&image.GetUsedRect()==new Rect2I(27,16,154,88),
       _=>false}))throw new InvalidOperationException("Unapproved M57 exact frame/bounds");
      if(strictM58&&!((enemyId,file) switch{
       ("enemy_raptor_egg","hatchprepare.png")=>frameSha=="79ad232860af0c73c0cc823f0af24b6c6790cacd627bce52ca6aad7e1e265474"&&image.GetUsedRect()==new Rect2I(11,15,50,43),
       ("enemy_raptor_egg","idle_00.png")=>frameSha=="66d25c569f50f6805ab8d25d9315cd3ed718668d23161b6b3b9e7752c003e58c"&&image.GetUsedRect()==new Rect2I(11,14,49,44),
       ("enemy_raptor_egg","idle_01.png")=>frameSha=="a9be0599617652c0b56fd0b6c291a340068ae914476854cbac1ceed2781f3c19"&&image.GetUsedRect()==new Rect2I(11,14,49,44),
       ("enemy_raptor_egg","idle_02.png")=>frameSha=="f8a80fcc19259bfbbea7729ae1d49b6267d90b9bb30977014316dd66c40f3229"&&image.GetUsedRect()==new Rect2I(11,14,49,44),
       ("enemy_raptor_egg","idle_03.png")=>frameSha=="a66bb4b240d8f2ca54d07fe404456e377366ad1dd4cb08a6cc82a898752c299a"&&image.GetUsedRect()==new Rect2I(11,13,49,45),
       ("enemy_raptor_egg","idle_04.png")=>frameSha=="1351f5d248d535f26cbc8de21c549f3ca16bac877ea76cb2e786baac1ea46b51"&&image.GetUsedRect()==new Rect2I(11,14,49,44),
       ("enemy_raptor_egg","idle_05.png")=>frameSha=="f72297d760b6a6ae953d8cf35fed67873217d91768d71e39764f1529661c2749"&&image.GetUsedRect()==new Rect2I(11,15,49,43),
       ("enemy_raptor_egg","idle_06.png")=>frameSha=="0be889b834c3bb46f959652173dd9321b8ca8805098f7283f16ebec5ad0701de"&&image.GetUsedRect()==new Rect2I(11,15,49,43),
       ("enemy_raptor_egg","idle_07.png")=>frameSha=="c057af437e79afab477564d1df662e1b14eb372734439ed838bb75d5e3768362"&&image.GetUsedRect()==new Rect2I(11,14,49,44),
       ("enemy_raptor_egg","hurt.png")=>frameSha=="5414ae9ea1cb14ce594e5b700e4338df82d60296561979494e86d91eae154318"&&image.GetUsedRect()==new Rect2I(12,14,48,44),
       ("enemy_raptor_egg","idle.png")=>frameSha=="ed717bffda6ee7e066a25bb3fe1ca151b3b3972e871c8419b3b9ecf7f1acdd0a"&&image.GetUsedRect()==new Rect2I(11,14,50,44),
       ("enemy_raptor_egg","quiverhold.png")=>frameSha=="4083856ef418d847a65814170f33bb94064303dfa57b7675f303101ceac9beb3"&&image.GetUsedRect()==new Rect2I(10,12,52,46),
       ("enemy_raptor_egg","quiverprepare.png")=>frameSha=="698a1359c1fa5e425a3815844cc7293b82b8d88dddf5a3a741c6de1ab0db3504"&&image.GetUsedRect()==new Rect2I(11,14,50,44),
       ("enemy_raptor_egg","recovery.png")=>frameSha=="f23e217ca7a00ccf3bc906d78a6f8dbaeaf6ace68ac114206c333e21d89ded26"&&image.GetUsedRect()==new Rect2I(11,15,50,43),
       ("enemy_raptor_egg","rest.png")=>frameSha=="66d25c569f50f6805ab8d25d9315cd3ed718668d23161b6b3b9e7752c003e58c"&&image.GetUsedRect()==new Rect2I(11,14,49,44),
       ("enemy_sludge_berserker","contacta.png")=>frameSha=="6f74523c3593f879b5207ec3f7dc40819a8712cf259695d68d5a12c95af74f60"&&image.GetUsedRect()==new Rect2I(9,16,176,88),
       ("enemy_sludge_berserker","contactb.png")=>frameSha=="83e541a61d00f46cb3cead9acd45d0c2424fc91aeeea60797c2b4e57b2cd1c3d"&&image.GetUsedRect()==new Rect2I(22,19,160,87),
       ("enemy_sludge_berserker","guardhold.png")=>frameSha=="2fb35bf13b5efeef36f94443ebd2ba8dd683a9823272729594fd66624a3f1166"&&image.GetUsedRect()==new Rect2I(84,20,98,84),
       ("enemy_sludge_berserker","guardprepare.png")=>frameSha=="d4e569abb7b36ddb2dbff2d874ad835c6125798af133ff1ba241384fbfa830a2"&&image.GetUsedRect()==new Rect2I(70,17,110,87),
       ("enemy_sludge_berserker","hugcontact.png")=>frameSha=="3406dab6d3fc9e1a4347a7d446a9cc495e5db1ea8dc084e63b22f62218a8fb10"&&image.GetUsedRect()==new Rect2I(76,15,104,89),
       ("enemy_sludge_berserker","hugprepare.png")=>frameSha=="5220e15b672f27c4cb8f0b2d0ab864a53fd94c43139721695318bdac0a66167f"&&image.GetUsedRect()==new Rect2I(58,15,124,89),
       ("enemy_sludge_berserker","idle_00.png")=>frameSha=="c923d2c33763fdce56b2a768279bb38751445e25fdb86599c4e421df73d9d888"&&image.GetUsedRect()==new Rect2I(32,16,143,88),
       ("enemy_sludge_berserker","idle_01.png")=>frameSha=="e82dfb70b0cfb0b6438c57d0d11502d1994d31bd87ee07c7c121871bb9eeb562"&&image.GetUsedRect()==new Rect2I(32,15,143,89),
       ("enemy_sludge_berserker","idle_02.png")=>frameSha=="6810e6685e848b895b1dd9041421060d5bfb4ce04cc0f91eb35faf30ffa73423"&&image.GetUsedRect()==new Rect2I(32,15,143,89),
       ("enemy_sludge_berserker","idle_03.png")=>frameSha=="00ad6d25c55268abeb2c9679ffa42548f9a0aa4455a067ffa396b0edbd99ce6b"&&image.GetUsedRect()==new Rect2I(33,14,143,90),
       ("enemy_sludge_berserker","idle_04.png")=>frameSha=="50d67a008ccc4388c59470ebf06e462173acdba0d03edb3c4920dc939257df56"&&image.GetUsedRect()==new Rect2I(33,14,143,90),
       ("enemy_sludge_berserker","idle_05.png")=>frameSha=="5b706ba48c4e6b3f5d5a0508d531b1411a9add97ff1a0567a19c0ca31930b9af"&&image.GetUsedRect()==new Rect2I(32,15,143,89),
       ("enemy_sludge_berserker","idle_06.png")=>frameSha=="309e6709216068e2762e0a10db96e5ea7d21cff4513f08100217d38817f89e13"&&image.GetUsedRect()==new Rect2I(31,16,144,88),
       ("enemy_sludge_berserker","idle_07.png")=>frameSha=="ee0fa0311297f6350112c8541bd9f0f17b6147ee040fb3ec9d9cbed4b39d31b1"&&image.GetUsedRect()==new Rect2I(31,16,144,88),
       ("enemy_sludge_berserker","hurt.png")=>frameSha=="54fc9968997baf5522ab0c54fc5b145c37a928d30f1dc147f0d91137ad408591"&&image.GetUsedRect()==new Rect2I(79,13,114,91),
       ("enemy_sludge_berserker","idle.png")=>frameSha=="02d44fd022b6f7218e4e9c58191d7399057498ebd3ba725ca92f95b1f2c089ec"&&image.GetUsedRect()==new Rect2I(42,16,144,88),
       ("enemy_sludge_berserker","pummelprepare.png")=>frameSha=="c66c1e962359abad49df64e55a044699e3abc2e4620577af0592fb2281e7f8d9"&&image.GetUsedRect()==new Rect2I(65,13,123,91),
       ("enemy_sludge_berserker","rebound.png")=>frameSha=="6b0aae74fd0f82b3f3e1f21247e7d65c0702bb8e79d43236405a4c9fd0564d87"&&image.GetUsedRect()==new Rect2I(59,11,129,93),
       ("enemy_sludge_berserker","recovery.png")=>frameSha=="b3a0a0f6c7ddef6b9f8116f0867ccc8e84f06c45c093706450cba3e616e93046"&&image.GetUsedRect()==new Rect2I(33,15,152,89),
       ("enemy_sludge_berserker","rest-original.png")=>frameSha=="123ed0f646a420bdb96747b935c22264f173ea6f0cb50d23f1e6f7e0adca8e33"&&image.GetUsedRect()==new Rect2I(8,16,143,88),
       ("enemy_sludge_berserker","rest.png")=>frameSha=="c923d2c33763fdce56b2a768279bb38751445e25fdb86599c4e421df73d9d888"&&image.GetUsedRect()==new Rect2I(32,16,143,88),
       ("enemy_sludge_berserker","smothercontact.png")=>frameSha=="48dd02024920292f747a6717895fd78f64fcf90fee8ade1910bad74b68756be2"&&image.GetUsedRect()==new Rect2I(52,15,128,89),
       ("enemy_sludge_berserker","smotherprepare.png")=>frameSha=="41873dca86b66b3f6ea03c12b2b1c9d0099d13e23af48fb8029a512c69953ea8"&&image.GetUsedRect()==new Rect2I(58,15,127,89),
       ("enemy_sludge_berserker","vomitapplied.png")=>frameSha=="f1f7ce9e03b5f350032e3b5fc3dbc1c4ed7246f21ee108912ba26d351d0504b6"&&image.GetUsedRect()==new Rect2I(55,20,130,84),
       ("enemy_sludge_berserker","vomitprepare.png")=>frameSha=="524b55f7793f6a9474360438fdad0d7add4bd5892f19dc7f887454bfa4b523c3"&&image.GetUsedRect()==new Rect2I(56,9,134,95),
       _=>false}))throw new InvalidOperationException("Unapproved M58 exact frame/bounds");
      if(strictM59&&!((enemyId,file) switch{
       ("enemy_moss_healer","guardhold.png")=>frameSha=="5ecac646be057ed01f385e423ebadff105911e60087ba86c5d1d0642e28e0fe5"&&image.GetUsedRect()==new Rect2I(40,17,48,63),
       ("enemy_moss_healer","guardprepare.png")=>frameSha=="422b088b1bcb35a3bbec1d0c36882c611c3eba0c359b390a1cdcae6ceef533c2"&&image.GetUsedRect()==new Rect2I(32,15,63,65),
       ("enemy_moss_healer","idle_00.png")=>frameSha=="ba2afa2c40643d1409b09d63ada2ef4cf330ed90a0feab0096d2dc5e8e85d71c"&&image.GetUsedRect()==new Rect2I(26,16,60,64),
       ("enemy_moss_healer","idle_01.png")=>frameSha=="113120a5207b3939ef275a194da5fcd6a2b2f07de8edc2e5f813dde6db52101e"&&image.GetUsedRect()==new Rect2I(26,17,60,63),
       ("enemy_moss_healer","idle_02.png")=>frameSha=="8cc6d953019e963582d4b878dbd7cb662936e3d00796a5d39063babf83397c66"&&image.GetUsedRect()==new Rect2I(26,17,60,63),
       ("enemy_moss_healer","idle_03.png")=>frameSha=="69fcf775d5e084b06178d037279a4007196aa2dde5b53cd36fc789668cda79e6"&&image.GetUsedRect()==new Rect2I(26,18,61,62),
       ("enemy_moss_healer","idle_04.png")=>frameSha=="59f6bee37aa9f195af71c4762a4b24b75d414db0a63527979a4ba750be0ddb15"&&image.GetUsedRect()==new Rect2I(26,17,61,63),
       ("enemy_moss_healer","idle_05.png")=>frameSha=="21c0b137a6d21357a744b095fde49bfe46c43077ed91a1b57e77a9a045df2dee"&&image.GetUsedRect()==new Rect2I(26,17,61,63),
       ("enemy_moss_healer","idle_06.png")=>frameSha=="01116c00f2c458277ce1c267e405a69fdbbf8e99da129bd061702c8071e9cce0"&&image.GetUsedRect()==new Rect2I(26,16,61,64),
       ("enemy_moss_healer","idle_07.png")=>frameSha=="4a33cfa4ae2e84cba290bf16a9a50a177329362545e70474dda15430914761d8"&&image.GetUsedRect()==new Rect2I(26,16,60,64),
       ("enemy_moss_healer","hurt.png")=>frameSha=="d3ed819f16f0d9f4a0bac7226621c5b64c1edc177582608c7f8653c63623726a"&&image.GetUsedRect()==new Rect2I(22,19,63,61),
       ("enemy_moss_healer","idle.png")=>frameSha=="5edc5c55833dacb7873f23a94a98e4b38cb007233ba8453591e0bab3ab0c7118"&&image.GetUsedRect()==new Rect2I(27,16,61,64),
       ("enemy_moss_healer","mendapplied.png")=>frameSha=="accb861032873570e8cb6800eb7d928ab90dab0a7a104513bead634464dc4442"&&image.GetUsedRect()==new Rect2I(30,8,58,72),
       ("enemy_moss_healer","mendprepare.png")=>frameSha=="7f4b66a3b50e78f69c76f87bd26d39a27408d581069f555f4840e79633b6028a"&&image.GetUsedRect()==new Rect2I(32,10,56,70),
       ("enemy_moss_healer","pokecontact.png")=>frameSha=="d227dbd4ff17b950d498eab5b26b2c3d95f855f3a4b768456a9e9d3c07d1b80a"&&image.GetUsedRect()==new Rect2I(7,22,96,58),
       ("enemy_moss_healer","pokeprepare.png")=>frameSha=="cf9aed58c2f210a3a5161cdcc2afb6ac054d66519ad33aaa95dcbd21648e1fc7"&&image.GetUsedRect()==new Rect2I(32,18,65,62),
       ("enemy_moss_healer","recovery.png")=>frameSha=="7462607baad54f3eb37cf86991da4b58988f09f9606bba68e43d705719fea950"&&image.GetUsedRect()==new Rect2I(23,17,64,63),
       ("enemy_moss_healer","rest-original.png")=>frameSha=="019f847aa4f978f35b9428c02d123aa8ccc19854c48f96ddd5b71a2f87013a64"&&image.GetUsedRect()==new Rect2I(14,16,60,64),
       ("enemy_moss_healer","rest.png")=>frameSha=="ba2afa2c40643d1409b09d63ada2ef4cf330ed90a0feab0096d2dc5e8e85d71c"&&image.GetUsedRect()==new Rect2I(26,16,60,64),
       ("enemy_thunder_globe","burstprepare.png")=>frameSha=="fe3876fc6d657b974e0a363388f3ee6a28b3b51db0971d4e9a9a485223e2d566"&&image.GetUsedRect()==new Rect2I(49,14,100,79),
       ("enemy_thunder_globe","burstrelease.png")=>frameSha=="b010e4d629830e9b560a78154ece5d60fceca1f33fbb5d84dc2fba7648527675"&&image.GetUsedRect()==new Rect2I(8,14,153,71),
       ("enemy_thunder_globe","guardhold.png")=>frameSha=="53803c6356d7ab8aa84f995038cd14e1039131067884cd87e7bd11b06e3e5fb0"&&image.GetUsedRect()==new Rect2I(59,13,77,77),
       ("enemy_thunder_globe","guardprepare.png")=>frameSha=="2b1311fcbb798ab2f6bd1aaface40b5c10707c3b048d1505d8ed02bbb1677445"&&image.GetUsedRect()==new Rect2I(56,14,88,78),
       ("enemy_thunder_globe","idle_00.png")=>frameSha=="e7fdbddc945dd1a5bedd32efc0f504e032086a79486c3def97a16244d040bdbd"&&image.GetUsedRect()==new Rect2I(17,16,133,88),
       ("enemy_thunder_globe","idle_01.png")=>frameSha=="81d0975e4726b8a3a30cd1949b28b88bb56462b8aad8b1ebf8503d9b899eed58"&&image.GetUsedRect()==new Rect2I(17,16,132,88),
       ("enemy_thunder_globe","idle_02.png")=>frameSha=="d2f69e72511b80e0bbcbd1e269a59a19871d501857273f3bd974d5446c5ec6da"&&image.GetUsedRect()==new Rect2I(18,16,131,87),
       ("enemy_thunder_globe","idle_03.png")=>frameSha=="d0d9a9a56167ecf5dfc21c4815dd2af762c3b26eea94a0ed68211f27116339b7"&&image.GetUsedRect()==new Rect2I(19,16,130,86),
       ("enemy_thunder_globe","idle_04.png")=>frameSha=="fc2a4151b3173c7fdd3eafc150944e405d6d0999fbaa975e88509a9756b90374"&&image.GetUsedRect()==new Rect2I(19,16,131,86),
       ("enemy_thunder_globe","idle_05.png")=>frameSha=="678bf9e8cc54627ec729ef216b5b09298f477077d9807dd271883070c5dee9e7"&&image.GetUsedRect()==new Rect2I(18,16,133,86),
       ("enemy_thunder_globe","idle_06.png")=>frameSha=="1b99afcd77b4d9c79b39ee1b013dbc78e016ec75b1c732a1e0ef0512ece384f6"&&image.GetUsedRect()==new Rect2I(18,16,133,87),
       ("enemy_thunder_globe","idle_07.png")=>frameSha=="d1502289bc00545bfa996b1373a61a1b3126a46193a7fcb5460d313c5c0cc512"&&image.GetUsedRect()==new Rect2I(17,16,134,88),
       ("enemy_thunder_globe","hurt.png")=>frameSha=="67301c3fa9882b8ebf6546c2b38b5b1db94d87fbbbacaf46ffa7d4e30c432e58"&&image.GetUsedRect()==new Rect2I(40,15,93,86),
       ("enemy_thunder_globe","idle.png")=>frameSha=="bda848ab68408cf0dcf29f5c6d0010e2b24a9a15e3779054c7ce601af9d9feef"&&image.GetUsedRect()==new Rect2I(24,16,121,84),
       ("enemy_thunder_globe","recovery.png")=>frameSha=="fd6aea0d02ec6f727c6f013288b1c88b7be118a041b52373febfd81b000745b5"&&image.GetUsedRect()==new Rect2I(21,16,126,86),
       ("enemy_thunder_globe","rest-original.png")=>frameSha=="e7fdbddc945dd1a5bedd32efc0f504e032086a79486c3def97a16244d040bdbd"&&image.GetUsedRect()==new Rect2I(17,16,133,88),
       ("enemy_thunder_globe","rest.png")=>frameSha=="e7fdbddc945dd1a5bedd32efc0f504e032086a79486c3def97a16244d040bdbd"&&image.GetUsedRect()==new Rect2I(17,16,133,88),
       ("enemy_thunder_globe","slapcontact.png")=>frameSha=="0f60c55698f4df4e874b2db44f47e4798557658ee0e589b1363e1dbbb3904d00"&&image.GetUsedRect()==new Rect2I(10,14,128,90),
       ("enemy_thunder_globe","slapprepare.png")=>frameSha=="31afeeba686a61341883ece5b41ba4d0ab7e468187ff8cd2d1ba6c88ef93067c"&&image.GetUsedRect()==new Rect2I(38,15,104,88),
       ("enemy_thunder_globe","thunderprepare.png")=>frameSha=="fe7b64099de2540a101cd443603043dc34e1e693a8acd3aebc6dca560ac58251"&&image.GetUsedRect()==new Rect2I(46,13,101,87),
       ("enemy_thunder_globe","thunderrebound.png")=>frameSha=="da1d0cecfb9ec8e24d41aa1d8d2cd2dfceb4008d5d100fd10b242035105dd528"&&image.GetUsedRect()==new Rect2I(34,13,110,89),
       ("enemy_thunder_globe","thunderrelease.png")=>frameSha=="972f068b583f5a2a9cdf3705d811582d75dafdaf16ed3b78aa7091ddaa8ea659"&&image.GetUsedRect()==new Rect2I(14,13,132,89),
       _=>false}))throw new InvalidOperationException("Unapproved M59 exact frame/bounds");
      if(strictM60&&!((enemyId,file) switch{
       ("enemy_kin_follower","boomerangprepare.png")=>frameSha=="5d4c014f9c1213d528647e678121c9fee2a5e7cdbc557496dad611b03e7e5b3e"&&image.GetUsedRect()==new Rect2I(39,21,96,87),
       ("enemy_kin_follower","boomerangrelease.png")=>frameSha=="a850343f1db15edd0c44f6b6b35d6f164d508d31e3f7b0efb86397f306cdbd7a"&&image.GetUsedRect()==new Rect2I(23,24,107,84),
       ("enemy_kin_follower","dancehold.png")=>frameSha=="9138197fa50638ed7fe2e2fcb798d9008a2a9630fa26ecb36ffbb3aaf22cd2c9"&&image.GetUsedRect()==new Rect2I(48,3,90,105),
       ("enemy_kin_follower","danceprepare.png")=>frameSha=="417f66a226ff7ad8b64478a455ffb0f2c824b4b1e96ba29ce7765295c372480c"&&image.GetUsedRect()==new Rect2I(49,3,85,105),
       ("enemy_kin_follower","guardhold.png")=>frameSha=="6ecc67873d23de1b908e06872099e5f6dd3a501083533d581650e5faea0ee9cf"&&image.GetUsedRect()==new Rect2I(39,30,84,78),
       ("enemy_kin_follower","guardprepare.png")=>frameSha=="52f91d78f7f0bba2784de095a66311ad61910e9165e03d1519760272569fb4df"&&image.GetUsedRect()==new Rect2I(39,27,93,81),
       ("enemy_kin_follower","idle_00.png")=>frameSha=="badc41f2a3eb50ad464eef8e2ded9694eef9d220f6147e50626948b0509702fb"&&image.GetUsedRect()==new Rect2I(31,20,97,88),
       ("enemy_kin_follower","idle_01.png")=>frameSha=="affb2ec9f630ccc8fa0fcdda15a7d2ebc7d6365062cf9f2b1b2630794c2a6bd0"&&image.GetUsedRect()==new Rect2I(31,19,97,89),
       ("enemy_kin_follower","idle_02.png")=>frameSha=="1ee5031ff5b2fe672d19ecf8b58ec8e896dcd275bed4783ebe47a2fdced0b2af"&&image.GetUsedRect()==new Rect2I(31,19,97,89),
       ("enemy_kin_follower","idle_03.png")=>frameSha=="506d35145da30036b36602053fa089f8b0b910f1d3129ceb05eb5b7d45f45d5e"&&image.GetUsedRect()==new Rect2I(31,19,97,89),
       ("enemy_kin_follower","idle_04.png")=>frameSha=="76bcfb2e4f3ec691a5e683d3b75b99a95ee96fee75677dcfe332133cdaa263d4"&&image.GetUsedRect()==new Rect2I(31,20,97,88),
       ("enemy_kin_follower","idle_05.png")=>frameSha=="4622f21a3979ccf57c52c5a98ad76ff09d9264c2f65b23af3ed56e55002414cf"&&image.GetUsedRect()==new Rect2I(31,20,97,88),
       ("enemy_kin_follower","idle_06.png")=>frameSha=="8b3632ed37d64aff0cf8df1dd5ba40c6eab05da8f23cbda4c707644b08cb2be7"&&image.GetUsedRect()==new Rect2I(31,20,98,88),
       ("enemy_kin_follower","idle_07.png")=>frameSha=="94305f3dd5b109fe71e6261a5a8616bea47dd3a6458eee2cc82248641b6ff28c"&&image.GetUsedRect()==new Rect2I(31,20,98,88),
       ("enemy_kin_follower","hurt.png")=>frameSha=="f276835bb3485b5c1968cc172ff3b3873d275147c3db18bbbda94db2172b3d7a"&&image.GetUsedRect()==new Rect2I(38,21,89,87),
       ("enemy_kin_follower","idle.png")=>frameSha=="030d3827f58b436575407c87171f92e4f354401c67747b7472bd7c363bafedd4"&&image.GetUsedRect()==new Rect2I(26,17,100,91),
       ("enemy_kin_follower","recovery.png")=>frameSha=="6bedc3145840ce9250f3f83e0edb3abfe2352fab36af2a1389ea871fa5bd3384"&&image.GetUsedRect()==new Rect2I(30,22,102,86),
       ("enemy_kin_follower","rest-original.png")=>frameSha=="d97d3e3f4e7d932255e5a28d7edb78ac5ff3f6abce44bce1e15354afaa49277d"&&image.GetUsedRect()==new Rect2I(31,16,97,88),
       ("enemy_kin_follower","rest.png")=>frameSha=="badc41f2a3eb50ad464eef8e2ded9694eef9d220f6147e50626948b0509702fb"&&image.GetUsedRect()==new Rect2I(31,20,97,88),
       ("enemy_kin_follower","slashcontact.png")=>frameSha=="f249e08f04dbe963f1a0bc9e5663e3152a1d00b6fb05b6ad9e2f5b727d578bce"&&image.GetUsedRect()==new Rect2I(22,29,112,79),
       ("enemy_kin_follower","slashprepare.png")=>frameSha=="f6d4b4702c84b16ab7d2d4bcd5a35b607894f561720102852824412e11774504"&&image.GetUsedRect()==new Rect2I(39,7,94,101),
       ("enemy_crab_crusher","adapthold.png")=>frameSha=="8f282c6319f56fe4b36a819ffdb7ad4613021f69f5d01deec00dbd05333f3992"&&image.GetUsedRect()==new Rect2I(67,27,172,80),
       ("enemy_crab_crusher","adaptprepare.png")=>frameSha=="a2e25823829b851eec72852354c39c096950b8901ff491f1ed7a6d8cdbc1574f"&&image.GetUsedRect()==new Rect2I(66,27,173,80),
       ("enemy_crab_crusher","guardedcontact.png")=>frameSha=="72c5fbe5676107309860972ab5e0bcb440239feb76a8aa387f74ca3544e2a1ee"&&image.GetUsedRect()==new Rect2I(48,25,191,84),
       ("enemy_crab_crusher","guardedprepare.png")=>frameSha=="b713d5d14ab985a1c74b2712b99ee87a82c559e06262bb7bc5bcfda8520d04cf"&&image.GetUsedRect()==new Rect2I(104,27,135,80),
       ("enemy_crab_crusher","guardhold.png")=>frameSha=="18653aca8c752d51936388af4198bb17ea1a4825773a6acbffb4076980fdf6c3"&&image.GetUsedRect()==new Rect2I(126,25,113,84),
       ("enemy_crab_crusher","guardprepare.png")=>frameSha=="1f32e26f04145b854d9e19cdb7244e505bfb4e6b0aa2cc7decb70d9780392d93"&&image.GetUsedRect()==new Rect2I(95,15,144,94),
       ("enemy_crab_crusher","idle_00.png")=>frameSha=="977ad32ce70ba5144494464eba79092517bf9df46e600ae08412b958297bbaa9"&&image.GetUsedRect()==new Rect2I(48,22,191,90),
       ("enemy_crab_crusher","idle_01.png")=>frameSha=="1050b16af27ecc7ee34c119f06383fb72f98b8c422a0660d05df8e92ac9dfe04"&&image.GetUsedRect()==new Rect2I(48,21,191,91),
       ("enemy_crab_crusher","idle_02.png")=>frameSha=="59a32a951d50b172f00685ae82e4bfd972d1c4e8440b82dcb9dc6e5e5dd82fe1"&&image.GetUsedRect()==new Rect2I(48,20,191,92),
       ("enemy_crab_crusher","idle_03.png")=>frameSha=="337419035a38605326c458f1218d41fb638ed8d7eca2c1f499e1f3835e108acd"&&image.GetUsedRect()==new Rect2I(48,19,191,93),
       ("enemy_crab_crusher","idle_04.png")=>frameSha=="da67d7833a2cf81fe81d04c55b2e29e33a7b414e107442eb24ab451998a7fdd7"&&image.GetUsedRect()==new Rect2I(48,20,191,92),
       ("enemy_crab_crusher","idle_05.png")=>frameSha=="7a97d8a4f477d70e1c85178900e59d32cf33c2f32dd5e4c99018e8c84b1a620e"&&image.GetUsedRect()==new Rect2I(48,21,191,91),
       ("enemy_crab_crusher","idle_06.png")=>frameSha=="50d2f92a104038858f5c3c971d2ce1d5b1e094346cde007e204ed1031eb23b7a"&&image.GetUsedRect()==new Rect2I(48,22,191,90),
       ("enemy_crab_crusher","idle_07.png")=>frameSha=="c2fbbdc422ee1f4cc791d41e718077ae55f14d420fcadfcbd0e7fa0af808acac"&&image.GetUsedRect()==new Rect2I(48,22,191,90),
       ("enemy_crab_crusher","hurt.png")=>frameSha=="063c7280890245b03d4e2dc43ffab8b715e515246326449905d92d23b3825637"&&image.GetUsedRect()==new Rect2I(99,24,140,86),
       ("enemy_crab_crusher","idle.png")=>frameSha=="740c5b56c5ef84af6a3d398074360a14bae3bbaa4e6b5068fa47805519204496"&&image.GetUsedRect()==new Rect2I(56,24,183,87),
       ("enemy_crab_crusher","recovery.png")=>frameSha=="840f97969f3339d4acedcc91600736384f8b1c7aaa4daf9fd98b8df9c972dc2d"&&image.GetUsedRect()==new Rect2I(57,24,182,86),
       ("enemy_crab_crusher","rest-original.png")=>frameSha=="d8cb9dd29facdbe3eb92deb5e5c8767cb1afd7ccf948db340fc3cf038f4122b2"&&image.GetUsedRect()==new Rect2I(16,22,191,90),
       ("enemy_crab_crusher","rest.png")=>frameSha=="977ad32ce70ba5144494464eba79092517bf9df46e600ae08412b958297bbaa9"&&image.GetUsedRect()==new Rect2I(48,22,191,90),
       ("enemy_crab_crusher","stingcontacta.png")=>frameSha=="49bdc43603295a58c47c547a1662d6682a623a3e0bbca623d525e56eeacb736c"&&image.GetUsedRect()==new Rect2I(33,26,206,82),
       ("enemy_crab_crusher","stingcontactb.png")=>frameSha=="ab743f6b3576a1d51c2185c14a1730843bb75b8aa383446a1ecfd96e07c5687a"&&image.GetUsedRect()==new Rect2I(52,26,187,82),
       ("enemy_crab_crusher","stingprepare.png")=>frameSha=="5b8dce8edcb196a13ed223487c10ae0242c083806ea19400e1c3b93246688b60"&&image.GetUsedRect()==new Rect2I(56,26,183,83),
       ("enemy_crab_crusher","stingrebound.png")=>frameSha=="893e89e826c3195ce19073e7091e06f13835b7455c767e1111e96c4eef7cc108"&&image.GetUsedRect()==new Rect2I(55,26,184,82),
       ("enemy_crab_crusher","swellcontact.png")=>frameSha=="5132fdcf0ca5541a529ac30e0d23524e009deafd7318c70f84c5cf837f8270cb"&&image.GetUsedRect()==new Rect2I(8,22,231,86),
       ("enemy_crab_crusher","swellprepare.png")=>frameSha=="b279fc2776ee77a276d96540beeddebbcca83a9cb7d13213fe4098c7fc51f07f"&&image.GetUsedRect()==new Rect2I(46,22,193,87),
       ("enemy_crab_crusher","thrashcontact.png")=>frameSha=="f4de35e863ba86eb152a68e759ba83bb5721b26cfbe92ef0f0f8838b70e6c80b"&&image.GetUsedRect()==new Rect2I(91,24,148,87),
       ("enemy_crab_crusher","thrashprepare.png")=>frameSha=="1d06e7801720657ea31f9160328b2177c453fc3a5f26e68babb5d49a5f98be41"&&image.GetUsedRect()==new Rect2I(70,19,169,91),
       _=>false}))throw new InvalidOperationException("Unapproved M60 exact frame/bounds");
      if(strictM61&&!((enemyId,file) switch{
       ("enemy_moss_spawner","guardhold.png")=>frameSha=="2a491269162a933a9a307827202ca225c40cb8898a09c03aa28b8796f2f6662e"&&image.GetUsedRect()==new Rect2I(64,41,91,79),
       ("enemy_moss_spawner","guardprepare.png")=>frameSha=="ef84fce8d0e969bd5547943c5a37ea97df9eec3f3e4b28d7baf712fa33bb1ebe"&&image.GetUsedRect()==new Rect2I(56,35,105,85),
       ("enemy_moss_spawner","idle_00.png")=>frameSha=="e7826c017719274dbd6ce35b4ca4f83964bfd3a1ec560067a1760b06ec9684b5"&&image.GetUsedRect()==new Rect2I(46,32,116,88),
       ("enemy_moss_spawner","idle_01.png")=>frameSha=="36cbf47b23af3ebcee4ba8c86216b9ed219740afc2f4ef3d7de89d156a3f570b"&&image.GetUsedRect()==new Rect2I(46,31,116,89),
       ("enemy_moss_spawner","idle_02.png")=>frameSha=="7e802536185ebadbb773199f71a59ba3b11873b5b738ed9d2fb7a50de4fa8d0e"&&image.GetUsedRect()==new Rect2I(45,31,117,89),
       ("enemy_moss_spawner","idle_03.png")=>frameSha=="f21ffab027d9ee3f29fbee730631ff222b6b97c44462eb1a2972e99af9a3cf4e"&&image.GetUsedRect()==new Rect2I(45,30,117,90),
       ("enemy_moss_spawner","idle_04.png")=>frameSha=="3c54a16a8edc0f404a3a426ea07513c588c67b6754564e81f1d2bea8333ba947"&&image.GetUsedRect()==new Rect2I(46,30,116,90),
       ("enemy_moss_spawner","idle_05.png")=>frameSha=="30dbb22b8ad470f7084fab6d2386f3e7c6417e3b1e77b5d00ba66e2296e860d0"&&image.GetUsedRect()==new Rect2I(47,31,115,89),
       ("enemy_moss_spawner","idle_06.png")=>frameSha=="def8fa9daa727ab1b6a55e70c13e84e639059d90c7fdaeed5d623f37dfdf4d30"&&image.GetUsedRect()==new Rect2I(47,32,115,88),
       ("enemy_moss_spawner","idle_07.png")=>frameSha=="c5c091633a38151fbe3e4cbae630df5ad3e66003d2b985476c37ee2a5582c5bb"&&image.GetUsedRect()==new Rect2I(46,32,116,88),
       ("enemy_moss_spawner","hurt.png")=>frameSha=="c348a422017231f43dc67e6b885fdb5db6a093fe5d6c1c44936658a2aedba537"&&image.GetUsedRect()==new Rect2I(60,21,101,99),
       ("enemy_moss_spawner","idle.png")=>frameSha=="723c27a1bb5fe77635b3d454df4b5d587ae53dba7d5a8570ed576102ae7c5a27"&&image.GetUsedRect()==new Rect2I(35,26,125,94),
       ("enemy_moss_spawner","lashcontact.png")=>frameSha=="6d9b96f493f6989f83fab9c7dd9c7ecad5066fc0cc296f883d3f3cd1cc6ae2dd"&&image.GetUsedRect()==new Rect2I(21,41,138,79),
       ("enemy_moss_spawner","lashprepare.png")=>frameSha=="de1f474ebdec8d0694f805450011a5c2c7c4408ba0154309bfd72c711115bfc5"&&image.GetUsedRect()==new Rect2I(60,24,100,96),
       ("enemy_moss_spawner","recovery.png")=>frameSha=="676135d69e701d1ce42c06f442b5f5330e7fd03d071996d250a0b1b7df9f1d0b"&&image.GetUsedRect()==new Rect2I(40,36,120,84),
       ("enemy_moss_spawner","rest-original.png")=>frameSha=="91433444b0b8440a97b9cbbfc31004047cc8c511ebb3b5f9c77260d2c11dca83"&&image.GetUsedRect()==new Rect2I(14,16,116,88),
       ("enemy_moss_spawner","rest.png")=>frameSha=="e7826c017719274dbd6ce35b4ca4f83964bfd3a1ec560067a1760b06ec9684b5"&&image.GetUsedRect()==new Rect2I(46,32,116,88),
       ("enemy_moss_spawner","spawnapplied.png")=>frameSha=="d2827d62ec6f0a0c3a98582d27050441035dcbd81cef2aadd2585981e7a37d24"&&image.GetUsedRect()==new Rect2I(35,16,125,104),
       ("enemy_moss_spawner","spawnprepare.png")=>frameSha=="fd4ad26f33f9522c2c47b219b08c60cf30e2ec69fa399a3d32870e6a1777dba9"&&image.GetUsedRect()==new Rect2I(65,42,94,78),
       ("enemy_crab_cannon","chargeapplied.png")=>frameSha=="15ef1f15824e76544670c59f90b214a8fd59539a1dc284168702c9f4d1a7da83"&&image.GetUsedRect()==new Rect2I(34,32,225,64),
       ("enemy_crab_cannon","chargeprepare.png")=>frameSha=="e5632972bd0804a37bc4f18fb77ce90628619caa5c4de7fcd289480b0ca4871c"&&image.GetUsedRect()==new Rect2I(64,32,195,65),
       ("enemy_crab_cannon","idle_00.png")=>frameSha=="eccdffafea5a24de0dc52ca885985eeb90f49976312a22e8131cfd6cea661afb"&&image.GetUsedRect()==new Rect2I(60,24,199,72),
       ("enemy_crab_cannon","idle_01.png")=>frameSha=="bc335737261af3e9dccd28aef83df425c8c5609bc188b8221043b9b3c4ede7b0"&&image.GetUsedRect()==new Rect2I(60,25,199,71),
       ("enemy_crab_cannon","idle_02.png")=>frameSha=="f1f5338a9a8a50f21d964ebf21e3e3eb8666b1d5a46cdb866e3c557a442b5c59"&&image.GetUsedRect()==new Rect2I(60,26,199,70),
       ("enemy_crab_cannon","idle_03.png")=>frameSha=="f07f2f712df99d8d13dc97fc8bae9782070d3c10b4886d8b734063a8e014a42d"&&image.GetUsedRect()==new Rect2I(60,27,199,69),
       ("enemy_crab_cannon","idle_04.png")=>frameSha=="880c7a3e466ff68799fab26e937185a7373869e298a012383053f85482bd43d7"&&image.GetUsedRect()==new Rect2I(60,27,199,69),
       ("enemy_crab_cannon","idle_05.png")=>frameSha=="3d4dabae8af4be11063244db136261b71f2be9fba6f7bb60eec5f7b334320aaa"&&image.GetUsedRect()==new Rect2I(60,26,199,70),
       ("enemy_crab_cannon","idle_06.png")=>frameSha=="53f0ae098edde59207226cf06b54685c0f1bba9eec58437eb3aebc869a668770"&&image.GetUsedRect()==new Rect2I(60,25,199,71),
       ("enemy_crab_cannon","idle_07.png")=>frameSha=="fe9a3d521428569a827b69126632c2894594ecb945d956fce6bdcad53a6a26e6"&&image.GetUsedRect()==new Rect2I(60,24,199,72),
       ("enemy_crab_cannon","hurt.png")=>frameSha=="28381b05dd67f345a26c7533bf7a4819ac3c285033b8debbfc58b23abbe8e2e5"&&image.GetUsedRect()==new Rect2I(113,25,146,72),
       ("enemy_crab_cannon","idle.png")=>frameSha=="0ba184e45f1cc37799633c6feb41123c369110177356f1d65db787213891dc6b"&&image.GetUsedRect()==new Rect2I(59,31,200,71),
       ("enemy_crab_cannon","laserprepare.png")=>frameSha=="c545b8d9da0737d22eb3bf7fda95581e383e09acd3f9499a46321be2365c59b7"&&image.GetUsedRect()==new Rect2I(101,32,158,64),
       ("enemy_crab_cannon","laserrelease.png")=>frameSha=="0aa928e90cb18c2e50b90bdecaf76fc2fad4655131f7ebe37f8579e2385268c6"&&image.GetUsedRect()==new Rect2I(37,32,222,64),
       ("enemy_crab_cannon","precisionprepare.png")=>frameSha=="819550b0f52942b47800ace3594a1ba46094eeb332314029859b41b077d50ff4"&&image.GetUsedRect()==new Rect2I(66,32,193,64),
       ("enemy_crab_cannon","precisionrelease.png")=>frameSha=="daff1f85524f125c957750e9fd68bf85a09941ad8e47973a19b4055d47306f50"&&image.GetUsedRect()==new Rect2I(63,31,196,66),
       ("enemy_crab_cannon","recharge.png")=>frameSha=="c831ace43644b8e2afe29492cad2217fb6f04de0851372dcde0fa685a61c8d90"&&image.GetUsedRect()==new Rect2I(57,27,202,80),
       ("enemy_crab_cannon","recovery.png")=>frameSha=="4617ed7bbac9b7c4088654950adefcfce2e2fdfa3052339e3732f08ff5740f2c"&&image.GetUsedRect()==new Rect2I(55,32,204,64),
       ("enemy_crab_cannon","rest-original.png")=>frameSha=="0664171004225725051eefbe6e303517612e01d01c5a7beffb68aa5de519d3a2"&&image.GetUsedRect()==new Rect2I(28,24,199,72),
       ("enemy_crab_cannon","rest.png")=>frameSha=="eccdffafea5a24de0dc52ca885985eeb90f49976312a22e8131cfd6cea661afb"&&image.GetUsedRect()==new Rect2I(60,24,199,72),
       ("enemy_crab_cannon","reticlecontact.png")=>frameSha=="a5dc92fd420153af798e8d28d62e0703de8329a101db22cd812a2ef42bf6b623"&&image.GetUsedRect()==new Rect2I(44,32,215,64),
       ("enemy_crab_cannon","reticleprepare.png")=>frameSha=="141591961db6debc464f77f9684f043024cd2a749b3ba400f0f5b1e9b14e2d7e"&&image.GetUsedRect()==new Rect2I(67,31,192,66),
       _=>false}))throw new InvalidOperationException("Unapproved M61 exact frame/bounds");
      if(strictM62&&!((enemyId,file) switch{
       ("enemy_set_breaker","guardhold.png")=>frameSha=="bdd9cd4a802134ee333d965553e904ad62d752c1741cbc33942a42b853cd7dab"&&image.GetUsedRect()==new Rect2I(44,30,61,66),
       ("enemy_set_breaker","guardprepare.png")=>frameSha=="8224e5eb2af8565d7fde245eb10ca56808c29c99563346ebcd81bcd7bece009a"&&image.GetUsedRect()==new Rect2I(42,30,63,66),
       ("enemy_set_breaker","idle_00.png")=>frameSha=="e6e0477ce0f43dafc052895bc9e4dd512dfc5462bc1b80e651bbfc99a9edca63"&&image.GetUsedRect()==new Rect2I(34,32,76,64),
       ("enemy_set_breaker","idle_01.png")=>frameSha=="5f487adf606ce6ee085edc330fc265ab007ed1eb9612f380bbd273dbce7df3c5"&&image.GetUsedRect()==new Rect2I(34,31,76,65),
       ("enemy_set_breaker","idle_02.png")=>frameSha=="4e6cc695f26bae364cab9ef0b981ea8132c9389f117201156748e2b54013b9b7"&&image.GetUsedRect()==new Rect2I(34,30,76,66),
       ("enemy_set_breaker","idle_03.png")=>frameSha=="bf941161bf38b0bc3cd3ca005a72a95fe26c295950aa6c1941c1496d48eb04ab"&&image.GetUsedRect()==new Rect2I(33,30,77,66),
       ("enemy_set_breaker","idle_04.png")=>frameSha=="da915dada549145cca3fd25e564fd9eb669a860b752dfa243e15f59bc9f0003b"&&image.GetUsedRect()==new Rect2I(33,31,77,65),
       ("enemy_set_breaker","idle_05.png")=>frameSha=="59d51de5de6480abae17ebab356d195e5bcfef9f544255c2c6eba6ca9dce6ec8"&&image.GetUsedRect()==new Rect2I(34,31,76,65),
       ("enemy_set_breaker","idle_06.png")=>frameSha=="2ed4fc2b5c6d2168afa8bec8597743beeb30bcc06f0f26f4f820da9a07dd7023"&&image.GetUsedRect()==new Rect2I(34,32,76,64),
       ("enemy_set_breaker","idle_07.png")=>frameSha=="e2ce69d5b9a161416baeaeea02b31c297b0d22403c0e7a8806d5869b0cbab462"&&image.GetUsedRect()==new Rect2I(34,32,76,64),
       ("enemy_set_breaker","hurt.png")=>frameSha=="206d4c940913f8d77078727dbe06f9346ba5eed2c526ea69740c161f9683f185"&&image.GetUsedRect()==new Rect2I(42,31,70,65),
       ("enemy_set_breaker","idle.png")=>frameSha=="13407fd6e0a1e959001693756e98fdedfd99f71e4ca8cb2eadbca76478112641"&&image.GetUsedRect()==new Rect2I(34,30,76,66),
       ("enemy_set_breaker","recovery.png")=>frameSha=="d408f06d1fe2bf6f9188fee7f71c08d5122d50d7d7beb79a1355bdbe3f38df9a"&&image.GetUsedRect()==new Rect2I(36,30,73,66),
       ("enemy_set_breaker","rest-original.png")=>frameSha=="7cd0c3102a1104c5cf06537170b9757b1570e15c05b566a5ba2f15975263c67c"&&image.GetUsedRect()==new Rect2I(18,16,76,64),
       ("enemy_set_breaker","rest.png")=>frameSha=="e6e0477ce0f43dafc052895bc9e4dd512dfc5462bc1b80e651bbfc99a9edca63"&&image.GetUsedRect()==new Rect2I(34,32,76,64),
       ("enemy_set_breaker","smashcontact.png")=>frameSha=="fe1f262b8e90553adf0a5e78f154e05e3c99223b48ae1e0be5f3bc8f0a6127c8"&&image.GetUsedRect()==new Rect2I(15,33,90,63),
       ("enemy_set_breaker","smashprepare.png")=>frameSha=="0971e4425fa0b80f65d09b2176d65d77495888463f65615379b9a472769e34f7"&&image.GetUsedRect()==new Rect2I(46,19,62,77),
       ("enemy_set_breaker","tokenapplied.png")=>frameSha=="02522289ca0394b52d404d67c5112d89100b8b8474af773553d88b35c58f38d2"&&image.GetUsedRect()==new Rect2I(21,30,86,66),
       ("enemy_set_breaker","tokenprepare.png")=>frameSha=="1661f82f080129437769b78fb6c698b3ec868927758d0f4d4c10b9793cefa4fa"&&image.GetUsedRect()==new Rect2I(26,31,79,65),
       ("enemy_set_breaker","tosscontact.png")=>frameSha=="d2101b3387c46274a176a7ada5296e78b5490efffc916f7b7a7a2fbede97597c"&&image.GetUsedRect()==new Rect2I(28,32,80,64),
       ("enemy_set_breaker","tossprepare.png")=>frameSha=="45cacc81c9b2422dd59d1c3372b417744c8e0059e3bcc1838048940690d88cdb"&&image.GetUsedRect()==new Rect2I(29,28,83,68),
       ("enemy_set_breaker","trapcontact.png")=>frameSha=="8ec10656da7d6be0f1bc2dfe0a2b606fa195506e22fee14fc389dd9331246ce1"&&image.GetUsedRect()==new Rect2I(12,42,93,54),
       ("enemy_set_breaker","trapprepare.png")=>frameSha=="56d5938a9803b1b259c863473d1d58bff552ba16abe7111061f801972b36c526"&&image.GetUsedRect()==new Rect2I(14,45,91,51),
       ("enemy_kin_priest","beamprepare.png")=>frameSha=="3cb828df8973ee7347c05bb32a72bc52f271e45eafefdc67166fca32552d5f3b"&&image.GetUsedRect()==new Rect2I(48,24,81,112),
       ("enemy_kin_priest","beamrebound.png")=>frameSha=="fd9c985ed40a9fafb8bc747258ea62587908ac8a769e545fc53219ff2f05f003"&&image.GetUsedRect()==new Rect2I(42,23,87,113),
       ("enemy_kin_priest","beamrelease.png")=>frameSha=="c484c254126c0a828924783cf654a8dec203850543660951fb1853df4fddf5b2"&&image.GetUsedRect()==new Rect2I(37,24,92,112),
       ("enemy_kin_priest","bloodprepare.png")=>frameSha=="722d2ecb8e272904f78b4c30af4f5039ab22ad25af2a429d474dd1ec6e4efa51"&&image.GetUsedRect()==new Rect2I(53,29,79,107),
       ("enemy_kin_priest","bloodrebound.png")=>frameSha=="2e9372ebf38492dce7a70c7b2e9c4dfd0d4330758f3b60c946d38dd6631eb140"&&image.GetUsedRect()==new Rect2I(34,28,95,108),
       ("enemy_kin_priest","bloodrelease.png")=>frameSha=="e915646e1cc5cf0303b4c73c728c0412d8296c53d2986e1d1f09ff8ef64599c6"&&image.GetUsedRect()==new Rect2I(31,28,101,108),
       ("enemy_kin_priest","frailprepare.png")=>frameSha=="c2eb2ea559adff28f2a61a0163c1b481953657258c83b9ee7d979006febc962d"&&image.GetUsedRect()==new Rect2I(56,23,70,113),
       ("enemy_kin_priest","frailrelease.png")=>frameSha=="abc880750becb67e8199bae617bcbba1fd22938f085583d56ec83eef48a9a4b1"&&image.GetUsedRect()==new Rect2I(36,24,93,112),
       ("enemy_kin_priest","guardhold.png")=>frameSha=="f59fea5f5abfbf52f71f500858af4211bd063adb23bcf24a8fb36e0a1338b9e7"&&image.GetUsedRect()==new Rect2I(53,24,75,112),
       ("enemy_kin_priest","guardprepare.png")=>frameSha=="8875e72a51e624b18c9fe99d3559cb9d1314cca0eb167cd94defcb8955ee10d6"&&image.GetUsedRect()==new Rect2I(55,24,74,112),
       ("enemy_kin_priest","idle_00.png")=>frameSha=="abc34946ec5d78643f9924167192c043dae004876862690c924efc12bcaf7174"&&image.GetUsedRect()==new Rect2I(51,24,73,112),
       ("enemy_kin_priest","idle_01.png")=>frameSha=="478d8b1469a1e45f791c9e62da3b7b70d603be546effa07acb2fe5538491a394"&&image.GetUsedRect()==new Rect2I(51,23,73,113),
       ("enemy_kin_priest","idle_02.png")=>frameSha=="6426ff2f97112b778634bd638f9e26e36ca06205f18a11bc10c137ab4a781efe"&&image.GetUsedRect()==new Rect2I(51,23,73,113),
       ("enemy_kin_priest","idle_03.png")=>frameSha=="5a7eb3a75f0ad63ff3bac6e34e17d628be71b7d8df4fbb89a41505e3fb227a2d"&&image.GetUsedRect()==new Rect2I(50,22,74,114),
       ("enemy_kin_priest","idle_04.png")=>frameSha=="d279f6922b2e9b76a7aeb6b36055ba0ecc0a49456b88272142fc64cba91d545b"&&image.GetUsedRect()==new Rect2I(50,22,74,114),
       ("enemy_kin_priest","idle_05.png")=>frameSha=="32492359ebc0fc94ed12cab1be64acf0e90f0ea5fd3a077b2a5e948e3f889fbd"&&image.GetUsedRect()==new Rect2I(51,23,73,113),
       ("enemy_kin_priest","idle_06.png")=>frameSha=="8e58af5391b1af917daeebd861ba1174cf1354fdac5da15d19c91e2fb7d3f513"&&image.GetUsedRect()==new Rect2I(51,24,73,112),
       ("enemy_kin_priest","idle_07.png")=>frameSha=="149ef7d41064f892c2399018d4eef875def563e6730a730e84d760a30cb00af7"&&image.GetUsedRect()==new Rect2I(51,24,73,112),
       ("enemy_kin_priest","hurt.png")=>frameSha=="052f8a335fa9015c63dcc80ed33feea99c38031e6b66cddf4b3480ae35ded784"&&image.GetUsedRect()==new Rect2I(52,26,78,110),
       ("enemy_kin_priest","idle.png")=>frameSha=="e6e0973e4f200dc81ea0375c474ac93bb6397e28fdf82e74894b93bc10615632"&&image.GetUsedRect()==new Rect2I(54,24,72,112),
       ("enemy_kin_priest","judgmentprepare.png")=>frameSha=="d8665ae616d66cb4375ee414d2522677b82f329250ddd96acec66546a54aa839"&&image.GetUsedRect()==new Rect2I(55,29,78,107),
       ("enemy_kin_priest","judgmentrelease.png")=>frameSha=="d246ea74ca37ca4f93d0cc7d592509612ad917a636b7133e79cfaa9cda60a9b1"&&image.GetUsedRect()==new Rect2I(33,28,97,108),
       ("enemy_kin_priest","recovery.png")=>frameSha=="253fdd13d1f7c311d7c53f280bdc50c3f54becc14a73ce39d67de4a9b7361041"&&image.GetUsedRect()==new Rect2I(52,24,76,112),
       ("enemy_kin_priest","rest-original.png")=>frameSha=="d6a210e79402644ec34763cc384b251f04cdc2abf1be4b6390dfb442b87f8568"&&image.GetUsedRect()==new Rect2I(51,24,73,112),
       ("enemy_kin_priest","rest.png")=>frameSha=="abc34946ec5d78643f9924167192c043dae004876862690c924efc12bcaf7174"&&image.GetUsedRect()==new Rect2I(51,24,73,112),
       ("enemy_kin_priest","ritualapplied.png")=>frameSha=="2f85523637797256502ebc5213b8ff04f3c0f3b90ca197e8b14f143c98bbda77"&&image.GetUsedRect()==new Rect2I(40,23,97,113),
       ("enemy_kin_priest","ritualprepare.png")=>frameSha=="5d42fe1f952e294b82998b17a3fa6e53260ee66f95f07bb49920fc5ade6a7cf4"&&image.GetUsedRect()==new Rect2I(55,23,74,113),
       ("enemy_kin_priest","weakprepare.png")=>frameSha=="5b2a252f63e7f24c33b1cf9d592791252e02047c0dbcae10eea226e941a607cd"&&image.GetUsedRect()==new Rect2I(56,24,73,112),
       ("enemy_kin_priest","weakrelease.png")=>frameSha=="1735dcba83120ab7985ed07e170814aaa21c24750ca335be71ce49b0115dae80"&&image.GetUsedRect()==new Rect2I(48,23,91,113),
       _=>false}))throw new InvalidOperationException("Unapproved M62 exact frame/bounds");
      if(strictM63&&!((enemyId,file) switch{
       ("enemy_seal_orb","beamprepare.png")=>frameSha=="95a80642cdb532ca7caa0a4a636b436eadefac99aaeebbdd9b317fd1856e68a9"&&image.GetUsedRect()==new Rect2I(12,12,64,47),
       ("enemy_seal_orb","beamrelease.png")=>frameSha=="131e4981ef3b93916e786d6a35dee56a01d5c705b7c10e9837199be41c5537d3"&&image.GetUsedRect()==new Rect2I(12,12,67,45),
       ("enemy_seal_orb","guardhold.png")=>frameSha=="a110851f1cd0c9c73196e8535565249f023cda184009b6d446df2ee5fb98f627"&&image.GetUsedRect()==new Rect2I(12,13,59,45),
       ("enemy_seal_orb","guardprepare.png")=>frameSha=="40e8ac747756e53cdd6467d477c503cb99c914f4650efd7f549427bc709709d0"&&image.GetUsedRect()==new Rect2I(12,12,63,46),
       ("enemy_seal_orb","idle_00.png")=>frameSha=="eea9bd71326e385a21f253bf274c9cdcafaef7968507d18707e454d5c0ab074c"&&image.GetUsedRect()==new Rect2I(12,12,64,44),
       ("enemy_seal_orb","idle_01.png")=>frameSha=="1b7ddc781d55d9904799dfad6e2bd46610539be749f897f5b7c909f402931c54"&&image.GetUsedRect()==new Rect2I(12,12,64,44),
       ("enemy_seal_orb","idle_02.png")=>frameSha=="be756d8d83d86723e47e4775e7b50f7e67e64944a8cd054b3338ea3b5eda99bf"&&image.GetUsedRect()==new Rect2I(12,11,64,44),
       ("enemy_seal_orb","idle_03.png")=>frameSha=="0a25b5948f00235d4b71b0f4568ca9f718cce1c02d30e51a6ea452787116c81b"&&image.GetUsedRect()==new Rect2I(12,11,64,44),
       ("enemy_seal_orb","idle_04.png")=>frameSha=="2a33617401a3686a653ddff4a1b6004139feecdcca7e1c0aea7bc4c6617b6c53"&&image.GetUsedRect()==new Rect2I(12,11,64,44),
       ("enemy_seal_orb","idle_05.png")=>frameSha=="d890113ec37195caaec930587753a5ab15e3af6c370afec33743caae79bb79e5"&&image.GetUsedRect()==new Rect2I(12,12,64,44),
       ("enemy_seal_orb","idle_06.png")=>frameSha=="2b634cf66a800708775dc2d2f31567f3becd75e862989b9e1c3a2e149c6295d1"&&image.GetUsedRect()==new Rect2I(12,13,65,44),
       ("enemy_seal_orb","idle_07.png")=>frameSha=="e652ce9ae07abb915e7165233457f62dc2328b7d7bd9c69bf2100efe10a41545"&&image.GetUsedRect()==new Rect2I(12,13,64,44),
       ("enemy_seal_orb","hurt.png")=>frameSha=="8ead5260efc93abf16dbf92c95514fd58e99b3840f2679e2eefd8484242104d7"&&image.GetUsedRect()==new Rect2I(12,13,61,45),
       ("enemy_seal_orb","idle.png")=>frameSha=="d3d12a3aace655b222b0de6ff763ca7de50a76c22c784216d5f5cbb055610d60"&&image.GetUsedRect()==new Rect2I(12,12,63,45),
       ("enemy_seal_orb","recovery.png")=>frameSha=="87634367bdcefc3f13b372866c3a64dfd097087a92f549e98985cc870e6ba446"&&image.GetUsedRect()==new Rect2I(12,12,64,45),
       ("enemy_seal_orb","rest-original.png")=>frameSha=="a59d211d7dc86f090e212530a44d27ebe777d343c2254dc9cde68f69d069639a"&&image.GetUsedRect()==new Rect2I(4,12,64,44),
       ("enemy_seal_orb","rest.png")=>frameSha=="eea9bd71326e385a21f253bf274c9cdcafaef7968507d18707e454d5c0ab074c"&&image.GetUsedRect()==new Rect2I(12,12,64,44),
       ("enemy_seal_orb","sealapplied.png")=>frameSha=="2a1a2e32b02173cb0e2ad03ee420ed97b36d23790f639e6d3b851027fc9db457"&&image.GetUsedRect()==new Rect2I(12,12,63,45),
       ("enemy_seal_orb","sealprepare.png")=>frameSha=="160cd7890601059964d8ed09f166fa60c32bd9ae5f0b6ff332cbaa55bd8b7e4f"&&image.GetUsedRect()==new Rect2I(12,13,56,46),
       ("enemy_warden","frenzycontacta.png")=>frameSha=="50b8c6ada41369ba959d8db04a82b3bca794ba724dca2ad8895833d1e3ed7687"&&image.GetUsedRect()==new Rect2I(24,60,168,125),
       ("enemy_warden","frenzycontactb.png")=>frameSha=="3e5129347cba723eeb5820fc5f4ed7674143a04c95879341ed7bf7f19476db66"&&image.GetUsedRect()==new Rect2I(30,60,162,116),
       ("enemy_warden","frenzyprepare.png")=>frameSha=="56cc9c77e66bda7bcd4582e2f81e8665740e57b296c41cb7d2313000ec99bc2c"&&image.GetUsedRect()==new Rect2I(71,44,135,132),
       ("enemy_warden","frenzyrebound.png")=>frameSha=="bd0febb5c8cbe52687e74cd3775d6f4df3295ee7e3c068171e7efd66b97e9b47"&&image.GetUsedRect()==new Rect2I(64,54,127,122),
       ("enemy_warden","gatecontact.png")=>frameSha=="b178b5412fe3bf8df26e3db996d54ab538e75e8f1dc32d9b2e87acefb8a3feb2"&&image.GetUsedRect()==new Rect2I(39,68,152,108),
       ("enemy_warden","gateprepare.png")=>frameSha=="5627a7164a13c67a634885f011c24b0297a8e4bef63d7ba477b3eb7783e81d0a"&&image.GetUsedRect()==new Rect2I(71,67,120,109),
       ("enemy_warden","guardhold.png")=>frameSha=="f5320a1870a307bf7a1008c2f1a19a5ca56d735b77f57dfe8e77c88e2865a1e8"&&image.GetUsedRect()==new Rect2I(58,67,133,109),
       ("enemy_warden","guardprepare.png")=>frameSha=="05a00bd38cb0c2097b31b16ffcbf2e6e97e9e5632fe85cdfb0da12ac12b1faa0"&&image.GetUsedRect()==new Rect2I(57,63,134,113),
       ("enemy_warden","idle_00.png")=>frameSha=="368a30e020e90fa8cc5b2ae8e1b4d4d16bf980adca579fd841fa1d63a9fba01d"&&image.GetUsedRect()==new Rect2I(62,56,132,120),
       ("enemy_warden","idle_01.png")=>frameSha=="3ebe053a5b36463298cdf50a4bed36bc1d32611402841838675bfe528145af31"&&image.GetUsedRect()==new Rect2I(62,55,132,121),
       ("enemy_warden","idle_02.png")=>frameSha=="530f51634517edc7e79be948ac6a566cb307502d9c20cf86b2a06c59fe9ccf55"&&image.GetUsedRect()==new Rect2I(62,55,131,121),
       ("enemy_warden","idle_03.png")=>frameSha=="38679800e9c9bb6329f1b58ac91013a6ef3f9d633c81c19b2c3f9bc925754229"&&image.GetUsedRect()==new Rect2I(61,54,132,122),
       ("enemy_warden","idle_04.png")=>frameSha=="1a98cb0f3aaa96adde9579e4550802ade187a99014b365a8847d66a49b23c012"&&image.GetUsedRect()==new Rect2I(61,54,133,122),
       ("enemy_warden","idle_05.png")=>frameSha=="ed430398c18214e4a1ed02c0faa9a86fbcfe2d18e4de2a2c87d1c523bf8a3723"&&image.GetUsedRect()==new Rect2I(62,55,133,121),
       ("enemy_warden","idle_06.png")=>frameSha=="680b8dde1fa3dd54acdd101b200a63ad81d049b2020b7b1e399408547739af67"&&image.GetUsedRect()==new Rect2I(63,56,132,120),
       ("enemy_warden","idle_07.png")=>frameSha=="35285d165ce53185868b5b5f81368dd49f5f283348a8f552c8de858cadfce8b8"&&image.GetUsedRect()==new Rect2I(62,56,132,120),
       ("enemy_warden","hurt.png")=>frameSha=="9e1ed1dc8b9aa1d0598642f1f52787a2e68c4edb31ccc8979df08beaab297a48"&&image.GetUsedRect()==new Rect2I(80,67,128,109),
       ("enemy_warden","hushcontact.png")=>frameSha=="52026ffcc9e49d72c74d8639e78017d586ae1f133011b2904bc25982f4645f8f"&&image.GetUsedRect()==new Rect2I(23,69,168,107),
       ("enemy_warden","hushprepare.png")=>frameSha=="6a5df74744a7ba0dac13027e7bcfec468db01baaba28dd38fbcc33e93a24ed65"&&image.GetUsedRect()==new Rect2I(51,61,140,115),
       ("enemy_warden","idle.png")=>frameSha=="2d285e05631c5c3e0038f3c69291fe1dabfce11bc1472e26f29b9f37405d2bd3"&&image.GetUsedRect()==new Rect2I(57,56,138,120),
       ("enemy_warden","recovery.png")=>frameSha=="428e8be301fd19a906f29962adba911da2c724620ba7a410bdbcda7c4ce8cc41"&&image.GetUsedRect()==new Rect2I(61,57,130,119),
       ("enemy_warden","rest-original.png")=>frameSha=="f902558c5c8918f99d2232c040ff968a9e0d1cb5e02261fc235bd0f4cf398842"&&image.GetUsedRect()==new Rect2I(30,24,132,120),
       ("enemy_warden","rest.png")=>frameSha=="368a30e020e90fa8cc5b2ae8e1b4d4d16bf980adca579fd841fa1d63a9fba01d"&&image.GetUsedRect()==new Rect2I(62,56,132,120),
       ("enemy_warden","strikecontact.png")=>frameSha=="f9e974af1f53546d586fd4a0f290361a4784688097d5a7867380e2150f862491"&&image.GetUsedRect()==new Rect2I(29,61,162,118),
       ("enemy_warden","strikeprepare.png")=>frameSha=="20327bae4215462aa3514387c07b74b6ee3ec80aab96f73b4903dab4eb8bc927"&&image.GetUsedRect()==new Rect2I(76,40,128,136),
       _=>false}))throw new InvalidOperationException("Unapproved M63 exact frame/bounds");
      if(strictM64&&!((enemyId,file) switch{
       ("enemy_chimera_1","bashcontact.png")=>frameSha=="81fee4c43415b329a89556eba0ed61cdaad08b9b0c5ed78772075beda8fca711"&&image.GetUsedRect()==new Rect2I(27,34,181,100),
       ("enemy_chimera_1","bashprepare.png")=>frameSha=="766a26a97b7c6c48a57a3f56abdd90e24e6363c0373a9806810687007ac04caf"&&image.GetUsedRect()==new Rect2I(16,34,168,103),
       ("enemy_chimera_1","bitecontact.png")=>frameSha=="c1377df109b0794bc333d9cfdb49a0a834e5f8a771efd5b04c62c7ce5d488b5c"&&image.GetUsedRect()==new Rect2I(27,32,182,104),
       ("enemy_chimera_1","biteprepare.png")=>frameSha=="479ac42aa5c556d1d0b83aaf76506eaafd157c977b85b9deaa1af843dda4be3b"&&image.GetUsedRect()==new Rect2I(12,20,174,116),
       ("enemy_chimera_1","idle_00.png")=>frameSha=="303b1966c56d9ca437bf5ce2bed0f12bca095c578fa582a1c48f2d57a042cfbc"&&image.GetUsedRect()==new Rect2I(16,24,175,112),
       ("enemy_chimera_1","idle_01.png")=>frameSha=="e94fff98770eac7a4878474aab69540053c7a4a6729c2620d9c653c7ce853b1d"&&image.GetUsedRect()==new Rect2I(16,23,175,113),
       ("enemy_chimera_1","idle_02.png")=>frameSha=="2dc185af54483a12abb7aa89b38554fa66222807b47e4a18e57d7d934476c726"&&image.GetUsedRect()==new Rect2I(16,23,176,113),
       ("enemy_chimera_1","idle_03.png")=>frameSha=="e4773c5b05a9cc2c43230a1654c9cd47708df61d4a0dfb4bfe254547afbce858"&&image.GetUsedRect()==new Rect2I(16,22,176,114),
       ("enemy_chimera_1","idle_04.png")=>frameSha=="7ab2c81e321ccb9bd3d6be1f71aeedd56b6c65e17ae19475a94022c40976439b"&&image.GetUsedRect()==new Rect2I(16,23,177,113),
       ("enemy_chimera_1","idle_05.png")=>frameSha=="d2747e3243dc537fd3565dbc5530434851333a03062c48425598c55d2c07c619"&&image.GetUsedRect()==new Rect2I(16,23,177,113),
       ("enemy_chimera_1","idle_06.png")=>frameSha=="33c4dea4fce60e44d322b150ecad1841c257bac891954f7eea5b81cb9353ee6c"&&image.GetUsedRect()==new Rect2I(16,24,176,112),
       ("enemy_chimera_1","idle_07.png")=>frameSha=="fd847325e89cca3f55d27bf26bf84a3f57221fc956d4edecea30bde746d094ee"&&image.GetUsedRect()==new Rect2I(16,24,176,112),
       ("enemy_chimera_1","hurt.png")=>frameSha=="8bdab2d265d8aab0a21397d284612c361b4b86bb1f183242c3301b52aa0d15f8"&&image.GetUsedRect()==new Rect2I(25,18,165,118),
       ("enemy_chimera_1","idle.png")=>frameSha=="e6afedd4f0b8d22b5324d2a789c7375b1cf296434b1d5a4eba010526b485f385"&&image.GetUsedRect()==new Rect2I(19,17,172,119),
       ("enemy_chimera_1","recovery.png")=>frameSha=="f4e38b275a0b28cf5243119056faaea6580abf9dee8f356e9303f150e7d12599"&&image.GetUsedRect()==new Rect2I(15,21,172,115),
       ("enemy_chimera_1","rest-original.png")=>frameSha=="0bd2e9e0b903e04825a405dfe56b357efdc5b8ccc67d08f30f613b53f686227b"&&image.GetUsedRect()==new Rect2I(16,24,175,112),
       ("enemy_chimera_1","rest.png")=>frameSha=="303b1966c56d9ca437bf5ce2bed0f12bca095c578fa582a1c48f2d57a042cfbc"&&image.GetUsedRect()==new Rect2I(16,24,175,112),
       ("enemy_chimera_2","clawcontacta.png")=>frameSha=="c10c4b9cdbf3e15e3146ddb5f4376090d00eb58ac255beafb6f88c3fd5468b80"&&image.GetUsedRect()==new Rect2I(3,24,210,112),
       ("enemy_chimera_2","clawcontactb.png")=>frameSha=="30f5307bcd80cfa0a9cd4a31ab0a97e09807ef35213d8eb47f3575c2ccc0e08d"&&image.GetUsedRect()==new Rect2I(9,4,200,128),
       ("enemy_chimera_2","clawprepare.png")=>frameSha=="6f2fbce5f91fde0532f58a7bb76defd626d2ac10a2761e4485e09b528e1098dd"&&image.GetUsedRect()==new Rect2I(36,3,182,133),
       ("enemy_chimera_2","clawrebound.png")=>frameSha=="211a514c369359b5dff32fecb7fbb654de3b95fe322bc6bc713282380f1dee0b"&&image.GetUsedRect()==new Rect2I(38,5,175,125),
       ("enemy_chimera_2","idle_00.png")=>frameSha=="6594eef4d10210dc02c1a6939aa2d18a83b6f2fb2a8b2bc1238a12c465e04756"&&image.GetUsedRect()==new Rect2I(38,25,188,112),
       ("enemy_chimera_2","idle_01.png")=>frameSha=="41728ba2ffa8b42debcff4564c80836ade4312c138092671509a8bbe47784afb"&&image.GetUsedRect()==new Rect2I(38,24,188,113),
       ("enemy_chimera_2","idle_02.png")=>frameSha=="a1d483910f564fd85926295a43b9bb47e939d6f2ae38464e6b9e403f3e726e89"&&image.GetUsedRect()==new Rect2I(38,24,188,113),
       ("enemy_chimera_2","idle_03.png")=>frameSha=="cef6a392c5800cc987f98154bcfc3b4b5fba9d186e56aab3c0c538388bd092fa"&&image.GetUsedRect()==new Rect2I(38,23,189,114),
       ("enemy_chimera_2","idle_04.png")=>frameSha=="ff868735d66d426b126edcc2de0efaa8fd4e36070e407d8cd04a3c65b725f8a8"&&image.GetUsedRect()==new Rect2I(38,23,189,114),
       ("enemy_chimera_2","idle_05.png")=>frameSha=="c51f26e9f0ef97efdaed2c419ae31931542d517e1ddddea84ba3f184f40c9b5f"&&image.GetUsedRect()==new Rect2I(38,24,189,113),
       ("enemy_chimera_2","idle_06.png")=>frameSha=="3705106b42ce322dd48472a47cf9c48718a00f30e624f2bfab8fe0ba982cb064"&&image.GetUsedRect()==new Rect2I(38,25,188,112),
       ("enemy_chimera_2","idle_07.png")=>frameSha=="4481eb7f3703070de876b27b7a7dc1c31b97f1c2966bee3c0c2c7f2cb7d22c60"&&image.GetUsedRect()==new Rect2I(38,25,188,112),
       ("enemy_chimera_2","hurt.png")=>frameSha=="096c1c29f17a45289e5b5296a09fd1cea8e271d5545840615cf24c4131f9d425"&&image.GetUsedRect()==new Rect2I(43,7,179,123),
       ("enemy_chimera_2","idle.png")=>frameSha=="6436b485273db7e2df2689a353bb408c4b56b25de1984a6b82f10980dc14a417"&&image.GetUsedRect()==new Rect2I(33,6,182,138),
       ("enemy_chimera_2","recovery.png")=>frameSha=="fd0b1272a60eb362c8d45ad278a581f73fe7666adb9801e7f2c70886c8fee0ab"&&image.GetUsedRect()==new Rect2I(30,16,189,125),
       ("enemy_chimera_2","rest-original.png")=>frameSha=="8989f54df0f8b4330465b033ecb06381fc9ddb30e89028cbae0325c51ead2b53"&&image.GetUsedRect()==new Rect2I(18,24,188,112),
       ("enemy_chimera_2","rest.png")=>frameSha=="6594eef4d10210dc02c1a6939aa2d18a83b6f2fb2a8b2bc1238a12c465e04756"&&image.GetUsedRect()==new Rect2I(38,25,188,112),
       _=>false}))throw new InvalidOperationException("Unapproved M64 exact frame/bounds");
      if(strictM65&&!((enemyId,file) switch{
       ("enemy_chimera_3","clawcontacta.png")=>frameSha=="979cd3cd7916c72d98acef536a293bb4c0b862f812b17327aeb1728fd1e9dcf5"&&image.GetUsedRect()==new Rect2I(39,41,183,125),
       ("enemy_chimera_3","clawcontactb.png")=>frameSha=="31a94379f7b66367b032c7c8d5bcca320c78b5aada9b88b623137d8336606729"&&image.GetUsedRect()==new Rect2I(46,30,177,138),
       ("enemy_chimera_3","clawprepare.png")=>frameSha=="bff040f735d86ce8619ab6b640d89821a3223e14945a6555340eb093845a544e"&&image.GetUsedRect()==new Rect2I(52,35,173,134),
       ("enemy_chimera_3","clawrebound.png")=>frameSha=="0e96105cb543f7e188a2c694ef2103ce8a55bce624db9e37e95010ab6d0a95d3"&&image.GetUsedRect()==new Rect2I(56,35,178,132),
       ("enemy_chimera_3","growlapplied.png")=>frameSha=="3a6ae8d8c155dabad5997f82b18ebb9c5d179354645c8c76c26e411dd7527106"&&image.GetUsedRect()==new Rect2I(49,27,173,143),
       ("enemy_chimera_3","growlprepare.png")=>frameSha=="81024909681dc83e61af3dd9e016b3fbdd33ffa3b161f72bf26d229e66975859"&&image.GetUsedRect()==new Rect2I(58,28,170,142),
       ("enemy_chimera_3","idle_00.png")=>frameSha=="ad05ccf8c391ddcbb27e2d57c1f205c3fc6563cdc68ad81fc0d79166453d52e6"&&image.GetUsedRect()==new Rect2I(70,48,179,120),
       ("enemy_chimera_3","idle_01.png")=>frameSha=="8e61e91591031d87e4dd6c6f1011ca9881b024ac1361e8fedb5bf82c86b37e09"&&image.GetUsedRect()==new Rect2I(70,48,179,120),
       ("enemy_chimera_3","idle_02.png")=>frameSha=="6e1d72ade3520d38883770fc318f33f78afdef797244e302d8cedc6a8257e5c0"&&image.GetUsedRect()==new Rect2I(70,47,180,121),
       ("enemy_chimera_3","idle_03.png")=>frameSha=="6c410fd71cd301e487526c7defd00c4263aaeea002575c0bcb02bca5fdf35fdc"&&image.GetUsedRect()==new Rect2I(70,47,180,121),
       ("enemy_chimera_3","idle_04.png")=>frameSha=="30afe0255730c5318e7523a9959979871f7f63630030d63ff3b285dbcacf09d0"&&image.GetUsedRect()==new Rect2I(70,47,181,121),
       ("enemy_chimera_3","idle_05.png")=>frameSha=="03c747b72839a9819bc5b3bfb10455ad10dfc34641b53dbddbe2dd68036e5a0c"&&image.GetUsedRect()==new Rect2I(70,48,181,120),
       ("enemy_chimera_3","idle_06.png")=>frameSha=="59cd623878a25dbc935b2882f2c4934dff12796250c240ae445b91273d1658a4"&&image.GetUsedRect()==new Rect2I(70,48,180,120),
       ("enemy_chimera_3","idle_07.png")=>frameSha=="2a2028c99aed41b5263d6a581e328fca3885e0091c27fa28d25174948849b1ef"&&image.GetUsedRect()==new Rect2I(70,48,180,120),
       ("enemy_chimera_3","hurt.png")=>frameSha=="f35ebc75700e43847f6de4cdc78f6a8782cdc2b927b08c890a2e6ff8f357becd"&&image.GetUsedRect()==new Rect2I(69,44,174,125),
       ("enemy_chimera_3","idle.png")=>frameSha=="6f136a7f205e26d7687ec4c834a6a59a0a14bab724651455f456724469ac7836"&&image.GetUsedRect()==new Rect2I(58,32,186,137),
       ("enemy_chimera_3","pouncecontact.png")=>frameSha=="dfc20d1ca5e7ebc87bd9b1acad134f1b5d598bcf4d69390157c2af7ea096e3a5"&&image.GetUsedRect()==new Rect2I(4,50,215,117),
       ("enemy_chimera_3","pounceprepare.png")=>frameSha=="34b55d85e2989be0c9fae8c0efd5d79dd5799b96e20b22770213d06747412441"&&image.GetUsedRect()==new Rect2I(52,41,169,131),
       ("enemy_chimera_3","recovery.png")=>frameSha=="e9b2e31648c14dce9a36a2f4dc49a34c6130ebd67fa31b662982223443c5b183"&&image.GetUsedRect()==new Rect2I(61,31,172,138),
       ("enemy_chimera_3","rest-original.png")=>frameSha=="3fce14bfe294b40a63dde85d2efa4e88109031715f1746b72ab4f416b0e1802e"&&image.GetUsedRect()==new Rect2I(22,24,179,120),
       ("enemy_chimera_3","rest.png")=>frameSha=="ad05ccf8c391ddcbb27e2d57c1f205c3fc6563cdc68ad81fc0d79166453d52e6"&&image.GetUsedRect()==new Rect2I(70,48,179,120),
       ("enemy_winch_warden","beamprepare.png")=>frameSha=="54ca418c6f165a82a1433a84f457025e8a1675d9ca776010a8e11821ab3a7e52"&&image.GetUsedRect()==new Rect2I(30,42,133,102),
       ("enemy_winch_warden","beamrelease.png")=>frameSha=="a12b765db09e6c1b2260522447ca955667e0cb23e26cb7e55b8e7cd347b6241a"&&image.GetUsedRect()==new Rect2I(31,42,138,102),
       ("enemy_winch_warden","flailcontacta.png")=>frameSha=="bf1bc2af752f7e413d01d64acb89c7a25dd385c14435239b8aa1c00956cc524c"&&image.GetUsedRect()==new Rect2I(17,43,138,101),
       ("enemy_winch_warden","flailcontactb.png")=>frameSha=="3cd154a7043829fb7b58153fff84e221e008de34f53996312d0d96319416fabc"&&image.GetUsedRect()==new Rect2I(27,34,143,110),
       ("enemy_winch_warden","flailprepare.png")=>frameSha=="a697bff840c3a4b072f83f506376089d6d57c682e0f0270683b2a0bea099fe46"&&image.GetUsedRect()==new Rect2I(44,36,118,108),
       ("enemy_winch_warden","flailrebound.png")=>frameSha=="6090bdfdc5fa3cc8bcb8b61c42cb24332a13ba854b3068e050cfb34cd9ea2ddd"&&image.GetUsedRect()==new Rect2I(31,32,136,112),
       ("enemy_winch_warden","idle_00.png")=>frameSha=="f63ad0230cf03bb1708959c04fbba4f36209a45ad3cb2d34493566d1232967b9"&&image.GetUsedRect()==new Rect2I(41,32,125,112),
       ("enemy_winch_warden","idle_01.png")=>frameSha=="43e22e06aaa59dbc25117db5255795cd9ef053c3400592aa9346f690b5a5df8c"&&image.GetUsedRect()==new Rect2I(41,31,125,113),
       ("enemy_winch_warden","idle_02.png")=>frameSha=="e8835ba44d7ac2f5493c29dfc3945de5ab3194b1e18bf494d0af779881ec038a"&&image.GetUsedRect()==new Rect2I(41,31,125,113),
       ("enemy_winch_warden","idle_03.png")=>frameSha=="f75a7b839b28397dde9b932d770a890dd0a26691bf5f22a598bd20b7e27c0d73"&&image.GetUsedRect()==new Rect2I(40,31,127,113),
       ("enemy_winch_warden","idle_04.png")=>frameSha=="7a49fb338561bd244c282a70e5afc3169deac12af4498ab5207eb7519e4eb873"&&image.GetUsedRect()==new Rect2I(40,31,127,113),
       ("enemy_winch_warden","idle_05.png")=>frameSha=="3f0768a264fd1f4afbd3d7aea30129dc29f45988d7756f311bf159324a6298ab"&&image.GetUsedRect()==new Rect2I(40,32,127,112),
       ("enemy_winch_warden","idle_06.png")=>frameSha=="8ab5b337d8e66e9aa227d46908608149f5c4acc4bfe2d1be1c11ed96fa586e6a"&&image.GetUsedRect()==new Rect2I(41,32,126,112),
       ("enemy_winch_warden","idle_07.png")=>frameSha=="10f261b59402e12f731ceba2735fe4d444361f68f35c9bb07f923c94cfada486"&&image.GetUsedRect()==new Rect2I(41,32,125,112),
       ("enemy_winch_warden","hurt.png")=>frameSha=="18829eeb0718282124f1b5f3c04b721db0af0241d4787d23685d20770a1fe818"&&image.GetUsedRect()==new Rect2I(41,43,137,101),
       ("enemy_winch_warden","idle.png")=>frameSha=="28163a3dfe26cd6e94072e2cdefb01835647c58eb157dc5f339a1036bbd032b1"&&image.GetUsedRect()==new Rect2I(43,30,123,114),
       ("enemy_winch_warden","recovery.png")=>frameSha=="37679503a6dbb549ed6c281e83e301db24c26826fe39bd399ee30410097f95b1"&&image.GetUsedRect()==new Rect2I(40,32,126,112),
       ("enemy_winch_warden","rest-original.png")=>frameSha=="fe6fe46360f771b507f5cb93e6de23d83b18f8f31ae75ba2fab042cbfdaabca0"&&image.GetUsedRect()==new Rect2I(25,24,125,112),
       ("enemy_winch_warden","rest.png")=>frameSha=="f63ad0230cf03bb1708959c04fbba4f36209a45ad3cb2d34493566d1232967b9"&&image.GetUsedRect()==new Rect2I(41,32,125,112),
       ("enemy_winch_warden","rewind.png")=>frameSha=="a03ca2b4f7629ddeaa382434fc578854205b30fab9ab7946bf547c7f76b3ec69"&&image.GetUsedRect()==new Rect2I(45,38,122,106),
       ("enemy_winch_warden","summonapplied.png")=>frameSha=="7432ad9f99017d3ac451a76877cc5a525cc312ea3bfa964614555d03522a878f"&&image.GetUsedRect()==new Rect2I(44,36,125,108),
       ("enemy_winch_warden","summonprepare.png")=>frameSha=="35d74f60b194f484cc0e44432f86674d28a0d52eba1e3cbc0c1fa8b645837514"&&image.GetUsedRect()==new Rect2I(40,35,130,109),
       ("enemy_winch_warden","windhold.png")=>frameSha=="cbd17652a5c66621cf6dd058044a28ca05b071b39177d89733f6fbbe3ae02b54"&&image.GetUsedRect()==new Rect2I(33,39,126,105),
       ("enemy_winch_warden","windprepare.png")=>frameSha=="561005c3f03a4c1a7b8a2adc18092fa9a3e06a780c7d512313294be78bf2f1a0"&&image.GetUsedRect()==new Rect2I(37,42,128,102),
       _=>false}))throw new InvalidOperationException("Unapproved M65 exact frame/bounds");
      if(strictM66&&!((enemyId,file) switch{
       ("enemy_ember_furnace","devourcontact.png")=>frameSha=="1d9749f7565d49b9f84f0de12a4a75c47bbc96407528f5325956367635988e2c"&&image.GetUsedRect()==new Rect2I(31,42,163,102),
       ("enemy_ember_furnace","devourprepare.png")=>frameSha=="51f62f7814f42e54399b79ea96de2a1b9b8c90a026a2db845884ac26a27105e0"&&image.GetUsedRect()==new Rect2I(39,40,148,104),
       ("enemy_ember_furnace","devourrebound.png")=>frameSha=="16651ae07c7b9b275b5abedb59cf666a6b814ace238fab2dc2a158e8785a82da"&&image.GetUsedRect()==new Rect2I(27,40,157,104),
       ("enemy_ember_furnace","idle_00.png")=>frameSha=="7ec6adf2398345be573fd62587091baef99e7ca29bdbe82f367239e7b95f6feb"&&image.GetUsedRect()==new Rect2I(43,32,137,112),
       ("enemy_ember_furnace","idle_01.png")=>frameSha=="d001c665bd49225fa373d56519cf4cb6430f0cd2855364d8008a06af65491fdd"&&image.GetUsedRect()==new Rect2I(43,31,137,113),
       ("enemy_ember_furnace","idle_02.png")=>frameSha=="54042d30956379a267a9bb329619921c870d942ded54132beb888fd953b71f81"&&image.GetUsedRect()==new Rect2I(43,30,137,114),
       ("enemy_ember_furnace","idle_03.png")=>frameSha=="358418e810c65961682d577daa06e08327024a7c2c46425c8b398a5cc44c7a1c"&&image.GetUsedRect()==new Rect2I(43,29,137,115),
       ("enemy_ember_furnace","idle_04.png")=>frameSha=="1d44a3a816e29508cdbff02b1f9cd4bad095dc80635536a3f320ae49815558a2"&&image.GetUsedRect()==new Rect2I(43,29,137,115),
       ("enemy_ember_furnace","idle_05.png")=>frameSha=="c360e246b7b85a70dd52e45f9255cd43bbc884955069436d8e69b5552493203c"&&image.GetUsedRect()==new Rect2I(43,30,137,114),
       ("enemy_ember_furnace","idle_06.png")=>frameSha=="2a3cb6692d0bc62a1ea6789af8b649bdf1cef998a0cf7e67ecd067dec07ced1a"&&image.GetUsedRect()==new Rect2I(43,31,137,113),
       ("enemy_ember_furnace","idle_07.png")=>frameSha=="8b76c27d7c901abfecdb39b06df6f4607e65e78a1d01d18fb35d727b2e767011"&&image.GetUsedRect()==new Rect2I(43,32,137,112),
       ("enemy_ember_furnace","hurt.png")=>frameSha=="2ba9f413cc602ae463728e985ff2db887896072d5ba00a1d595a27fffb09d964"&&image.GetUsedRect()==new Rect2I(35,45,145,99),
       ("enemy_ember_furnace","idle.png")=>frameSha=="629aaa459c0eebc02e22172e45e78f4e51a1ea625d89013f180ecb35c2b454d3"&&image.GetUsedRect()==new Rect2I(42,33,147,111),
       ("enemy_ember_furnace","infernocontact.png")=>frameSha=="547db37be6597a55b750ea969c0354bd977c7f4ed0a9d16542f4162193ebc00e"&&image.GetUsedRect()==new Rect2I(25,49,159,95),
       ("enemy_ember_furnace","infernoprepare.png")=>frameSha=="400d73a1b00a61332ebb0c4e8f1b79feddd47a87e8c15bcbae934af5fe37577e"&&image.GetUsedRect()==new Rect2I(30,47,155,97),
       ("enemy_ember_furnace","infernorebound.png")=>frameSha=="6901c4e78478c0a7055fde911d580a382712140cc3e07689f562108e2fd3a6af"&&image.GetUsedRect()==new Rect2I(34,48,151,96),
       ("enemy_ember_furnace","inflamehold.png")=>frameSha=="42f8df3504cc150fb3add802ed4cab66b1b6a34371552405caeb17d595ff93bb"&&image.GetUsedRect()==new Rect2I(34,47,156,97),
       ("enemy_ember_furnace","inflameprepare.png")=>frameSha=="c45366f146cb81e25950cf628533d41502ad0aa6efd599339304f79136b0cfec"&&image.GetUsedRect()==new Rect2I(29,42,146,102),
       ("enemy_ember_furnace","recovery.png")=>frameSha=="6d25f064c8c49d16356ab88f5a1f28d732b5cc8a33953d06095eba2286b6726a"&&image.GetUsedRect()==new Rect2I(39,36,146,108),
       ("enemy_ember_furnace","rest-original.png")=>frameSha=="fa2eb10ec7c4c82d77604f0e1b044fd7985f1296b40d553021c40c9f6339cecb"&&image.GetUsedRect()==new Rect2I(35,24,137,112),
       ("enemy_ember_furnace","rest.png")=>frameSha=="7ec6adf2398345be573fd62587091baef99e7ca29bdbe82f367239e7b95f6feb"&&image.GetUsedRect()==new Rect2I(43,32,137,112),
       ("enemy_ember_furnace","searcontact.png")=>frameSha=="717a2c281d39c0811dde94216c0992030ac86f7b1c99f09e85a61210c945bc36"&&image.GetUsedRect()==new Rect2I(36,41,157,103),
       ("enemy_ember_furnace","searprepare.png")=>frameSha=="537382a7a982e581f99d85a2b84bdf1f8326a4e84b470aac975e7fd4efcb5150"&&image.GetUsedRect()==new Rect2I(38,38,145,106),
       ("enemy_ember_furnace","stokehold.png")=>frameSha=="cc3657fe515f2c86c8cb7ee4a0a236b8d6733dcefd83fee51dcd9677977b578b"&&image.GetUsedRect()==new Rect2I(36,38,145,106),
       ("enemy_ember_furnace","stokeprepare.png")=>frameSha=="db3603035fc3e6af9266b9f928149715c7ae5b98b66d77f989cec143317e7460"&&image.GetUsedRect()==new Rect2I(32,37,154,107),
       ("enemy_ember_furnace","tacklecontacta.png")=>frameSha=="ce0f76f04e455a0e6eec4a165488e30007e87d74a77430e617879fbb33903d25"&&image.GetUsedRect()==new Rect2I(30,48,159,96),
       ("enemy_ember_furnace","tacklecontactb.png")=>frameSha=="bc9897790f5c3551c9db82beb6e3f3d22eacead01888e470b91b31aea3e0b9d1"&&image.GetUsedRect()==new Rect2I(33,48,164,96),
       ("enemy_ember_furnace","tackleprepare.png")=>frameSha=="59ddc4a46568250e30f4efa365edbe99ae40e064b541f5b16e99952d59efb463"&&image.GetUsedRect()==new Rect2I(31,53,152,91),
       ("enemy_ember_furnace","tacklerebound.png")=>frameSha=="2aa0f10608f98dcf4ea66198634e3777b643fdccc8c0dc84cbf7157fe9776fdb"&&image.GetUsedRect()==new Rect2I(33,53,149,91),
       _=>false}))throw new InvalidOperationException("Unapproved M66 exact frame/bounds");
      if(!strictM66&&!strictM65&&!strictM64&&!strictM63&&!strictM62&&!strictM61&&!strictM60&&!strictM59&&!strictM58&&!strictM57&&!strictM56&&!strictM54&&!strictM53&&!strictM52&&!strictM51&&!strictM50&&!strictM49&&!strictM48&&!strictM47&&!floatingWraith&&enemyId!="enemy_forgotten_soul"&&enemyId!="enemy_lost_soul"&&enemyId!="enemy_elite_stab_book"&&image.GetUsedRect().End.Y!=canvas.GroundMaxY+1&&!approvedAxeBlade&&!approvedMudScoop)throw new InvalidOperationException("Registered authored ground row mismatch");}
     var texture=ImageTexture.CreateFromImage(image);pending.Add(texture);var bounds=image.GetUsedRect();result._opaque.Add(texture,bounds);result.OpaqueEnvelope=result._opaque.Count==1?bounds:result.OpaqueEnvelope.Merge(bounds);textures.Add(texture);durations.Add(duration);
    }
    result.Textures.Add(clip.Name,textures.ToArray());result.Durations.Add(clip.Name,durations.ToArray());
   }
   if(!result.Textures.ContainsKey("rest"))throw new InvalidOperationException("Authored rest missing");
   if(enemyId is "enemy_thorn_squirrel" or "enemy_snap_fruit" or "enemy_set_wary" or "enemy_thief"){if(!new[]{"prepare","contact","recovery","idle","hurt"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((enemyId is "enemy_snap_fruit" or "enemy_set_wary")&&!result.Textures.ContainsKey("block")))throw new InvalidOperationException("Required real small-pair clips missing");}
   if(enemyId=="enemy_thief"&&!new[]{"sneak","reach","stow","turn","flee","block"}.All(result.Textures.ContainsKey))throw new InvalidOperationException("Required thief event clips missing");
   if(enemyId=="enemy_crossbow_archer"&&(!new[]{"windup","release","recovery","idle","hurt","guardprepare","guardhold","block"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required crossbow completion clips missing");
   if(enemyId=="enemy_probe"&&(!new[]{"windup","impact","recovery","idle","hurt","block","lungeprepare","lungecontact","guardprepare","guardhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed probe clips missing");
   if(enemyId=="enemy_wolf"&&(!new[]{"windup","impact","recovery","idle","hurt","block","howlprepare","howlcall","howlrecovery","guardprepare","guardhold","guardrecovery"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed wolf clips missing");
   if(enemyId=="enemy_turtle"&&(!new[]{"windup","impact","recovery","idle","hurt","block","shellprepare","shellhold","shellrecovery","crushprepare","crushimpact"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed turtle clips missing");
   if(enemyId=="enemy_vine_walker"&&(!new[]{"prepare","contact","reopen","recovery","idle","hurt","block","graspingprepare","graspingcontact","chompprepare","chompcontact","guardprepare","guardhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed vine-walker clips missing");
   if(enemyId=="enemy_drummer"&&(!new[]{"prepare","beat","recovery","idle","hurt","block","thumpprepare","thumpimpact","guardprepare","guardhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed drummer clips missing");
   if(enemyId=="enemy_spore_cap"&&(!new[]{"prepare","release","recovery","idle","hurt","block","smashprepare","smashcontact","guardprepare","guardhold","vulnprepare","vulnemission"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed spore clips missing");
   if(enemyId=="enemy_hexer"&&(!new[]{"prepare","release","recovery","idle","hurt","block","slapprepare","slapcontact","guardprepare","guardhold","curseprepare","cursegesture"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed hexer clips missing");
   if(enemyId=="enemy_brute"&&(!new[]{"windup","impact","rebound","recovery","idle","hurt","block","wildwindup","wildimpact","warcryprepare","warcryroar","warroarprepare","warroarroar","guardprepare","guardhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed ogre clips missing");
   if(enemyId=="enemy_apprentice_colossus"&&(!new[]{"attackprepare","attackcontact","recovery","idle","hurt","block","braceprepare","bracehold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed colossus clips missing");
   if(enemyId=="enemy_cultist"&&(!new[]{"bladeprepare","bladecontact","recovery","idle","hurt","block","guardprepare","guardhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed cultist clips missing");
   if(enemyId=="enemy_moss_slime"&&(!new[]{"bumpprepare","bumpcontact","recovery","idle","hurt","block","oozeprepare","oozehold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2))throw new InvalidOperationException("Required completed moss-slime clips missing");
   if(enemyId=="enemy_elite_gold_raven"&&(!new[]{"diveprepare","divecontact","peckprepare","peckcontact","recovery","idle","hurt","block","soarprepare","soarhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed gold-raven clips missing");
   if(enemyId=="enemy_elite_owl"&&(!new[]{"wingprepare","wingcontact","diveprepare","divecontact","recovery","idle","hurt","block","braceprepare","bracehold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed owl clips missing");
   if(enemyId=="enemy_wide_power"&&(!new[]{"surgeprepare","surgecontact","recovery","idle","hurt","block","coilprepare","coilhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed wide-beast clips missing");
   if(enemyId=="enemy_big_slime"&&(!new[]{"slamprepare","slamcontact","recovery","idle","hurt","block","guardprepare","guardhold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_big_slime clips missing");
   if(enemyId=="enemy_shield_squire"&&(!new[]{"bashprepare","bashcontact","rushprepare","rushcontact","recovery","idle","hurt","block","braceprepare","bracehold"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_shield_squire clips missing");
   if(enemyId=="enemy_bond_wolf"&&(!new[]{"biteprepare","bitecontact","bitereopen","bitecontact2","lungeprepare","lungecontact","prowlprepare","prowlhold","idle","hurt","block","recovery"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_bond_wolf clips missing");
   if(enemyId=="enemy_elite_sergeant"&&(!new[]{"whipprepare","whipcontact","doublecontact1","rebound","doublecontact2","recovery","guardprepare","guardhold","idle","hurt","block","recovery"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_elite_sergeant clips missing");
   if(enemyId=="enemy_mourn_beast"&&(!new[]{"biteprepare","bitecontact","bitereopen","bitecontact2","leapprepare","leapcontact","recovery","lickprepare","lickhold","idle","hurt","block","recovery"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_mourn_beast clips missing");
   if(enemyId=="enemy_devoted_sculptor"&&(!new[]{"carveprepare","carvecontact","savageprepare","savagecontact","recovery","guardprepare","guardhold","idle","hurt","block","recovery"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_devoted_sculptor clips missing");
   if(enemyId=="enemy_brood_raptor"&&(!new[]{"clawprepare","clawcontact","recovery","shelterprepare","shelterhold","idle","hurt","block"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_brood_raptor clips missing");
   if(enemyId=="enemy_bowl_bug"&&(!new[]{"rollprepare","rollcontact","tackleprepare","tacklecontact1","tacklerebound","tacklecontact2","recovery","curlprepare","curlhold","idle","hurt","block"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_bowl_bug clips missing");
   if(enemyId=="enemy_brood_toad"&&(!new[]{"recovery","squathold","squatprepare","tonguecontact","tongueprepare","idle","hurt","block"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_brood_toad clips missing");
   if(enemyId=="enemy_broodling"&&(!new[]{"gnawprepare","gnawcontact","recovery","idle","hurt","block"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest.png"))throw new InvalidOperationException("Required completed enemy_broodling clips missing");
   if(enemyId=="enemy_raptor_chick"&&(!new[]{"rest","idle","hurt","peckprepare","peckcontact","recovery","block"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_raptor_chick clips missing");
   if(enemyId=="enemy_axe_automaton_reboot"&&(!new[]{"rest","idle","hurt","block","recovery","bootprepare","boothold","twoprepare","axecontact","tworebound","fistcontact","upperprepare","uppercontact"}.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest_canvas.png"))throw new InvalidOperationException("Required completed enemy_axe_automaton_reboot clips missing");
   if(strictM66){
    string[] required=enemyId=="enemy_ember_furnace"?new[]{"rest","idle","hurt","recovery","stokeprepare","stokehold","devourprepare","devourcontact","devourrebound","searprepare","searcontact","tackleprepare","tacklecontacta","tacklerebound","tacklecontactb","inflameprepare","inflamehold","infernoprepare","infernocontact","infernorebound","block"}:throw new InvalidOperationException("Unregistered M66 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M66 clips missing or unsafe");
   }
   if(strictM65){
    string[] required=enemyId=="enemy_chimera_3"?new[]{"rest","idle","hurt","recovery","clawprepare","clawcontacta","clawrebound","clawcontactb","pounceprepare","pouncecontact","growlprepare","growlapplied"}:enemyId=="enemy_winch_warden"?new[]{"rest","idle","hurt","recovery","summonprepare","summonapplied","flailprepare","flailcontacta","flailrebound","flailcontactb","windprepare","windhold","beamprepare","beamrelease","rewind","block"}:throw new InvalidOperationException("Unregistered M65 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle"&&x!="rewind").Any(x=>result.Durations[x].Sum()>.321f)||(enemyId=="enemy_winch_warden"&&result.Durations["rewind"].Sum()>.421f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M65 clips missing or unsafe");
   }
   if(strictM64){
    string[] required=enemyId=="enemy_chimera_1"?new[]{"rest","idle","hurt","recovery","biteprepare","bitecontact","bashprepare","bashcontact"}:enemyId=="enemy_chimera_2"?new[]{"rest","idle","hurt","recovery","clawprepare","clawcontacta","clawrebound","clawcontactb"}:throw new InvalidOperationException("Unregistered M64 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M64 clips missing or unsafe");
   }
   if(strictM63){
    string[] required=enemyId=="enemy_seal_orb"?new[]{"rest","idle","hurt","recovery","sealprepare","sealapplied","beamprepare","beamrelease","guardprepare","guardhold","block"}:enemyId=="enemy_warden"?new[]{"rest","idle","hurt","recovery","strikeprepare","strikecontact","gateprepare","gatecontact","hushprepare","hushcontact","guardprepare","guardhold","frenzyprepare","frenzycontacta","frenzyrebound","frenzycontactb","block"}:throw new InvalidOperationException("Unregistered M63 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M63 clips missing or unsafe");
   }
   if(strictM62){
    string[] required=enemyId=="enemy_set_breaker"?new[]{"rest","idle","hurt","recovery","smashprepare","smashcontact","tossprepare","tosscontact","guardprepare","guardhold","trapprepare","trapcontact","tokenprepare","tokenapplied","block"}:enemyId=="enemy_kin_priest"?new[]{"rest","idle","hurt","recovery","frailprepare","frailrelease","weakprepare","weakrelease","beamprepare","beamrelease","beamrebound","ritualprepare","ritualapplied","guardprepare","guardhold","judgmentprepare","judgmentrelease","bloodprepare","bloodrelease","bloodrebound","block"}:throw new InvalidOperationException("Unregistered M62 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M62 clips missing or unsafe");
   }
   if(strictM61){
    string[] required=enemyId=="enemy_moss_spawner"?new[]{"rest","idle","hurt","recovery","spawnprepare","spawnapplied","lashprepare","lashcontact","guardprepare","guardhold","block"}:enemyId=="enemy_crab_cannon"?new[]{"rest","idle","hurt","recovery","reticleprepare","reticlecontact","precisionprepare","precisionrelease","laserprepare","laserrelease","chargeprepare","chargeapplied","recharge"}:throw new InvalidOperationException("Unregistered M61 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle"&&x!="recharge").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f||(enemyId=="enemy_crab_cannon"&&result.Durations["recharge"].Sum()>.501f))throw new InvalidOperationException("Required exact finite M61 clips missing or unsafe");
   }
   if(strictM60){
    string[] required=enemyId=="enemy_kin_follower"?new[]{"rest","idle","hurt","recovery","slashprepare","slashcontact","boomerangprepare","boomerangrelease","danceprepare","dancehold","guardprepare","guardhold","block"}:enemyId=="enemy_crab_crusher"?new[]{"rest","idle","hurt","recovery","thrashprepare","thrashcontact","swellprepare","swellcontact","stingprepare","stingcontacta","stingrebound","stingcontactb","adaptprepare","adapthold","guardedprepare","guardedcontact","guardprepare","guardhold","block"}:throw new InvalidOperationException("Unregistered M60 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M60 clips missing or unsafe");
   }
   if(strictM59){
    string[] required=enemyId=="enemy_moss_healer"?new[]{"rest","idle","hurt","recovery","pokeprepare","pokecontact","mendprepare","mendapplied","guardprepare","guardhold","block"}:enemyId=="enemy_thunder_globe"?new[]{"rest","idle","hurt","recovery","slapprepare","slapcontact","thunderprepare","thunderrelease","thunderrebound","burstprepare","burstrelease","guardprepare","guardhold","block"}:throw new InvalidOperationException("Unregistered M59 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.201f)throw new InvalidOperationException("Required exact finite M59 clips missing or unsafe");
   }
   if(strictM58){
    string[] required=enemyId=="enemy_raptor_egg"?new[]{"rest","idle","hurt","quiverprepare","quiverhold","recovery","hatchprepare","quiverreturn","block"}:enemyId=="enemy_sludge_berserker"?new[]{"rest","idle","hurt","recovery","pummelprepare","contacta","rebound","contactb","hugprepare","hugcontact","smotherprepare","smothercontact","vomitprepare","vomitapplied","guardprepare","guardhold","block"}:throw new InvalidOperationException("Unregistered M58 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.161f||required.Where(x=>x!="idle").Any(x=>result.Durations[x].Sum()>.321f)||result.Durations["recovery"].Sum()>.261f)throw new InvalidOperationException("Required exact finite M58 clips missing or unsafe");
   }
   if(strictM57){
    string[] required=enemyId=="enemy_elite_mirror_djinn"?new[]{"rest","idle","hurt","mirrorprepare","contacta","rebound","contactb","recovery","gazeprepare","gazeapplied","guardprepare","guardhold","block"}:enemyId=="enemy_elite_devourer"?new[]{"rest","idle","hurt","recovery","biteprepare","bitecontact","devourprepare","devourapplied","devourreturn","block"}:throw new InvalidOperationException("Unregistered M57 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x is "contacta" or "contactb" or "bitecontact" or "guardhold").Any(x=>result.Durations[x].Sum()>.055f)||required.Where(x=>x is "gazeapplied" or "devourapplied").Any(x=>result.Durations[x].Sum()>.101f)||result.Durations["recovery"].Sum()>.261f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.261f))throw new InvalidOperationException("Required finite M57 clips missing or unsafe");
   }
   if(strictM55){
    string[] required={"rest","idle","hurt","prepare","contacta","rebound","contactb","recovery"};
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest.png"||result.Durations["hurt"].Sum()>.18f||result.Durations["contacta"].Sum()>.055f||result.Durations["contactb"].Sum()>.055f||result.Durations["rebound"].Sum()>.12f||result.Durations["recovery"].Sum()>.24f)throw new InvalidOperationException("Required finite M55 clips missing or unsafe");
   }
   if(strictM56){
    string[] required=enemyId=="enemy_elite_iron_egg"?new[]{"rest","idle","hurt","recovery","tailprepare","tailcontact","sleepprepare","sleephold","sleepreturn","awakenprepare","awakenhold","awakenreturn","block"}:enemyId=="enemy_elite_sentry"?new[]{"rest","idle","hurt","recovery","boltprepare","boltrelease","beamprepare","beamrelease","guardprepare","guardhold","block"}:throw new InvalidOperationException("Unregistered M56 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x is "tailcontact" or "sleephold" or "awakenhold" or "guardhold").Any(x=>result.Durations[x].Sum()>.055f)||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M56 clips missing or unsafe");
   }
   if(strictM54){
    string[] required=enemyId=="enemy_frog_knight"?new[]{"rest","idle","hurt","tongueprepare","tonguecontact","strikeprepare","strikecontact","chargeprepare","chargecontact","queenprepare","queenhold","recovery","guardprepare","guardhold","guardreturn","block","queenreturn"}:enemyId=="enemy_elite_slaver"?new[]{"rest","idle","hurt","chainprepare","chaincontact","weightprepare","weightcontact","lashprepare","lashcontact","netprepare","netapplied","recovery","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("Unapproved M54 profile");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x=="queenhold"||x=="guardhold").Any(x=>result.Durations[x].Sum()>.055f)||(enemyId=="enemy_elite_slaver"&&result.Durations["netapplied"].Sum()>.12f)||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M54 clips missing or unsafe");
   }
   if(strictM53){
    string[] required=enemyId=="enemy_gaping_maw"?new[]{"rest","idle","hurt","clawprepare","clawcontact1","clawrebound","clawcontact2","rendprepare","rendcontact","roarprepare","roarapplied","recovery","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_joker"?new[]{"rest","idle","hurt","wildprepare","wildcontact","jabprepare","jabcontact","bluffprepare","bluffcontact","recovery","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("M53 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||(enemyId=="enemy_gaping_maw"&&(result.Durations["roarapplied"].Sum()>.12f||result.Durations["clawrebound"].Sum()>.12f))||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M53 authored clips missing or unsafe duration");
   }
   if(strictM52){
    string[] required=enemyId=="enemy_maw_hunter"?new[]{"rest","idle","hurt","biteprepare","bitecontact","clawsprepare","clawscontact1","clawsrebound","clawscontact2","webprepare","webapply","recovery","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_cog_construct"?new[]{"rest","idle","hurt","repeaterprepare","repeatercontact","expelprepare","expelcontact1","expelrebound","expelcontact2","chargeprepare","chargepulse","chargereturn","recovery","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("M52 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||(enemyId=="enemy_maw_hunter"&&(result.Durations["webapply"].Sum()>.12f||result.Durations["clawsrebound"].Sum()>.12f))||(enemyId=="enemy_cog_construct"&&result.Durations["expelrebound"].Sum()>.12f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M52 authored clips missing or unsafe duration");
   }
   if(strictM51){
    string[] required=enemyId=="enemy_bomber"?new[]{"rest","idle","hurt","emberprepare","emberrelease","boomprepare","boomrelease","recovery","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_chomper"?new[]{"rest","idle","hurt","clampprepare","clampcontact1","clamprebound","clampcontact2","screechprepare","screechapply","recovery","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("M51 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||(enemyId=="enemy_bomber"&&(result.Durations["emberrelease"].Sum()>.12f||result.Durations["boomrelease"].Sum()>.12f))||(enemyId=="enemy_chomper"&&(result.Durations["screechapply"].Sum()>.12f||result.Durations["clamprebound"].Sum()>.12f))||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M51 authored clips missing or unsafe duration");
   }
   if(strictM50){
    string[] required=enemyId=="enemy_cinder_imp"?new[]{"rest","idle","hurt","biteprepare","bitecontact","sparkprepare","sparkrelease","fanprepare","fanapply","recovery","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_scald_gnat"?new[]{"rest","idle","hurt","biteprepare","bitecontact","suckprepare","suckcontact","scaldprepare","scaldapply","recovery","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("M50 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||result.Durations[enemyId=="enemy_cinder_imp"?"fanapply":"scaldapply"].Sum()>(enemyId=="enemy_cinder_imp"?.1f:.12f)||(enemyId=="enemy_cinder_imp"&&result.Durations["sparkrelease"].Sum()>.12f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M50 authored clips missing or unsafe duration");
   }
   if(strictM49){
    string[] required=enemyId=="enemy_slug"?new[]{"rest","idle","hurt","lickprepare","lickcontact","spitprepare","spitrelease","tackleprepare","tacklecontact","recovery","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_haze_stag"?new[]{"rest","idle","hurt","jabprepare","jabcontact","flurryprepare","flurrycontact1","flurryrebound","flurrycontact2","chargeprepare","chargecontact","recovery","fadeprepare","fadehold","fadereturn","block"}:throw new InvalidOperationException("M49 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||result.Durations[enemyId=="enemy_haze_stag"?"fadehold":"guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>(enemyId=="enemy_haze_stag"?.26f:.24f)||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M49 authored clips missing or unsafe duration");
   }
   if(strictM48){
    string[] required=enemyId=="enemy_axe_automaton"?new[]{"rest","idle","hurt","twoprepare","axecontact","tworebound","fistcontact","upperprepare","uppercontact","recovery","sharpenprepare","sharpenpulse","sharpenreturn","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_biting_scroll"?new[]{"rest","idle","hurt","chompprepare","chompcontact","chewprepare","chewcontact1","chewrebound","chewcontact2","recovery","teethprepare","teethpulse","teethreturn","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("M48 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M48 authored clips missing or unsafe duration");
   }
   if(strictM47){
    string[] required=enemyId=="enemy_elite_giant_face"?new[]{"rest","idle","hurt","crushprepare","crushcontact","recovery","glareprepare","glarepulse","glarereturn","guardprepare","guardhold","guardreturn","block"}:enemyId=="enemy_elite_doom_chanter"?new[]{"rest","idle","hurt","chantprepare","chantcontact1","chantrebound","chantcontact2","recovery","crescendoprepare","crescendopulse","crescendoreturn","guardprepare","guardhold","guardreturn","block"}:throw new InvalidOperationException("M47 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.35f))throw new InvalidOperationException("Required finite M47 authored clips missing or unsafe duration");
   }
   if(strictM46){
    string[] required=enemyId=="enemy_whetstone_colossus"?new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","honereturn","guillotineprepare","guillotinecontact","honeprepare","honepulse"}:enemyId=="enemy_iron_clam"?new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","pressurereturn","jetprepare","jetrelease","pressureprepare","pressurepulse"}:throw new InvalidOperationException("M46 profile art not approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.EndsWith("contact",StringComparison.Ordinal)||x.EndsWith("pulse",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||required.Where(x=>x.EndsWith("release",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.101f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||required.Where(x=>x.EndsWith("return",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.24f))throw new InvalidOperationException("Required finite M46 authored clips missing or unsafe duration");
   }
   if(strictM45){
    string[] required=enemyId=="enemy_mud_lump"?new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","slopprepare","sloprelease","tossprepare","tossrelease","slamprepare","slamcontact"}:enemyId=="enemy_sludge_spider"?new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","oilprepare","oilrelease","slamprepare","slamcontact","rageprepare","ragecontact"}:throw new InvalidOperationException("M45 profile art not yet approved");
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.EndsWith("contact",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||required.Where(x=>x.EndsWith("release",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.101f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||result.Durations["guardreturn"].Sum()>.24f)throw new InvalidOperationException("Required finite M45 authored clips missing or unsafe duration");
   }
   if(strictM44){
    string[] required=enemyId=="enemy_strangler_serpent"?new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","constrictprepare","constrictcontact","lashprepare","lashcontact","thwackprepare","thwackcontact"}:new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","stabprepare","stabcontact1","stabcontact2","stabrebound"};
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||result.Durations["guardreturn"].Sum()>.24f||(enemyId=="enemy_elite_stab_book"&&result.Durations["stabrebound"].Sum()>.12f))throw new InvalidOperationException("Required finite M44 authored clips missing or unsafe duration");
   }
   if(strictM43){
    string[] required=enemyId=="enemy_mudling"?new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","bumpprepare","bumpcontact","slopprepare","slopapply"}:new[]{"rest","idle","hurt","recovery","block","guardprepare","guardhold","guardreturn","smogprepare","smogcontact","beamprepare","release"};
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||result.Durations[enemyId=="enemy_mudling"?"bumpcontact":"smogcontact"].Sum()>.055f||result.Durations[enemyId=="enemy_mudling"?"slopapply":"release"].Sum()>.1f||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||result.Durations["guardreturn"].Sum()>.24f)throw new InvalidOperationException("Required finite M43 authored clips missing or unsafe duration");
   }
   if(strictM42){
    string[] required={"rest","idle","hurt","prepare","contact","recovery","block","guardprepare","guardhold","guardreturn"};
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=m["restFrameFile"]?.Value<string>()||result.Durations["hurt"].Sum()>.18f||result.Durations["contact"].Sum()>.055f||result.Durations["guardhold"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f||result.Durations["guardreturn"].Sum()>.24f)throw new InvalidOperationException("Required finite M42 authored clips missing or unsafe duration");
   }
   if(strictM41){
    string[] required={"rest","idle","hurt","prepare","contact","recovery","block"};
    if(!new HashSet<string>(result.Textures.Keys).SetEquals(required)||result.Textures["idle"].Length!=8||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest.png"||result.Durations["hurt"].Sum()>.18f||result.Durations["contact"].Sum()>.055f||result.Durations["recovery"].Sum()>.24f)throw new InvalidOperationException("Required finite M41 authored clips missing or unsafe duration");
   }
   if(strictM40){
    bool axe=enemyId=="enemy_axe_ogre";string[] required=axe?new[]{"rest","idle","hurt","swingprepare","swingcontact","recovery","block","windupprepare","winduphold","gaspprepare","gasp","return"}:new[]{"rest","idle","hurt","miasmaprepare","miasmacontact","dreadprepare","dreadcontact","recovery","block","guardprepare","guardhold","guardreturn"};
    if(!required.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest.png"||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||(axe&&(Math.Abs(result.Durations["gaspprepare"].Sum()-.12f)>.00001f||Math.Abs(result.Durations["gasp"].Sum()-.60f)>.00001f||Math.Abs(result.Durations["return"].Sum()-.24f)>.00001f)))throw new InvalidOperationException("Required finite M40 authored clips missing or unsafe duration");
   }
   if(strictM39){
    bool worm=enemyId=="enemy_burrow_worm";string[] required=worm?new[]{"rest","idle","hurt","tremorprepare","tremorcontact","tremorrebound","tremorcontact2","biteprepare","bitecontact","recovery","block","guardprepare","guardhold","guardreturn"}:new[]{"rest","idle","hurt","curseprepare","cursecontact","scytheprepare","scythecontact","recovery","block"};
    if(!required.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest.png"||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.Contains("contact",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f)||(worm&&result.Durations["tremorrebound"].Sum()>.09f))throw new InvalidOperationException("Required finite M39 authored clips missing or unsafe duration");
   }
   if(strictM38){
    bool beetle=enemyId=="enemy_rock_beetle";string[] required=beetle?new[]{"rest","idle","hurt","hornprepare","horncontact","biteprepare","bitecontact","recovery","block","guardprepare","guardhold","guardreturn"}:new[]{"rest","idle","hurt","cleaveprepare","cleavecontact","stanceprepare","stancecontact","recovery","block","guardprepare","guardhold","guardreturn"};
    if(!required.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!="rest.png"||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.EndsWith("contact",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f))throw new InvalidOperationException("Required finite M38 authored clips missing or unsafe duration");
   }
   if(strictM37){
    bool shell=enemyId=="enemy_shell_guard";string[] required=shell?new[]{"rest","idle","hurt","bashprepare","bashcontact","rageprepare","ragecontact","recovery","block","guardprepare","guardhold","guardreturn"}:new[]{"rest","idle","hurt","slamprepare","slamcontact","biteprepare","bitecontact","recovery","block","guardprepare","guardhold","guardreturn"};
    if(!required.All(result.Textures.ContainsKey)||result.Textures["idle"].Length<2||((JArray)clips["rest"]!)[0]?["file"]?.Value<string>()!=(shell?"rest.png":"rest_canvas.png")||result.Durations["hurt"].Sum()>.18f||required.Where(x=>x.EndsWith("contact",StringComparison.Ordinal)).Any(x=>result.Durations[x].Sum()>.055f))throw new InvalidOperationException("Required finite M37 authored clips missing or unsafe duration");
   }
   return result;
  }catch{foreach(var texture in pending)texture.Dispose();result.Textures.Clear();result.Durations.Clear();result._opaque.Clear();throw;}
 }
 public bool TryBounds(Texture2D texture,bool envelope,out Rect2I bounds){if(!_opaque.TryGetValue(texture,out bounds))return false;if(envelope)bounds=OpaqueEnvelope;return true;}
 public void Dispose(){foreach(var textures in Textures.Values)foreach(var texture in textures)texture.Dispose();Textures.Clear();Durations.Clear();_opaque.Clear();OpaqueEnvelope=default;}
}

// Raw resource reads must keep res:// paths intact: an exported PCK/APK has no
// corresponding OS file. Export the original JSON/PNG bytes, not only imports.
// Kept in this existing source file so the explicit compile list is unchanged.
internal static class EnemyResourceFile
{
 public static byte[] ReadAllBytes(string path)
 {
  if(!path.StartsWith("res://",StringComparison.Ordinal))return File.ReadAllBytes(path);
  using var file=Godot.FileAccess.Open(path,Godot.FileAccess.ModeFlags.Read);
  if(file==null){
   var error=Godot.FileAccess.GetOpenError();
   if(error==Error.FileNotFound)throw new FileNotFoundException("Resource file not found: "+path,path);
   if(error==Error.FileNoPermission||error==Error.Unauthorized)throw new UnauthorizedAccessException("Cannot read resource: "+path);
   throw new IOException($"Cannot read resource {path}: {error}");
  }
  ulong length=file.GetLength();
  if(length>int.MaxValue)throw new IOException("Resource is too large: "+path);
  byte[] bytes=file.GetBuffer(checked((long)length));var readError=file.GetError();
  if((ulong)bytes.LongLength!=length||(readError!=Error.Ok&&readError!=Error.FileEof))throw new IOException($"Cannot completely read resource {path}: {readError}");
  return bytes;
 }
 public static string ReadAllText(string path)
 {
  if(!path.StartsWith("res://",StringComparison.Ordinal))return File.ReadAllText(path);
  using var stream=new MemoryStream(ReadAllBytes(path),writable:false);
  // Match File.ReadAllText's UTF-8 default and BOM detection without changing bytes.
  using var reader=new StreamReader(stream,System.Text.Encoding.UTF8,detectEncodingFromByteOrderMarks:true);
  return reader.ReadToEnd();
 }
 public static Image LoadPng(string path)
 {
  if(!path.StartsWith("res://",StringComparison.Ordinal))return Image.LoadFromFile(path);
  byte[] bytes=ReadAllBytes(path);var image=new Image();
  try{
   var error=image.LoadPngFromBuffer(bytes);
   if(error!=Error.Ok)throw new InvalidOperationException($"Cannot load authored PNG {path}: {error}");
   return image;
  }catch{image.Dispose();throw;}
 }
}
