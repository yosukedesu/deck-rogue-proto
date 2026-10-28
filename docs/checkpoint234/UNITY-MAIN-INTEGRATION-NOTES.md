# Published selected234 qualification
This document retains chronological233 HOLD then234 selected correction. Latest234 terminal section supersedes earlier pending wording. FixedUnity main audit is historical, not current or localUnity verification. Do not replace Unity-specific renderer/math/HDR-gain/index510/URP handling.

# Unity mainを壊さない移植手順（追加監査）

固定GitHub main HEAD: `5184f0d5e563c8613b83348b3a47186477f89568`。このmainと、素材キットのGodot基準branch公開223は別です。現在の手元Unity未push変更は未調査。確認済みmainには新WideWater A/最終227、専用LightTorrent factory/shader、234固有pose holdは未移植です。これはユーザー手元にも未実装と断定するものではありません。

## 必ず維持する既存Unity実装

- [unity/Assets/Game/Hd2d/Hd2dAttackPrototype.cs](https://github.com/yosukedesu/deck-rogue-proto/blob/5184f0d5e563c8613b83348b3a47186477f89568/unity/Assets/Game/Hd2d/Hd2dAttackPrototype.cs#L1048): 既存shared CreateLight。1276 CreateRollingWater、1279 old phase01/02/03。CreateLightTorrentなし。今回のchanged factoryを移し、Unityのusing/namespace/Hd2dMath/Enabled適応を維持。
- [unity/Assets/Game/Hd2d/Hd2dAttackFx.cs](https://github.com/yosukedesu/deck-rogue-proto/blob/5184f0d5e563c8613b83348b3a47186477f89568/unity/Assets/Game/Hd2d/Hd2dAttackFx.cs#L127): Linear color space必須。144 paletteはc.linear。190 triCount=Indices.Length−Indices.Length%3で512を510へ切る修正。191/272 mesh.SetUVs(1,d.Gain)でHDR gainを別UV channelへ送る。Godot renderer全文で置き換えない。
- [unity/Assets/Game/Hd2d/Hd2dMath.cs](https://github.com/yosukedesu/deck-rogue-proto/blob/5184f0d5e563c8613b83348b3a47186477f89568/unity/Assets/Game/Hd2d/Hd2dMath.cs#L27): 27/53 zero-safe NormalizedなどGodot互換shimを維持。新演出のためにGodot型へ戻す／このmathファイルを置換する必要はない。
- [unity/Assets/Resources/Shaders/Hd2dPrototype.shader](https://github.com/yosukedesu/deck-rogue-proto/blob/5184f0d5e563c8613b83348b3a47186477f89568/unity/Assets/Resources/Shaders/Hd2dPrototype.shader#L16): URP shader。29 gain:TEXCOORD1。345 sRGB textureはサンプリング時点でlinear、Godot OUTPUT_IS_SRGB分岐は不要。既存共通shaderを上書きせず、新Torrent専用materialを追加。
- [unity/Assets/Game/StageCombat.cs](https://github.com/yosukedesu/deck-rogue-proto/blob/5184f0d5e563c8613b83348b3a47186477f89568/unity/Assets/Game/StageCombat.cs#L67): ShowcaseApproach。77 release=profile.Contact+Max(0,hits−1)*profile.Interval+.045、81 FrameDur[attack]=attackFrameDur、82 duration+.05で元duration復元、86 MotionVersion一致時にattack開始。既存clock/owner/復元処理を壊さない。

Hd2dAttackFx.cs L82のPoseDurationsとL116のStage.ShowcaseApproachが既存入口です。Godot BattleActor/BattleStage全文は比較用で、Unityの同名機能への丸ごと代替ではありません。StageUnits/StageCombat/Hd2d factoryの確認範囲ではTorrent固有holdは未検出。

## 変更を写す順番

1. Godot `CreateRollingWater` 最終227と `CreateLightTorrent` 最終233を、既存Unity factory/math APIへ意味移植。既存プロフィールやゲームルールを巻き戻さない。
2. 新光流原本と専用URP optical shader/materialを追加。sRGB texture→linearはUnity samplerに任せ、二重decodeしない。新shaderも既存meshのHDR gain/TEXCOORD1規約を受け取る。Godot CUSTOM0をそのまま期待しない。三角形index有効数の510修正を維持。
3. rendererにexact tuple＋Kind33／Space0／bodytexture／effectshader／paletteの全条件opt-inを追加。既存LightBolt等は元material。共有shaderを一括置換しない。
4. 234の放出frame3 holdを、Unity既存action/MotionVersion/clockへ写す。最終の既存予定/実contact+.10まで、消費済frame3だけ保持。0〜2通常、死亡/owner変更/expiry/cancelで解除。Unity既存+.045／duration+.05の復元処理との整合を設計し、全攻撃の定数を+.10へ一括変更しない。Godotのdefault .94を固定値にもしない。
5. Unity撮影で3beat・source発光点・透過面・最後の復帰を照合。神経となるゲーム側イベント／damage／receipt／.22間隔は変更せず、描画・poseだけ近似可能。

新optical shaderの有無確認は固定HEAD shader directoryのHD2D/Torrent/Prism名一致範囲（既存Hd2dPrototype.shaderのみ）です。別名の未検出実装や手元変更の不存在を保証するものではありません。追加監査は移植注意の補足であり、Unityコード自体は一切編集していません。
