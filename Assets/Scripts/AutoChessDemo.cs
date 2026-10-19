using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 자동 시연 모드: 게임이 스스로 상점 구매 → 배치 → 전투 → 보스전을 보여 주고 카메라가 움직인다. 트레일러·발표 영상용.
//   실행:  AutoChess.exe -demo                        (그냥 시연 재생)
//          AutoChess.exe -demo -capture <폴더> [-fps 30] [-theme 0~4]   (프레임을 JPG 로 저장하고 끝나면 자동 종료)
//   프레임 → MP4 변환은 Tools/make_trailer.py (Blender 사용). 설명은 Tools/README_트레일러.md
// 시연용이라 구매한 유닛을 조금 강화하고 보스전 라인업을 미리 만들어 둔다(실제 게임 밸런스와 무관).
public partial class AutoChess
{
    bool demo, hideHud;
    float demoFade; string endTitle = "", endSub = "";

    static string Arg(string name)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length; i++) if (a[i] == name) return i + 1 < a.Length ? a[i + 1] : "";
        return null;
    }
    public static bool DemoFlag => Arg("-demo") != null || PlayerPrefs.GetInt("AutoChessDemo", 0) == 1;

    void DemoBegin()
    {
        if (!DemoFlag) return;
        demo = true; Application.runInBackground = true;
        StartCoroutine(DemoRoutine());
    }

    // 시간: 프레임 저장 중에는 프레임당 고정 시간(1/fps), 평소에는 실제 경과 시간
    // 배너 등의 시간: 프레임 저장 중에는 저장한 프레임 수 기준(영상 길이와 일치), 평소에는 실제 시간
    static float NowT => AutoChessCapture.Active ? AutoChessCapture.T : Time.unscaledTime;
    static float DemoDt => AutoChessCapture.Active ? 1f / AutoChessCapture.Fps : Time.unscaledDeltaTime;
    IEnumerator DWait(float sec) { float t = 0; while (t < sec) { yield return null; t += DemoDt; } }

    // ---------- 카메라 ----------
    static readonly Vector3 HomePos = new Vector3(3.5f, 16.2f, -4.25f), HomeLook = new Vector3(3.5f, 0, 2.3f);
    bool camCtl; Vector3 camPos = HomePos, camLook = HomeLook; float camFov = 40; Coroutine camCo;

    void LateUpdate()
    {
        if (!camCtl) return;
        var c = Camera.main;
        c.transform.position = camPos; c.transform.LookAt(camLook); c.fieldOfView = camFov;
    }

    static float Ease(float t) => t * t * (3 - 2 * t);

    void CamGo(Vector3 pos, Vector3 look, float fov, float dur)
    {
        if (camCo != null) StopCoroutine(camCo);
        camCtl = true; camCo = StartCoroutine(CamMove(pos, look, fov, dur));
    }
    IEnumerator CamMove(Vector3 p1, Vector3 l1, float f1, float dur)
    {
        Vector3 p0 = camPos, l0 = camLook; float f0 = camFov, t = 0;
        while (t < dur) { t += DemoDt; float e = Ease(Mathf.Clamp01(t / dur)); camPos = Vector3.Lerp(p0, p1, e); camLook = Vector3.Lerp(l0, l1, e); camFov = Mathf.Lerp(f0, f1, e); yield return null; }
    }
    // 중심을 돌며 천천히 이동(각도는 도, 0°=+Z 방향 위치, 180°=-Z 방향 위치)
    void CamOrbit(Vector3 center, float radius, float height, float a0, float a1, float dur, float lookY, float fov)
    {
        if (camCo != null) StopCoroutine(camCo);
        camCtl = true; camCo = StartCoroutine(CamOrbitRoutine(center, radius, height, a0, a1, dur, lookY, fov));
    }
    IEnumerator CamOrbitRoutine(Vector3 c, float r, float h, float a0, float a1, float dur, float lookY, float fov)
    {
        float t = 0, f0 = camFov;
        while (t < dur)
        {
            t += DemoDt; float u = Mathf.Clamp01(t / dur), a = Mathf.Lerp(a0, a1, u) * Mathf.Deg2Rad;
            camPos = new Vector3(c.x + Mathf.Sin(a) * r, h, c.z + Mathf.Cos(a) * r);
            camLook = new Vector3(c.x, lookY, c.z); camFov = Mathf.Lerp(f0, fov, Ease(u));
            yield return null;
        }
    }
    void CamHome(float dur) => CamGo(HomePos, HomeLook, 40, dur);

    // ---------- 시연 진행 ----------
    // 적 체력을 키워 전투를 길게 만든다(StartBattle 전에 호출: StartBattle 이 max 기준으로 hp 를 다시 계산함)
    void DemoBoostFoes(float m)
    {
        foreach (var u in us) if (u.team == 1 && !u.temp) { u.max = (int)(u.max * m); u.hp = u.max; }
    }

    // 아군 체력만 키운다(보스의 전체 공격에 한 번에 쓰러지지 않게). Prep 이 hp 를 max 로 되돌리므로 Prep 전에 호출
    void DemoBoostHp(float m)
    {
        foreach (var u in us) if (u.team == 0 && !u.temp) { u.max = (int)(u.max * m); u.hp = u.max; }
    }

    void DemoBoost(float m)
    {
        foreach (var u in us) if (u.team == 0 && !u.temp) { u.max = (int)(u.max * m); u.hp = u.max; u.atk = (int)(u.atk * m); }
    }

    IEnumerator DemoWaitBattle(float limit)
    {
        float t = 0, heal = 0;
        while (battle && t < limit)
        {
            yield return null; t += DemoDt; heal -= DemoDt;
            foreach (var u in us) if (u.Active && !u.temp && u.team == 0) { int floor = (int)(Cap(u) * .35f); if (u.hp < floor) u.hp = floor; } // 시연용: 아군은 매 프레임 체력 35% 아래로 쓰러지지 않게 보호
            if (t > limit * .6f && speed < 3) { speed = 3; ApplyTime(); } // 너무 길면 빨리 감기
            if (heal <= 0) // 시연용: 밀리면 아군을 조금씩 회복시켜 전투는 길고 치열하게, 결과는 승리로 끝나게 한다
            {
                heal = .5f; int mine = 0, foe = 0;
                foreach (var u in us) if (u.Active && !u.temp) { if (u.team == 0) mine += u.hp; else foe += u.hp; }
                if (mine < foe * .95f)
                {
                    foreach (var u in us) if (u.Active && !u.temp && u.team == 0) u.hp = Mathf.Min(Cap(u), u.hp + (int)(u.max * .10f));
                    foreach (var e in us.ToArray()) if (e.Active && !e.temp && e.team == 1) Dmg(e, Mathf.Max(1, (int)(e.max * .05f)), false, false, 0); // 정상 사망 처리를 거치도록 Dmg 사용
                }
            }
        }
    }

    IEnumerator DemoRoutine()
    {
        yield return null; yield return null;
        var th = AutoChessThemes.I; string ta = Arg("-theme");
        if (th != null && ta != null && int.TryParse(ta, out int ti)) th.Apply(Mathf.Clamp(ti, 0, AutoChessThemes.Count - 1));
        helpOpen = false; quitConfirm = false; speed = 1;
        Reset(); round = 17; level = 6; gold = 44; xp = 0; helpOpen = false; Prep();   // 2스테이지 7라운드: 적 7마리
        hideHud = true; demoFade = 1;
        camPos = new Vector3(-8f, 9f, -9f); camLook = new Vector3(3.5f, 0, 3.5f); camFov = 38; camCtl = true;

        // 1) 오프닝: 높은 곳에서 천천히 내려오며 전장을 보여 준다
        StartCoroutine(Fade(0, 1.2f));
        CamOrbit(new Vector3(3.5f, 0, 3.5f), 14.5f, 10.5f, 235, 168, 7.5f, 0f, 40);
        yield return DWait(1.2f);
        ShowBanner(GameTitle, new Color(1f, .85f, .3f), 3.8f);
        yield return DWait(6.3f);

        // 2) 준비 단계: 상점에서 유닛을 사서 배치
        hideHud = false; shopOpen = true;
        CamGo(new Vector3(3.5f, 11.8f, -2.4f), new Vector3(3.5f, 0, 3.9f), 40, 1.2f); // 상점 패널이 가리지 않도록 전장을 화면 아래쪽에 둔다
        var modeled = new List<int>(); for (int id = 0; id < Kinds; id++) if (AutoChessVisualManager.I.HasUnit(id)) modeled.Add(id);
        if (modeled.Count >= 3) for (int i = 0; i < shop.Length; i++) shop[i] = modeled[Random.Range(0, modeled.Count)]; // 3D 모델이 있는 유닛으로 상점을 채운다
        yield return DWait(1.2f);
        for (int n = 0; n < 7 && gold >= 1; n++)
        {
            int best = -1;
            for (int i = 0; i < shop.Length; i++) if (shop[i] >= 0 && Dt(shop[i]).cost <= gold && (best < 0 || Score(shop[i]) > Score(shop[best]))) best = i;
            if (best < 0) { if (gold < 2) break; gold--; Roll(); yield return DWait(.5f); continue; } // 살 게 없으면 리롤
            if (Buy(shop[best])) shop[best] = -1;
            yield return DWait(.5f);
            if (n == 3) { BuyXp(); yield return DWait(.6f); }   // 경험치로 레벨업
        }
        int[] cols = { 3, 4, 2, 5, 1, 6, 0, 7 };
        foreach (var u in us.FindAll(x => x.team == 0 && x.bench && !x.temp))
        {
            if (FieldCount() >= level) break;
            int row = u.d.range >= 3 ? 1 : u.d.range == 2 ? 2 : 3, ci = 0;
            while (ci < cols.Length - 1 && UnitAt(cols[ci], row, false) != null) ci++;
            SetPos(u, cols[ci], row, false);
            yield return DWait(.4f);
        }
        DemoBoost(1.0f); DemoBoostFoes(1.6f);
        yield return DWait(1.4f);

        // 3) 전투 1: 카메라가 전장을 돌며 보여 준다
        shopOpen = false; hideHud = true;
        StartBattle(); speed = 1; ApplyTime();
        CamOrbit(new Vector3(3.5f, 0, 3.7f), 8.6f, 6.4f, 165, 225, 18f, 0.6f, 38);
        yield return DemoWaitBattle(60f);
        Debug.Log($"[DEMO] 전투1 종료: 목숨 {life}, 라운드 {round}");
        yield return DWait(.6f);
        hideHud = false; CamHome(1.8f);
        yield return DWait(3.2f);

        // 4) 보스전: 3스테이지급 라인업을 미리 구성해 스킬을 보여 준다
        foreach (var u in us.ToArray()) if (u.team == 0) { Destroy(u.go); us.Remove(u); }
        int[,] slots = { { 2, 3, 3 }, { 2, 4, 3 }, { 2, 2, 3 }, { 2, 3, 1 }, { 2, 5, 1 }, { 2, 2, 1 }, { 2, 4, 1 }, { 2, 6, 0 } }; // 성급, x, z (앞줄이 먼저)
        var pick = new List<int> { 16, 25, 20, 22, 29, 28, 35, 21 }; // 보기 좋은 대표 유닛(강철 수호자, 용암 거인, 드래곤 나이트, 대성학자, 시간 술사, 별빛 사제, 천둥 주술사, 저격 명수)
        foreach (int id in modeled) if (pick.Count < 8 && !pick.Contains(id)) pick.Add(id); // 모델이 없는 유닛이 있으면 있는 것으로 대체
        for (int i = 0; i < slots.GetLength(0); i++) Spawn(0, pick[i % pick.Count], slots[i, 1], slots[i, 2], 0.85f, false, slots[i, 0]);
        DemoBoostHp(3.5f);
        round = 30; level = 8; gold = 30; Prep();   // 최종 보스 3-10 제우스
        hideHud = false; shopOpen = false; DemoBoostFoes(1.4f);
        yield return DWait(1.6f);
        hideHud = true; speed = 1;
        StartBattle(); speed = 1; ApplyTime();
        CamOrbit(new Vector3(3.5f, 0.3f, 4.4f), 9.5f, 5.2f, 200, 150, 9f, 1.0f, 40);
        yield return DWait(9.2f);
        CamOrbit(new Vector3(3.5f, 0.3f, 4.4f), 11f, 8f, 150, 205, 16f, 0.6f, 40);
        yield return DemoWaitBattle(80f);
        Debug.Log($"[DEMO] 보스전 종료: 목숨 {life}, 클리어 {cleared}");
        yield return DWait(.8f);

        // 5) 엔딩 카드
        yield return Fade(1, 1.2f);
        endTitle = GameTitle; endSub = "30라운드 PvE 오토체스";
        hideHud = true; camCtl = false;
        yield return DWait(.3f); demoFade = 0; // 엔딩 카드는 검은 바탕 위에 그린다(endTitle 이 있으면 검은 화면 유지)
        yield return DWait(5f);
        if (AutoChessCapture.Active) AutoChessCapture.Finish();
        else GoMenu();
    }

    int Score(int id) => Dt(id).cost + (AutoChessVisualManager.I.HasUnit(id) ? 10 : 0); // 3D 모델이 있는 유닛을 우선 구매

    IEnumerator Fade(float to, float dur)
    {
        float f0 = demoFade, t = 0;
        while (t < dur) { t += DemoDt; demoFade = Mathf.Lerp(f0, to, Mathf.Clamp01(t / dur)); yield return null; }
        demoFade = to;
    }

    GUIStyle demoTitle, demoSub;
    void DemoGUI()
    {
        if (!demo || Event.current.type != EventType.Repaint) return;
        float W = Screen.width, H = Screen.height;
        bool end = endTitle != "";
        if (end || demoFade > 0)
        {
            GUI.color = new Color(0, 0, 0, end ? 1 : demoFade); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
        }
        if (!end) return;
        demoTitle ??= new GUIStyle(GUI.skin.label); demoSub ??= new GUIStyle(GUI.skin.label);
        demoTitle.alignment = demoSub.alignment = TextAnchor.MiddleCenter; demoTitle.fontStyle = FontStyle.Bold; demoSub.fontStyle = FontStyle.Italic;
        demoTitle.fontSize = Mathf.RoundToInt(H * .09f); demoSub.fontSize = Mathf.RoundToInt(H * .035f);
        void Sh(Rect r, string s, GUIStyle st, Color c)
        {
            st.normal.textColor = new Color(0, 0, 0, .8f); GUI.Label(new Rect(r.x + 3, r.y + 4, r.width, r.height), s, st);
            st.normal.textColor = c; GUI.Label(r, s, st);
        }
        Sh(new Rect(0, H * .36f, W, H * .14f), endTitle, demoTitle, new Color(1, .85f, .3f));
        Sh(new Rect(0, H * .5f, W, H * .07f), endSub, demoSub, new Color(.9f, .9f, .95f));
    }
}

