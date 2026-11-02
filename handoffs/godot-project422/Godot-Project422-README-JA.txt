DECK ROGUE — Godot プロジェクト 422（復元用）

■ 内容と到達範囲
Godot .NET 4.6.3 / .NET SDK 9 で使用する実際の制作プロジェクトです。
保存チェックポイント422: libfile_7b0a2e99571c8191aa991fdaccc40a97 / version 0
チェックポイントZIP SHA-256:
0cd58a882b6e7ef267d3e80f178623fc11e8fc048c50eb2be36d706c0c5717c2
これは元の保存チェックポイントの識別値です。本配布ZIPのハッシュとは異なります。

今回特定したエフェクト（VFX）素材の制作・修正は、プロジェクト422に統合済みです。追加検証423/424では、実際の黒・イゼットの使用条件で既存エフェクトを確認し、製品変更は不要と判断しました。
新しいキャラクター画像・アニメーションの制作は、このVFX依頼とは別の範囲です。キャラクター画像の不足を、今回のエフェクト作業が未完了である理由にはしていません。
ただし、全カード・全条件・全端末を網羅的に検証したわけではありません。Android実機での動作と音声は未確認です。

■ 展開方法
Full ZIP を1つ展開する方法、または Part ZIP 全部を展開する方法のいずれかを選びます。
Part ZIP は通常の独立したZIPです。.z01 形式の分割ファイルではありません。
全Partを「同じ新しい空フォルダー」へ展開すると、同じ project フォルダーへ内容が統合されます。
Partごとに別フォルダーへ展開しないでください。既存プロジェクトへ上書きしないでください。
全Partと配布マニフェストが揃っていること、各ZIPのSHA-256と全ファイルのSHA-256を確認してください。

■ 起動の準備
1. Godot .NET 4.6.3 と .NET SDK 9 を用意します（通常版GodotのみではC#を実行できません）。
2. project/project.godot をGodot .NETで開き、エディターの正式な全インポートを完了させます。
   コマンド例: Godot.NET --headless --editor --path <projectフォルダー> --import
3. project フォルダーで dotnet build DeckRogue.csproj -c Debug を実行します。
4. 通常のプロジェクト実行を使用します。既定シーンは scenes/Main.tscn、実装は scripts/app/Main.cs です。
   QAシーンやテスト用Mainへ置き換える必要はありません。

■ 復元資源についての重要事項
配布には製品ソース・素材8,837件、補完インポートレシピ1,748件、欠損原素材に対応する既存の必須コンパイル済み依存71件を含みます。
3件の同一パス重複を統合した実ファイル集合は10,653件です。
欠損した元素材そのものを再生成したものではありません。配布された .godot/imported 内の必須依存を削除しないでください。
過去の巨大なQAキャッシュや小さな試験用UIDレジストリは含めません。UIDレジストリは正式インポートで新規生成します。
単にPNGをコピーしただけでは、実行時の画像・シェーダー結合が準備されたことにはなりません。

■ クリーン復元の確認
上記の配布予定ファイル集合だけから別フォルダーへ復元し、正式インポート終了0、製品ビルド終了0（エラー0・警告1,926）を確認しました。
新規UIDレジストリは5,306件で、ヒバナ画像の登録を含みます。必須依存71件はインポート後も全SHA-256一致しました。
インポートで5件の既存レシピにUID行だけが追加されました（waterwide phase01～03、wood contact01～02）。配布する元レシピはこの正式インポートで利用できたものです。
製品の既定Mainから通常の新規開始→店→マップ→最初の戦闘まで確認しました。緑のキャラクター・敵3体・森の背景・テクスチャ付き手札5枚が表示され、通常のセーブ終了後に正常終了（終了コード0）しました。QA用Mainは使用していません。資源読込エラーは0でした。
最後のPartとFullには、全ファイルのマニフェストと技術検証結果が含まれます。
クラウドの音声デバイスを開けずダミードライバーへ移行するログと、VSync非対応のログがあります。この検証では、音声の正常動作とAndroid実機での動作は確認していません。

■ 公開用差分
authored-frames/chimera-3/clipset.json の管理用フィールド authorizedApexCorrection.authorizationMessageId だけを公開版から除去しました。
それ以外の解析済みJSON内容は一致し、製品コードから同フィールドへの参照はありません。保存済み正本は変更していません。
正本SHA-256: de5b9e3b5fd7bad0b55fc1392e237e48b64dcc9f1be8074a2cbfafb28a748b7c
公開版SHA-256: 74efc4e220eff4e9ab2bbd33e7d80ef7016c5c7d0eca8f4eb4821ca093dda4d7

■ 配布ZIP検証値（SHA-256）
Godot-Project422-Full.zip
167,631,893 bytes
4265f3ce355dae34bda3940542a2ee42a29e9e3b266660fbf5d8f61ab73b9997

Godot-Project422-Part01of09.zip
20,477,662 bytes
55c227fc0caea5687678f71dcf033f8749607f77b2722ebb6793efe35240b2b0

Godot-Project422-Part02of09.zip
20,736,420 bytes
ffce4ed3bd15f89e85389aa953599ac4bb1a9f04aacaaccbb57c71de5bd3ff52

Godot-Project422-Part03of09.zip
20,658,921 bytes
ea1c7debbd7144c35e51a6aa280a139ef4f4ba02b82fcc9c3f87500d2ff62df1

Godot-Project422-Part04of09.zip
20,917,275 bytes
32de0d30431ac88c8ec6516fa8af33910ae7dc1c73e42e5bb2a7caf86f141f2c

Godot-Project422-Part05of09.zip
20,676,443 bytes
b21c973b5a3c1aa5985a01be5c3ae586138d7f5175e88c637d338a046c85b32b

Godot-Project422-Part06of09.zip
20,942,258 bytes
500b1d3530dba601205cbefe39861b2a97a39e670fa0d19787d3e0267571371e

Godot-Project422-Part07of09.zip
19,731,565 bytes
c46123610d05085a0607850d4998a84fac0fd1ff6cb458066f898de3156a643d

Godot-Project422-Part08of09.zip
20,090,958 bytes
a0c069857bf929e2b89d9c9fa5ed531e3c82b300093b539e3e8072c46068515e

Godot-Project422-Part09of09.zip
3,400,567 bytes
220190e015ca6384eae7f8487c41bb5d45caa7e4fbe9719caf1559ed983f159c
