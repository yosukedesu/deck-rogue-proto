// ArtImporter.cs — Resources/Art 配下の PNG (PixelLab のドット絵) を置くだけで正しく取り込む (2026-09-07 M1)。
// Point フィルタ・非圧縮・ミップマップなし・Sprite (Single)・PPU 100。9スライスの border は名前で判定 (panel/btn_/card_frame)。
// HD-2D 見本 (2026-09-30 P06) の例外: /Resources/Art/stage/ は Bilinear・ミップあり (アルファの被覆を保つ)・読み取り可、
// 名前が _n で終わる絵 (法線) は sRGB なし、_e で終わる絵 (発光) は sRGB。stage 以外の _n/_e は Point・ミップなしのまま。
using UnityEditor;
using UnityEngine;

namespace DeckRogue.EditorTools
{
    public class ArtImporter : AssetPostprocessor
    {
        /// <summary>BGM は長いのでストリーミング (Decompress On Load だと1曲で数十MBのメモリ)。効果音は圧縮のままメモリへ</summary>
        void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            var st = imp.defaultSampleSettings;
            bool bgm = path.Contains("/Audio/bgm/");
            st.loadType = bgm ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            st.quality = bgm ? 0.6f : 0.7f;
            imp.defaultSampleSettings = st;
            imp.forceToMono = !bgm;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
            var imp = (TextureImporter)assetImporter;
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            // HD-2D 見本 (2026-09-30 P06): 箱庭の絵 (/Art/stage/) はミップつきのなめらかな読み (シェーダがドットの縁だけ1画素なじませる)・
            // 法線 (<名前>_n) は色でなくデータ (sRGB なし)・発光 (<名前>_e) は色 (sRGB)。今ある絵にはどれも当たらない (W1 は見た目を変えない)
            string pathN = assetPath.Replace('\\', '/');
            bool stage = pathN.Contains("/Resources/Art/stage/");
            bool normal = name.EndsWith("_n", System.StringComparison.Ordinal);
            bool emission = name.EndsWith("_e", System.StringComparison.Ordinal);
            // 札の裏 (2026-10-04): ドットでなく「紙」側のなめらかな絵 (Gemini の工芸風)。424×632 を 200×290 前後へ縮めるので Bilinear＋ミップ (Point だと真鍮の細線がちらつく)
            bool smooth = stage || name == "cardback";
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = smooth;
            if (stage)
            {
                imp.mipMapsPreserveCoverage = true;   // 半立体の葉 (アルファで切る) が遠くで痩せない
                imp.alphaTestReferenceValue = 0.5f;
            }
            imp.sRGBTexture = emission || !normal;   // 発光は色・法線はデータ。法線は tex×2−1 で自分でほどく (UnpackNormal は使わない) ので NormalMap 型にしない
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = 100f;
            imp.wrapMode = pathN.Contains("/Art/tiles/") || (stage && pathN.Contains("/tiles/")) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; // 舞台のタイルは敷き詰める
            imp.isReadable = true; // 貼り絵の縁 (PaperFx.Silhouette) が GetPixels32 で読む。箱庭のタイルも CPU で Texture2DArray へ写す (P04)
            var ts = new TextureImporterSettings();
            imp.ReadTextureSettings(ts);
            ts.spriteMeshType = SpriteMeshType.FullRect; // 透明部分を切り詰めない (整数倍配置と影絵の基準を rect に揃える)
            imp.SetTextureSettings(ts);
            int border = name == "panel" || name.StartsWith("btn_") ? 6 : name.StartsWith("card_") ? 12
                : name == "paper_panel" ? 14 : name == "paper_tag" ? 10 : name == "paper_button" ? 12 : name == "paper_card" ? 16 : 0;
            if (border > 0) imp.spriteBorder = new Vector4(border, border, border, border);
        }
    }
}
