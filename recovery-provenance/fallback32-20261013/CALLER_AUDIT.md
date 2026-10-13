# Doll fallback-size caller audit

## Finding

All discovered Godot doll fallback callers map to Unity's explicit 32-dot request. This is a source-backed correction of the existing fallback invocation, not newly authored artwork. Exact a7f86b5 source is unavailable; evidence uses the recovered Stage41/R9 presentation source and shared Unity Theme implementation. `manifest.json` records exact file hashes and line references.

## Godot call graph at accepted ccdce85

- `DeckRogue/GodotPort/Presentation/BattleStage.cs:918–938`: stage dolls are `Retainer == true && Innate != true`. A single `actor2.Configure("dolls", p.Def.Id, ...)` call at931 serves those actors. The other two Configure callers at878 and897 are leaders and enemies.
- `BattleActor.cs:352–369`: first requests the exact `res://assets/Art/{category}/{id}.png`. Phone96 replacement is restricted to enemies. `Placeholder` is determined before fallback. Only null textures reach the single `ProceduralActorTexture.Doll(id, 64)` invocation at368; other categories use Generate(...,64).
- `BattleActor.cs:544–547`: TryTexture uses ResourceLoader.Exists followed by GD.Load. A missing loose PNG is not proof of missing authored artwork; the recovered project uses .png.import remaps into existing .ctex resources.
- `ProceduralActorTexture.cs:231–465`: the existing Doll method uses `n = Max(16,size)` and `u = n/32f`. Its ID-dependent pixel construction, one-pixel outline, orientation conversion, color palette and alpha logic are untouched.
- `BattleActor.cs:436–452`: animation resolution remains unchanged, including the dimension-checked r2-idle branch. Native validation must report both base and currently bound source textures, not assume they are identical.
- `BattleStage.cs:976–985` and `SourceActorSizing.cs:51–54`: final image size is recomputed from base texture dimensions. Thus the old64 fallback inflates the source doll target. The initial 1.35 world-height fallback in BattleStage is followed by this layout refresh and does not justify leaving the procedural raster at64.

Search scope: all `DeckRogue/**/*.cs` production classes for direct Doll references and BattleActor construction/Configure calls. `scripts/presentation/BattleActor.cs` is a one-byte recovered placeholder, not another active implementation. There is exactly one direct Doll invocation and one Configure("dolls",...) caller. No additional rendering fallback calls were found.

## Unity matching source

Stage41 extracted `payload/unity/Assets/Game`:

- `BattleView.cs:457–464`: the same retainer/non-innate classification, additionally excluding lifecycle-retired dolls. This lifecycle detail is not changed by the size package.
- `BattleScreen.cs:1838–1848`: FillDollPanel calls `Creature.Get("dolls", d.Def.Id, true, 32)` at1841 and sizes using `Max(32, Max(art.rect.width, art.rect.height))*4*ArtScale*HeroScale` at1843.
- `BattleView.cs:570–576`: IsWideDoll also requests `Creature.Get("dolls", d.Def.Id, true, 32)` at574. Authored dimensions above32, such as48-dot dragon/lion, classify as wide. This package changes no seat allocation policy.
- Those are the only `Creature.Get("dolls",...)` callers in the available Stage41 source. Shared Unity BattleScreen/BattleView independently repeat the same32 request at1833/572.

Shared `deckrogue_cloud_migration/worktree/unity/Assets/Game/Theme.cs:452–460`: Creature.Get first resolves authored Theme.Art and only then calls Doll(id,size). Lines477–552 implement the existing32-based procedural fallback. The default64 parameter is used by other Creature callers; both doll callers override it explicitly with32.

## Size and alpha consequences

With existing default scales:

- Phone source target: 153.6 to76.8, with per-dot PixelScale2.4 unchanged.
- Desktop source target:192 to96, with per-dot PixelScale3 unchanged.
- At render1170×540, phone UI-scale1.6, whole-image rounded screen height changes123 to61. Do not require the opaque silhouette crop to be exactly half: the existing outline remains one raster pixel wide.
- The pure production-algorithm test for white_perm_wick gives64×64 top/bottom alpha margins4/1,1402 opaque pixels;32×32 margins1/0,398 opaque pixels. Values use the actor's0.0625 alpha threshold. Native confirmation is still required.
- Body height, contact/shadow geometry and relative label offsets follow existing dimensions/alpha formulas. Seat centers and state stay unchanged. No authored32/48 texture is resized by this patch.

## Existing evidence and actual authored controls

`unity-parity-20261013/halo-v4-combined/backing-four100/texture-handles-before-exit.json` has `actor-source:white_perm_wick`, empty Path and64×64. It establishes the old fallback identity only; it is frozen evidence from a different candidate assembly, not a new before/after acceptance capture.

Existing `.ctex` assets, verified by their import remaps and header dimensions:

- white_perm_squire:32×32
- white_perm_dragon:48×48
- white_perm_lion:48×48

Their exact resource paths, import hashes and .ctex hashes are in the manifest. No asset replacement, alias, new art, archive search or repeat of the prior113-archive wick audit was performed. The rich fixture already includes authored squire plus wick, monk, bonfire and page, with an innate-excluded control.
