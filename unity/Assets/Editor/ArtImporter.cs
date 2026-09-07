// ArtImporter.cs — Resources/Art 配下の PNG (PixelLab のドット絵) を置くだけで正しく取り込む (2026-09-07 M1)。
// Point フィルタ・非圧縮・ミップマップなし・Sprite (Single)・PPU 100。9スライスの border は名前で判定 (panel/btn_/card_frame)。
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
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = 100f;
            imp.wrapMode = assetPath.Replace('\\', '/').Contains("/Art/tiles/") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; // 舞台のタイルは敷き詰める
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            imp.isReadable = true; // 貼り絵の縁 (PaperFx.Silhouette) が GetPixels32 で読む
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
