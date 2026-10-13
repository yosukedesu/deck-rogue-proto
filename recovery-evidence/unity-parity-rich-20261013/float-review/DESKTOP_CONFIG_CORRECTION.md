# Desktop source-mode capture correction

The first ui-rich-r2/float-desktop100 and130 attempts do not cover natural128 art. The UI is desktop-sized, but StageWorld remains phone because the launch retained -uiscale1.6. Actual native telemetry selects enemy_brute_96, SourceUiProxyHeight307.2, unit1.3333334. The independent validator rejects the expected natural128 height471.027246/unit0.91997509. Preserve these captures as a configuration-qualified failure; do not relabel96 as128 or treat it as a float-formula failure.

Source StageWorld.cs337–343 initializes PhoneLayout from platform/--ui-phone then overrides it from -uiscale >1.001. BattleStage.cs962–980 reads the same source scale. The UI PhoneLayout setting is separate. Genuine desktop requires removing --ui-phone AND omitting -uiscale or explicitly setting -uiscale1, while saved100/130 text accessibility stays independent.

This supplements the immutable float-size fixture schedule, which mentioned removing --ui-phone but omitted the second independent source-mode override. No fixture bytes, engine, original command or source code needs changing. Rerun natural128 under a new capture directory and verify actual selected art and metrics before accepting coverage.
