using System; using System.IO; using UnityEngine;
namespace SteamEngineer
{
    [Serializable]
    public class SaveData
    {
        public int cores;                 // 永続コア(基礎強化に使う)
        public int commonMaterials;       // 汎用素材
        public int[] baseLevel = new int[4];   // 部位ごとの基礎強化(周回開始時の上限レベル)
        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
        public static SaveData Load()
        {
            try { if (File.Exists(FilePath)) return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); } catch { }
            return new SaveData();
        }
        public void Write() { try { File.WriteAllText(FilePath, JsonUtility.ToJson(this)); } catch (Exception e) { Debug.LogWarning(e); } }
        public bool SpendCore(EquipSlot slot) { if (cores <= 0) return false; cores--; baseLevel[(int)slot]++; return true; }
        public void Apply(RunState run, bool win)
        {
            commonMaterials += run.areasCleared * 5 + (win ? 10 : 0);   // 進行度に応じた汎用素材
            if (win) cores += run.coresCollected;                       // 最終ボス撃破でコア獲得
        }
    }
}
