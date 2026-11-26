using System; using System.Collections.Generic; using System.Text; using UnityEngine;
namespace SteamEngineer
{
    public static class Unlocks { public const string BaseEnhance = "base_enhance", TierRank = "tier_rank"; public const string StarterEvolution = "starter_evolution"; }
    public enum AchRoute { Main, Sub }
    public class AchCond { public string label; public Func<SaveData, int> cur; public int goal; }
    public class AchDef
    {
        public string id, name, desc, reward; public AchRoute route;
        public AchCond[] conds;            // すべて満たすと達成
        public string[] unlocks;           // 達成で立てる解放フラグ
        public int rankUp;                 // 達成で上がる工房ランク
    }

    public static class Achievements
    {
        static AchCond C(string label, Func<SaveData, int> cur, int goal) => new AchCond { label = label, cur = cur, goal = goal };

        // ★ 実績を増やすときは、ここに足す。記録は s.Stat("キー") で読み、SaveData.AddStat("キー", 1) で増やす
        public static readonly List<AchDef> All = new List<AchDef>
        {
            new AchDef { id = "main_first", route = AchRoute.Main, name = "はじめての帰還", desc = "1周をクリアする",
                reward = "装備一覧の「基礎強化」が解放される",
                conds = new[] { C("周回クリア", s => s.Stat("runs_cleared"), 1) }, unlocks = new[] { Unlocks.BaseEnhance } },
            new AchDef { id = "main_workshop", route = AchRoute.Main, name = "工房の腕前", desc = "5周クリアし、3種類の装備を作る",
                reward = "「ティア上昇」と「工房ランク」が解放される",
                conds = new[] { C("周回クリア", s => s.Stat("runs_cleared"), 5), C("作成した装備の種類", s => s.owned.Count, 3) }, unlocks = new[] { Unlocks.TierRank } },
            new AchDef { id = "main_starter_master", route = AchRoute.Main, name = "\u521d\u671f\u88c5\u5099\u306e\u719f\u7df4\u8005", desc = "\u521d\u671f\u88c5\u5099\u3060\u3051\u30673\u5468\u30af\u30ea\u30a2\u3059\u308b",
                reward = "\u521d\u671f\u88c5\u5099\u306e\u9032\u5316\u304c\u89e3\u653e\u3055\u308c\u308b",
                conds = new[] { C("\u521d\u671f\u88c5\u5099\u3060\u3051\u3067\u5468\u56de\u30af\u30ea\u30a2", s => s.Stat("runs_starter_cleared"), 3) }, unlocks = new[] { Unlocks.StarterEvolution } },
        };

        public static AchDef Find(string id) { foreach (var d in All) if (d.id == id) return d; return null; }
        public static bool Met(AchDef d, SaveData s) { foreach (var c in d.conds) if (c.cur(s) < c.goal) return false; return true; }

        // 新しく達成したものを処理して、表示用の文を返す
        public static List<string> CheckAll(SaveData s)
        {
            var news = new List<string>(); bool again = true;
            while (again)
            {
                again = false;
                foreach (var d in All)
                {
                    if (s.IsAchieved(d.id) || !Met(d, s)) continue;
                    s.achieved.Add(d.id);
                    if (d.unlocks != null) foreach (var u in d.unlocks) if (!s.unlocks.Contains(u)) s.unlocks.Add(u);
                    if (d.rankUp > 0) s.rank = Mathf.Min(4, s.rank + d.rankUp);
                    news.Add($"{d.name}  →  {d.reward}"); again = true;
                }
            }
            if (news.Count > 0) s.Write();
            return news;
        }

        static string Bar(int c, int g) { int f = Mathf.RoundToInt(10f * c / Mathf.Max(1, g)); return new string('■', f) + new string('□', 10 - f); }
        public static string Describe(AchDef d, SaveData s)
        {
            bool done = s.IsAchieved(d.id); var sb = new StringBuilder();
            sb.AppendLine(done ? $"<color=#FFD060>★ {d.name}  [達成]</color>" : $"◇ {d.name}");
            sb.AppendLine(d.desc);
            foreach (var c in d.conds) { int cur = Mathf.Min(c.cur(s), c.goal); sb.AppendLine($"・{c.label}  {cur}/{c.goal}  {Bar(cur, c.goal)}"); }
            sb.Append("報酬: " + d.reward + (done ? "(解放済み)" : ""));
            return sb.ToString();
        }
    }
}