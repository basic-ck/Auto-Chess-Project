using System.Collections.Generic;
using UnityEngine;

// 8x8 오토체스 + 대기석(1x8). 씬 설정 없이 플레이하면 자동 생성됨.
public class AutoChess : MonoBehaviour
{
    // ================= 유닛 데이터베이스 (40종 + 소환 유령) =================
    enum Race { Human, Beast, Undead, Spirit, Golem, Machine, Goblin, Orc, Elf, Dragon, Dwarf, Naga, Demon, God }
    enum Job { Knight, Hunter, Assassin, Mage, Warrior, Priest, Shaman, Summoner, Warlock, Mecha }
    static readonly string[] RaceName = { "인간", "야수", "언데드", "정령", "골렘", "기계", "고블린", "오크", "엘프", "용", "드워프", "나가", "악마", "신" };
    static readonly string[] JobName = { "기사", "사냥꾼", "암살자", "마법사", "전사", "사제", "주술사", "소환사", "흑마법사", "메카" };
    const int RaceN = 14, Tags = 24, Kinds = 40; // 시너지 태그 = 종족 14 + 직업 10, Kinds = 상점에 나오는 유닛 수

    // 패시브 특성(Fx)과 마나 완충 시 발동하는 액티브 스킬(Sk)
    [System.Flags] enum Fx { None = 0, Taunt = 1, Infiltrate = 2, Explode = 4, Berserk = 8, Poison = 16, Pierce = 32, Bounce = 64, Lifesteal = 128, Crit = 256, Gold = 512, Aura = 1024, Armor = 2048, Double = 4096, Snipe = 8192 }
    enum Sk { None, Fireball, Heal, Stun, Slow, Summon, Whirl, Dragon, Blast, Zone, Slam, Column, Invuln, HealAll, TimeStop, Petrify, Silence, Bomb, Ward, Drain, Chain, Tsunami, BlackHole, Bombard, Smite }

    // 인덱스 = UnitId. Ghost(소환 유령)는 상점에 안 나오는 전투 중 임시 유닛.
    enum UnitId
    {
        TrainingKnight, ForestApprentice, AlleyAssassin, FlameNovice, RockSentinel, ClockworkRat, VillageDoctor, WildWolf, GoblinThief, DesertShaman,
        Berserker, VenomScorpion, ShadowNinja, IceScholar, PaladinCommander, SoulSummoner, SteelGuardian, WindTracker, PirateBlade, VampireNoble,
        DragonKnight, MasterSniper, GrandScholar, MoonDancer, ToxicAlchemist, LavaGiant, Commander, DeepSeaAssassin, StarPriest, TimeMage,
        MedusaQueen, AbyssLord, BombManiac, HolyGuardian, PlagueArchpriest, ThunderShaman,
        TidalGod, CalamitySpirit, AirFleetBomber, Zeus,
        Ghost,
    }

    class UD
    {
        public string name, desc; public int cost, race, job, hp, atk, range, mana; public float aspd, sh; public bool glow;
        public PrimitiveType prim; public Color col; public Vector3 sz; public Fx fx; public Sk sk;
    }

    static UD D(string name, int cost, Race race, Job job, PrimitiveType prim, Color col, Vector3 sz, int hp, int atk, int range, float aspd, int mana, Sk sk, Fx fx, string desc, float sh = 0, bool glow = false)
        => new UD { name = name, cost = cost, race = (int)race, job = (int)job, prim = prim, col = col, sz = sz, hp = hp, atk = atk, range = range, aspd = aspd, mana = mana, sk = sk, fx = fx, desc = desc, sh = sh, glow = glow };

    static Vector3 S(float s) => Vector3.one * s;
    static Color C(float r, float g, float b, float a = 1) => new Color(r, g, b, a);
    const PrimitiveType Cube = PrimitiveType.Cube, Caps = PrimitiveType.Capsule, Sph = PrimitiveType.Sphere, Cyl = PrimitiveType.Cylinder;

    static readonly UD[] UnitDataDatabase =
    {
        // ---- 1골드 ----
        D("수련기사", 1, Race.Human, Job.Knight, Cube, C(.2f, .4f, 1), S(.7f), 100, 12, 1, 1, 0, Sk.None, Fx.Taunt, "도발, 전투 시작 시 작은 보호막", .15f),
        D("숲의 견습사수", 1, Race.Beast, Job.Hunter, Caps, C(.2f, .8f, .2f), S(.65f), 60, 14, 3, 1, 0, Sk.None, Fx.None, "사거리 3 기본 원거리 딜러"),
        D("골목 암살자", 1, Race.Undead, Job.Assassin, Caps, C(.08f, .08f, .1f), new Vector3(.3f, .55f, .3f), 45, 9, 1, 1, 0, Sk.None, Fx.Infiltrate, "전투 시작 시 적 후열로 침투"),
        D("불꽃 수습생", 1, Race.Spirit, Job.Mage, Sph, C(1, .5f, 0), S(.65f), 50, 22, 2, 1, 4, Sk.Fireball, Fx.None, "마나 완충 시 강력한 화염구"),
        D("바위 파수꾼", 1, Race.Golem, Job.Warrior, Cube, C(.5f, .5f, .55f), S(1f), 130, 8, 1, 1, 0, Sk.None, Fx.None, "단단한 체력의 근접 탱커"),
        D("태엽 쥐", 1, Race.Machine, Job.Mecha, Cube, C(1, .9f, .2f), S(.4f), 40, 8, 1, 1.2f, 0, Sk.None, Fx.Explode, "사망 시 자폭해 주변 적에게 피해"),
        D("마을 의사", 1, Race.Human, Job.Priest, Cyl, C(1, 1, 1), new Vector3(.5f, .45f, .5f), 55, 6, 2, 1, 3, Sk.Heal, Fx.None, "주기적으로 체력 낮은 아군 회복"),
        D("야생 늑대", 1, Race.Beast, Job.Warrior, Cyl, C(.45f, .28f, .1f), new Vector3(.55f, .35f, .55f), 55, 9, 1, 1.6f, 0, Sk.None, Fx.None, "빠른 공격 속도"),
        D("고블린 도둑", 1, Race.Goblin, Job.Assassin, Caps, C(.55f, .9f, .3f), S(.5f), 50, 9, 1, 1, 0, Sk.None, Fx.Gold, "공격 시 20% 확률로 +1골드"),
        D("사막 주술사", 1, Race.Orc, Job.Shaman, Sph, C(.6f, .2f, .8f), S(.6f), 50, 8, 2, 1, 4, Sk.Stun, Fx.None, "마나 완충 시 적 1명 기절"),
        // ---- 2골드 ----
        D("광전사", 2, Race.Orc, Job.Warrior, Cube, C(.9f, .1f, .1f), S(.75f), 110, 12, 1, 1, 0, Sk.None, Fx.Berserk, "체력이 낮을수록 공격 속도 증가"),
        D("독침 전갈", 2, Race.Beast, Job.Hunter, Caps, C(0, .7f, .7f), S(.55f), 65, 11, 2, 1, 0, Sk.None, Fx.Poison, "공격 시 지속 중독 피해"),
        D("그림자 닌자", 2, Race.Demon, Job.Assassin, Caps, C(.25f, .05f, .35f), new Vector3(.4f, .5f, .4f), 60, 10, 1, 1, 0, Sk.None, Fx.Infiltrate | Fx.Double, "적 후열 침투 후 2연속 타격"),
        D("얼음 학자", 2, Race.Spirit, Job.Mage, Sph, C(.5f, .8f, 1), S(.6f), 60, 13, 3, 1, 4, Sk.Slow, Fx.None, "마나 완충 시 주변 적 감속"),
        D("성기사 단장", 2, Race.Human, Job.Knight, Cube, C(1, .8f, .1f), S(.85f), 130, 9, 1, 1, 0, Sk.None, Fx.Aura, "주변 아군이 받는 피해 15% 감소"),
        D("영혼 소환사", 2, Race.Undead, Job.Summoner, Sph, C(.25f, .5f, .15f), S(.6f), 65, 8, 2, 1, 4, Sk.Summon, Fx.None, "미니 유령 소환 (최대 3마리)"),
        D("강철 수호자", 2, Race.Machine, Job.Warrior, Cube, C(.8f, .82f, .85f), S(1f), 150, 9, 1, 1, 0, Sk.None, Fx.Taunt | Fx.Armor, "도발, 받는 피해 30% 감소"),
        D("바람 추적자", 2, Race.Elf, Job.Hunter, Caps, C(.4f, 1, .4f), S(.6f), 65, 14, 4, 1, 0, Sk.None, Fx.Pierce, "관통하는 바람 화살"),
        D("해적 칼잡이", 2, Race.Human, Job.Warrior, Cube, C(.1f, .3f, .8f), S(.7f), 100, 13, 1, 1, 4, Sk.Whirl, Fx.None, "마나 완충 시 광역 회전 베기"),
        D("흡혈 귀족", 2, Race.Undead, Job.Warlock, Cyl, C(.6f, .1f, .3f), S(.55f), 80, 12, 2, 1, 0, Sk.None, Fx.Lifesteal, "공격 시 체력 회복"),
        // ---- 3골드 ----
        D("드래곤 나이트", 3, Race.Dragon, Job.Knight, Cube, C(1, .45f, 0), S(.95f), 140, 16, 1, 1, 5, Sk.Dragon, Fx.None, "마나 완충 시 화염 드래곤 변신"),
        D("저격 명수", 3, Race.Dwarf, Job.Hunter, Caps, C(.45f, .28f, .1f), new Vector3(.35f, .9f, .35f), 70, 38, 99, .5f, 0, Sk.None, Fx.Snipe, "전장 어디든 최저 체력 적 저격"),
        D("대성학자", 3, Race.Human, Job.Mage, Sph, C(.3f, .6f, 1), S(.7f), 75, 20, 3, 1, 5, Sk.Blast, Fx.None, "마나 완충 시 광역 마법 폭발", 0, true),
        D("달빛 무희", 3, Race.Elf, Job.Knight, Cyl, C(1, .5f, .7f), S(.55f), 85, 15, 2, 1, 0, Sk.None, Fx.Bounce, "튕기는 투사체 공격"),
        D("독폭탄 연금술사", 3, Race.Goblin, Job.Warlock, Sph, C(.8f, 1, .2f), S(.6f), 80, 12, 3, 1, 5, Sk.Zone, Fx.None, "마나 완충 시 독 장판 설치"),
        D("용암 거인", 3, Race.Spirit, Job.Warrior, Cube, C(.6f, .25f, .2f), S(1.05f), 170, 14, 1, 1, 5, Sk.Slam, Fx.None, "마나 완충 시 적을 던져 광역 기절"),
        D("사령관", 3, Race.Human, Job.Warrior, Cube, C(.2f, .3f, .45f), S(.8f), 130, 14, 1, 1, 5, Sk.Column, Fx.None, "마나 완충 시 일렬 돌격 광역 기절"),
        D("심해 암살자", 3, Race.Naga, Job.Assassin, Caps, C(.05f, .15f, .5f), new Vector3(.4f, .55f, .4f), 75, 14, 1, 1, 5, Sk.Invuln, Fx.Infiltrate | Fx.Crit, "침투, 치명타, 마나 완충 시 순간 무적"),
        D("별빛 사제", 3, Race.Elf, Job.Priest, Cyl, C(1, 1, .95f), S(.55f), 75, 8, 3, 1, 5, Sk.HealAll, Fx.None, "마나 완충 시 아군 전체 대량 회복", 0, true),
        D("시간 술사", 3, Race.God, Job.Mage, Sph, C(.7f, .3f, 1), S(.65f), 70, 14, 3, 1, 6, Sk.TimeStop, Fx.None, "마나 완충 시 넓은 범위 적 정지", 0, true),
        // ---- 4골드 ----
        D("메두사 여왕", 4, Race.Naga, Job.Hunter, Caps, C(.1f, .8f, .45f), S(.7f), 110, 20, 3, 1, 6, Sk.Petrify, Fx.None, "마나 완충 시 전방 적 전체 석화"),
        D("심연의 군주", 4, Race.Demon, Job.Warrior, Cube, C(.35f, .05f, .05f), S(1.05f), 190, 20, 1, 1, 5, Sk.Silence, Fx.None, "마나 완충 시 적 침묵 + 지속 피해"),
        D("폭탄 광인", 4, Race.Machine, Job.Mecha, Cube, C(1, 0, 0), S(.7f), 100, 16, 3, 1, 5, Sk.Bomb, Fx.None, "마나 완충 시 대형 시한폭탄 설치"),
        D("성스러운 대수호자", 4, Race.God, Job.Knight, Cube, C(1, .85f, .3f), S(1f), 200, 12, 1, 1, 7, Sk.Ward, Fx.None, "마나 완충 시 아군 전체 무적 보호막", 0, true),
        D("대사제 역병의 주인", 4, Race.Undead, Job.Warlock, Cyl, C(.1f, .25f, .1f), S(.65f), 120, 14, 3, 1, 7, Sk.Drain, Fx.None, "마나 완충 시 적 전체 체력 흡수"),
        D("천둥 주술사", 4, Race.Orc, Job.Shaman, Sph, C(1, 1, 0), S(.65f), 105, 20, 3, 1, 5, Sk.Chain, Fx.None, "마나 완충 시 체인 라이트닝", 0, true),
        // ---- 5골드 ----
        D("해일의 신", 5, Race.Naga, Job.Warrior, Cube, C(.02f, .1f, .4f), S(1.25f), 280, 24, 1, 1, 8, Sk.Tsunami, Fx.None, "마나 완충 시 전장 전체 에어본(기절)"),
        D("대재앙의 정령", 5, Race.Spirit, Job.Warlock, Sph, C(.03f, .03f, .05f), S(1.15f), 220, 26, 3, 1, 8, Sk.BlackHole, Fx.None, "마나 완충 시 블랙홀 생성"),
        D("비행함대 폭격기", 5, Race.Dwarf, Job.Mecha, Caps, C(.8f, .82f, .85f), new Vector3(.8f, .9f, .8f), 230, 24, 4, 1, 8, Sk.Bombard, Fx.None, "마나 완충 시 필드 전체 융단폭격"),
        D("태초의 신 제우스", 5, Race.God, Job.Mage, Sph, C(1, 1, 1), S(1.05f), 250, 32, 4, 1, 8, Sk.Smite, Fx.None, "마나 완충 시 모든 적에게 뇌전", 0, true),
        // ---- 임시(상점 제외) ----
        new UD { name = "소환 유령", desc = "소환사가 부른 임시 유닛", cost = 0, race = -1, job = -1, prim = Sph, col = C(.7f, .85f, .7f), sz = S(.35f), hp = 40, atk = 6, range = 1, aspd = 1, fx = Fx.None, sk = Sk.None },
    };

