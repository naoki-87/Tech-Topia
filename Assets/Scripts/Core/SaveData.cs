using System; using System.Collections.Generic; using System.IO; using UnityEngine;
namespace SteamEngineer
{
    [Serializable] public class OwnedEquip { public string id; public int tier; }

    [Serializable]
    public class SaveData
    {
        public int cores;                       // 永続コア(基礎強化に使う)
        public int commonMaterials;             // 汎用素材
        public int rank;                        // 工房ランク 0=素人 … 4=伝説
        public int[] baseLevel = new int[4];    // 部位ごとの基礎強化
        public int[] startLevel = { -1, -1, -1, -1 };               // 開始レベル(-1 = 基礎レベルと同じ)
        public List<OwnedEquip> owned = new List<OwnedEquip>();     // 作成して解放した装備(初期装備は含まない)
        [Serializable] public class StockEntry { public string area; public int tier; public int count; }
        [Serializable] public class TierEntry { public string id; public int tier; }
        public string[] loadout = { "", "", "", "" };               // 部位ごとの装着指定(""=初期装備)
        public List<StockEntry> stock = new List<StockEntry>();    // 清算済みの素材(エリア×ティア)
        public List<TierEntry> tiers = new List<TierEntry>();      // 装備ごとのティア(初期装備を含む)
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