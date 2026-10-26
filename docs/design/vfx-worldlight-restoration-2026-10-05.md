# 周囲照明の復元とVFX合成の分離

この修正は a0dec4b の属性ライト弱化を撤回する。旧配布APKは保持し、a0dec4b由来の NOT-DISTRIBUTED APKも引き続き配布禁止。この修正からAndroid APKは作っていない。

## 実装

通常の属性攻撃は既存 WorldStrike を呼ぶ。contact18/range7/0.32s/hold0.3125（0.10秒）、bounce28/range13/0.34s/hold0.2941176（0.10秒）。heavyは倍率1.65、半径8.5/15、従来のshadow条件も復元。色は属性色。弱いライトは -autopilot と -vfx-light-review weak を両方指定した比較実行だけで有効。

元のWorldBattleFxも非受光であり、追加ライトをVFXから外すだけでは白いにじみは変わらなかった。そこで9属性の描画だけを専用 ElementBattleFx material/shader と ElementVfxPass に分離し、ステージのブルーム・トーンマップ後に合成する。シェーダーは元のRGB、元のalpha、SrcAlpha/OneMinusSrcAlpha、ZTest LEqualを保持。加算合成・premultiply・余分なTint/HDR gainなし。キャラ・床・ステージの受光と露出・ブルーム設定は変更していない。物理斬撃・結晶guardは元の描画経路を保持。

新パスはStage.Cameraだけ、専用素材が実際に活動中のフレームだけ積む。既存24/12個のプールを再利用し、追加の全画面テクスチャを作らない。原画像9枚は変更なし。コマンドライン比較モードは起動時にキャッシュし、毎フレームの引数配列生成を避ける。

## 比較方法と結果

同じseed4242、通常カードUI、固定30Hz、同じフレームで火・氷・闇・物理斬撃を比較。
1. baseline: 好評だったWorldStrike値＋新9素材を既存unlit材質で描画。
2. weak: 棄却した弱いライト＋既存unlit材質。
3. separated: 復元WorldStrike＋専用材質をブルーム後合成。

baselineは過去のゲーム全体の再現ではなく、ライト値の比較基準。原素材は3条件共通。比較PNGと10秒無音MP4はWindows taskフォルダに保存。床・キャラを含む画面の明るさは vfx-light-measurements.json を参照。数値は表示RGBの輝度近似であり、luxやHDR放射輝度ではない。characterAndVfx領域は効果本体も含むため、VFXの遮蔽をライト低下と混同しない。playerHeadOutsideMainImpactは主な命中演出の外だが闇カードは自己回復も発生する。

初回の非受光材質だけの分離はbaselineとほぼ画素一致で、見た目の改善は無かった。その後のブルーム後合成で、床の強い照り返しを維持しながら火の橙色、氷の青い筋、闇の暗い紫の渦が明瞭になった。透明部分越しの明るいキャラは意図的に残す。

## 検証と制限

Windowsビルド成功 errors0。25経路36イベントの全表示、テンポ9件、9属性の全8コマと専用パスの実描画を検証。明示分離後の9属性検証に続き、非活動時のパス省略とモードキャッシュだけを追加し、4属性比較・25経路・テンポ9件を再実行。ログにException/Error/RenderGraph attachment errorなし。通常save4件、旧APK、mainを保持。git diff --check合格。

Androidの新描画パス・GPU性能・MSAA各設定・他機種は未検証。新パスは舞台カメラ限定のため、専用属性像の水面反射は今後の確認対象（床への照り返しとは別）。全カードの個別実行ではなく、静的定義監査＋代表経路の動的検証。Library送信は承認待ちで再試行していない。
