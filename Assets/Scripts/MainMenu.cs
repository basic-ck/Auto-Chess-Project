using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 씬(MainMenu)의 화면: 배경 일러스트 + 제목 + [게임 시작] [난이도] [종료]. 고른 난이도는 PlayerPrefs로 게임 씬(GameScene)에 넘긴다.
//
// 이미지 파일(없으면 코드로 그린 임시 배경/글자 제목을 쓴다) — Resources/Visuals/UI/ 에 넣는다:
//   Menu_Background (배경 일러스트, 16:9 권장 1920x1080 이상)   Menu_Logo (제목 로고, 투명 PNG, 선택)
[ExecuteAlways] // 에디터(Play 전)에서도 Game 뷰에 메뉴가 보이게 한다. 버튼은 Play 중에만 작동
public class MainMenu : MonoBehaviour
{
    static readonly string[] DiffName = { "쉬움", "보통", "어려움" };
    static readonly Color Gold = new Color(1f, .84f, .38f), Cream = new Color(1f, .95f, .8f);
    int diff = 1;
    float startTime; // Play 시작 시각(까만 화면에서 서서히 밝아지는 연출 계산용)
    bool loading;
    // 프레임 저장 중에는 실시간이 아니라 프레임당 고정 시간(unscaledTime)으로 움직여야 영상이 일정하다
    static float Now => Application.isPlaying ? (AutoChessCapture.Active ? AutoChessCapture.T : Time.unscaledTime) : Time.realtimeSinceStartup;

    // 에디터가 컴포넌트의 비공개 값까지 스크립트 재컴파일 뒤에도 보존해서, 예전 텍스처가 남지 않게 저장 대상에서 뺀다
    [System.NonSerialized] Texture2D bg, logo, sky, edge, dot, boxN, boxH, boxA, boxSel;
    [System.NonSerialized] GUIStyle title, sub, label, btn, tog, small;

    void Start()
    {
        if (!Application.isPlaying) return;
        Time.timeScale = 1f; startTime = Now;
        diff = Mathf.Clamp(PlayerPrefs.GetInt(AutoChess.DiffKey, 1), 0, 2);
    }

    void Update()
    {
        // 자동 시연(-demo): 메뉴를 잠깐 보여 준 뒤 게임 씬으로 넘어간다
        if (Application.isPlaying && !loading && AutoChess.DemoFlag && Now - startTime > 5.5f) { loading = true; SceneManager.LoadScene(AutoChess.GameScene); }
    }

