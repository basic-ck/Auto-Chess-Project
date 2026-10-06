using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 씬(MainMenu)의 화면: 제목 + [게임 시작] [난이도] [종료]. 고른 난이도는 PlayerPrefs로 게임 씬(GameScene)에 넘긴다.
[ExecuteAlways] // 에디터(Play 전)에서도 Game 뷰에 메뉴가 보이게 한다. 버튼은 Play 중에만 작동
public class MainMenu : MonoBehaviour
{
    static readonly string[] DiffName = { "쉬움", "보통", "어려움" };
    int diff = 1;
    GUIStyle title, sub, label;

    void Start()
    {
        if (!Application.isPlaying) return;
        Time.timeScale = 1f;
        diff = Mathf.Clamp(PlayerPrefs.GetInt(AutoChess.DiffKey, 1), 0, 2);
    }

    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;
        title ??= new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter; title.fontStyle = FontStyle.Bold; title.fontSize = 34; title.normal.textColor = new Color(1, .85f, .3f);
        sub ??= new GUIStyle(GUI.skin.label);
        sub.alignment = TextAnchor.MiddleCenter; sub.fontSize = 14; sub.normal.textColor = new Color(.8f, .8f, .85f);
        label ??= new GUIStyle(GUI.skin.label);
        label.alignment = TextAnchor.MiddleCenter; label.normal.textColor = Color.white;

        GUI.Label(new Rect(0, H * .16f, W, 60), AutoChess.Title, title);
        GUI.Label(new Rect(0, H * .16f + 56, W, 24), "30라운드 PvE 오토체스", sub);

        GUI.enabled = Application.isPlaying; // Play 전에는 보기만 가능(버튼 비활성)
        float bw = 220, bh = 42, x = W / 2 - bw / 2, y = H * .40f;
        if (GUI.Button(new Rect(x, y, bw, bh), "게임 시작"))
        {
            PlayerPrefs.SetInt(AutoChess.DiffKey, diff); PlayerPrefs.Save();
            SceneManager.LoadScene(AutoChess.GameScene);
        }
        GUI.Label(new Rect(x, y + 56, bw, 20), "난이도", label);
        for (int i = 0; i < 3; i++)
            if (GUI.Toggle(new Rect(x + i * 74, y + 78, 72, 26), diff == i, DiffName[i], GUI.skin.button)) diff = i;
#if UNITY_EDITOR
        if (GUI.Button(new Rect(x, y + 120, bw, bh), "종료")) UnityEditor.EditorApplication.isPlaying = false;
#else
        if (GUI.Button(new Rect(x, y + 120, bw, bh), "종료")) Application.Quit();
#endif
        GUI.enabled = true;
    }
}
