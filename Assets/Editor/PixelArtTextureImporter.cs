using UnityEditor;
using UnityEngine;

namespace PlumSword.Editor
{
    /// <summary>
    /// Assets/Resources/Art/ 아래 그림을 가져올 때 docs/dev/art.md "가져오기 설정"을 맞춘다:
    /// Sprite (2D and UI), PPU 32, Point, 압축 없음, 밉맵 끔.
    /// 애니메이션 시트는 한 장(Single)으로 가져오고, 프레임 자르기와 피벗은 화면 코드가 한다 (예: DiscipleSprites).
    /// 9-slice 테두리(Sprite Border)는 건드리지 않는다.
    /// 예외: Art/StartMenu/ 의 시작 화면 그림은 원본 해상도(1672×941 등)를 화면 크기로 정수배가 아니게 줄여 보이므로
    /// Trilinear 와 밉맵을 쓴다 (Point 로 줄이면 글자와 선이 깨진다). docs/dev/art.md "가져오기 설정".
    /// </summary>
    public sealed class PixelArtTextureImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Resources/Art/";
        private const string SmoothRoot = "Assets/Resources/Art/StartMenu/";
        private const float PixelsPerUnit = 32f;

        public override uint GetVersion() => 3;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            // 24프레임 공격 시트는 너비가 2048px을 넘는다. 자동 축소를 막는다.
            if (assetPath.StartsWith("Assets/Resources/Art/Enemies/")
                || assetPath == "Assets/Resources/Art/UI/damage_numbers.png") importer.maxTextureSize = 4096;

            if (assetPath == "Assets/Resources/Art/UI/ending_dawn.png"
                || assetPath == "Assets/Resources/Art/UI/jongnam_stage_hud.png"
                || assetPath == "Assets/Resources/Art/UI/jongnam_title_plum_duel.png"
                || assetPath.StartsWith("Assets/Resources/Art/UI/Settings/")
                || assetPath.StartsWith("Assets/Resources/Art/UI/StatUpgrade/")
                || assetPath.StartsWith("Assets/Resources/Art/UI/OfflineReward/")
                || assetPath.StartsWith("Assets/Resources/Art/UI/RealmStatus/")
                || assetPath.StartsWith("Assets/Resources/Art/UI/Substage/"))
            {
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 4096;
            }

            if (assetPath.StartsWith(SmoothRoot))
            {
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
            }
        }
    }
}