    // ---------- 그래픽(코드로 생성) ----------
    static Texture2D Tex(int w, int h, System.Func<int, int, Color> px, bool bilinear = true)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = bilinear ? FilterMode.Bilinear : FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, px(x, y));
        t.Apply();
        return t;
    }

    // 테두리가 있는 버튼 바탕(9분할로 늘어남)
    static Texture2D Box(Color fill, Color line)
    {
        const int n = 16;
        return Tex(n, n, (x, y) =>
        {
            int d = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
            if (d == 0) return line;
            if (d == 1) return Color.Lerp(line, fill, .6f);
            return fill;
        });
    }

    void Build()
    {
        if (sky != null) return;
        bg = Resources.Load<Texture2D>("Visuals/UI/Menu_Background");
        logo = Resources.Load<Texture2D>("Visuals/UI/Menu_Logo");
        // 임시 배경: 짙은 남색 하늘 → 보랏빛 → 노을빛 지평선
        sky = Tex(2, 256, (x, y) =>
        {
            float v = y / 255f; // 0=아래 1=위
            var top = new Color(.04f, .05f, .14f); var mid = new Color(.2f, .12f, .32f); var low = new Color(.85f, .45f, .3f);
            return v > .5f ? Color.Lerp(mid, top, (v - .5f) * 2) : Color.Lerp(low, mid, v * 2);
        });
        edge = Tex(2, 128, (x, y) => new Color(0, 0, 0, Mathf.Pow(y / 127f, 2f))); // 맨 위(가장자리)가 가장 어둡고 안쪽으로 옅어지는 비네트용(텍스처 y=127 이 위쪽)
        dot = Tex(32, 32, (x, y) => { float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f; return new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 2)); });
        boxN = Box(new Color(.07f, .09f, .18f, .82f), new Color(.75f, .6f, .25f, 1));
        boxH = Box(new Color(.16f, .2f, .36f, .92f), new Color(1f, .84f, .38f, 1));
        boxA = Box(new Color(.04f, .05f, .12f, .95f), new Color(1f, .7f, .2f, 1));
        boxSel = Box(new Color(.6f, .45f, .12f, .95f), new Color(1f, .88f, .5f, 1));
    }

    // 마우스를 올리면 GUIStyle 이 hover 상태의 글자색(기본값)으로 바뀌어, 겹쳐 그린 그림자 글자와 색이 어긋나 겹쳐 보인다 → 모든 상태를 같은 색으로 고정
    static void SetColor(GUIStyle st, Color c)
    {
        st.normal.textColor = st.hover.textColor = st.active.textColor = st.focused.textColor = c;
        st.onNormal.textColor = st.onHover.textColor = st.onActive.textColor = st.onFocused.textColor = c;
    }

    static GUIStyle Style(GUIStyle src, int size, Color col, FontStyle fs, TextAnchor a)
    {
        var s = src ?? new GUIStyle(GUI.skin.label);
        s.fontSize = size; s.fontStyle = fs; s.alignment = a; s.wordWrap = false;
        SetColor(s, col);
        return s;
    }

    // 글자에 그림자(4방향 어두운 복사) + 본 글자
    static void Shadowed(Rect r, string text, GUIStyle st, Color main)
    {
        SetColor(st, new Color(0, 0, 0, .75f));
        float o = Mathf.Max(2, st.fontSize * .06f);
        GUI.Label(new Rect(r.x + o, r.y + o * 1.4f, r.width, r.height), text, st);
        GUI.Label(new Rect(r.x - o * .5f, r.y + o, r.width, r.height), text, st);
        SetColor(st, main);
        GUI.Label(r, text, st);
    }

    void OnGUI()
    {
        Build();
        float W = Screen.width, H = Screen.height;
        float s = Mathf.Clamp(H / 800f, .75f, 2.2f); // 화면 크기에 맞춘 배율(기준 높이 800)
        float t = Now;
        if (Event.current.type == EventType.Repaint) DrawBackground(W, H, t);

        // 제목: 로고 이미지가 있으면 로고, 없으면 글자
        float ty = H * .13f;
        if (logo != null)
        {
            float lw = Mathf.Min(W * .6f, logo.width * s), lh = lw * logo.height / logo.width;
            GUI.DrawTexture(new Rect((W - lw) / 2, ty, lw, lh), logo, ScaleMode.ScaleToFit);
            ty += lh;
        }
        else
        {
            title = Style(title, Mathf.RoundToInt(54 * s), Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            Shadowed(new Rect(0, ty, W, 70 * s), AutoChess.Title, title, Gold);
            ty += 70 * s;
        }
        sub = Style(sub, Mathf.RoundToInt(17 * s), new Color(.88f, .88f, .95f), FontStyle.Italic, TextAnchor.MiddleCenter);
        Shadowed(new Rect(0, ty + 2 * s, W, 28 * s), "30라운드 PvE 오토체스", sub, sub.normal.textColor);

        // 버튼
        btn ??= new GUIStyle(GUI.skin.button);
        btn.fontSize = Mathf.RoundToInt(22 * s); btn.fontStyle = FontStyle.Bold; btn.alignment = TextAnchor.MiddleCenter;
        btn.border = new RectOffset(4, 4, 4, 4);
        btn.normal.background = boxN; btn.hover.background = boxH; btn.active.background = boxA; btn.focused.background = boxN;
        btn.normal.textColor = Cream; btn.hover.textColor = Color.white; btn.active.textColor = Gold; btn.focused.textColor = Cream;
        tog ??= new GUIStyle(btn);
        tog.fontSize = Mathf.RoundToInt(17 * s); tog.border = btn.border;
        tog.normal.background = boxN; tog.hover.background = boxH; tog.active.background = boxA;
        tog.onNormal.background = boxSel; tog.onHover.background = boxSel; tog.onActive.background = boxSel;
        tog.onNormal.textColor = new Color(.1f, .06f, 0); tog.onHover.textColor = tog.onNormal.textColor; tog.onActive.textColor = tog.onNormal.textColor;
        tog.normal.textColor = Cream; tog.hover.textColor = Color.white;

        float bw = 300 * s, bh = 54 * s, x = (W - bw) / 2, y = H * .44f;
        GUI.enabled = Application.isPlaying; // Play 전에는 보기만 가능(버튼 비활성)
        if (GUI.Button(new Rect(x, y, bw, bh), "게임 시작", btn))
        {
            PlayerPrefs.SetInt(AutoChess.DiffKey, diff); PlayerPrefs.Save();
            SceneManager.LoadScene(AutoChess.GameScene);
        }
        label = Style(label, Mathf.RoundToInt(15 * s), new Color(.9f, .9f, .95f), FontStyle.Normal, TextAnchor.MiddleCenter);
        Shadowed(new Rect(x, y + bh + 14 * s, bw, 22 * s), "난이도", label, label.normal.textColor);
        float tw = (bw - 2 * 6 * s) / 3, ty2 = y + bh + 40 * s, th = 36 * s;
        for (int i = 0; i < 3; i++)
            if (GUI.Toggle(new Rect(x + i * (tw + 6 * s), ty2, tw, th), diff == i, DiffName[i], tog)) diff = i;
        float ey = ty2 + th + 22 * s;
#if UNITY_EDITOR
        if (GUI.Button(new Rect(x, ey, bw, bh), "종료", btn)) UnityEditor.EditorApplication.isPlaying = false;
#else
        if (GUI.Button(new Rect(x, ey, bw, bh), "종료", btn)) Application.Quit();
#endif
        GUI.enabled = true;

        small = Style(small, Mathf.RoundToInt(12 * s), new Color(1, 1, 1, .55f), FontStyle.Normal, TextAnchor.LowerRight);
        GUI.Label(new Rect(W - 220 * s - 12, H - 28 * s, 220 * s, 22 * s), "v" + Application.version, small);

        float fade = Application.isPlaying ? Mathf.Clamp01(1 - (Now - startTime) / .9f) : 0; // 편집 모드에서는 연출 없음
        if (fade > 0 && Event.current.type == EventType.Repaint)
        {
            GUI.color = new Color(0, 0, 0, fade); GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
        }
    }

    // 배경: 일러스트가 있으면 화면을 꽉 채워(비율 유지) 아주 천천히 확대·이동, 없으면 임시 그라데이션. 그 위에 반짝이는 입자와 가장자리 어둠.
    void DrawBackground(float W, float H, float t)
    {
        GUI.BeginGroup(new Rect(0, 0, W, H));
        if (bg != null)
        {
            float zoom = 1.04f + .03f * Mathf.Sin(t * .15f);                       // 천천히 숨 쉬듯 확대
            float sc = Mathf.Max(W / bg.width, H / bg.height) * zoom;
            float dw = bg.width * sc, dh = bg.height * sc;
            float dx = (W - dw) / 2 + Mathf.Sin(t * .1f) * W * .01f, dy = (H - dh) / 2;
            GUI.DrawTexture(new Rect(dx, dy, dw, dh), bg, ScaleMode.StretchToFill);
        }
        else GUI.DrawTexture(new Rect(0, 0, W, H), sky, ScaleMode.StretchToFill);

        // 반짝이는 입자(정령의 빛): 천천히 위로 떠오르며 깜빡임
        for (int i = 0; i < 46; i++)
        {
            float r1 = Frac(Mathf.Sin(i * 12.9898f) * 43758.5f), r2 = Frac(Mathf.Sin(i * 78.233f) * 12345.6f), r3 = Frac(Mathf.Sin(i * 39.346f) * 9876.5f);
            float sp = .012f + r3 * .02f;
            float py = H * (1 - Frac(r2 + t * sp));
            float px = W * r1 + Mathf.Sin(t * (.3f + r3) + i) * 14;
            float sz = (3 + r3 * 7) * (H / 800f);
            float a = (.25f + .55f * (.5f + .5f * Mathf.Sin(t * (1 + r3 * 2) + i * 1.7f))) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(1 - py / H));
            GUI.color = new Color(1f, .92f, .6f, Mathf.Clamp01(a));
            GUI.DrawTexture(new Rect(px - sz, py - sz, sz * 2, sz * 2), dot);
        }
        // 가장자리 어둠(비네트): 위·아래
        GUI.color = new Color(1, 1, 1, .8f);
        GUI.DrawTexture(new Rect(0, 0, W, H * .22f), edge, ScaleMode.StretchToFill);
        GUI.matrix = Matrix4x4.TRS(new Vector3(0, H, 0), Quaternion.identity, new Vector3(1, -1, 1)); // 위아래 뒤집어 아래쪽에도
        GUI.DrawTexture(new Rect(0, 0, W, H * .3f), edge, ScaleMode.StretchToFill);
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
        GUI.EndGroup();
    }

    static float Frac(float v) => v - Mathf.Floor(v);
}
