using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 효과음 재생기. Resources/Audio/SFX/<이름> 클립을 읽어 재생한다(파일이 없으면 조용히 건너뜀). 배경음악은 쓰지 않는다.
// 같은 소리가 한꺼번에 몰리지 않게 소리마다 최소 간격을 두고, 길게 녹음된 소리는 앞부분만 재생한 뒤 부드럽게 줄인다.
// 전체 음소거: M 키 / 상단 [소리] 버튼, 설정은 PlayerPrefs 에 저장.
// 트레일러 촬영(-capture) 중에는 소리가 영상에 안 담기므로 '몇 번째 프레임에 어떤 소리'인지 audio_events.tsv 로 기록해
// Tools/make_trailer.py 가 영상에 합쳐 넣는다.
public class AutoChessAudio : MonoBehaviour
{
    const string MuteKey = "AutoChessMute";
    static AutoChessAudio inst;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> last = new Dictionary<string, float>();
    readonly List<AudioSource> pool = new List<AudioSource>();
    readonly List<string> log = new List<string>();
    readonly float[] endT = new float[10], baseVol = new float[10];
    int next;

    // 길게 녹음된 소리는 앞부분만 쓴다(초). 목록에 없으면 끝까지 재생
    static readonly Dictionary<string, float> MaxLen = new Dictionary<string, float>
    {
        { "atk_fire1", .9f }, { "atk_fire2", .9f }, { "atk_wind", .9f }, { "atk_warp", .9f }, { "atk_gun", .7f }, { "atk_blast", 1.3f },
    };

    public static bool Muted
    {
        get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
        set { PlayerPrefs.SetInt(MuteKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Init()
    {
        if (inst != null) return;
        var go = new GameObject("AutoChessAudio"); DontDestroyOnLoad(go);
        inst = go.AddComponent<AutoChessAudio>();
        for (int i = 0; i < 10; i++) { var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0; inst.pool.Add(s); }
    }

    static float Now => AutoChessCapture.Active ? AutoChessCapture.T : Time.unscaledTime;

    AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out var c)) clips[name] = c = Resources.Load<AudioClip>("Audio/SFX/" + name);
        return c;
    }

    // 소리 재생. gap = 같은 소리의 최소 간격(초), pitchJitter 로 반복감을 줄인다.
    public static void Sfx(string name, float vol = 1f, float gap = .06f, float pitchJitter = 0f)
    {
        if (inst == null) return;
        inst.Play(name, vol, gap, pitchJitter);
    }

    void Play(string name, float vol, float gap, float pitchJitter)
    {
        float t = Now;
        if (last.TryGetValue(name, out var lt) && t - lt < gap) return;
        last[name] = t;
        var c = Clip(name);
        if (c == null) return;
        MaxLen.TryGetValue(name, out float maxLen);
        if (AutoChessCapture.Active) log.Add(AutoChessCapture.Frames + "\t" + name + "\t" + vol.ToString("F2") + "\t" + maxLen.ToString("F2"));
        if (Muted) return;
        int i = next++ % pool.Count; var s = pool[i];
        s.clip = c; s.pitch = 1f + (pitchJitter > 0 ? Random.Range(-pitchJitter, pitchJitter) : 0);
        baseVol[i] = Mathf.Clamp01(vol) * .8f; s.volume = baseVol[i];
        endT[i] = maxLen > 0 ? t + maxLen : 0;
        s.Play();
    }

    void Update() // 길게 녹음된 소리는 정해진 길이에서 끝을 부드럽게 줄이며 멈춘다
    {
        float t = Now;
        for (int i = 0; i < pool.Count; i++)
        {
            if (endT[i] <= 0) continue;
            float rem = endT[i] - t;
            if (rem <= 0) { pool[i].Stop(); endT[i] = 0; }
            else if (rem < .25f) pool[i].volume = baseVol[i] * rem / .25f;
        }
    }

    public static void Click() => Sfx("click", .7f, .05f);
    public static void Place() => Sfx("place", .6f, .08f);

    // 직업별 공격 소리 (Job: 0 기사, 1 사냥꾼, 2 암살자, 3 마법사, 4 전사, 5 사제, 6 주술사, 7 소환사, 8 흑마법사, 9 메카)
    public static void Attack(int job)
    {
        string n; float v = .5f;
        switch (job)
        {
            case 0: n = Random.value < .5f ? "atk_sword1" : "atk_sword2"; break;
            case 4: n = Random.value < .5f ? "atk_sword2" : "atk_sword1"; break;
            case 2: n = "atk_punch2"; break;
            case 1: n = Random.value < .5f ? "atk_arrow1" : "atk_arrow2"; break;
            case 3: n = Random.value < .5f ? "atk_fire1" : "atk_fire2"; v = .35f; break;
            case 8: n = "atk_wind"; v = .35f; break;
            case 6: n = "atk_warp"; v = .3f; break;
            case 5: case 7: n = "atk_bubble"; break;
            case 9: n = "atk_gun"; v = .35f; break;
            default: n = "atk_punch"; break;
        }
        Sfx(n, v, .09f, .06f);
    }

    public static void DumpEvents(string dir)
    {
        if (inst == null || inst.log.Count == 0) return;
        var sb = new StringBuilder("frame\tname\tvolume\tmaxlen\n");
        foreach (var l in inst.log) sb.AppendLine(l);
        File.WriteAllText(Path.Combine(dir, "audio_events.tsv"), sb.ToString(), new UTF8Encoding(false));
    }
}
