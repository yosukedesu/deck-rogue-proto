**2026-11-09 公開更新：この素材束は指定の GitHub 枝へ公開済みです（初回素材コミット `3cbd2156b89f66e6ef4cd08bf7d68429ae678fb6`）。ゲーム統合・GPU/実機確認は引き続き未実施です。下記の旧公開状態は ZIP 作成時点の記録です。**

# C3 幕2・3の施設6枚：400×224完成素材

焚火・店・工房を各幕1枚ずつ、計6枚。幕2は坑木と軌道のある坑道・宿場跡、幕3は石の回廊・水路・古代都市。背景は不透明、人物なし。

| 場面 | 幕2 | 幕3 |
|---|---|---|
| 焚火 | bd_campfire_act2.png | bd_campfire_act3.png |
| 店 | bd_shop_act2.png | bd_shop_act3.png |
| 工房 | bd_workshop_act2.png | bd_workshop_act3.png |

## Claudeへの取り込み要点

1. `assets/Art/scenes/` のPNGを `unity/Assets/Resources/Art/scenes/` へコピーし、幕番号でキーを選ぶ。基準114ee430では焚火と店は固定キーなので、自動切替には別途接続が必要。
2. 幕3店は浮いたアーチの問題を生成段階で修正済み。今回の最終PNGでも柱への接続を確認。
3. 炎は絵に含まれる。`FireOverlay` を重ねるなら旧座標(205,178)は流用しない。manifestの400×224座標案を起点に調整。光の中心・火の根元は目測案で実ゲーム未確認。
4. 画面のcover表示では上下左右が切れる。焚火、店の商品、工房の炉・金床の収まりを実画面で確認する。

枝: `codex/assets-c3-20261108`。handoff: `handoffs/assets-c3-act23-facilities/`。

## 決めたこと・制作方法

- 生成原本を保管したまま、承諾されたプログラム処理で400×224へ縮小。6枚/3枚とも同じBOX方式。NEAREST・BOX・LANCZOSを原寸で比較し、独立QAでBOXを選択した。特に幕2表紙の線路で、NEARESTに出る金色の細かいまだらを軽減する。
- クロップ・描き足し・パレット強制・ディザ・シャープ化なし。縦横比差は0.06%以下で、画面全体を維持。最終素材はRGBの不透明PNG。
- これは生成由来のピクセル調イラストを指定寸法に整えたもの。**手打ちネイティブドット絵や、旧素材と完全に同じ色数・粒度とは称さない。** 原寸で主要物の読めることを優先した。
- 夜硝子と真鍮の方向。寒色の夜、暖色は灯の周りに限定。人物・文字・UI枠は最終画像に含めない。
- `raw/`は生成原本のバイト保存。`references/`は既存比較用で新作数に含めない。取り込むのは **`assets/Art/scenes/*.png`だけ**。

## 検査・検証の区別

- 400×224実寸、RGB不透明、PNGとして読めること、原本の無改変保存、SHA-256を検査。
- `qa/contactsheet_native.png` は各画像を原寸で配置。`qa/*_2x.png` は最終PNGを整数2倍で拡大した検査用。
- `qa/reference_comparison.png` と `qa/INDEPENDENT_FINAL_SIZE_QA.md` で既存との比較・独立評価を確認できる。
- `qa/SOURCE_VISUAL_QA_HISTORICAL.md` は加工前の原本評価を保存した履歴。その「サイズ未達・許可待ち」は今回の完成品には適用しない。
- **素材として取り込み可能。ゲームへの接続、Unity import、GPU、Android実機、実戦闘は未検証。** `readyForImport: true` は素材規格を示し、統合済みを意味しない。
- インポート推奨はPoint/無圧縮/mipmapなし。既に夜色を含むため、強い夜色補正を重ねない。最終判断は取り込み後の実画面で行う。
- 再検査は `python3 scripts/validate.py`。`requirements.txt`のPillowを使い、`python3 scripts/resize_assets.py`で原本から取り込みPNGを再生成できる。生成後のSHA-256も照合する。

## 公開状態・編集範囲

指定の枝は存在するが、このZIP作成時点ではmainと同じで素材コミットは未公開。以前のGitHub承認の状態を確認できていないため、その操作を迂回していない。ZIPを使った取り込みは進められる。

立ち絵・BGM・キービジュアル・src/engine・src/data・Unityコード・旧Art/bg・既存bd_titleは変更していない。決定・素材・manifest・QAのみ。
