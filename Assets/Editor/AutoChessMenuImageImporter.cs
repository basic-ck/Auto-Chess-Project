using UnityEditor;

// Resources/Visuals/UI/Menu_* 이미지(메인 메뉴 배경·로고)를 가져올 때 원본 비율·크기를 유지한다.
// (Unity 기본값은 가로세로를 2의 거듭제곱으로 바꿔 1920x1080 이 2048x1024 로 찌그러진다.)
public class AutoChessMenuImageImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Visuals/UI/Menu_")) return;
        var ti = (TextureImporter)assetImporter;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = 4096;
        ti.mipmapEnabled = false;
        ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        ti.filterMode = UnityEngine.FilterMode.Bilinear;
        ti.alphaIsTransparency = true; // 로고(투명 PNG) 가장자리 번짐 방지
    }
}
