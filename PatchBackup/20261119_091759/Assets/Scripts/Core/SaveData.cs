using System; using System.Collections.Generic; using System.IO; using UnityEngine;
namespace SteamEngineer
{
    [Serializable] public class OwnedEquip { public string id; public int tier; }
    [Serializable] public class StockEntry { public string area; public int tier; public int count; }
    [Serializable] public class TierEntry { public string id; public int tier; }
    [Serializable] public class StatEntry { public string key; public int value; }
    [Serializable] public class ItemEntry { public string id; public int count; }

    [Serializable]
    public class SaveData
    {
        public int cores;                       // 永続コア(基礎強化に使う)
        public int commonMaterials;             // 汎用素材
        public int rank;                        // 工房ランク 0=素人 … 4=伝説
        public int[] baseLevel = new int[4];    // 部位ごとの基礎強化
        public int[] startLevel = { -1, -1, -1, -1 };               // 開始レベル(-1 = 基礎レベルと同じ)
        public List<OwnedEquip> owned = new List<OwnedEquip>();     // 作成して解放した装備(初期装備は含まない)
        public string[] loadout = { "", "", "", "" };               // 部位ごとの装着指定(""=初期装備)
        public List<StockEntry> stock = new List<StockEntry>();    // 清算済みの素材(エリア×ティア)
        public List<TierEntry> tiers = new List<TierEntry>();      // 装備ごとのティア(初期装備を含む)
        public List<StatEntry> stats = new List<StatEntry>();        // 実績の記録(周回クリア数など)
        public List<ItemEntry> items = new List<ItemEntry>();        // 作成済みの補助アイテム(次の周回に持ち込める)
        public List<ItemEntry> carry = new List<ItemEntry>();        // 次の周回に持ち込む数
        public List<string> achieved = new List<string>();           // 達成済みの実績
        public List<string> discovered = new List<string>();         // 見つけた遺物などのコレクション
        public List<string> unlocks = new List<string>();          // 解放済みの機能・装備・エリア(段階3で使う)
        [NonSerialized] public string lastSummary = "";            // 直近の清算内容(リザルト表示用)

        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
        public int StartLevel(int i) => startLevel[i] < 0 ? baseLevel[i] : Mathf.Min(startLevel[i], baseLevel[i]);

        public static SaveData Load()
        {
            SaveData s = null;
            try { if (File.Exists(FilePath)) s = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); } catch { }
            if (s == null) s = new SaveData();
            s.Fix(); return s;
        }
        void Fix()   // 古いセーブに無い項目や、長さの違う配列を直す
        {
            if (baseLevel == null || baseLevel.Length != 4) baseLevel = new int[4];
            if (startLevel == null || startLevel.Length != 4) startLevel = new[] { -1, -1, -1, -1 };
            if (loadout == null || loadout.Length != 4) loadout = new[] { "", "", "", "" };
            for (int i = 0; i < 4; i++) if (loadout[i] == null) loadout[i] = "";
            if (owned == null) owned = new List<OwnedEquip>();
            if (stock == null) stock = new List<StockEntry>();
            if (tiers == null) tiers = new List<TierEntry>();
            if (stats == null) stats = new List<StatEntry>();
            if (items == null) items = new List<ItemEntry>();
            if (carry == null) carry = new List<ItemEntry>();
            if (achieved == null) achieved = new List<string>();
            if (discovered == null) discovered = new List<string>();
            if (unlocks == null) unlocks = new List<string>();
        }
        public void Write() { try { File.WriteAllText(FilePath, JsonUtility.ToJson(this)); } catch (Exception e) { Debug.LogWarning(e); } }

