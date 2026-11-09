# 同梱Godotカタログの確認

Godot4.6.3、独立のXDGディレクトリを指定して `--headless --editor --import` と `--headless --quit-after 3` を実行。エラー0。

headlessはdummy rendererのためGPU描画・影・本番照明の検証ではありません。Godotによる16個のGLBインスタンス化、FBX再取り込み、モデルの構造検査は独立QAを参照。
