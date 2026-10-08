using UnityEngine;

// 맵 주변 배경 테마 5종. 게임 중 T 키(또는 상단 [배경] 버튼)로 바꿔 가며 비교할 수 있다.
// 소품: Resources/Visuals/Themes/<키>/<키>_NN 3D 파일이 있으면 그것을, 없으면 간단한 도형 임시 소품을 쓴다.
//  - NN 은 near 슬롯 → far 슬롯 순서의 번호(01부터). 예: Volcano 는 near 2종(01~02) + far 2종(03~04)
// 카메라가 위에서 내려다보기 때문에 하늘은 안 보이고, 바닥·가장자리 소품·입자 효과가 분위기를 만든다.
public class AutoChessThemes : MonoBehaviour
{
    public const string Key = "AutoChessTheme";
    static AutoChessThemes inst;
    public static AutoChessThemes I => inst != null ? inst : inst = FindFirstObjectByType<AutoChessThemes>();

    // 소품 한 종류: kind=임시 도형 모양, h=높이, c=색, glow=발광색(알파 0이면 없음), y=바닥에서 띄우는 높이
    class Slot
    {
        public string kind; public float h, y; public Color c, glow;
        public Slot(string kind, float h, Color c, Color? glow = null, float y = 0) { this.kind = kind; this.h = h; this.c = c; this.glow = glow ?? Color.clear; this.y = y; }
    }

    class Theme
    {
        public string name, key, fx; public Color sky, groundNear, groundFar, light; public bool ground = true; public float intensity = 1;
        public Slot[] near, far;
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    static readonly Theme[] Themes =
    {
        new Theme { name = "고대 거인의 투기장", key = "Arena", fx = "dust", sky = C(.1f, .1f, .13f), groundNear = C(.30f, .29f, .31f), groundFar = C(.30f, .29f, .31f), light = C(1, .96f, .88f),
            near = new[] { new Slot("pillar", 3.4f, C(.66f, .63f, .58f)), new Slot("brokenpillar", 2f, C(.6f, .58f, .54f)), new Slot("block", 1.1f, C(.5f, .49f, .47f)), new Slot("stand", 1.5f, C(.55f, .53f, .5f)) } },
        new Theme { name = "정령의 숲 성소", key = "Forest", fx = "firefly", sky = C(.1f, .2f, .14f), groundNear = C(.2f, .38f, .22f), groundFar = C(.17f, .32f, .2f), light = C(.95f, 1, .85f),
            near = new[] { new Slot("tree", 4.2f, C(.25f, .62f, .36f)), new Slot("tree", 3.4f, C(.36f, .72f, .4f)), new Slot("crystal", 1.4f, C(.4f, .85f, .95f), C(.2f, .8f, .9f)), new Slot("mushroom", 1.1f, C(.85f, .5f, .75f), C(.5f, .2f, .45f)) } },
        new Theme { name = "하늘 위 부유 섬", key = "Sky", fx = "mote", sky = C(.55f, .78f, .96f), ground = false, light = C(1, 1, 1), intensity = 1.1f,
            near = new[] { new Slot("floatrock", 2.4f, C(.55f, .5f, .45f), null, 0f), new Slot("cloud", 1.6f, C(1, 1, 1), null, -1.6f), new Slot("cloud", 1.2f, C(.95f, .97f, 1), null, -2.6f), new Slot("floatrock", 1.6f, C(.5f, .48f, .5f), null, .6f) } },
        new Theme { name = "화산과 얼음 대립 지대", key = "Volcano", fx = "ember", sky = C(.1f, .05f, .05f), groundNear = C(.72f, .86f, .94f), groundFar = C(.26f, .09f, .07f), light = C(1, .95f, .95f),
            near = new[] { new Slot("crystal", 2.4f, C(.6f, .86f, 1), C(.3f, .6f, .9f)), new Slot("snowrock", 1.5f, C(.92f, .96f, 1)) },
            far = new[] { new Slot("lavarock", 2.1f, C(.2f, .1f, .09f), C(.95f, .35f, .08f)), new Slot("spire", 3.4f, C(.28f, .09f, .08f), C(.8f, .2f, .05f)) } },
        new Theme { name = "별빛 신전", key = "Night", fx = "stardust", sky = C(.03f, .04f, .1f), groundNear = C(.09f, .1f, .19f), groundFar = C(.09f, .1f, .19f), light = C(.62f, .7f, 1), intensity = .7f,
            near = new[] { new Slot("lantern", 1.7f, C(.5f, .5f, .62f), C(1, .75f, .35f)), new Slot("pillar", 3.1f, C(.36f, .4f, .58f)), new Slot("crystal", 1.8f, C(.7f, .5f, 1), C(.55f, .3f, .95f)), new Slot("block", 1.0f, C(.3f, .33f, .48f)) } },
    };

