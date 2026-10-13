# Source UI-proxy float anchor: off-tree candidate

Status 2026-10-13 04:20 UTC: compiled and independently source-reviewed; native validation pending. Apply only in the runtime owner's next explicit window. No live files were changed by this package.

## Exact bounded change

Three guarded hunks across BattleActor.cs, BattleStage.cs and BattleStage.SourceCombatText.cs, plus one pure SourceFloatProxyModel.cs. No new Godot-derived type or callback requires generated bindings.

1. Actor stores scalar unrounded UI-proxy height and source canvas unit during existing RefreshActorLayout.
2. Sizing reads render-texture pixels, whereas Camera.UnprojectPosition and float Labels use the logical viewport. Both height and source unit explicitly multiply by visibleRectHeight / sizingInputHeight. At native1170×540 / logical1950×900, mudling height is204.8 logical units and source unit1.3333333.
3. At the existing once-only damage-float creation boundary, origin is:

   projection(Home + UiLunge.Offset) − (0, baseProxyHeight × (0.5 × ExecutionScale.Y + 0.1) + 20 × sourceProxyUnit)

The bottom-pinned sprite proxy receives UI lunge and execution scale. Its billboard alone receives transparent feet padding, world approach/recoil, breathing and pose-frame enlargement. Those must not shift the source float origin. Base art dimensions remain fixed during animation. There is no hardcoded15-pixel correction and no alpha readback.

Existing font/number/color/pop/rise/lifetime/capture-once behavior remains unchanged. The source horizontal visual random±30 remains the prior centered renderer adaptation. ActorScreenBody is untouched for its other callers. This package does not restore other floating-text roles.

## Source evidence

Verified Stage41 payload under unity-source-provenance-20261013/extracted/DeckRogue-Cumulative-Stage41-from-2f31050-20261010/payload/unity/Assets/Game:
- BattleView.cs419–423 returns persistent UI sprite proxy.
- BattleScreen.cs580–627 creates base-art rect and binds billboard.
- Presenter.cs754–759 takes proxy center plus0.1×local rect height;2062 adds20 source units;2121 emits Float.
- Tween.cs807–835 execution squash/bottom compensation;968–978 UI lunge;1030–1035 TransformPoint(rect.center).
- StageUnits.cs1579–1593 applies combat/recoil, feet padding, breathing and frame enlargement to billboard only.
- Shared PaperFx.cs686–692 preserves full base-art rect with bottom at source feet.

Exact a7 source is unavailable. Its supplied layout metadata supports the measured15.39 native-pixel transparent-foot displacement of the old mudling anchor; this package reconstructs the traced contract, not a tuned screenshot offset.

## Checks

- Full composed project:0 errors,15 existing baseline warnings (compile.log).
-28,886 tests using the exact production pure model and SourceActorSizing: five named sprite roles/sizes, phone/desktop sizing, execution squash/stretch, UI-lunge translation,100/130 text roles, identity/stretched viewports, alternate source UISCALE. These are formula checks, not native projection evidence.
-22 installer/source-anchor guards: all-files preflight, dry-run unchanged, independent text retained, duplicate/missing guards, helper collision, repeat idempotence and injected mid-write rollback.
- Independent source review found and rejected the first native/logical mismatch; corrected version approved. No remaining bounded source discrepancy identified.

Reproduce from workspace root:

    DOTNET_CLI_HOME=/tmp/ui-parity-dotnet source-recovery-20261012/toolchain/dotnet/dotnet run --project ui-parity-restoration-20261013/float-proxy-anchor-delta/tests/Tests.csproj
    python ui-parity-restoration-20261013/float-proxy-anchor-delta/tests/test_guards.py
    python ui-parity-restoration-20261013/float-proxy-anchor-delta/apply.py runtime-recovery-20261012/project

The last command is dry-run. Only the runtime owner may add --apply in an authorized window. payload-sha256.json protects helper/hunks; manifest.json records delivered package source/test evidence. bin/obj and composed compile copies are not integration payload.

## Native acceptance, still pending

At100% and130%, capture continuous first contact and source-normalized same-state overlay. Include mudling64, short48 sprite,80 elite with top-margin cap,96/128 brute, and execution squash/UI lunge phases. Confirm native1170×540→logical1950×900 origin is correctly converted, identical across text scales; no feet/recoil/pose-frame drift; glyph and pop remain unchanged; one float per accepted receipt. Test scene/reset cancellation with no late label. Existing font/pop acceptance does not automatically accept this new anchor.