// 프레임 저장(-capture <폴더>): 게임 시간을 프레임당 고정(1/fps)으로 돌리면서 매 프레임 JPG 로 저장한다. 느려도 영상은 부드럽게 나온다.
public class AutoChessCapture : MonoBehaviour
{
    public static bool Active { get; private set; }
    public static int Fps = 30;
    public static float T => Frames / (float)Fps; // 저장한 프레임 수 기준 경과 시간(영상 길이와 같음)
    public static int Frames;
    static AutoChessCapture inst;
    string dir; int frame;

    public static void Init()
    {
        var d = Arg("-capture");
        if (string.IsNullOrEmpty(d) || inst != null) return;
        if (int.TryParse(Arg("-fps"), out int f)) Fps = Mathf.Clamp(f, 10, 60);
        Directory.CreateDirectory(d);
        var go = new GameObject("AutoChessCapture"); DontDestroyOnLoad(go);
        inst = go.AddComponent<AutoChessCapture>(); inst.dir = d;
        Time.captureFramerate = Fps; Active = true; Application.runInBackground = true;
    }

    static string Arg(string name)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length; i++) if (a[i] == name) return i + 1 < a.Length ? a[i + 1] : "";
        return null;
    }

    public static void Finish() { Active = false; if (inst != null) AutoChessAudio.DumpEvents(inst.dir); Application.Quit(); }

    IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (!Active) continue;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, $"f{frame++:D5}.jpg"), tex.EncodeToJPG(92)); Frames = frame;
            Destroy(tex);
            if (frame > Fps * 240) Finish(); // 안전장치: 최대 4분
        }
    }
}