    public static int Count => Themes.Length;
    public int Index { get; private set; }
    public string CurrentName => Themes[Index].name;

    // 소품 자리(x, z, 높이 배율). 전장은 x 0~7, z 0~7(아군 0~3, 적 4~7), 대기석 z=-1.6
    static readonly float[][] Spots =
    {
        new[] { -2.8f, -3.5f, .9f }, new[] { -2.8f, -.5f, 1 }, new[] { -2.8f, 2.5f, 1 }, new[] { -2.8f, 5.5f, 1 }, new[] { -2.8f, 8.5f, 1.1f },
        new[] { 9.8f, -3.5f, .9f }, new[] { 9.8f, -.5f, 1 }, new[] { 9.8f, 2.5f, 1 }, new[] { 9.8f, 5.5f, 1 }, new[] { 9.8f, 8.5f, 1.1f },
        new[] { -1f, 11f, 1.3f }, new[] { 3.5f, 11.5f, 1.4f }, new[] { 8f, 11f, 1.3f },
        new[] { -2.5f, -5.5f, .8f }, new[] { 1.2f, -5.8f, .7f }, new[] { 5.8f, -5.8f, .7f }, new[] { 9.5f, -5.5f, .8f },
    };

    Transform root; Light sun; Color sunColor; float sunIntensity; Color camColor; bool saved; Texture2D soft;

    void Awake() { inst = this; }

    void Start() { Apply(Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, Themes.Length - 1)); }

    public void Cycle() => Apply((Index + 1) % Themes.Length);

