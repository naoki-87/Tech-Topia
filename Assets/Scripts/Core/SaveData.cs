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
        public string[] loadout = { "", "", "", "" };               // 部位ごとの装着指定(""=初期装備)

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
        }
        public void Write() { try { File.WriteAllText(FilePath, JsonUtility.ToJson(this)); } catch (Exception e) { Debug.LogWarning(e); } }

        public bool SpendCore(EquipSlot slot) { if (cores <= 0) return false; cores--; baseLevel[(int)slot]++; return true; }
        public void Apply(RunState run, bool win)
        {
            commonMaterials += run.areasCleared * 5 + (win ? 10 : 0);   // 進行度に応じた汎用素材
            if (win) cores += run.coresCollected;                       // 最終ボス撃破でコア獲得
        }

        // 装備の解放(作成・ティア強化のときに呼ぶ)。初期装備は対象外
        public void Unlock(EquipmentData e, Tier t)
        {
            if (e == null || e.isStarter) return;
            foreach (var o in owned)
                if (o.id == e.name) { if ((int)t > o.tier) { o.tier = (int)t; Write(); } return; }
            owned.Add(new OwnedEquip { id = e.name, tier = (int)t }); Write();
        }
    }
}