    static UD Dt(int id) => UnitDataDatabase[id];
    static readonly List<int>[] ByCost = BuildByCost();
    static List<int>[] BuildByCost()
    {
        var l = new List<int>[6];
        for (int i = 0; i < l.Length; i++) l[i] = new List<int>();
        for (int id = 0; id < Kinds; id++) l[UnitDataDatabase[id].cost].Add(id);
        return l;
    }

    // 플레이어 레벨별 1~5골드 유닛 등장 확률(%)
    static readonly int[][] Odds =
    {
        new[] { 100, 0, 0, 0, 0 }, new[] { 100, 0, 0, 0, 0 }, new[] { 75, 25, 0, 0, 0 }, new[] { 55, 30, 15, 0, 0 }, new[] { 45, 33, 20, 2, 0 },
        new[] { 30, 40, 25, 5, 0 }, new[] { 19, 30, 35, 15, 1 }, new[] { 15, 20, 35, 25, 5 }, new[] { 10, 15, 30, 30, 15 }, new[] { 5, 10, 20, 40, 25 },
    };

    // ================= 런타임 =================
    // bench=true 이면 대기석 유닛(x=칸 번호, z=-1). 전투에 참여하지 않는다.
    class U
    {
        public bool boss, temp, bench, prefab; public Vector3 kick; public float flash, acc;
        public float y0 = .5f, top; public Vector3 baseSz; // y0=바닥에서 오브젝트 원점 높이(3D 유닛은 발 밑이 원점이라 0), top=머리 위 표식 기준 높이(3D 유닛), baseSz=기본 크기
        public int star = 1, team, id, hp, max, atk, range, x, z, sx, sz, shield, mana, stun, slow, silence, poison, pdmg, invuln, buff; public GameObject go;
        public UD d => UnitDataDatabase[id];
        public bool Alive => hp > 0;
        public bool Active => hp > 0 && !bench; // 전투/타겟 대상
    }

    class Zone { public int x, z, r, ticks, team, dmg; public bool pull, bomb; public GameObject go; }

    const int N = 8;
    const float BenchZ = -1.6f;   // 대기석 줄의 월드 z

    List<U> us = new List<U>();
    List<Zone> zs = new List<Zone>();
    int[] shop = new int[5]; // 상점 슬롯 5개
    U sel, dragU;
    const int MaxTicks = 250; // 전투 제한 시간 100초(0.4초 x 250)
    int gold, life, round, tickNo, finalRound;
    bool battle, over, shopOpen, cleared, timedOut;
    float t;
    string msg = "";
    readonly List<Rect> ui = new List<Rect>(); // 이번 OnGUI에서 그린 UI 영역(맵 클릭 제외용)
    GUIStyle center, card, wrap, titleStyle, smallStyle;

    [RuntimeInitializeOnLoadMethod]
    static void Boot()
    {
        var go = new GameObject("AutoChess");
        go.AddComponent<AutoChessVisualManager>(); // 비주얼 에셋(3D 타일·유닛·배경, UI 스프라이트) 연결. 에셋이 없으면 기본 도형/UI
        go.AddComponent<AutoChess>();
    }

    void Start()
    {
        var cam = Camera.main;
        cam.fieldOfView = 40;
        cam.transform.SetPositionAndRotation(new Vector3(3.5f, 16.2f, -4.25f), Quaternion.Euler(68, 0, 0));
        var K = AutoChessVisualManager.TileKind.PlayerLight; // 타일 역할: 아군 밝은/어두운, 적 밝은/어두운, 대기석 A/B
        for (int x = 0; x < N; x++)
        {
            for (int z = 0; z < N; z++)
            {
                bool light = (x + z) % 2 == 0;
                if (z < N / 2) Tile(light ? K : K + 1, x, z, light ? new Color(.25f, .25f, .3f) : new Color(.4f, .4f, .45f));
                else Tile(light ? K + 2 : K + 3, x, z, new Color(.35f, .2f, .2f) * (light ? 1 : 1.3f));
            }
            Tile(x % 2 == 0 ? K + 4 : K + 5, x, BenchZ, new Color(.15f, .35f, .35f) * (1 + x % 2 * .3f)); // 대기석 1x8
        }
        AutoChessVisualManager.I.BuildStage(3.5f, (N - 1 + BenchZ) / 2, 10.5f, N, N / 2 - .5f); // 경계선·바닥 받침·배경(있을 때만)
        Reset();
    }

    // 3D 타일 메쉬(Resources/Visuals/Map/Map_Tile_NN)가 있으면 그것을, 없으면 기본 큐브 타일을 놓는다
    static void Tile(AutoChessVisualManager.TileKind kind, float x, float z, Color col)
    {
        if (AutoChessVisualManager.I.Tile(kind, x, z) != null) return;
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = new Vector3(x, -.1f, z);
        c.transform.localScale = new Vector3(.95f, .2f, .95f);
        c.GetComponent<Renderer>().material = Mat(col);
        Destroy(c.GetComponent<Collider>());
    }