    public void Apply(int i)
    {
        Index = i; PlayerPrefs.SetInt(Key, i);
        if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); } // Destroy 는 프레임 끝에 처리되므로 먼저 숨겨 둔다
        root = new GameObject("Theme_" + Themes[i].key).transform;
        var t = Themes[i];
        var cam = Camera.main;
        if (sun == null) sun = FindFirstObjectByType<Light>();
        if (!saved) { saved = true; if (sun != null) { sunColor = sun.color; sunIntensity = sun.intensity; } if (cam != null) camColor = cam.backgroundColor; }
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = t.sky; }
        if (sun != null) { sun.color = t.light; sun.intensity = sunIntensity * t.intensity; }

        if (t.ground)
        {
            Ground(t.groundNear, -16, 3.5f);
            Ground(t.groundFar, 3.5f, 18);
        }
        var rnd = new System.Random(1234 + i * 77); // 같은 테마는 항상 같은 배치
        int ni = 0, fi = 0;
        foreach (var s in Spots)
        {
            bool nearSide = s[1] < 3.5f;
            var list = nearSide || t.far == null ? t.near : t.far;
            int idx = nearSide || t.far == null ? ni++ % list.Length : fi++ % list.Length;
            int assetNo = (nearSide || t.far == null ? 0 : t.near.Length) + idx + 1;
            Place(t, list[idx], assetNo, s[0] + Jit(rnd, .5f), s[1] + Jit(rnd, .6f), s[2] * (.9f + (float)rnd.NextDouble() * .3f), (float)rnd.NextDouble() * 360);
        }
        Effects(t);
    }

    static float Jit(System.Random r, float a) => ((float)r.NextDouble() * 2 - 1) * a;

    void Ground(Color c, float z0, float z1)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Ground";
        g.transform.SetParent(root, false);
        g.transform.position = new Vector3(3.5f, -.35f, (z0 + z1) / 2);
        g.transform.localScale = new Vector3(44, .2f, z1 - z0);
        g.GetComponent<Renderer>().sharedMaterial = Mat(c);
        Destroy(g.GetComponent<Collider>());
    }

    void Place(Theme t, Slot s, int assetNo, float x, float z, float hm, float yaw)
    {
        float h = s.h * hm;
        var prefab = Resources.Load<GameObject>($"Visuals/Themes/{t.key}/{t.key}_{assetNo:00}");
        GameObject go;
        if (prefab != null)
        {
            go = new GameObject(prefab.name);
            var inst = Instantiate(prefab, go.transform);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
            var b = BoundsOf(inst);
            if (b.size.y > .0001f) inst.transform.localScale *= h / b.size.y;
            b = BoundsOf(inst);
            inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z); // 발 밑 중앙 = 원점
        }
        else go = Placeholder(s, h);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(x, s.y, z);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    // ---------- 임시 도형 소품 (높이 1 기준으로 만든 뒤 h 배로 키움) ----------
    GameObject Placeholder(Slot s, float h)
    {
        var go = new GameObject("ph_" + s.kind);
        var T = go.transform;
        Color c = s.c, dark = c * .75f; dark.a = 1;
        switch (s.kind)
        {
            case "pillar":
                Part(T, PrimitiveType.Cube, 0, .04f, 0, .42f, .08f, .42f, c);
                Part(T, PrimitiveType.Cylinder, 0, .5f, 0, .26f, .84f, .26f, c);
                Part(T, PrimitiveType.Cube, 0, .96f, 0, .42f, .08f, .42f, c);
                break;
            case "brokenpillar":
                Part(T, PrimitiveType.Cube, 0, .05f, 0, .5f, .1f, .5f, c);
                Part(T, PrimitiveType.Cylinder, 0, .4f, 0, .3f, .6f, .3f, c);
                Part(T, PrimitiveType.Cube, .42f, .1f, .1f, .3f, .18f, .26f, dark, 0, 25, 8);
                break;
            case "block":
                Part(T, PrimitiveType.Cube, 0, .2f, 0, .62f, .4f, .5f, c, 0, 15, 0);
                Part(T, PrimitiveType.Cube, .1f, .56f, 0, .36f, .3f, .34f, dark, 0, 40, 0);
                break;
            case "stand":
                Part(T, PrimitiveType.Cube, 0, .17f, 0, 1.3f, .34f, .9f, c);
                Part(T, PrimitiveType.Cube, 0, .5f, -.05f, 1f, .32f, .7f, dark);
                Part(T, PrimitiveType.Cube, 0, .82f, -.1f, .7f, .32f, .5f, c);
                break;
            case "tree":
                Part(T, PrimitiveType.Cylinder, 0, .22f, 0, .14f, .45f, .14f, C(.4f, .27f, .17f));
                Part(T, PrimitiveType.Sphere, 0, .62f, 0, .62f, .5f, .62f, c);
                Part(T, PrimitiveType.Sphere, 0, .86f, 0, .44f, .36f, .44f, dark);
                break;
            case "crystal":
                Part(T, PrimitiveType.Cube, 0, .5f, 0, .2f, 1f, .2f, c, 0, 45, 6, s.glow);
                Part(T, PrimitiveType.Cube, .16f, .3f, .05f, .14f, .6f, .14f, c, 12, 20, -14, s.glow);
                Part(T, PrimitiveType.Cube, -.14f, .26f, -.06f, .12f, .5f, .12f, c, -10, 70, 18, s.glow);
                break;
            case "mushroom":
                Part(T, PrimitiveType.Cylinder, 0, .2f, 0, .12f, .4f, .12f, C(.9f, .88f, .8f));
                Part(T, PrimitiveType.Sphere, 0, .5f, 0, .6f, .3f, .6f, c, 0, 0, 0, s.glow);
                break;
            case "floatrock":
                Part(T, PrimitiveType.Sphere, 0, .42f, 0, .8f, .7f, .8f, c);
                Part(T, PrimitiveType.Cylinder, 0, .78f, 0, .86f, .1f, .86f, C(.4f, .72f, .38f));
                break;
            case "cloud":
                Part(T, PrimitiveType.Sphere, 0, .5f, 0, 1.6f, .7f, 1.2f, c);
                Part(T, PrimitiveType.Sphere, .6f, .4f, .1f, 1.1f, .55f, .9f, c);
                Part(T, PrimitiveType.Sphere, -.6f, .35f, -.1f, 1f, .5f, .8f, c);
                break;
            case "lantern":
                Part(T, PrimitiveType.Cylinder, 0, .3f, 0, .2f, .6f, .2f, c);
                Part(T, PrimitiveType.Cube, 0, .66f, 0, .34f, .06f, .34f, dark);
                Part(T, PrimitiveType.Sphere, 0, .82f, 0, .3f, .3f, .3f, C(1, .85f, .5f), 0, 0, 0, s.glow);
                break;
            case "snowrock":
                Part(T, PrimitiveType.Sphere, 0, .3f, 0, .8f, .6f, .7f, c);
                Part(T, PrimitiveType.Sphere, .35f, .16f, .2f, .5f, .32f, .46f, c * .92f);
                break;
            case "lavarock":
                Part(T, PrimitiveType.Sphere, 0, .3f, 0, .8f, .6f, .7f, c);
                Part(T, PrimitiveType.Cube, 0, .56f, 0, .3f, .08f, .3f, s.glow, 0, 30, 0, s.glow);
                Part(T, PrimitiveType.Cube, .3f, .2f, .22f, .2f, .06f, .2f, s.glow, 0, 10, 0, s.glow);
                break;
            case "spire":
                Part(T, PrimitiveType.Cube, 0, .5f, 0, .26f, 1f, .26f, c, 0, 45, 4);
                Part(T, PrimitiveType.Cube, .22f, .3f, 0, .16f, .6f, .16f, c, 0, 45, -10);
                Part(T, PrimitiveType.Cube, 0, .96f, 0, .12f, .12f, .12f, s.glow, 0, 45, 0, s.glow);
                break;
        }
        T.localScale = Vector3.one * h;
        return go;
    }

    static void Part(Transform parent, PrimitiveType type, float x, float y, float z, float sx, float sy, float sz, Color c, float rx = 0, float ry = 0, float rz = 0, Color? glow = null)
    {
        var p = GameObject.CreatePrimitive(type);
        p.transform.SetParent(parent, false);
        p.transform.localPosition = new Vector3(x, y, z);
        p.transform.localRotation = Quaternion.Euler(rx, ry, rz);
        // 원기둥은 높이 2 기준이라 y 크기를 절반으로 맞춘다
        p.transform.localScale = new Vector3(sx, type == PrimitiveType.Cylinder ? sy / 2 : sy, sz);
        p.GetComponent<Renderer>().sharedMaterial = Mat(c, glow);
        Destroy(p.GetComponent<Collider>());
    }

    static Material Mat(Color c, Color? glow = null)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", .15f);
        if (glow.HasValue && glow.Value.a > 0)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", glow.Value * 1.4f);
        }
        return m;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ---------- 입자 효과 ----------
    void Effects(Theme t)
    {
        switch (t.fx)
        {
            case "dust": Particles(new Vector3(3.5f, 1.5f, 3.5f), new Vector3(18, 3, 20), 14, 6, .08f, C(.9f, .85f, .75f), C(.7f, .68f, .62f), .15f, .3f); break;
            case "firefly": Particles(new Vector3(3.5f, 1.4f, 3.5f), new Vector3(18, 2.5f, 20), 22, 5, .14f, C(.9f, 1, .4f), C(.6f, 1, .7f), .05f, 1f); break;
            case "mote": Particles(new Vector3(3.5f, 0, 3.5f), new Vector3(18, 2, 20), 18, 6, .1f, Color.white, C(.9f, .95f, 1), .6f, .5f); break;
            case "ember":
                Particles(new Vector3(3.5f, 0, 8.5f), new Vector3(18, 1, 9), 26, 4, .1f, C(1, .55f, .1f), C(1, .3f, .05f), 1.2f, .6f);   // 적 진영: 불씨가 위로
                Particles(new Vector3(3.5f, 5f, -.5f), new Vector3(18, 1, 9), 26, 6, .1f, Color.white, C(.7f, .9f, 1), -.8f, .6f); // 아군 진영: 눈이 아래로
                break;
            case "stardust": Particles(new Vector3(3.5f, 1.8f, 3.5f), new Vector3(20, 3.5f, 22), 24, 4, .09f, C(1, .95f, .7f), C(.7f, .8f, 1), .1f, .8f); break;
        }
    }

    void Particles(Vector3 center, Vector3 box, float rate, float life, float size, Color c1, Color c2, float rise, float wobble)
    {
        var go = new GameObject("Fx");
        go.transform.SetParent(root, false);
        go.transform.position = center;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(size * .6f, size * 1.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(c1, c2); main.maxParticles = 400;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = 0; vel.z = 0; vel.y = rise;
        var noise = ps.noise; noise.enabled = true; noise.strength = wobble; noise.frequency = .3f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(1, .75f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = Soft() };
        r.sharedMaterial = m;
        ps.Play();
    }

    Texture2D Soft()
    {
        if (soft != null) return soft;
        const int n = 32;
        soft = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - .5f, n / 2f - .5f)) / (n / 2f);
                soft.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
            }
        soft.Apply();
        return soft;
    }
}
