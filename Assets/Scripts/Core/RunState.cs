using System.Collections.Generic; using UnityEngine;
namespace SteamEngineer
{
    public class RunState
    {
        public const int Slots = 4, StagesPerArea = 5;
        public EquipmentData[] equipped = new EquipmentData[Slots];
        public Tier[] tier = new Tier[Slots];                 // 部位ごとの現在ティア
        public int[] level = new int[Slots];
        public List<AreaData> remaining, choices = new List<AreaData>();
        public List<GameObject> stageOrder = new List<GameObject>();
        public AreaData current;
        public int areasCleared, stageIndex, rerolls = 1, coresCollected, hp;
        public bool won, swapped;
        public Dictionary<PassiveBase, int> counters = new Dictionary<PassiveBase, int>();
        float matCarry;      // 素材獲得量アップの端数
        public Dictionary<(AreaData, Tier), int> mats = new Dictionary<(AreaData, Tier), int>();
        readonly SaveData save;

        public int Rank => save.rank;
        public bool IsBossStage => stageIndex == StagesPerArea - 1;
        // 作成・強化・合成できる最大ティア = ランク+1
        public Tier MaxCraftTier => (Tier)Mathf.Min(4, save.rank + 1);
        // 進行度で決まるドロップティアを、解放済みの範囲に抑える
        public Tier DropTier => (Tier)Mathf.Min(Mathf.Min(4, areasCleared * 5 / 6), (int)MaxCraftTier);

        public RunState(AreaData[] all, EquipmentData[] starters, SaveData save, int[] startLevels = null)
        {
            this.save = save; remaining = new List<AreaData>();
            foreach (var a in all) if (a != null && a.requiredRank <= save.rank) remaining.Add(a);   // ランクで解放
            for (int i = 0; i < Slots; i++)
            {
                equipped[i] = starters[i]; tier[i] = Tier.Crude;
                int want = startLevels != null ? startLevels[i] : save.baseLevel[i];
                level[i] = Mathf.Clamp(want, 0, save.baseLevel[i]);
            }
            hp = PlayerStats.Build(this).maxHp;
        }

        // ---------- エリア選択 ----------
        public void RollChoices()
        {
            choices.Clear(); var pool = new List<AreaData>(remaining);
            for (int i = 0; i < 3 && pool.Count > 0; i++) { int k = Random.Range(0, pool.Count); choices.Add(pool[k]); pool.RemoveAt(k); }
        }
        public bool Reroll() { if (rerolls <= 0) return false; rerolls--; RollChoices(); return true; }

        public void BeginArea(AreaData a)
        {
            current = a; stageIndex = 0; stageOrder.Clear();
            var pool = new List<GameObject>();
            foreach (var p in a.stagePrefabs) if (p != null && p != a.bossStagePrefab) pool.Add(p);
            if (pool.Count == 0) { Debug.LogError("Stage Prefabs が空です: " + a.name); return; }
            int need = StagesPerArea - 1;
            while (stageOrder.Count < need)
            {
                var bag = new List<GameObject>(pool);
                for (int i = bag.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (bag[i], bag[j]) = (bag[j], bag[i]); }
                if (stageOrder.Count > 0 && bag.Count > 1 && bag[0] == stageOrder[stageOrder.Count - 1]) (bag[0], bag[1]) = (bag[1], bag[0]);
                foreach (var b in bag) { if (stageOrder.Count >= need) break; stageOrder.Add(b); }
            }
        }
        public GameObject CurrentStagePrefab => IsBossStage ? current.bossStagePrefab : stageOrder[stageIndex];
        public void CompleteArea() { remaining.Remove(current); current = null; areasCleared++; }

