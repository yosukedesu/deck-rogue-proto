# DeckRogue runtime recovery baseline

Recovered from the published APK checkpoint 74e982fd31b31c6005a5dffb0bf39113b5126082. This is a reconstructed playable baseline, not the later lost b05a0da checkout.

## Verified in an actual native window

Godot 4.6.3 Mono (official commit 7d41c59c4) on Linux X11 with OpenGL Compatibility/llvmpipe rendered the leader screen, departure shop, map, battle, and reward screens. Native pointer/keyboard inputs verified out-of-target card drag cancellation, targeted card play, effect-choice cancellation and re-entry, end turn, enemy actions, battle victory, and reward selection. Cancellation preserved the session revision; the battle reached turn 2 and victory with normal cards and rules.

The exact recovered engine is unchanged. Its separate 15 golden fixtures/3,103 state-hash comparisons passed before runtime reconstruction.

## Run

Install official Godot 4.6.3 .NET/Mono and .NET 9 SDK (verified SDK9.0.318). A prebuilt Linux-independent managed Debug assembly is in project/.godot/mono/temp/bin/Debug. From this archive's root:

    dotnet build project/DeckRogue.csproj
    /path/to/Godot_v4.6.3-stable_mono_linux.x86_64 --path project

On Windows use the corresponding official Godot4.6.3 .NET executable. Windows/Android execution has not been verified by this baseline checkpoint.

## Provenance and limitations

- project/engine/runtime contains exact original source files recovered from retained archives and checked against compile-time PDB hashes.
- project/DeckRogue is reconstructed ILSpy C# with documented decompiler repairs; generated script bindings are retained in the source.
- project/assets and .godot/imported retain exact exported APK resources and import metadata, rather than claiming recovery of original source PNG/font/audio files. Keep the hidden .godot directory. Editor reimport robustness remains separate work.
- Compiled scene remaps and project.binary are exact exported resources. project.godot was decoded via Godot ProjectSettings.save_custom.
- DeckRogue.csproj is adapted for desktop runtime output and runtimeconfig. No gameplay/RNG changes were made.
- The restored early APK predates accepted UI, actor-color, and atmosphere work. Those are separate reconstruction overlays.
- Native screenshots are actual viewport pixels. Software rendering is not Android performance evidence. Cloud audio hardware is unavailable; audio playback has not been audibly verified.
- No signing keys, private credentials, user saves, SDK installations, public releases, or user-device changes are in this archive.