        public bool SpendCore(EquipSlot slot) { if (cores <= 0) return false; cores--; baseLevel[(int)slot]++; return true; }
        public void Apply(RunState run, bool win)
        {
            int common = run.areasCleared * 5 + (win ? 10 : 0);
            commonMaterials += common;
            if (win) cores += run.coresCollected;
            var sb = new System.Text.StringBuilder();            // 周回中の素材を、すべて在庫へ清算する
            foreach (var kv in run.mats)
            {
                if (kv.Value <= 0 || kv.Key.Item1 == null) continue;
                AddStock(kv.Key.Item1.name, kv.Key.Item2, kv.Value);
                sb.AppendLine($"{kv.Key.Item1.materialName}[{kv.Key.Item2.Label()}] x{kv.Value}");
            }
            lastSummary = (sb.Length > 0 ? "【素材を清算】\n" + sb : "【素材を清算】なし") + $"\n汎用素材 +{common}" + (win ? $"  コア +{run.coresCollected}" : "");
            AddStat("runs", 1); if (win) AddStat("runs_cleared", 1); AddStat("areas_cleared", run.areasCleared);
            var news = Achievements.CheckAll(this);
            if (news.Count > 0) lastSummary += "\n\n★ 実績を達成！\n" + string.Join("\n", news);
        }
        public int Stock(string area, Tier t) { foreach (var s in stock) if (s.area == area && s.tier == (int)t) return s.count; return 0; }
        public void AddStock(string area, Tier t, int n)
        {
            if (n == 0) return;
            foreach (var s in stock) if (s.area == area && s.tier == (int)t) { s.count = Mathf.Max(0, s.count + n); return; }
            if (n > 0) stock.Add(new StockEntry { area = area, tier = (int)t, count = n });
        }
        public bool Has(string flag) => unlocks.Contains(flag);
        public void UnlockFlag(string flag) { if (!unlocks.Contains(flag)) { unlocks.Add(flag); Write(); } }
        public Tier MaxTier => Has(Unlocks.TierRank) ? (Tier)Mathf.Min(4, rank + 1) : Tier.Crude;   // 作成・ティアの上限
        public int Stat(string key) { foreach (var x in stats) if (x.key == key) return x.value; return 0; }
        static int Get(List<ItemEntry> l, string id) { foreach (var e in l) if (e.id == id) return e.count; return 0; }
        static void Add(List<ItemEntry> l, string id, int n)
        {
            foreach (var e in l) if (e.id == id) { e.count = Mathf.Max(0, e.count + n); return; }
            if (n > 0) l.Add(new ItemEntry { id = id, count = n });
        }
        public int ItemOwned(string id) => Get(items, id);
        public int ItemCarry(string id) => Get(carry, id);
        public void AddItem(string id, int n) => Add(items, id, n);
        public void SetCarry(string id, int n) { n = Mathf.Clamp(n, 0, Mathf.Min(ItemOwned(id), ItemManager.CarryPerKind)); foreach (var e in carry) if (e.id == id) { e.count = n; return; } if (n > 0) carry.Add(new ItemEntry { id = id, count = n }); }
        public void AddStat(string key, int n)
        {
            foreach (var x in stats) if (x.key == key) { x.value += n; return; }
            stats.Add(new StatEntry { key = key, value = n });
        }
        public bool IsAchieved(string id) => achieved.Contains(id);
        public void Discover(string key) { if (!discovered.Contains(key)) discovered.Add(key); }

        // 装備の解放(作成・ティア強化のときに呼ぶ)。初期装備は対象外
        public void Unlock(EquipmentData e, Tier t)
        {
            if (e == null || e.isStarter) return;
            bool has = false; foreach (var o in owned) if (o.id == e.name) { has = true; break; }
            if (!has) owned.Add(new OwnedEquip { id = e.name, tier = (int)t });
            if (!has || (int)t > (int)GetTier(e.name)) SetTier(e.name, t);
            Write();
        }
        public Tier GetTier(string id) { foreach (var x in tiers) if (x.id == id) return (Tier)Mathf.Clamp(x.tier, 0, 4); return Tier.Crude; }
        public void SetTier(string id, Tier t)
        {
            foreach (var x in tiers) if (x.id == id) { x.tier = (int)t; return; }
            tiers.Add(new TierEntry { id = id, tier = (int)t });
        }
    }
}