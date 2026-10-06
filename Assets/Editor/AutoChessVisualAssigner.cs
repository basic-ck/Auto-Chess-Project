using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 메뉴 [AutoChess > 비주얼 에셋 전체 자동 매칭]
// Assets/Resources/Visuals/{Map,Units,Environment,UI} 를 탐색해 파일 이름 규칙대로 AutoChessVisualSet 에 연결한다.
//   Map/Map_Tile_01~08  Units/Unit_01~Unit_41(41=소환 유령)  Environment/(모든 3D 파일)  UI/(모든 이미지, 파일 이름이 키)
// 파일이 없어도 오류 없이 넘어가며, 빈 칸은 게임에서 기본 도형·기본 UI로 대체된다.
public static class AutoChessVisualAssigner
{
    const string Root = "Assets/Resources/Visuals";
    static readonly string[] Folders = { "Map", "Units", "Environment", "UI" };

    [MenuItem("AutoChess/비주얼 에셋 전체 자동 매칭")]
    public static void AssignAll()
    {
        EnsureFolders();
        FixUiImportSettings();

        string setPath = Root + "/VisualSet.asset";
        var set = AssetDatabase.LoadAssetAtPath<AutoChessVisualSet>(setPath);
        if (set == null) { set = ScriptableObject.CreateInstance<AutoChessVisualSet>(); AssetDatabase.CreateAsset(set, setPath); }
        Undo.RecordObject(set, "AutoChess 비주얼 에셋 매칭");

        var report = new List<string>();
        var unmatched = new List<string>();

        set.units = new GameObject[41];
        set.mapTiles = new GameObject[8];
        Match(set.units, Root + "/Units", @"^Unit_(\d+)$", unmatched);
        Match(set.mapTiles, Root + "/Map", @"^Map_Tile_(\d+)$", unmatched);

        set.environment = FindAll<GameObject>(Root + "/Environment").OrderBy(g => g.name).ToArray();

        set.sprites = new List<AutoChessVisualSet.SpriteEntry>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) set.sprites.Add(new AutoChessVisualSet.SpriteEntry { key = Path.GetFileNameWithoutExtension(path), sprite = sp });
        }

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int u = set.units.Count(x => x != null), m = set.mapTiles.Count(x => x != null);
        report.Add($"유닛 {u}/40 (Unit_01~40)" + Missing(set.units, 40, "Unit_"));
        report.Add($"맵 타일 {m}/8 (Map_Tile_01~08)" + Missing(set.mapTiles, 8, "Map_Tile_"));
        report.Add($"배경/관중석 {set.environment.Length}개, UI 스프라이트 {set.sprites.Count}개");
        if (unmatched.Count > 0) report.Add("이름 규칙에 안 맞아 건너뜀: " + string.Join(", ", unmatched));
        string msg = "[AutoChess] 비주얼 에셋 매칭 완료 → " + setPath + "\n" + string.Join("\n", report) + "\n(빈 칸은 기본 도형/기본 UI로 표시됩니다)";
        Debug.Log(msg);
        if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.ShowNotification(new GUIContent("비주얼 에셋 매칭 완료 (콘솔 확인)")); // 모달 창 대신 알림(자동화 중에도 멈추지 않음)
    }

    // 폴더 구조를 만들어 둔다 (이미 있으면 그대로)
    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/Resources", "Visuals");
        foreach (var f in Folders) if (!AssetDatabase.IsValidFolder(Root + "/" + f)) AssetDatabase.CreateFolder(Root, f);
    }

    // UI 폴더의 이미지는 자동으로 Sprite 타입으로 바꾼다 (PNG 기본값은 일반 텍스처라 Sprite 로 불러올 수 없음)
    static int FixUiImportSettings()
    {
        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/UI" }))
        {
            var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (imp == null || imp.textureType == TextureImporterType.Sprite) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            changed++;
        }
        return changed;
    }

    // 폴더 안의 3D 파일(fbx·프리팹)을 이름의 번호로 배열 칸에 넣는다 (1번 → 인덱스 0)
    static void Match(GameObject[] slots, string folder, string pattern, List<string> unmatched)
    {
        var re = new Regex(pattern, RegexOptions.IgnoreCase);
        foreach (var go in FindAll<GameObject>(folder))
        {
            var mt = re.Match(go.name);
            if (!mt.Success) { unmatched.Add(go.name); continue; }
            int idx = int.Parse(mt.Groups[1].Value) - 1;
            if (idx < 0 || idx >= slots.Length) { unmatched.Add(go.name + "(번호 범위 밖)"); continue; }
            slots[idx] = go;
        }
    }

    static List<T> FindAll<T>(string folder) where T : Object
    {
        var list = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder)) return list;
        foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null) list.Add(a);
        }
        return list;
    }

    static string Missing(GameObject[] slots, int count, string prefix)
    {
        var miss = new List<string>();
        for (int i = 0; i < count; i++) if (slots[i] == null) miss.Add((i + 1).ToString("00"));
        if (miss.Count == 0) return "";
        return miss.Count == count ? "  — 아직 파일 없음" : "  — 없는 번호: " + string.Join(",", miss);
    }
}