        // ---------- 素材 ----------
        public void AddStageMaterial(int n)
        {
            if (current == null) return;
            float bonus = 0f;
            for (int i = 0; i < Slots; i++)
                if (equipped[i] != null && equipped[i].passive != null) bonus += equipped[i].passive.MatGainBonus(tier[i]);
            matCarry += n * (1f + bonus);                                   // 端数は持ち越す
            int gain = Mathf.FloorToInt(matCarry + 0.0001f);
            matCarry = Mathf.Max(0f, matCarry - gain);
            if (gain <= 0) return;

            var k = (current, DropTier);
            mats[k] = (mats.TryGetValue(k, out var v) ? v : 0) + gain;

            var p = Object.FindFirstObjectByType<PlayerController>();      // ステージ中のみ存在する
            for (int i = 0; i < Slots; i++)
                if (equipped[i] != null && equipped[i].passive != null) equipped[i].passive.OnMaterialGained(this, p, tier[i], gain);
        }
        public int TotalMaterials() { int s = 0; foreach (var kv in mats) s += kv.Value; return s; }
        public int AttackBonus()
        {
            int b = 0;
            for (int i = 0; i < Slots; i++)
                if (equipped[i] != null && equipped[i].passive != null) b += equipped[i].passive.AttackBonus(this, tier[i]);
            return b;
        }
        public int Mat(AreaData a, Tier t) => mats.TryGetValue((a, t), out var v) ? v : 0;

        // 同種素材3つ → 1段階上位の素材(解放済みティアまで)
        public bool Fuse(AreaData a, Tier t)
        {
            if (t >= MaxCraftTier || Mat(a, t) < 3) return false;
            mats[(a, t)] -= 3; mats[(a, t + 1)] = Mat(a, t + 1) + 1; return true;
        }

        // ---------- 装備の作成 ----------
        public static int CommonCraft(EquipmentData e, Tier t) => e.commonCost * ((int)t + 1);
        public bool CanCraft(EquipmentData e, Tier t) =>
            e != null && e.sourceArea != null && t <= MaxCraftTier &&
            Mat(e.sourceArea, t) >= e.coreCost && save.commonMaterials >= CommonCraft(e, t);
        public bool Craft(EquipmentData e, Tier t)
        {
            if (!CanCraft(e, t)) return false;
            mats[(e.sourceArea, t)] -= e.coreCost; save.commonMaterials -= CommonCraft(e, t);
            int i = (int)e.slot; equipped[i] = e; tier[i] = t; level[i] = 0; swapped = true;
            ClampHp(); CheckRankUp(); return true;
        }

        // ---------- 装備のティア強化(下位装備 + 上位素材) ----------
        public int UpgradeCore(EquipmentData e) => Mathf.Max(1, e.coreCost - 1);
        public static int CommonUpgrade(EquipmentData e, Tier next) => Mathf.Max(1, e.commonCost * ((int)next + 1) / 2);
        public bool CanUpgradeTier(int slot)
        {
            var e = equipped[slot];
            if (e == null || e.sourceArea == null || tier[slot] == Tier.Supreme) return false;
            var next = tier[slot] + 1; if (next > MaxCraftTier) return false;
            return Mat(e.sourceArea, next) >= UpgradeCore(e) && save.commonMaterials >= CommonUpgrade(e, next);
        }
        public bool UpgradeTier(int slot)
        {
            if (!CanUpgradeTier(slot)) return false;
            var e = equipped[slot]; var next = tier[slot] + 1;
            mats[(e.sourceArea, next)] -= UpgradeCore(e); save.commonMaterials -= CommonUpgrade(e, next);
            tier[slot] = next; ClampHp(); CheckRankUp(); return true;
        }

        // ---------- 工房ランク ----------
        // 「次のティア」の装備を4部位そろえた時にランクアップ(例: 凡作一式 → 職人)
        void CheckRankUp()
        {
            bool up = false;
            while (save.rank < 4)
            {
                bool all = true;
                for (int i = 0; i < Slots; i++) if ((int)tier[i] < save.rank + 1) { all = false; break; }
                if (!all) break;
                save.rank++; up = true;
            }
            if (up) save.Write();
        }
        void ClampHp() { hp = Mathf.Min(hp, PlayerStats.Build(this).maxHp); }

        // ---------- レベル強化(ガレージの3択) ----------
        public void Upgrade(EquipSlot s)
        {
            int i = (int)s; level[i]++;
            var e = equipped[i];   // 初期装備を交換せず十分強化すると進化
            if (e != null && e.isStarter && e.evolvesTo != null && !swapped && level[i] >= e.evolveLevel) equipped[i] = e.evolvesTo;
        }
        public void Finish(bool win) { won = win; }
    }
}