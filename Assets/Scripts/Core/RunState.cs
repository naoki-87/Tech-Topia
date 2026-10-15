using System.Collections.Generic; using UnityEngine;
namespace SteamEngineer
{
    public class RunState
    {
        public const int Slots = 4, StagesPerArea = 2;
        public EquipmentData[] equipped = new EquipmentData[Slots];
        public int[] level = new int[Slots];
        public List<AreaData> remaining, choices = new List<AreaData>();
        public AreaData current;
        public int areasCleared, stageIndex, rerolls = 1, coresCollected, hp;
        public bool won, swapped;
        public Dictionary<(AreaData, Tier), int> mats = new Dictionary<(AreaData, Tier), int>();
        readonly SaveData save;
        public bool IsBossStage => stageIndex == StagesPerArea - 1;
        // 進行度が上がるほど高ティア素材が出る(0,0,1,2,3,4)
        public Tier DropTier => (Tier)Mathf.Min(4, areasCleared * 5 / 6);

        public RunState(AreaData[] all, EquipmentData[] starters, SaveData save, int[] startLevels = null)
        {
            this.save = save; remaining = new List<AreaData>(all);
            for (int i = 0; i < Slots; i++)
            {
                equipped[i] = starters[i];
                int want = startLevels != null ? startLevels[i] : save.baseLevel[i];   // 縛りプレイ: 基礎強化の範囲内で下げられる
                level[i] = Mathf.Clamp(want, 0, save.baseLevel[i]);
            }
            hp = PlayerStats.Build(this).maxHp;
        }
        public void RollChoices()
        {
            choices.Clear(); var pool = new List<AreaData>(remaining);
            for (int i = 0; i < 3 && pool.Count > 0; i++) { int k = Random.Range(0, pool.Count); choices.Add(pool[k]); pool.RemoveAt(k); }
        }
        public bool Reroll() { if (rerolls <= 0) return false; rerolls--; RollChoices(); return true; }
        public void BeginArea(AreaData a) { current = a; stageIndex = 0; }
        public void CompleteArea() { remaining.Remove(current); current = null; areasCleared++; }

        public void AddStageMaterial(int n)
        {
            if (current == null) return; var k = (current, DropTier);
            mats[k] = (mats.TryGetValue(k, out var v) ? v : 0) + n;
        }
        public int Mat(AreaData a, Tier t) => mats.TryGetValue((a, t), out var v) ? v : 0;

        // 同種素材3つ → 1段階上位の素材
        public bool Fuse(AreaData a, Tier t)
        {
            if (t == Tier.Supreme || Mat(a, t) < 3) return false;
            mats[(a, t)] -= 3; mats[(a, t + 1)] = Mat(a, t + 1) + 1; return true;
        }
        public bool CanCraft(EquipmentData e) => Mat(e.sourceArea, e.tier) >= e.coreCost && save.commonMaterials >= e.commonCost;
        public bool Craft(EquipmentData e)
        {
            if (!CanCraft(e)) return false;
            mats[(e.sourceArea, e.tier)] -= e.coreCost; save.commonMaterials -= e.commonCost;
            equipped[(int)e.slot] = e; level[(int)e.slot] = 0; swapped = true; return true;
        }
        public void Upgrade(EquipSlot s)
        {
            int i = (int)s; level[i]++;
            var e = equipped[i];   // 初期装備を交換せず十分強化すると進化
            if (e != null && e.isStarter && e.evolvesTo != null && !swapped && level[i] >= e.evolveLevel) equipped[i] = e.evolvesTo;
        }
        public void Finish(bool win) { won = win; }
    }
}
