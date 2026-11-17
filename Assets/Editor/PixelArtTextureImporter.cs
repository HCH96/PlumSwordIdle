using UnityEditor;
using UnityEngine;

namespace PlumSword.Editor
{
    /// <summary>
    /// Assets/Resources/Art/ 아래 그림을 가져올 때 docs/dev/art.md "가져오기 설정"을 맞춘다:
    /// Sprite (2D and UI), PPU 32, Point, 압축 없음, 밉맵 끔.
    /// 애니메이션 시트는 한 장(Single)으로 가져오고, 프레임 자르기와 피벗은 화면 코드가 한다 (예: DiscipleSprites).
    /// 9-slice 테두리(Sprite Border)는 건드리지 않는다.
    /// </summary>
    public sealed class PixelArtTextureImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Resources/Art/";
        private const float PixelsPerUnit = 32f;

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