    static Material Mat(Color c, bool glow = false)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.3f); }
        return m;
    }

    void Reset()
    {
        foreach (var u in us) Destroy(u.go);
        us.Clear();
        gold = 10; life = 3; round = 1; over = false; cleared = false; level = 1; xp = 0; shopOpen = false; msg = "";
        winStreak = 0; incomeLog = "";
        Time.timeScale = 1f; fts.Clear(); banner = ""; // 결과창에서 멈췄던 시간·연출 초기화
        Prep();
    }

    // 게임 종료(클리어/패배): 시간을 멈추고 결과 팝업을 띄운다. finalRound = 마지막으로 싸운 라운드.
    void EndGame(bool win)
    {
        cleared = win; over = true; battle = false; finalRound = round;
        Time.timeScale = 0f;
    }

    // 유닛 생성: 데이터베이스의 도형·색·크기로 시각 자동 생성. star 성급(스탯 x1.8씩), statMul은 체력·공격력 배율(적 난이도 등).
    U Spawn(int team, int id, int x, int z, float statMul = 1, bool bench = false, int star = 1)
    {
        var d = Dt(id);
        // 3D 유닛(Resources/Visuals/Units/Unit_NN)이 있으면 사용(적은 어둡게), 없으면 기본 도형
        float th = Mathf.Clamp(d.sz.y * (d.prim == PrimitiveType.Capsule || d.prim == PrimitiveType.Cylinder ? 2f : 1f), .7f, 1.5f), top;
        bool pf = AutoChessVisualManager.I.TryUnit(id, th, out var go, out top);
        if (pf) { if (team == 1) AutoChessVisualManager.I.Tint(go, new Color(.62f, .62f, .62f)); }
        else
        {
            go = GameObject.CreatePrimitive(d.prim);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material = Mat(team == 0 ? d.col : d.col * .5f, d.glow);
            go.transform.localScale = d.sz;
        }
        float m = statMul * Mathf.Pow(1.8f, star - 1);
        var u = new U { team = team, id = id, star = star, hp = (int)(d.hp * m), atk = (int)(d.atk * m), range = d.range, x = x, z = z, sx = x, sz = z, go = go, bench = bench,
            prefab = pf, y0 = pf ? 0 : .5f, top = top, baseSz = pf ? Vector3.one : d.sz };
        u.max = u.hp;
        Place(u);
        us.Add(u);
        return u;
    }

    static Vector3 Pos(U u) => new Vector3(u.x, u.y0, u.bench ? BenchZ : u.z);
    static void Place(U u) => u.go.transform.position = Pos(u);
    U UnitAt(int x, int z, bool bench) => us.Find(u => u.Alive && u.bench == bench && u.x == x && u.z == z);
    U At(int x, int z) => UnitAt(x, z, false);
    int FieldCount() => us.FindAll(u => u.team == 0 && !u.bench && !u.temp).Count;

    void Prep()
    {
        battle = false; sel = null; dragU = null; Roll();
        foreach (var p in ps) Destroy(p.go);
        ps.Clear();
        foreach (var z in zs) Destroy(z.go);
        zs.Clear();
        foreach (var v in vs) Destroy(v.go);
        vs.Clear();
        for (int i = us.Count - 1; i >= 0; i--)
            if (us[i].temp) { Destroy(us[i].go); us.RemoveAt(i); } // 전투 중 소환수 정리
        foreach (var u in us)
        {
            u.hp = u.max; u.shield = u.mana = u.stun = u.slow = u.silence = u.poison = u.invuln = u.buff = 0; u.acc = 0;
            if (!u.bench) { u.x = u.sx; u.z = u.sz; }
            u.go.SetActive(true); Place(u);
        }
        SpawnEnemies();
    }

    // 난이도: 적의 체력·공격력 배율 (쉬움 0.85 / 보통 1.0 / 어려움 1.25)
    static readonly string[] DiffName = { "쉬움", "보통", "어려움" };
    static readonly float[] DiffMul = { .85f, 1f, 1.25f };
    int diff = 1;
    bool Boss => StageManager.IsBoss(round); // 1-10, 2-10, 3-10 보스 라운드: 처치 시 +10G

    // ================= 30라운드 PvE 스테이지 (톡톡이의 시험 방식) =================
    // 스테이지 1~3 × 라운드 1~10 = 30라운드. 라운드 10, 20, 30은 보스(1-10 바위 파수꾼, 2-10 메두사 여왕, 3-10 제우스).
    static class StageManager
    {
        public const int Rounds = 30;
        public const float BossScale = 1.8f, BossHp = 1.5f; // 보스는 크기 1.8배, 추가 체력 1.5배
        public static readonly int[] BossId = { (int)UnitId.RockSentinel, (int)UnitId.MedusaQueen, (int)UnitId.Zeus };
        public static readonly int[] BossStar = { 2, 2, 2 };

        // 라운드별 편성: {유닛ID, 성급, 수량} 3개 값씩 이어 붙임. 보스 라운드는 보스를 제외한 호위 부대.
        // 1스테이지=1·2골드급 1성, 2스테이지=2·3골드급(일부 2성), 3스테이지=3~5골드급 2~3성.
        static readonly int[][] Waves =
        {
            /* 1-1 */ new[] { 0,1,1 },
            /* 1-2 */ new[] { 0,1,1, 1,1,1 },
            /* 1-3 */ new[] { 0,1,2, 1,1,1 },
            /* 1-4 */ new[] { 4,1,1, 0,1,1, 1,1,1, 3,1,1 },
            /* 1-5 */ new[] { 0,1,2, 1,1,2, 2,1,1 },
            /* 1-6 */ new[] { 4,1,1, 7,1,1, 1,1,2, 3,1,1 },
            /* 1-7 */ new[] { 0,1,2, 7,1,1, 1,1,2, 9,1,1 },
            /* 1-8 */ new[] { 4,1,2, 7,1,1, 3,1,2, 6,1,1 },
            /* 1-9 */ new[] { 0,1,2, 4,1,1, 1,1,2, 3,1,1 },
            /* 1-10 보스 */ new[] { 0,1,1, 1,1,2 },
            /* 2-1 */ new[] { 4,1,1, 10,1,1, 1,1,2, 11,1,1, 3,1,1 },
            /* 2-2 */ new[] { 16,1,1, 0,1,1, 17,1,1, 1,1,2, 13,1,1 },
            /* 2-3 */ new[] { 10,1,1, 4,1,1, 11,1,1, 13,1,1, 17,1,1, 12,1,1 },
            /* 2-4 */ new[] { 16,1,1, 10,1,1, 17,1,1, 13,1,1, 19,1,1, 11,1,1 },
            /* 2-5 */ new[] { 18,1,1, 14,1,1, 16,1,1, 17,1,1, 13,1,1, 12,1,1 },
            /* 2-6 */ new[] { 16,1,1, 10,1,1, 18,1,1, 19,1,1, 20,1,1, 17,1,1 },
            /* 2-7 */ new[] { 14,1,1, 25,1,1, 16,1,1, 17,1,1, 24,1,1, 15,1,1, 12,1,1 },
            /* 2-8 */ new[] { 25,1,1, 26,1,1, 14,1,1, 23,1,1, 22,1,1, 27,1,1 },
            /* 2-9 */ new[] { 20,1,1, 25,1,1, 18,1,1, 17,1,1, 21,1,1, 28,1,1, 27,1,1 },
            /* 2-10 보스 */ new[] { 16,1,1, 23,1,1, 21,1,1 },
            /* 3-1 */ new[] { 25,1,1, 26,1,1, 16,1,1, 22,1,1, 23,1,1, 27,1,1, 29,1,1 },
            /* 3-2 */ new[] { 20,1,1, 25,1,1, 31,1,1, 17,1,1, 24,1,1, 27,1,1, 28,1,1 },
            /* 3-3 */ new[] { 26,1,1, 14,2,1, 31,1,1, 21,1,1, 22,1,1, 35,1,1, 12,1,1 },
            /* 3-4 */ new[] { 31,1,1, 25,2,1, 33,1,1, 23,1,1, 22,1,1, 34,1,1, 27,1,1 },
            /* 3-5 */ new[] { 20,2,1, 26,1,1, 31,2,1, 16,2,1, 35,1,1, 28,1,1, 27,1,1 },
            /* 3-6 */ new[] { 36,1,1, 31,2,1, 25,2,1, 22,1,1, 29,1,1, 34,1,1, 12,2,1 },
            /* 3-7 */ new[] { 36,1,1, 26,2,1, 33,1,1, 35,2,1, 21,2,1, 28,1,1, 27,2,1 },
            /* 3-8 */ new[] { 36,1,1, 31,2,1, 14,2,1, 38,1,1, 22,2,1, 34,1,1, 35,1,1 },
            /* 3-9 */ new[] { 36,2,1, 37,1,1, 33,1,1, 31,2,1, 38,1,1, 29,2,1, 35,2,1, 28,2,1 },
            /* 3-10 보스 */ new[] { 36,1,1, 31,2,1, 33,1,1, 35,1,1, 27,2,1 },
        };

        public static bool IsBoss(int round) => round % 10 == 0;
        public static string Label(int round) => $"{(round - 1) / 10 + 1}-{(round - 1) % 10 + 1}";

        // 이 라운드의 적 목록 (보스가 있으면 맨 앞)
        public static List<(int id, int star, bool boss)> Enemies(int round)
        {
            var l = new List<(int, int, bool)>();
            if (IsBoss(round)) l.Add((BossId[round / 10 - 1], BossStar[round / 10 - 1], true));
            var w = Waves[round - 1];
            for (int i = 0; i < w.Length; i += 3)
                for (int c = 0; c < w[i + 2]; c++) l.Add((w[i], w[i + 1], false));
            return l;
        }
    }

    // 적 진영(위쪽 4줄) 자동 진형: 근접 탱커·전사=맨 앞줄(z=4), 암살자=둘째 줄(z=5), 사거리 2~3 원거리·마법사·사제=셋째 줄(z=6), 사거리 4 이상=맨 뒷줄(z=7)
    static readonly int[] Cols = { 3, 4, 2, 5, 1, 6, 0, 7 };
    static int Row(UD d) => d.job == (int)Job.Assassin ? N / 2 + 1 : d.range >= 4 ? N - 1 : d.range >= 2 ? N - 2 : N / 2;

    void SpawnEnemies()
    {
        for (int i = us.Count - 1; i >= 0; i--)
            if (us[i].team == 1) { Destroy(us[i].go); us.RemoveAt(i); }
        var used = new int[N];
        foreach (var (id, star, boss) in StageManager.Enemies(round))
        {
            int z = Row(Dt(id));
            if (used[z] >= N) z = Mathf.Max(N / 2, z - 1); // 줄이 가득 차면 앞줄로
            var u = Spawn(1, id, Cols[used[z]++ % N], z, DiffMul[diff], false, star);
            if (!boss) continue;
            u.boss = true; // 크기 1.8배(Update에서 반영), 체력 추가 상승, 대표색 그대로 표시
            u.max = u.hp = (int)(u.hp * StageManager.BossHp);
            if (!u.prefab) u.go.GetComponent<Renderer>().material = Mat(u.d.col, true); // 3D 유닛은 원래 색 그대로
        }
    }

    static int AutoXp = 2;

    // ================= 라운드 정산 (TFT 방식 골드 수급) =================
    // 기본(1-1~1-3: 2·3·4G, 이후 5G) + 이자(보유 골드 10G당 1G, 최대 5G) + 승리 +1G + 연승(2~3연승 +1G / 4~5연승 +2G / 6연승↑ +3G).
    // 패배하면 기본+이자만 받고 연승이 끊긴다. 이자는 정산 시점(다음 준비 단계 직전)의 보유 골드 기준.
    int winStreak;
    string incomeLog = ""; // 마지막 정산 내역 (준비 단계 동안 화면에 표시)

    static int Interest(int g) => Mathf.Min(5, g / 10);
    static int StreakBonus(int s) => s >= 6 ? 3 : s >= 4 ? 2 : s >= 2 ? 1 : 0;

    void CalculateRoundIncome(bool win)
    {
        int basic = round <= 3 ? round + 1 : 5;
        int interest = Interest(gold);
        winStreak = win ? winStreak + 1 : 0;
        int streak = StreakBonus(winStreak), victory = win ? 1 : 0;
        int total = basic + interest + streak + victory;
        gold += total;
        incomeLog = $"[라운드 정산] 기본 {basic}G" + (interest > 0 ? $" + 이자 {interest}G" : "") + (streak > 0 ? $" + 연승 {streak}G" : "") + (victory > 0 ? $" + 승리 {victory}G" : "") + $" = 총 +{total}G 수급!";
    }

    void Update()
    {
        foreach (var u in us) // 이동 보간 + 공격 돌진 + 피격 확대
        {
            u.kick = Vector3.MoveTowards(u.kick, Vector3.zero, 3 * Time.deltaTime);
            u.flash = Mathf.Max(0, u.flash - Time.deltaTime);
            u.go.transform.position = Vector3.MoveTowards(u.go.transform.position, Pos(u) + u.kick, 12 * Time.deltaTime);
            u.go.transform.localScale = u.baseSz * (1 + .2f * (u.star - 1)) * (u.boss ? 1.8f : 1) * (1 + u.flash * 2);
        }
        foreach (var pr in ps) // 투사체 비행(연출): 피해는 LandProjectiles에서 틱 단위로 처리. 타겟이 판매돼 사라졌으면 제자리
            if (pr.tgt.go != null) pr.go.transform.position = Vector3.MoveTowards(pr.go.transform.position, WP(pr.tgt), pr.spd * Time.deltaTime);
        fts.RemoveAll(f => Time.time - f.t0 > FTextLife);
        UpdateVfx();
        UpdateMarks();
        if (!battle) return;
        t += Time.deltaTime;
        if (t < .4f) return;
        t = 0;
        Tick();
    }

    // 전투 1틱(0.4초): 장판 → 각 유닛(상태이상 → 마나/스킬 → 공격 또는 이동) → 승패 판정
    void Tick()
    {
        tickNo++;
        LandProjectiles();
        for (int i = zs.Count - 1; i >= 0; i--)
        {
            var z = zs[i];
            foreach (var e in new List<U>(us))
            {
                if (!e.Active || e.team == z.team || Dist(e.x, e.z, z.x, z.z) > z.r) continue;
                if (z.pull) StepToward(e, z.x, z.z);
                if (!z.bomb) Dmg(e, z.dmg, false, true);
            }
            if (z.pull) Ring(new Vector3(z.x, 0, z.z), z.r + .5f, C(.4f, 0, .7f), .35f); // 블랙홀 맥동
            if (--z.ticks > 0) continue;
            if (z.bomb)
            {
                Ring(new Vector3(z.x, 0, z.z), z.r + .5f, C(1, .45f, 0), .6f); // 시한폭탄 폭발
                foreach (var e in new List<U>(us)) if (e.Active && e.team != z.team && Dist(e.x, e.z, z.x, z.z) <= z.r) Dmg(e, z.dmg);
            }
            Destroy(z.go); zs.RemoveAt(i);
        }
        foreach (var u in new List<U>(us))
        {
            if (!u.Active) continue;
            if (u.invuln > 0) u.invuln--;
            if (u.poison > 0) { u.poison--; Dmg(u, u.pdmg, true); if (!u.Active) continue; }
            if (u.slow > 0) u.slow--;
            if (u.silence > 0) u.silence--;
            if (u.buff > 0) u.buff--;
            if (u.stun > 0) { u.stun--; continue; }
            var d = u.d;
            if (d.mana > 0 && u.silence == 0 && ++u.mana >= d.mana && Cast(u)) u.mana = 0;
            var tgt = Pick(u);
            if (tgt == null) continue;
            if (Dist(u.x, u.z, tgt.x, tgt.z) <= u.range) Attack(u, tgt);
            else if (u.slow == 0 || (tickNo & 1) == 0) StepToward(u, tgt.x, tgt.z);
        }
        bool p = us.Exists(u => u.Active && !u.temp && u.team == 0), e2 = us.Exists(u => u.Active && !u.temp && u.team == 1);
        if (p && e2)
        {
            if (tickNo < MaxTicks) return;
            // 시간 초과(힐·무적으로 안 끝나는 전투 방지): 남은 체력 합이 더 많은 쪽 승리 (동률이면 적)
            int hp0 = 0, hp1 = 0;
            foreach (var u in us) if (u.Active && !u.temp) { if (u.team == 0) hp0 += u.hp; else hp1 += u.hp; }
            p = hp0 > hp1; timedOut = true;
        }
        if (p) { msg = Boss ? "보스 격파!" : "승리!"; ShowBanner("[VICTORY!]", new Color(1, .85f, .2f), 1.5f); }
        else { life--; msg = "패배..."; ShowBanner("[DEFEAT...]", new Color(1, .35f, .35f), 1.5f); }
        if (p && round >= StageManager.Rounds) { msg = "전 스테이지 클리어!"; EndGame(true); return; } // 3-10 보스 처치
        if (timedOut) { msg = "시간 초과: " + msg; timedOut = false; }
        if (life <= 0) { EndGame(false); return; } // 최종 도달 = 방금 싸운 라운드
        CalculateRoundIncome(p); // 다음 라운드로 넘어가기 직전에 정산 (round는 방금 싸운 라운드)
        round++;
        AddXp(AutoXp);
        Prep();
    }

    static int Dist(int x1, int z1, int x2, int z2) => Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(z1 - z2));
    List<U> Foes(U u) => us.FindAll(e => e.Active && e.team != u.team);
    List<U> Friends(U u) => us.FindAll(e => e.Active && e.team == u.team);

    // 타겟: 기본은 가장 가까운 적(도발 유닛은 2칸 더 가깝게 취급). 저격 유닛은 사거리 안에서 체력이 가장 낮은 적.
    U Pick(U u)
    {
        U tgt = null; int best = 99;
        bool sn = (u.d.fx & Fx.Snipe) != 0;
        foreach (var e in us)
        {
            if (!e.Active || e.team == u.team) continue;
            int d = Dist(u.x, u.z, e.x, e.z);
            if (sn && d <= u.range) { if (best > 0 || e.hp < tgt.hp) { tgt = e; best = 0; } }
            else if (best > 0) { int m = d - ((e.d.fx & Fx.Taunt) != 0 ? 2 : 0); if (m < best) { tgt = e; best = m; } }
        }
        return tgt;
    }

    int BaseDmg(U u) => (int)(u.atk * AtkMul(u) * (u.buff > 0 ? 2 : 1));
    int Cap(U u) => (int)(u.max * HpMul(u));

    float AtkSpeed(U u)
    {
        float s = u.d.aspd;
        if ((u.d.fx & Fx.Berserk) != 0) s *= 2 - u.hp / (float)Cap(u); // 체력이 낮을수록 최대 2배
        return u.slow > 0 ? s * .5f : s;
    }

    void Attack(U u, U tgt)
    {
        var d = u.d;
        u.acc += AtkSpeed(u);
        for (int g = 0; u.acc >= 1 && tgt.Alive && g < 4; g++)
        {
            u.acc -= 1;
            Strike(u, tgt);
            if ((d.fx & Fx.Double) != 0 && tgt.Alive) Strike(u, tgt);
            if (d.mana > 0) u.mana++; // 공격할 때마다 마나 +1
        }
    }

    // 기본 공격: 근접은 즉시 타격, 사거리 2 이상 원거리는 투사체가 도착(다음 틱)했을 때 타격
    void Strike(U u, U tgt)
    {
        int dmg = BaseDmg(u);
        bool crit = (u.d.fx & Fx.Crit) != 0 && Random.value < .3f;
        if (crit) dmg *= 2;
        if (u.range > 1) Shoot(u, tgt, () => Hit(u, tgt, dmg, crit));
        else
        {
            u.kick = new Vector3(tgt.x - u.x, 0, tgt.z - u.z).normalized * .5f;
            Hit(u, tgt, dmg, crit);
        }
    }

    // 기본 공격이 실제로 맞았을 때의 처리(피해 + 흡혈·중독·관통·튕김·확산·골드 효과)
    void Hit(U u, U tgt, int dmg, bool crit)
    {
        var fx = u.d.fx;
        Dmg(tgt, dmg, false, false, crit ? 2 : 0);
        if ((fx & Fx.Lifesteal) != 0) Restore(u, dmg * 3 / 10);
        if ((fx & Fx.Poison) != 0) { tgt.poison = 4; tgt.pdmg = Mathf.Max(1, dmg / 3); }
        if ((fx & Fx.Gold) != 0 && u.team == 0 && Random.value < .2f) gold++;
        if ((fx & Fx.Pierce) != 0) // 대상 바로 뒤(공격자보다 먼) 적 1명 추가 피해
            foreach (var e in Foes(u)) if (e != tgt && Dist(tgt.x, tgt.z, e.x, e.z) == 1 && Dist(u.x, u.z, e.x, e.z) > Dist(u.x, u.z, tgt.x, tgt.z)) { Dmg(e, dmg * 6 / 10, false, false, 0); break; }
        if ((fx & Fx.Bounce) != 0) // 가까운 적 2명에게 튕김
        {
            var seen = new List<U> { tgt }; var cur = tgt;
            for (int b = 0; b < 2; b++)
            {
                U nx = null; int bd = 4;
                foreach (var e in Foes(u)) { int dd = Dist(cur.x, cur.z, e.x, e.z); if (!seen.Contains(e) && dd < bd) { bd = dd; nx = e; } }
                if (nx == null) break;
                Dmg(nx, dmg * 6 / 10, false, false, 0); Beam(WP(cur), WP(nx), C(1, .6f, .8f), .07f, .25f); seen.Add(nx); cur = nx;
            }
        }
        if (u.buff > 0) foreach (var e in Foes(u)) if (e != tgt && Dist(tgt.x, tgt.z, e.x, e.z) <= 1) Dmg(e, dmg / 2, false, false, 0); // 드래곤 변신: 주변 확산
    }

    // 피해 처리: 무적/방어 → 보호막 → 체력. 사망 시 자폭 처리. type: 0=일반 공격(흰색), 1=스킬·상태이상(주황), 2=치명타(큼직한 빨강)
    void Dmg(U t, int dmg, bool pure = false, bool quiet = false, int type = 1)
    {
        if (!t.Active || t.invuln > 0) return;
        var fx = t.d.fx;
        if ((fx & Fx.Armor) != 0) dmg = dmg * 7 / 10;
        foreach (var a in us) if (a.Active && a.team == t.team && (a.d.fx & Fx.Aura) != 0 && Dist(a.x, a.z, t.x, t.z) <= 2) { dmg = dmg * 85 / 100; break; }
        if (dmg > 0) AddText(t, dmg.ToString(), type == 2 ? new Color(1, .15f, .15f) : type == 1 ? new Color(1, .55f, 0) : Color.white, type == 2 ? 26 : 16);
        if (!pure) { int a = Mathf.Min(t.shield, dmg); t.shield -= a; dmg -= a; }
        t.hp -= dmg;
        if (!quiet) t.flash = .15f;
        if (t.Alive) return;
        t.go.SetActive(false);
        if ((fx & Fx.Explode) != 0) // 태엽 쥐: 사망 시 주변 적에게 폭발
        {
            Ring(WP(t), 1.6f, C(1, .6f, .1f), .4f);
            foreach (var e in Foes(t)) if (Dist(t.x, t.z, e.x, e.z) <= 1) Dmg(e, t.atk * 3);
        }
    }

    // 회복: 실제로 회복된 양을 초록 숫자로 표시
    void Restore(U u, int n)
    {
        int before = u.hp;
        u.hp = Mathf.Min(Cap(u), u.hp + n);
        u.flash = .15f;
        if (u.hp > before) AddText(u, "+" + (u.hp - before), new Color(.3f, 1, .3f), 18);
    }

    void StepToward(U u, int tx, int tz)
    {
        int bx = u.x, bz = u.z;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                int nx = u.x + dx, nz = u.z + dz;
                if (nx < 0 || nz < 0 || nx >= N || nz >= N || At(nx, nz) != null) continue;
                if (Dist(nx, nz, tx, tz) < Dist(bx, bz, tx, tz)) { bx = nx; bz = nz; }
            }
        u.x = bx; u.z = bz;
    }

    // 마나 완충 시 액티브 스킬. 대상이 없으면 false(마나 유지).
    bool Cast(U u)
    {
        var tgt = Pick(u);
        if (tgt == null && u.d.sk != Sk.Heal && u.d.sk != Sk.HealAll && u.d.sk != Sk.Ward && u.d.sk != Sk.Invuln) return false;
        var foes = Foes(u);
        int dmgBase = BaseDmg(u);
        switch (u.d.sk)
        {
            case Sk.Fireball: Shoot(u, tgt, () => { Dmg(tgt, dmgBase * 2); Ring(WP(tgt), .9f, C(1, .5f, 0), .35f); }); break;
            case Sk.Heal: { var low = LowestAlly(u); if (low == null) return false; Restore(low, dmgBase * 4); break; }
            case Sk.Stun: tgt.stun = Mathf.Max(tgt.stun, 2); tgt.flash = .15f; break;
            case Sk.Slow: foreach (var e in foes) if (Dist(tgt.x, tgt.z, e.x, e.z) <= 2) { e.slow = 4; e.flash = .15f; } break;
            case Sk.Summon: return Summon(u);
            case Sk.Whirl: foreach (var e in foes) if (Dist(u.x, u.z, e.x, e.z) <= 1) Dmg(e, dmgBase * 3 / 2); break;
            case Sk.Dragon: u.buff = 6; Restore(u, u.max / 5); break;
            case Sk.Blast: foreach (var e in foes) if (Dist(tgt.x, tgt.z, e.x, e.z) <= 2) Dmg(e, dmgBase * 2); break;
            case Sk.Zone: AddZone(u, tgt.x, tgt.z, 1, 5, dmgBase * 8 / 10, false, false, new Color(.5f, 1, .1f)); break;
            case Sk.Slam: foreach (var e in foes) if (Dist(tgt.x, tgt.z, e.x, e.z) <= 1) { Dmg(e, dmgBase * 12 / 10); e.stun = Mathf.Max(e.stun, 2); } break;
            case Sk.Column: foreach (var e in foes) if (e.x == tgt.x) { Dmg(e, dmgBase * 3 / 2); e.stun = Mathf.Max(e.stun, 2); } break;
            case Sk.Invuln: u.invuln = 3; u.flash = .15f; break;
            case Sk.HealAll: foreach (var a in Friends(u)) Restore(a, dmgBase * 3); break;
            case Sk.TimeStop: foreach (var e in foes) if (Dist(tgt.x, tgt.z, e.x, e.z) <= 2) { e.stun = Mathf.Max(e.stun, 3); e.flash = .15f; } break;
            case Sk.Petrify: foreach (var e in foes) if (Dist(u.x, u.z, e.x, e.z) <= 3) { e.stun = Mathf.Max(e.stun, 3); e.flash = .15f; } break;
            case Sk.Silence: tgt.silence = 4; tgt.poison = 4; tgt.pdmg = Mathf.Max(1, dmgBase * 8 / 10); Dmg(tgt, dmgBase); break;
            case Sk.Bomb: AddZone(u, tgt.x, tgt.z, 2, 3, dmgBase * 6, true, false, new Color(1, .3f, 0)); break;
            case Sk.Ward: foreach (var a in Friends(u)) { a.invuln = 2; a.shield += a.max / 5; a.flash = .15f; } break;
            case Sk.Drain: { int sum = 0; foreach (var e in foes) { int before = e.hp; Dmg(e, dmgBase * 12 / 10); sum += Mathf.Max(0, before - Mathf.Max(0, e.hp)); } Restore(u, sum / 2); break; }
            case Sk.Chain:
            {
                var seen = new List<U> { tgt }; var cur = tgt; float m = 2f;
                var spark = C(1, 1, .2f);
                Dmg(tgt, (int)(dmgBase * m));
                Beam(WP(u) + Vector3.up * .3f, WP(tgt), spark, .12f, .35f);
                for (int h = 0; h < 3; h++)
                {
                    U nx = null; int bd = 99;
                    foreach (var e in foes) { int dd = Dist(cur.x, cur.z, e.x, e.z); if (e.Active && !seen.Contains(e) && dd < bd) { bd = dd; nx = e; } }
                    if (nx == null) break;
                    m *= .8f; Dmg(nx, (int)(dmgBase * m)); Beam(WP(cur), WP(nx), spark, .12f, .35f); seen.Add(nx); cur = nx;
                }
                break;
            }
            case Sk.Tsunami: foreach (var e in foes) { Dmg(e, dmgBase); e.stun = Mathf.Max(e.stun, 2); } break;
            case Sk.BlackHole:
            {
                int cx = 0, cz = 0; foreach (var e in foes) { cx += e.x; cz += e.z; }
                int bx = cx / Mathf.Max(1, foes.Count), bz = cz / Mathf.Max(1, foes.Count);
                AddZone(u, bx, bz, 3, 4, dmgBase * 7 / 10, false, true, new Color(.1f, 0, .2f));
                Ring(new Vector3(bx, 0, bz), 3.5f, C(.4f, 0, .7f), .6f);
                break;
            }
            case Sk.Bombard: foreach (var e in foes) Dmg(e, dmgBase * 2); break;
            case Sk.Smite: foreach (var e in foes) Dmg(e, dmgBase * 3); break;
        }
        CastFx(u, tgt, foes);
        return true;
    }

    U LowestAlly(U p)
    {
        U low = null; float r = 1;
        foreach (var a in Friends(p)) { float q = a.hp / (float)Cap(a); if (q < r) { r = q; low = a; } }
        return low;
    }

    // 소환: 가장 가까운 적을 향한 빈 칸에 임시 유령(어그로용). 팀당 최대 3마리.
    bool Summon(U s)
    {
        if (us.FindAll(a => a.Alive && a.temp && a.team == s.team).Count >= 3) return false;
        var tgt = Pick(s);
        if (tgt == null) return false;
        int bx = -1, bz = -1, bd = 99;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                int nx = s.x + dx, nz = s.z + dz;
                if (nx < 0 || nz < 0 || nx >= N || nz >= N || At(nx, nz) != null) continue;
                int d = Dist(nx, nz, tgt.x, tgt.z);
                if (d < bd) { bd = d; bx = nx; bz = nz; }
            }
        if (bx < 0) return false;
        Spawn(s.team, (int)UnitId.Ghost, bx, bz, (float)s.max / s.d.hp).temp = true;
        Ring(new Vector3(bx, 0, bz), 1f, C(.5f, 1, .6f)); // 소환 이펙트
        return true;
    }

    // 장판(독 장판·블랙홀) / 시한폭탄: 바닥에 납작한 원판으로 표시
    void AddZone(U u, int x, int z, int r, int ticks, int dmg, bool bomb, bool pull, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material = Mat(col);
        go.transform.position = new Vector3(x, .03f, z);
        go.transform.localScale = new Vector3(r * 2 + 1, .02f, r * 2 + 1);
        zs.Add(new Zone { x = x, z = z, r = r, ticks = ticks, team = u.team, dmg = dmg, bomb = bomb, pull = pull, go = go });
    }

    // 침투(암살자 계열): 전투 시작 시 상대 진영 가장 뒤쪽 유닛 바로 뒤 빈 칸으로 순간이동.
    // 적 암살자는 플레이어 진영의 맨 뒤(z가 가장 작은 줄) 유닛, 내 암살자는 적의 맨 뒤(z가 가장 큰 줄) 유닛을 노린다.
    void Teleport(U a)
    {
        U far = null; int fs = int.MinValue;
        foreach (var e in us)
            if (e.Active && !e.temp && e.team != a.team)
            {
                int s = a.team == 0 ? e.z : -e.z;
                if (s > fs || (s == fs && Dist(a.x, a.z, e.x, e.z) < Dist(a.x, a.z, far.x, far.z))) { fs = s; far = e; } // 같은 줄이면 가까운 쪽
            }
        if (far == null) return;
        for (int dz = 1; dz >= -1; dz--)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = far.x + dx, nz = far.z + (a.team == 0 ? dz : -dz); // 적 뒤쪽 칸부터 시도
                if (nx < 0 || nz < 0 || nx >= N || nz >= N || At(nx, nz) != null) continue;
                Ring(WP(a), .8f, C(.6f, .2f, .9f), .5f); // 출발지
                a.x = nx; a.z = nz; Place(a);
                Ring(WP(a), .8f, C(.6f, .2f, .9f), .5f); // 도착지
                return;
            }
    }

    // ---------------- UI ----------------
    // 상단: 상태줄+난이도 / 하단: 경험치·전투 시작·상점 열기 / 팝업: 상점 / 오른쪽: 유닛 정보 / 왼쪽: 시너지
    void OnGUI()
    {
        DrawUI();
        DrawFloatTexts(); // 패널 위에 덮어 그림
        DrawBanner();
    }

    void DrawUI()
    {
        float W = Screen.width, H = Screen.height;
        center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        card ??= new GUIStyle(GUI.skin.button) { wordWrap = true, fontSize = 11 };
        wrap ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
        ui.Clear();

        ui.Add(new Rect(0, 0, W, 34));
        GUI.Label(new Rect(10, 4, W - 130, 20), $"[라운드 {StageManager.Label(round)} | 골드 {gold} | 목숨 {life} | 레벨 {level} ({(level < MaxLv ? $"{xp}/{Need[level - 1]}" : "MAX")})]");
        smallStyle ??= new GUIStyle(GUI.skin.label);
        smallStyle.alignment = TextAnchor.MiddleLeft; smallStyle.fontSize = 10; smallStyle.wordWrap = false; smallStyle.normal.textColor = new Color(.9f, .9f, .6f);
        bool goldIcon = AutoChessVisualManager.I.DrawIcon(new Rect(10, 22, 13, 13), "UI_Icon_Gold"); // 골드 아이콘(Resources/Visuals/UI/UI_Icon_Gold)이 있으면 표시
        GUI.Label(new Rect(goldIcon ? 26 : 10, 21, 400, 14), $"+{Interest(gold)}G 이자 중 · 연승 {winStreak}{(StreakBonus(winStreak) > 0 ? $" (+{StreakBonus(winStreak)}G)" : "")}", smallStyle); // 골드 옆 이자 단계 표시
        GUI.enabled = !battle && !over;
        for (int i = 0; i < 3; i++)
            if (GUI.Toggle(new Rect(W - 100, 4 + i * 22, 90, 22), diff == i, DiffName[i]) && diff != i) { diff = i; SpawnEnemies(); }
        GUI.enabled = true;
        ui.Add(new Rect(W - 110, 0, 110, 72));

        DrawUnitLabels();

        if (over) // 결과 팝업: 화면을 어둡게 덮고 다른 조작을 막는다
        {
            ui.Add(new Rect(0, 0, W, H));
            GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
            var r = new Rect(W / 2 - 220, H / 2 - 65, 440, 130);
            GUI.Box(r, "");
            titleStyle ??= new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter; titleStyle.fontStyle = FontStyle.Bold; titleStyle.fontSize = 18; titleStyle.wordWrap = false;
            titleStyle.normal.textColor = cleared ? new Color(1, .85f, .2f) : new Color(1, .4f, .4f);
            GUI.Label(new Rect(r.x, r.y + 18, r.width, 40), cleared ? "[VICTORY - 톡톡이의 시험 클리어!]" : $"[GAME OVER - 최종 도달: Stage {StageManager.Label(finalRound)}]", titleStyle);
            if (GUI.Button(new Rect(r.x + r.width / 2 - 70, r.y + 78, 140, 36), "[다시 하기]")) Reset();
            return;
        }

        // 하단 바
        ui.Add(new Rect(0, H - 52, W, 52));
        GUI.Label(new Rect(W / 2 - 250, H - 76, 500, 22), (Boss ? "[보스 라운드]  " : "") + msg, center);
        if (!battle && incomeLog != "") // 라운드 정산 내역(다음 전투 시작 전까지 표시)
        {
            smallStyle.alignment = TextAnchor.MiddleCenter; smallStyle.fontSize = 12; smallStyle.normal.textColor = new Color(1, .95f, .55f);
            GUI.Label(new Rect(W / 2 - 330, H - 98, 660, 20), incomeLog, smallStyle);
        }
        GUI.enabled = !battle && level < MaxLv;
        if (GUI.Button(new Rect(10, H - 44, 130, 36), "경험치 (4G)")) BuyXp();
        GUI.enabled = !battle;
        if (GUI.Button(new Rect(W / 2 - 70, H - 44, 140, 36), "전투 시작")) StartBattle();
        if (GUI.Button(new Rect(W - 140, H - 44, 130, 36), shopOpen ? "상점 닫기" : "상점 열기")) shopOpen = !shopOpen;
        GUI.enabled = true;

        if (shopOpen && !battle) DrawShop(W); // 상점이 열려 있으면 옆 패널은 숨김(겹침 방지)
        else { DrawSynergy(); DrawInfo(W); }
        HandleMouse();
    }

    static string Tag(int tag) => tag < RaceN ? RaceName[tag] : JobName[tag - RaceN];
    static string TagsOf(UD d) => d.race < 0 ? "-" : RaceName[d.race] + " / " + JobName[d.job];

    void DrawShop(float W)
    {
        float pw = Mathf.Min(790, W - 20), cw = (pw - 20 - 4 * 6) / 5;
        var rs = new Rect((W - pw) / 2, 78, pw, 176); ui.Add(rs); // 난이도 버튼(오른쪽 위)을 가리지 않도록 아래에 배치
        GUI.Box(rs, "상점");
        smallStyle ??= new GUIStyle(GUI.skin.label);
        smallStyle.alignment = TextAnchor.MiddleRight; smallStyle.fontSize = 11; smallStyle.wordWrap = false; smallStyle.normal.textColor = new Color(.85f, .85f, .85f);
        var od = Odds[level - 1]; // 현재 레벨의 1~5골드 유닛 등장 확률
        GUI.Label(new Rect(rs.x + rs.width - 410, rs.y + 3, 400, 18), $"Lv.{level} 등장 확률  1골 {od[0]}% · 2골 {od[1]}% · 3골 {od[2]}% · 4골 {od[3]}% · 5골 {od[4]}%", smallStyle);
        for (int i = 0; i < shop.Length; i++)
        {
            var rc = new Rect(rs.x + 10 + i * (cw + 6), rs.y + 24, cw, 110);
            int k = shop[i];
            if (k < 0) GUI.Box(rc, "품절");
            else if (GUI.Button(rc, $"{Dt(k).name}  ({Dt(k).cost}G)\n{TagsOf(Dt(k))}\n{Dt(k).desc}", card) && Buy(k)) shop[i] = -1;
        }
        if (GUI.Button(new Rect(rs.x + rs.width / 2 - 60, rs.y + 140, 120, 30), "리롤 (1G)"))
        {
            if (gold < 1) msg = "골드 부족";
            else { gold--; msg = ""; Roll(); }
        }
    }

    // 왼쪽 시너지 패널: 필드에 배치된 유닛이 있을 때만 표시.
    // 위쪽에는 활성화된 시너지(2종 이상, 많이 모은 순)를 `n/다음 단계 +보너스` 로, 그 아래에는 활성화에 가까운 시너지(1종 배치, 회색 `1/2`)를 최대 4개까지.
    // 아무것도 배치되지 않은 항목은 숨긴다.
    void DrawSynergy()
    {
        if (FieldCount() == 0) return;
        var act = new List<(int tag, int n)>(); var near = new List<(int tag, int n)>();
        for (int tag = 0; tag < Tags; tag++)
        {
            int n = Cnt(0, tag);
            if (n >= 2) act.Add((tag, n)); else if (n == 1) near.Add((tag, n));
        }
        act.Sort((a, b) => { int c = b.n.CompareTo(a.n); return c != 0 ? c : a.tag.CompareTo(b.tag); });
        int max = Mathf.Max(3, (int)((Screen.height - 150) / 22f)); // 화면을 넘지 않도록 줄 수 제한
        if (act.Count > max) act.RemoveRange(max, act.Count - max);
        int nearShow = Mathf.Min(near.Count, 4, max - act.Count);
        int rows = act.Count + Mathf.Max(0, nearShow);
        if (rows == 0) return;
        var r = new Rect(10, 44, 170, 34 + 22 * rows); ui.Add(r);
        GUI.Box(r, "시너지");
        int y = 0;
        foreach (var (tag, n) in act)
        {
            string next = n >= 6 ? "MAX" : (n >= 4 ? 6 : 4).ToString(); // 다음 단계(2→4→6)까지 필요한 수
            float tx = SynBadge(r.x + 8, r.y + 27 + y * 22, tag);
            GUI.Label(new Rect(tx, r.y + 26 + y++ * 22, 175 - (tx - r.x), 22), $"{Tag(tag)} {n}/{next}  +{Bon(0, tag) * 100:0}%");
        }
        GUI.color = new Color(1, 1, 1, .5f); // 활성화에 가까운 항목은 흐리게
        for (int i = 0; i < nearShow; i++)
        {
            float tx = SynBadge(r.x + 8, r.y + 27 + y * 22, near[i].tag);
            GUI.Label(new Rect(tx, r.y + 26 + y++ * 22, 175 - (tx - r.x), 22), $"{Tag(near[i].tag)} {near[i].n}/2");
        }
        GUI.color = Color.white;
    }

    // 시너지 배지(Resources/Visuals/UI/UI_Badge_<Human|Beast|...|Knight|Hunter|...>)가 있으면 그리고 글자 시작 x를 돌려준다
    float SynBadge(float x, float y, int tag)
    {
        string en = tag < RaceN ? ((Race)tag).ToString() : ((Job)(tag - RaceN)).ToString();
        return AutoChessVisualManager.I.DrawIcon(new Rect(x, y, 18, 18), "UI_Badge_" + en) ? x + 22 : x + 2;
    }

    // 오른쪽: 선택한 유닛 정보 + 판매
    void DrawInfo(float W)
    {
        if (sel == null || !us.Contains(sel)) { sel = null; return; }
        var r = new Rect(W - 240, 80, 230, 280); ui.Add(r);
        GUI.Box(r, "유닛 정보");
        int cap = battle ? Cap(sel) : sel.max, atk = battle ? BaseDmg(sel) : sel.atk;
        GUI.Label(new Rect(r.x + 10, r.y + 26, 210, 22), $"{sel.d.name}  {new string('★', sel.star)}{(sel.team == 1 ? "  (적)" : "")}");
        GUI.Label(new Rect(r.x + 10, r.y + 48, 210, 22), $"{TagsOf(sel.d)}  ({sel.d.cost}G)");
        GUI.Label(new Rect(r.x + 10, r.y + 72, 210, 22), $"체력  {sel.hp} / {cap}");
        GUI.Label(new Rect(r.x + 10, r.y + 94, 210, 22), $"공격력  {atk}    사거리  {(sel.range > 20 ? "전체" : sel.range.ToString())}");
        GUI.Label(new Rect(r.x + 10, r.y + 120, 210, 80), "스킬: " + sel.d.desc + (sel.d.mana > 0 ? $"  (마나 {sel.d.mana})" : ""), wrap);
        if (sel.team == 0 && !sel.temp && GUI.Button(new Rect(r.x + 10, r.y + r.height - 44, 210, 34), $"유닛 판매 (+{Price(sel)}G)")) Sell(sel); // 전투 중에도 판매 가능
    }

    // 유닛 위 라벨: 대기석은 이름(3글자)+성급, 필드는 성급+체력(+보호막/상태)과 체력바/마나바
    void DrawUnitLabels()
    {
        foreach (var u in us)
        {
            if (!u.Alive) continue;
            var s = Camera.main.WorldToScreenPoint(WP(u));
            float sy = Screen.height - s.y;
            GUI.color = u == sel ? Color.yellow : Color.white;
            if (u.bench)
            {
                string nm = u.d.name.Length > 3 ? u.d.name.Substring(0, 3) : u.d.name;
                GUI.Label(new Rect(s.x - 32, sy - 30 - (u.x % 2) * 16, 64, 20), nm + new string('★', u.star), center); // 홀짝 높이를 달리해 이름표 겹침 방지
                continue;
            }
            string st = (u.stun > 0 ? " 기절" : "") + (u.silence > 0 ? " 침묵" : "") + (u.poison > 0 ? " 독" : "") + (u.invuln > 0 ? " 무적" : "");
            GUI.Label(new Rect(s.x - 30, sy - 30, 150, 20), new string('★', u.star) + " " + u.hp + (u.shield > 0 ? " +" + u.shield : "") + st);
            float r = Mathf.Clamp01(u.hp / (float)(battle ? Cap(u) : u.max));
            var bar = new Rect(s.x - 20, sy - 8, 50, 6);
            GUI.color = Color.black; GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = u.team == 0 ? Color.green : Color.red;
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * r, bar.height), Texture2D.whiteTexture);
            if (battle && u.d.mana > 0) // 마나바: 체력바 바로 밑의 작은 파란 막대. 가득 차면 스킬 발동
            {
                var mbar = new Rect(bar.x, bar.y + 7, bar.width, 4);
                GUI.color = new Color(0, 0, 0, .8f); GUI.DrawTexture(mbar, Texture2D.whiteTexture); // 빈 상태도 보이도록 배경
                GUI.color = new Color(.25f, .55f, 1f);
                GUI.DrawTexture(new Rect(mbar.x, mbar.y, mbar.width * Mathf.Clamp01((float)u.mana / u.d.mana), mbar.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
        GUI.color = Color.white;
    }

    // 클릭: 유닛 선택(정보창) / 선택 후 빈 칸 클릭으로 이동 / 드래그로 이동·교환
    void HandleMouse()
    {
        var ev = Event.current;
        if (ev.button != 0) return;
        if (ev.type == EventType.MouseDown && !OverUI(ev.mousePosition))
        {
            if (!Cell(ev.mousePosition, out int x, out int z, out bool bn)) { sel = null; return; }
            var hit = UnitAt(x, z, bn);
            if (hit != null) { sel = hit; dragU = !battle && hit.team == 0 && !hit.temp ? hit : null; }
            else if (sel != null && sel.team == 0 && !battle) MoveUnit(sel, x, z, bn);
            else sel = null;
        }
        else if (ev.type == EventType.MouseUp && dragU != null)
        {
            var d = dragU; dragU = null;
            if (!battle && !OverUI(ev.mousePosition) && Cell(ev.mousePosition, out int x, out int z, out bool bn) && (x != d.x || z != d.z || bn != d.bench))
                MoveUnit(d, x, z, bn);
        }
    }

    bool OverUI(Vector2 m) { foreach (var r in ui) if (r.Contains(m)) return true; return false; }

    // 화면 좌표 → 칸. 대기석(z≈-1.6) 줄과 필드(0..7) 구분.
    bool Cell(Vector2 m, out int x, out int z, out bool bench)
    {
        x = z = 0; bench = false;
        var ray = Camera.main.ScreenPointToRay(new Vector3(m.x, Screen.height - m.y));
        if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) return false;
        var p = ray.GetPoint(d);
        x = Mathf.RoundToInt(p.x);
        if (x < 0 || x >= N) return false;
        if (p.z < -1.05f && p.z > BenchZ - .6f) { bench = true; z = -1; return true; }
        z = Mathf.RoundToInt(p.z);
        return z >= 0 && z < N;
    }

    void SetPos(U u, int x, int z, bool bench)
    {
        u.bench = bench; u.x = u.sx = x; u.z = u.sz = z;
        Place(u);
    }

    // 대기석↔필드 이동. 빈 칸이면 이동, 내 유닛이 있으면 교환. 필드는 내 진영(아래 4줄)만, 출전 수는 레벨 이하.
    bool MoveUnit(U u, int x, int z, bool toBench)
    {
        if (battle) { msg = "전투 중에는 유닛을 옮길 수 없습니다"; return false; } // 배치 조작은 준비 단계에서만
        if (!toBench && z >= N / 2) return false;
        var o = UnitAt(x, z, toBench);
        if (o == u || (o != null && (o.team != 0 || o.temp))) return false;
        if (o == null)
        {
            if (!toBench && u.bench && FieldCount() >= level) { msg = $"출전 한도 (레벨 {level})"; return false; }
            SetPos(u, x, z, toBench);
        }
        else
        {
            int ux = u.x, uz = u.z; bool ub = u.bench;
            SetPos(u, x, z, toBench);
            SetPos(o, ux, uz, ub);
        }
        msg = "";
        return true;
    }

    // 시너지: 필드에 배치된 "서로 다른 유닛" 수 기준 종족·직업별 2명 +15%, 4명 +30%, 6명 +50%. 적에게도 적용.
    // 태그별 적용 스탯 가중치(체력/공격력). 태그 0~13 = 종족, 14~23 = 직업.
    static readonly float[] TagHp = { 1, 0, .5f, 0, 1, 0, .5f, 1, 0, 0, 0, 1, 0, .5f, 1, 0, 0, 0, 1, 1, .5f, 1, 0, .5f };
    static readonly float[] TagAtk = { 0, 1, .5f, 1, 0, 1, .5f, 0, 1, 1, 1, 0, 1, .5f, 0, 1, 1, 1, 0, 0, .5f, 0, 1, .5f };
    float[,] bon = new float[2, Tags];
    int Cnt(int team, int tag)
    {
        var ids = new HashSet<int>();
        foreach (var u in us) if (!u.temp && !u.bench && u.team == team && Has(u.id, tag)) ids.Add(u.id);
        return ids.Count;
    }
    static bool Has(int id, int tag) { var d = Dt(id); return d.race >= 0 && (d.race == tag || RaceN + d.job == tag); }
    float Bon(int team, int tag)
    {
        int n = Cnt(team, tag);
        return n >= 6 ? .5f : n >= 4 ? .3f : n >= 2 ? .15f : 0;
    }
    float HpMul(U u) => u.d.race < 0 ? 1 : 1 + bon[u.team, u.d.race] * TagHp[u.d.race] + bon[u.team, RaceN + u.d.job] * TagHp[RaceN + u.d.job];
    float AtkMul(U u) => u.d.race < 0 ? 1 : 1 + bon[u.team, u.d.race] * TagAtk[u.d.race] + bon[u.team, RaceN + u.d.job] * TagAtk[RaceN + u.d.job];

    void StartBattle()
    {
        if (FieldCount() == 0) { msg = "필드에 유닛을 배치하세요"; return; }
        for (int tm = 0; tm < 2; tm++) for (int k = 0; k < Tags; k++) bon[tm, k] = Bon(tm, k);
        foreach (var u in us)
        {
            u.hp = (int)(u.max * HpMul(u)); u.shield = (int)(u.max * u.d.sh);
            u.mana = u.stun = u.slow = u.silence = u.poison = u.invuln = u.buff = 0; u.acc = 0;
        }
        foreach (var u in new List<U>(us)) if ((u.d.fx & Fx.Infiltrate) != 0 && u.Active) Teleport(u);
        tickNo = 0; msg = ""; incomeLog = ""; battle = true; shopOpen = false;
        ShowBanner($"[STAGE {StageManager.Label(round)}]", Boss ? new Color(1, .5f, .2f) : Color.white, 1.5f);
    }

    // 원거리 투사체(사거리 2 이상 공격·화염구): 구체가 타겟을 향해 날아가고, 다음 틱 시작(=도착)에 피해가 들어간다.
    // 비행 시간은 0.35초로 맞춰 틱 간격(0.4초) 안에 도착한다. 색은 유닛 대표색.
    class P { public GameObject go; public U tgt; public int hit; public float spd; public System.Action on; }
    List<P> ps = new List<P>();
    Material[] pm = new Material[Kinds + 1];

    void Shoot(U u, U tgt, System.Action onHit)
    {
        pm[u.id] ??= Mat(u.d.col, true);
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material = pm[u.id];
        go.transform.localScale = Vector3.one * .25f;
        go.transform.position = WP(u);
        ps.Add(new P { go = go, tgt = tgt, hit = tickNo + 1, spd = Vector3.Distance(go.transform.position, tgt.go.transform.position) / .35f, on = onHit });
    }

    // 다음 틱 시작: 도착한 투사체의 피해 적용 (타겟이 이미 죽었으면 소멸)
    void LandProjectiles()
    {
        for (int i = ps.Count - 1; i >= 0; i--)
        {
            var pr = ps[i];
            if (pr.hit > tickNo) continue;
            ps.RemoveAt(i);
            Destroy(pr.go);
            if (pr.tgt.Active) pr.on?.Invoke();
        }
    }

    // ================= 전투 연출: 플로팅 텍스트 & 중앙 알림 =================
    // 플로팅 텍스트: 유닛 머리 위(3D 위치)에서 위로 떠오르며 사라짐. 일반=흰색, 스킬=주황, 치명타=큼직한 빨강, 힐=초록.
    class FText { public Vector3 pos; public string text; public Color col; public int size; public float t0; }
    readonly List<FText> fts = new List<FText>();
    const float FTextLife = .9f;
    GUIStyle ftStyle, bannerStyle;
    string banner = ""; Color bannerCol; float bannerT0, bannerLife;

    void AddText(U u, string text, Color col, int size)
    {
        if (u.go == null) return; // 전투 중 판매돼 사라진 유닛
        if (fts.Count > 150) fts.RemoveAt(0);
        fts.Add(new FText { pos = WP(u) + new Vector3(Random.Range(-.25f, .25f), .9f, 0), text = text, col = col, size = size, t0 = Time.time });
    }

    // 상단 중앙의 작고 심플한 알림 (스테이지 시작·승리·패배 공통)
    void ShowBanner(string text, Color col, float life) { banner = text; bannerCol = col; bannerT0 = Time.unscaledTime; bannerLife = life; }

    void DrawFloatTexts()
    {
        if (Event.current.type != EventType.Repaint) return;
        ftStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        foreach (var f in fts)
        {
            float a = (Time.time - f.t0) / FTextLife;
            if (a < 0 || a >= 1) continue;
            var s = Camera.main.WorldToScreenPoint(f.pos + Vector3.up * a * 1.1f);
            if (s.z < 0) continue;
            ftStyle.fontSize = f.size;
            var r = new Rect(s.x - 60, Screen.height - s.y - 16, 120, 32);
            float alpha = 1 - a * a;
            GUI.color = new Color(0, 0, 0, alpha); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), f.text, ftStyle); // 그림자
            GUI.color = new Color(f.col.r, f.col.g, f.col.b, alpha); GUI.Label(r, f.text, ftStyle);
        }
        GUI.color = Color.white;
    }

    // 화면 중앙 큰 알림: 전투 시작 [STAGE X-X], 결과 [VICTORY!]/[DEFEAT...]. 페이드 인/아웃.
    void DrawBanner()
    {
        if (Event.current.type != EventType.Repaint || banner == "") return;
        float t = Time.unscaledTime - bannerT0;
        if (t >= bannerLife) { banner = ""; return; }
        float a = t < .15f ? t / .15f : t > bannerLife * .6f ? 1 - (t - bannerLife * .6f) / (bannerLife * .4f) : 1;
        bannerStyle ??= new GUIStyle(GUI.skin.label);
        bannerStyle.alignment = TextAnchor.MiddleCenter; bannerStyle.fontStyle = FontStyle.Bold; bannerStyle.wordWrap = false; bannerStyle.normal.textColor = Color.white;
        bannerStyle.fontSize = Mathf.Max(14, (int)(Screen.height * .04f));
        var r = new Rect(0, 34, Screen.width, 26);
        GUI.color = new Color(0, 0, 0, a * .7f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), banner, bannerStyle); // 얇은 그림자
        GUI.color = new Color(bannerCol.r, bannerCol.g, bannerCol.b, a); GUI.Label(r, banner, bannerStyle);
        GUI.color = Color.white;
    }

    // ================= 스킬 이펙트 =================
    // 도형(링·빔·번개)으로 40종 스킬의 범위·대상·효과를 보여준다. 이펙트는 짧게 커졌다/가늘어지며 사라지고, 같은 색은 재질을 공유한다.
    class V { public GameObject go; public float t0, life, w; public Vector3 s0, s1; public bool beam; }
    readonly List<V> vs = new List<V>();
    readonly Dictionary<Color, Material> vm = new Dictionary<Color, Material>();
    class Mk { public GameObject head, ground; } // 상태이상 표식: head=머리 위(기절·침묵·중독·감속), ground=발밑(무적·변신·보호막)
    readonly Dictionary<U, Mk> mks = new Dictionary<U, Mk>();

    Material VMat(Color c) { if (!vm.TryGetValue(c, out var m)) vm[c] = m = Mat(c, true); return m; }

    GameObject VObj(PrimitiveType p, Color c)
    {
        var go = GameObject.CreatePrimitive(p);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = VMat(c);
        return go;
    }

    void AddV(V v)
    {
        if (vs.Count >= 160) { Destroy(vs[0].go); vs.RemoveAt(0); }
        vs.Add(v);
    }

    // 바닥에서 반지름 r까지 퍼지는 원판 (광역 범위·충격파·소환 등)
    void Ring(Vector3 pos, float r, Color c, float life = .45f)
    {
        var go = VObj(PrimitiveType.Cylinder, c);
        go.transform.position = new Vector3(pos.x, .08f, pos.z);
        go.transform.localScale = new Vector3(.2f, .02f, .2f);
        AddV(new V { go = go, t0 = Time.time, life = life, s0 = go.transform.localScale, s1 = new Vector3(r * 2, .02f, r * 2) });
    }

    // 두 점을 잇는 선 (체인 라이트닝·흡수·침묵·튕김)
    void Beam(Vector3 a, Vector3 b, Color c, float w = .1f, float life = .3f)
    {
        float len = Vector3.Distance(a, b);
        if (len < .01f) return;
        var go = VObj(PrimitiveType.Cube, c);
        go.transform.position = (a + b) / 2;
        go.transform.rotation = Quaternion.LookRotation(b - a);
        go.transform.localScale = new Vector3(w, w, len);
        AddV(new V { go = go, t0 = Time.time, life = life, w = w, s1 = new Vector3(0, 0, len), beam = true });
    }

    void Bolt(Vector3 p, Color c) => Beam(p + Vector3.up * 4, p + Vector3.up * .3f, c, .12f, .3f); // 위에서 내리꽂는 번개 (카메라를 가리지 않게 낮고 가늘게)

    void UpdateVfx()
    {
        for (int i = vs.Count - 1; i >= 0; i--)
        {
            var v = vs[i];
            float a = (Time.time - v.t0) / v.life;
            if (a >= 1) { Destroy(v.go); vs.RemoveAt(i); continue; }
            v.go.transform.localScale = v.beam ? new Vector3(v.w * (1 - a), v.w * (1 - a), v.s1.z) : Vector3.Lerp(v.s0, v.s1, 1 - (1 - a) * (1 - a));
        }
    }

    // 유닛의 "몸 중심" 월드 위치(이펙트·텍스트·투사체 기준). 3D 유닛은 발 밑이 원점이라 0.5 올린다.
    static Vector3 WP(U u) => u.go.transform.position + (u.prefab ? Vector3.up * .5f : Vector3.zero);

    // 스킬 발동 시각 효과 (foes = 발동 직전의 적 목록)
    void CastFx(U u, U tgt, List<U> foes)
    {
        var p = WP(u);
        switch (u.d.sk)
        {
            case Sk.Heal: { var low = LowestAlly(u); if (low != null) Ring(WP(low), .7f, C(.3f, 1, .3f)); break; }
            case Sk.Stun: Ring(WP(tgt), .8f, C(1, .9f, .1f)); break;
            case Sk.Slow: Ring(WP(tgt), 2f, C(.3f, .8f, 1)); break;
            case Sk.Whirl: Ring(p, 1.2f, C(.3f, .5f, 1), .3f); break;
            case Sk.Dragon: Ring(p, 1.6f, C(1, .5f, 0), .6f); break;
            case Sk.Blast: Ring(WP(tgt), 2f, C(.3f, .6f, 1), .5f); break;
            case Sk.Zone: Ring(WP(tgt), 1f, C(.7f, 1, .1f)); break;
            case Sk.Slam: Ring(WP(tgt), 1.2f, C(1, .2f, .1f)); break;
            case Sk.Column: Beam(new Vector3(tgt.x, .5f, -1), new Vector3(tgt.x, .5f, N), C(1, .3f, .2f), .5f, .5f); break;
            case Sk.Invuln: Ring(p, 1f, Color.white); break;
            case Sk.HealAll: foreach (var a in Friends(u)) Ring(WP(a), .8f, C(.3f, 1, .3f)); break;
            case Sk.TimeStop: Ring(WP(tgt), 2f, C(.7f, .3f, 1), .6f); break;
            case Sk.Petrify: Ring(p, 3f, C(.1f, .9f, .5f), .6f); break;
            case Sk.Silence: Beam(p, WP(tgt), C(.7f, .2f, 1), .15f, .4f); break;
            case Sk.Bomb: Ring(WP(tgt), .8f, C(1, .4f, 0)); break;
            case Sk.Ward: foreach (var a in Friends(u)) Ring(WP(a), .9f, C(1, .85f, .3f), .6f); break;
            case Sk.Drain: foreach (var e in foes) Beam(WP(e), p, C(.8f, .1f, .3f), .1f, .4f); break;
            case Sk.Tsunami: Ring(p, 9f, C(.1f, .3f, .9f), .7f); break;
            case Sk.Bombard: foreach (var e in foes) Ring(WP(e), 1f, C(1, .5f, 0), .5f); break;
            case Sk.Smite: foreach (var e in foes) Bolt(WP(e), Color.white); break;
        }
    }

    // 상태이상 표식: 머리 위 구체(기절=노랑, 침묵=보라, 중독=초록, 감속=하늘) / 발밑 원판(무적=흰색, 변신=주황, 보호막=파랑)
    void Mark(ref GameObject go, Color? c, PrimitiveType prim, Vector3 scale, Vector3 pos)
    {
        if (c == null) { if (go != null) go.SetActive(false); return; }
        if (go == null) go = VObj(prim, c.Value);
        go.SetActive(true);
        go.GetComponent<Renderer>().sharedMaterial = VMat(c.Value);
        go.transform.position = pos;
        go.transform.localScale = scale;
    }

    void UpdateMarks()
    {
        foreach (var u in new List<U>(mks.Keys))
            if (!us.Contains(u)) { Destroy(mks[u].head); Destroy(mks[u].ground); mks.Remove(u); }
        foreach (var u in us)
        {
            Color? hc = null, gc = null;
            if (u.Active)
            {
                hc = u.stun > 0 ? C(1, .9f, .1f) : u.silence > 0 ? C(.7f, .2f, 1) : u.poison > 0 ? C(.4f, 1, .2f) : u.slow > 0 ? C(.3f, .8f, 1) : (Color?)null;
                gc = u.invuln > 0 ? Color.white : u.buff > 0 ? C(1, .5f, 0) : u.shield > 0 ? C(.4f, .6f, 1) : (Color?)null;
            }
            if (hc == null && gc == null && !mks.ContainsKey(u)) continue;
            if (!mks.TryGetValue(u, out var m)) mks[u] = m = new Mk();
            var p = u.go.transform.position; // 원점(기본 도형은 몸 중심, 3D 유닛은 발 밑)
            float mult = (1 + .2f * (u.star - 1)) * (u.boss ? 1.8f : 1);
            float top = u.prefab ? u.top * mult : .55f + .5f * u.d.sz.y * mult;
            Mark(ref m.head, hc, PrimitiveType.Sphere, Vector3.one * .22f, new Vector3(p.x, p.y + top, p.z));
            Mark(ref m.ground, gc, PrimitiveType.Cylinder, new Vector3(.9f, .02f, .9f), new Vector3(p.x, .05f, p.z));
            if (m.head != null && m.head.activeSelf) m.head.transform.Rotate(0, 360 * Time.deltaTime, 0);
        }
    }

    // 레벨: 출전 유닛 한도 = 레벨. 라운드 종료 시 +2XP, 4G로 +4XP 구매.
    static readonly int[] Need = { 2, 2, 6, 10, 20, 36, 56, 68, 68 }; // Need[레벨-1] = 다음 레벨까지 필요 XP (톡톡이의 시험 표)
    const int MaxLv = 10;
    int level = 1, xp;

    void AddXp(int n)
    {
        if (level >= MaxLv) return;
        xp += n;
        while (level < MaxLv && xp >= Need[level - 1]) { xp -= Need[level - 1]; level++; }
        if (level >= MaxLv) xp = 0;
    }

    void BuyXp()
    {
        if (gold < 4) { msg = "골드 부족"; return; }
        gold -= 4; msg = ""; AddXp(4);
    }

    // 상점: 플레이어 레벨의 확률표로 코스트를 뽑고, 그 코스트의 유닛 중 무작위 선택
    int RollOne()
    {
        var w = Odds[level - 1];
        int r = Random.Range(0, 100), c = 0;
        while (c < 4 && r >= w[c]) { r -= w[c]; c++; }
        var pool = ByCost[c + 1];
        return pool[Random.Range(0, pool.Count)];
    }

    void Roll() { for (int i = 0; i < shop.Length; i++) shop[i] = RollOne(); }

    // 구매한 유닛은 대기석 첫 빈칸에 생성. 대기석이 가득 차 있어도 같은 1성 유닛이 이미 2마리 있어 즉시 합성되면 구매할 수 있다
    // (구매 → 합성 순서로 처리해 대기석 칸이 비워진다). 합성이 안 되고 가득 찼으면 구매 불가.
    bool Buy(int id)
    {
        if (gold < Dt(id).cost) { msg = "골드 부족"; return false; }
        int slot = -1;
        for (int i = 0; i < N && slot < 0; i++) if (UnitAt(i, -1, true) == null) slot = i;
        bool merges = us.FindAll(u => u.team == 0 && u.id == id && u.star == 1).Count >= 2; // 사면 바로 3개가 모여 합성됨
        if (slot < 0 && !merges) { msg = "대기석이 가득 찼습니다"; return false; }
        gold -= Dt(id).cost;
        var nu = Spawn(0, id, slot < 0 ? N : slot, -1, 1, true); // 가득 찼으면 임시로 대기석 밖(x=8)에 생성
        msg = ""; Merge();
        if (us.Contains(nu) && nu.x >= N) // 합성 후 살아남은 임시 유닛은 비워진 대기석 칸으로 이동
        {
            int free = -1;
            for (int i = 0; i < N && free < 0; i++) if (UnitAt(i, -1, true) == null) free = i;
            if (free >= 0) SetPos(nu, free, -1, true);
            else { gold += Dt(id).cost; Destroy(nu.go); us.Remove(nu); msg = "대기석이 가득 찼습니다"; return false; } // 안전장치: 합성이 안 됐으면 환불
        }
        return true;
    }

    // 판매 환급: 합성에 들어간 원가(코스트 x 3^(성급-1)) 대비 1성 100%, 2성 75%, 3성 70% (반올림)
    // 예) 3골드 유닛: 1성 3G, 2성 7G(9G의 75%), 3성 19G(27G의 70%)
    int Price(U u)
    {
        float rate = u.star == 1 ? 1f : u.star == 2 ? .75f : .7f;
        return Mathf.Max(1, Mathf.FloorToInt(u.d.cost * Mathf.Pow(3, u.star - 1) * rate + .5f));
    }

    // 판매는 전투 중에도 가능. 전투 중이면 hp=0으로 만들어 날아가는 투사체·스킬이 사라진 유닛을 건드리지 않게 한다.
    void Sell(U u)
    {
        gold += Price(u);
        u.hp = 0;
        Destroy(u.go); us.Remove(u); sel = null; msg = "";
    }

    // 같은 유닛·같은 별 3개(필드+대기석) → 별 +1 (스탯 x1.8), 최대 3성. 필드 유닛을 우선 남긴다.
    void Merge()
    {
        for (int id = 0; id < Kinds; id++)
            for (int s = 1; s < 3; s++)
            {
                var g = us.FindAll(u => u.team == 0 && u.id == id && u.star == s);
                if (g.Count < 3) continue;
                g.Sort((a, b) => a.bench.CompareTo(b.bench));
                var m = g[0];
                for (int i = 1; i < 3; i++) { if (sel == g[i]) sel = m; Destroy(g[i].go); us.Remove(g[i]); }
                m.star++; m.max = m.hp = (int)(m.max * 1.8f); m.atk = (int)(m.atk * 1.8f);
                msg = $"{m.d.name} {m.star}성 합성!";
                id--; break; // 연쇄 합성 재검사
            }
    }
}
