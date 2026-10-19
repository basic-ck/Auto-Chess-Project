using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 난이도 시험 모드(개발용): 화면 없이 봇이 게임을 끝까지 여러 번 돌려 라운드별 승률을 기록한다.
//   SpiritsAndGiants.exe -batchmode -nographics -sim 30 -logFile sim.log      (난이도 3종 각각 30판)
//   옵션: -simdiff 0|1|2 (한 난이도만), 결과는 로그의 [SIM] 줄
// 봇은 합성 우선 구매·경험치·리롤·배치만 하는 단순한 봇이라 사람보다 약하다(시너지 고려 없음). '벽'이 어디인지 찾는 용도.
public partial class AutoChess
{
    void SimBegin()
    {
        if (Arg("-sim") == null) return;
        helpOpen = false; Application.runInBackground = true;
        StartCoroutine(SimRoutine());
    }

    IEnumerator SimRoutine()
    {
        yield return null;
        int n = int.TryParse(Arg("-sim"), out int v) ? Mathf.Max(1, v) : 20;
        var only = Arg("-simdiff"); int d0 = 0, d1 = 2;
        if (only != null && int.TryParse(only, out int od)) d0 = d1 = Mathf.Clamp(od, 0, 2);
        var sb = new StringBuilder();
        for (int d = d0; d <= d1; d++)
        {
            var fights = new int[StageManager.Rounds + 2]; var wins = new int[StageManager.Rounds + 2];
            var reached = new List<int>(); int clears = 0;
            for (int g = 0; g < n; g++)
            {
                diff = d; Reset();
                yield return null;
                int guard = 0;
                while (!over && guard++ < 80)
                {
                    SimPrep();
                    int r = round, lifeBefore = life;
                    StartBattle();
                    if (!battle) { EndGame(false); break; } // 필드에 유닛이 없으면 종료
                    int t = 0; while (battle && t++ < MaxTicks + 5) Tick();
                    fights[r]++; if (life == lifeBefore && (!over || cleared)) wins[r]++;
                    if (guard % 3 == 0) yield return null; // 가끔 프레임을 넘겨 임시 오브젝트 정리
                }
                reached.Add(cleared ? StageManager.Rounds + 1 : finalRound); if (cleared) clears++;
                yield return null;
            }
            float avg = 0; foreach (var r in reached) avg += r; avg /= reached.Count;
            sb.Clear(); sb.Append($"[SIM] 난이도 {DiffName[d]}: {n}판, 평균 도달 {avg:F1}, 클리어 {clears}판 | 라운드별 승률: ");
            for (int r = 1; r <= StageManager.Rounds; r++) if (fights[r] > 0) sb.Append($"{StageManager.Label(r)}={wins[r] * 100 / fights[r]}%({fights[r]}) ");
            Debug.Log(sb.ToString());
        }
        Debug.Log("[SIM] 끝");
        Application.Quit();
    }

    // ----- 봇 -----
    float SimValue(U u) => u.d.cost * Mathf.Pow(2.2f, u.star - 1);

    void SimPrep()
    {
        int reserve = round >= 4 ? 10 : 0; // 이자를 위해 남겨 둘 골드
        int targetLv = Mathf.Min(MaxLv, 3 + round / 4);
        for (int g = 0; g < 6 && level < targetLv && gold >= 4 + reserve; g++) BuyXp();
        for (int pass = 0; pass < 14; pass++)
        {
            int best = -1; float bs = -1;
            for (int i = 0; i < shop.Length; i++)
            {
                if (shop[i] < 0) continue; int id = shop[i];
                if (Dt(id).cost > gold - reserve && !(Dt(id).cost <= gold && us.FindAll(x => x.team == 0 && x.id == id).Count >= 2)) continue;
                int owned = us.FindAll(x => x.team == 0 && x.id == id && x.star == 1).Count;
                float sc = Dt(id).cost + (owned >= 2 ? 100 : owned == 1 ? 12 : 0);
                if (sc > bs) { bs = sc; best = i; }
            }
            if (best >= 0 && Buy(shop[best])) { shop[best] = -1; continue; }
            if (gold - reserve >= 6 && pass < 3) { gold--; Roll(); continue; } // 살 게 없고 여유가 있으면 리롤
            break;
        }
        SimPlace();
    }

    void SimPlace()
    {
        int[] cols = { 3, 4, 2, 5, 1, 6, 0, 7 };
        for (int it = 0; it < 14; it++)
        {
            var bench = us.FindAll(x => x.team == 0 && x.bench && !x.temp); if (bench.Count == 0) break;
            bench.Sort((a, b) => SimValue(b).CompareTo(SimValue(a)));
            var b = bench[0];
            if (FieldCount() < level)
            {
                int row = b.d.range >= 3 ? 1 : b.d.range == 2 ? 2 : 3; bool done = false;
                foreach (int rr in new[] { row, 3, 2, 1, 0 })
                {
                    foreach (int c in cols) if (UnitAt(c, rr, false) == null && MoveUnit(b, c, rr, false)) { done = true; break; }
                    if (done) break;
                }
                if (!done) break;
            }
            else
            {
                U worst = null;
                foreach (var f in us) if (f.team == 0 && !f.bench && !f.temp && (worst == null || SimValue(f) < SimValue(worst))) worst = f;
                if (worst != null && SimValue(b) > SimValue(worst) * 1.2f) MoveUnit(b, worst.x, worst.z, false); else break;
            }
        }
    }
}
