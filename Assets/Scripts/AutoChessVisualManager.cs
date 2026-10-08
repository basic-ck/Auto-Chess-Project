using System.Collections.Generic;
using UnityEngine;

// 비주얼 에셋(3D 타일·유닛·배경, UI 스프라이트)을 게임에 연결하는 매니저.
// 에셋이 없으면 어떤 함수도 오류 없이 null/false 를 돌려주고, 호출하는 쪽(AutoChess)이 기본 도형·기본 UI로 대체한다.
// 연결 우선순위: AutoChessVisualSet 에셋(메뉴로 자동 매칭) → 없으면 Resources/Visuals/ 아래 파일 이름 규칙으로 직접 검색.
public class AutoChessVisualManager : MonoBehaviour
{
    // 정적 참조가 스크립트 재컴파일(도메인 리로드) 등으로 끊겨도 씬에서 다시 찾아 복구한다
    static AutoChessVisualManager inst;
    public static AutoChessVisualManager I => inst != null ? inst : inst = FindFirstObjectByType<AutoChessVisualManager>();

    // 맵 타일(Map_Tile_01~08)의 역할
    public enum TileKind { PlayerLight, PlayerDark, EnemyLight, EnemyDark, BenchA, BenchB, Divider, Base }

    AutoChessVisualSet set;
    Transform stage;
    readonly Dictionary<string, GameObject> goCache = new Dictionary<string, GameObject>();
    readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    void Awake()
    {
        inst = this;
        set = Resources.Load<AutoChessVisualSet>(AutoChessVisualSet.ResourcePath);
        stage = new GameObject("VisualStage").transform;
    }

    // ---------- 조회 (없으면 null) ----------
    GameObject LoadGO(string path)
    {
        if (!goCache.TryGetValue(path, out var go)) goCache[path] = go = Resources.Load<GameObject>(path);
        return go;
    }

    GameObject UnitPrefab(int id) => set != null && id < set.units.Length && set.units[id] != null ? set.units[id] : LoadGO($"Visuals/Units/Unit_{id + 1:00}");
    GameObject TilePrefab(int kind) => set != null && kind < set.mapTiles.Length && set.mapTiles[kind] != null ? set.mapTiles[kind] : LoadGO($"Visuals/Map/Map_Tile_{kind + 1:00}");

    public Sprite Icon(string key)
    {
        if (!spriteCache.TryGetValue(key, out var sp))
            spriteCache[key] = sp = (set != null ? set.GetSprite(key) : null) ?? Resources.Load<Sprite>("Visuals/UI/" + key);
        return sp;
    }

    // 스프라이트를 IMGUI 영역에 그린다. 스프라이트가 없으면 false(호출하는 쪽이 기본 UI 사용).
    public bool DrawIcon(Rect r, string key)
    {
        var sp = Icon(key);
        if (sp == null || sp.texture == null) return false;
        if (Event.current.type != EventType.Repaint) return true;
        var t = sp.texture; var tr = sp.textureRect;
        GUI.DrawTextureWithTexCoords(r, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
        return true;
    }

    // ---------- 유닛 ----------
    // 3D 유닛이 있으면 "발 밑 중앙이 원점"인 래퍼 오브젝트로 만들어 돌려준다(높이를 height로 맞춤). 없으면 false.
    // top = 유닛 머리 위 표식·텍스트 기준 높이(원점 기준)
    public bool TryUnit(int id, float height, out GameObject root, out float top)
    {
        root = null; top = 1;
        var prefab = UnitPrefab(id);
        if (prefab == null) return false;
        root = new GameObject("Unit_" + (id + 1).ToString("00"));
        var inst = Instantiate(prefab, root.transform);
        foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c); // 클릭은 바닥 평면 좌표로 처리하므로 충돌체 불필요
        var b = BoundsOf(inst);
        if (b.size.y < .0001f) { Destroy(root); root = null; return false; }
        inst.transform.localScale *= height / b.size.y;
        b = BoundsOf(inst);
        inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z); // 발 밑 중앙 = 원점
        top = b.size.y + .3f;
        return true;
    }

    // 3D 유닛에 색 배율(예: 적은 어둡게)을 입힌다. 텍스처는 유지되고 URP/Standard 색 속성만 곱해진다.
    public void Tint(GameObject go, Color c)
    {
        var pb = new MaterialPropertyBlock();
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(pb);
            pb.SetColor("_BaseColor", c); pb.SetColor("_Color", c);
            r.SetPropertyBlock(pb);
        }
    }

    // ---------- 맵 ----------
    // 3D 타일 한 칸. 타일 메쉬가 없으면 null(호출하는 쪽이 기본 도형 타일을 만든다).
    // 가로세로를 footprint에 맞추고 윗면이 y=0에 오도록 배치한다.
    public GameObject Tile(TileKind kind, float x, float z, float footprint = .95f, float y = 0)
    {
        var prefab = TilePrefab((int)kind) ?? (kind == TileKind.BenchB ? TilePrefab((int)TileKind.BenchA) : null); // 대기석 B(06)가 없으면 A(05)를 그대로 쓴다
        if (prefab == null) return null;
        var root = new GameObject("Tile_" + kind);
        root.transform.SetParent(stage, false);
        var inst = Instantiate(prefab, root.transform);
        foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
        var b = BoundsOf(inst);
        float s = footprint / Mathf.Max(.0001f, Mathf.Max(b.size.x, b.size.z));
        inst.transform.localScale *= s;
        b = BoundsOf(inst);
        inst.transform.localPosition -= new Vector3(b.center.x, b.max.y, b.center.z); // 윗면 y=0, 가운데 정렬
        root.transform.position = new Vector3(x, y, z);
        return root;
    }

    // 무대 장식: 중앙 경계선(Map_Tile_07, 아군/적 진영 사이), 바닥 받침(Map_Tile_08), Environment/ 의 배경·관중석.
    // 파일이 없는 항목은 건너뛴다. centerX/centerZ = 전장+대기석 전체의 중심.
    public void BuildStage(float centerX, float centerZ, float boardSize, int cols, float dividerZ)
    {
        for (int x = 0; x < cols; x++) Tile(TileKind.Divider, x, dividerZ, .95f, .02f);
        Tile(TileKind.Base, centerX, centerZ, boardSize, -.2f);

        var env = set != null && set.environment.Length > 0 ? set.environment : Resources.LoadAll<GameObject>("Visuals/Environment");
        foreach (var e in env)
        {
            if (e == null) continue;
            var inst = Instantiate(e, new Vector3(centerX, 0, centerZ), e.transform.rotation, stage); // 배경은 만든 크기 그대로, 전장 중심에 배치
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
        }
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
