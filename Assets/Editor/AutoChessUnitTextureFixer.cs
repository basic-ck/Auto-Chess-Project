using System.IO;
using UnityEditor;
using UnityEngine;

// 바르코 3D FBX는 색(base_color.png 4096x4096)이 파일 안에 들어 있기만 하고 Unity가 재질에 연결해 주지 않아 회색으로 보인다.
// 메뉴 AutoChess > 유닛 색상 텍스처 추출·적용: Resources/Visuals/Units(유닛)·Map(타일)·Themes(배경 소품)의 FBX 마다
//  1) 내장 텍스처 추출  2) base_color 를 1024px JPG 로 축소 저장(UnitTextures/Unit_NN_color.jpg), 노멀맵은 버림
//  3) URP Lit 재질(UnitTextures/Unit_NN_Mat.mat) 생성  4) FBX 의 material_0 을 그 재질로 교체
public static class AutoChessUnitTextureFixer
{
    // {FBX 폴더, 텍스처·재질 폴더}: 유닛과 타일 모두 같은 방식으로 처리
    static readonly string[][] Targets =
    {
        new[] { "Assets/Resources/Visuals/Units", "Assets/Resources/Visuals/UnitTextures" },
        new[] { "Assets/Resources/Visuals/Map", "Assets/Resources/Visuals/MapTextures" },
        new[] { "Assets/Resources/Visuals/Themes", "Assets/Resources/Visuals/ThemeTextures" }, // 배경 테마 소품(하위 폴더 포함)
    };
    const int Size = 1024;

    [MenuItem("AutoChess/유닛 색상 텍스처 추출·적용")]
    static void Run()
    {
        int ok = 0, skip = 0;
        foreach (var t in Targets)
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { t[0] }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Fix(path, t[1])) ok++; else skip++;
            }
        AssetDatabase.Refresh();
        Debug.Log($"[유닛 색상] 적용 {ok}개, 건너뜀 {skip}개");
    }

    public static bool Fix(string fbxPath, string TexDir)
    {
        string name = Path.GetFileNameWithoutExtension(fbxPath);
        var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (imp == null) return false;
        Directory.CreateDirectory(TexDir);

        string jpg = $"{TexDir}/{name}_color.jpg";
        if (!File.Exists(jpg)) // Blender 정리 스크립트(Tools/clean_units.py)가 이미 JPG 를 만들어 뒀으면 추출을 건너뛴다
        {
            string tmp = $"{TexDir}/_tmp_{name}";
            Directory.CreateDirectory(tmp);
            imp.ExtractTextures(tmp);
            AssetDatabase.Refresh();
            var big = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tmp}/base_color.png");
            if (big == null) { AssetDatabase.DeleteAsset(tmp); Debug.LogWarning($"{name}: base_color 텍스처를 찾지 못함"); return false; }

            // 4096 → 1024 축소(GPU 블릿) 후 JPG 저장
            var tex = ReadableCopy(big, Size);
            File.WriteAllBytes(jpg, tex.EncodeToJPG(90));
            Object.DestroyImmediate(tex);
            AssetDatabase.DeleteAsset(tmp); // 추출 원본(PNG 약 15MB)·노멀맵 삭제
        }
        AssetDatabase.ImportAsset(jpg);
        var ti = (TextureImporter)AssetImporter.GetAtPath(jpg);
        ti.maxTextureSize = Size; ti.mipmapEnabled = true; ti.sRGBTexture = true;
        ti.SaveAndReimport();

        string matPath = $"{TexDir}/{name}_Mat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(jpg));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", .2f);
        EditorUtility.SetDirty(mat);

        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "material_0"), mat);
        imp.SaveAndReimport();
        return true;
    }

    static Texture2D ReadableCopy(Texture2D src, int size)
    {
        var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var t = new Texture2D(size, size, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, size, size), 0, 0); t.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        return t;
    }
}
