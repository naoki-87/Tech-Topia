using System.Collections.Generic; using UnityEngine;
namespace SteamEngineer
{
    public class EquipState { public Tier tier; public int level; }

    public class RunState
    {
        public const int Slots = 4, StagesPerArea = 3;      // 展示用は3。変更していた場合は、その値にする
        const float PlayedWeight = 0.03f;                    // 遊んだステージの抽選の重み(通常は1)

        public EquipmentData[] equipped = new EquipmentData[Slots];
        public Tier[] tier = new Tier[Slots];
        public int[] level = new int[Slots];
        public List<AreaData> remaining, choices = new List<AreaData>();
        public List<GameObject> stageOrder = new List<GameObject>();
        public HashSet<GameObject> playedStages = new HashSet<GameObject>();
        public Dictionary<AreaData, int> visits = new Dictionary<AreaData, int>();
        public AreaData current;
        public int areasCleared, totalAreas, stageIndex, stagesCleared, rerolls = 1, coresCollected, hp;
        public bool won, swapped, reviveUsed;
        public Dictionary<(AreaData, Tier), int> mats = new Dictionary<(AreaData, Tier), int>();
        public Dictionary<EquipmentData, EquipState> stash = new Dictionary<EquipmentData, EquipState>();
        public Dictionary<PassiveBase, int> counters = new Dictionary<PassiveBase, int>();
        public List<RelicData> relics = new List<RelicData>(), relicOffers = new List<RelicData>();
        float matCarry;
        readonly SaveData save;

        public int Rank => save.rank;
        public bool IsBossStage => stageIndex == StagesPerArea - 1;
        public Tier MaxCraftTier => (Tier)Mathf.Min(4, save.rank + 1);
        // 進行度でドロップのティアが上がる(4エリアで 0→3)。解放済みの範囲に抑える
        public Tier DropTier => (Tier)Mathf.Min(Mathf.Min(4, areasCleared * 4 / Mathf.Max(1, totalAreas)), (int)MaxCraftTier);

        public RunState(AreaData[] all, EquipmentData[] starters, SaveData save, int[] startLevels = null, int visitsPerArea = 1,
                    System.Func<string, EquipmentData> find = null, bool keepTier = false)
        {
            this.save = save; remaining = new List<AreaData>();
            foreach (var a in all)
                if (a != null && a.requiredRank <= save.rank)
                    for (int v = 0; v < Mathf.Max(1, visitsPerArea); v++) remaining.Add(a);    // エリアを複数回使う
            totalAreas = remaining.Count;
            for (int i = 0; i < Slots; i++)
            {
                equipped[i] = starters[i]; tier[i] = Tier.Crude;
                int want = startLevels != null ? startLevels[i] : save.baseLevel[i];
                level[i] = Mathf.Clamp(want, 0, save.baseLevel[i]);
            }
            // 解放済みの装備: 装着の指定があれば装着し、ほかは持ち物へ(ガレージで切り替えられる)
            if (find != null)
                foreach (var o in save.owned)
                {
                    var e = find(o.id); if (e == null) continue;
                    int s = (int)e.slot;
                    var t = keepTier ? (Tier)Mathf.Min(o.tier, (int)MaxCraftTier) : Tier.Crude;
                    if (save.loadout[s] == o.id)
                    {
                        if (equipped[s] != null) stash[equipped[s]] = new EquipState { tier = tier[s], level = level[s] };   // 初期装備は持ち物へ
                        equipped[s] = e; tier[s] = t; swapped = true;
                    }
                    else stash[e] = new EquipState { tier = t, level = level[s] };
                }
            hp = PlayerStats.Build(this).maxHp;
        }

        // ---------- エリア選択 ----------
        public int Visits(AreaData a) => visits.TryGetValue(a, out var v) ? v : 0;
        public void RollChoices()
        {
            choices.Clear(); var pool = new List<AreaData>();
            foreach (var a in remaining) if (!pool.Contains(a)) pool.Add(a);          // 同じエリアは1枚にまとめる
            for (int i = 0; i < 3 && pool.Count > 0; i++) { int k = Random.Range(0, pool.Count); choices.Add(pool[k]); pool.RemoveAt(k); }
        }
        public bool Reroll() { if (rerolls <= 0) return false; rerolls--; RollChoices(); return true; }

        // 通常ステージの抽選: 遊んだステージは重みを大きく下げる。全部遊んだら記録をリセットする
        public void BeginArea(AreaData a)
        {
            current = a; stageIndex = 0; stageOrder.Clear();
            var pool = new List<GameObject>();
            foreach (var p in a.stagePrefabs) if (p != null && p != a.bossStagePrefab) pool.Add(p);
            if (pool.Count == 0) { Debug.LogError("Stage Prefabs が空です: " + a.name); return; }
            int need = StagesPerArea - 1;
            for (int n = 0; n < need; n++)
            {
                bool all = true; foreach (var p in pool) if (!playedStages.Contains(p)) { all = false; break; }
                if (all) foreach (var p in pool) playedStages.Remove(p);
                var w = new float[pool.Count]; float total = 0f;
                for (int i = 0; i < pool.Count; i++) { w[i] = playedStages.Contains(pool[i]) ? PlayedWeight : 1f; total += w[i]; }
                float r = Random.value * total; int pick = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++) { r -= w[i]; if (r <= 0f) { pick = i; break; } }
                stageOrder.Add(pool[pick]); playedStages.Add(pool[pick]);
            }
        }
        public GameObject CurrentStagePrefab => IsBossStage ? current.bossStagePrefab : stageOrder[stageIndex];
        public void CompleteArea() { visits[current] = Visits(current) + 1; remaining.Remove(current); current = null; areasCleared++; }

        // ---------- 素材 ----------
        public void AddStageMaterial(int n)
        {
            if (current == null) return;
            float bonus = RelicValue(RelicEffect.Greed);
            for (int i = 0; i < Slots; i++)
                if (equipped[i] != null && equipped[i].passive != null) bonus += equipped[i].passive.MatGainBonus(tier[i]);
            matCarry += n * (1f + bonus);
            int gain = Mathf.FloorToInt(matCarry + 0.0001f);
            matCarry = Mathf.Max(0f, matCarry - gain);
            if (gain <= 0) return;
            var k = (current, DropTier);
            mats[k] = (mats.TryGetValue(k, out var v) ? v : 0) + gain;
            var p = Object.FindFirstObjectByType<PlayerController>();
            for (int i = 0; i < Slots; i++)
                if (equipped[i] != null && equipped[i].passive != null) equipped[i].passive.OnMaterialGained(this, p, tier[i], gain);
        }
        public int Mat(AreaData a, Tier t) => mats.TryGetValue((a, t), out var v) ? v : 0;
        public int TotalMaterials() { int s = 0; foreach (var kv in mats) s += kv.Value; return s; }
        public int AttackBonus()
        {
            int b = 0;
            for (int i = 0; i < Slots; i++)
                if (equipped[i] != null && equipped[i].passive != null) b += equipped[i].passive.AttackBonus(this, tier[i]);
            return b;
        }
        public int EquipPower() { int p = 0; for (int i = 0; i < Slots; i++) p += 3 * (int)tier[i] + level[i]; return p; }

        public bool Fuse(AreaData a, Tier t)
        {
            if (t >= MaxCraftTier || Mat(a, t) < 3) return false;
            mats[(a, t)] -= 3; mats[(a, t + 1)] = Mat(a, t + 1) + 1; return true;
        }

        // 素材の分解: ステージで集めた素材 → 汎用素材(1個あたり 基準値×(ティア+1))
        public static int DisassembleYield(Tier t, int baseYield) => Mathf.Max(1, baseYield) * ((int)t + 1);
        public int Disassemble(AreaData a, Tier t, int count, int baseYield)
        {
            int have = Mat(a, t); count = Mathf.Min(count, have); if (count <= 0) return 0;
            mats[(a, t)] = have - count;
            int gain = DisassembleYield(t, baseYield) * count; save.commonMaterials += gain; return gain;
        }

        // ---------- 装備の作成・切り替え ----------
        public static int CommonCraft(EquipmentData e, Tier t) => e.commonCost * ((int)t + 1);
        public bool Owns(EquipmentData e)
        {
            if (e == null) return false;
            foreach (var q in equipped) if (q == e) return true;
            return stash.ContainsKey(e);
        }
        public bool CanCraft(EquipmentData e, Tier t) =>
            e != null && e.sourceArea != null && !Owns(e) && t <= MaxCraftTier &&
            Mat(e.sourceArea, t) >= e.coreCost && save.commonMaterials >= CommonCraft(e, t);
        public bool Craft(EquipmentData e, Tier t)
        {
            if (!CanCraft(e, t)) return false;
            mats[(e.sourceArea, t)] -= e.coreCost; save.commonMaterials -= CommonCraft(e, t);
            int i = (int)e.slot; Stow(i);
            equipped[i] = e; tier[i] = t; level[i] = 0; swapped = true;
            save.Unlock(e, t);
            ClampHp(); CheckRankUp(); return true;
        }
        void Stow(int i)
        {
            var cur = equipped[i];
            if (cur != null) stash[cur] = new EquipState { tier = tier[i], level = level[i] };
        }
        public bool Equip(EquipmentData e)
        {
            if (e == null || !stash.TryGetValue(e, out var st)) return false;
            int i = (int)e.slot; Stow(i); stash.Remove(e);
            equipped[i] = e; tier[i] = st.tier; level[i] = st.level; swapped = true;
            ClampHp(); return true;
        }
        public List<KeyValuePair<EquipmentData, EquipState>> StashList()
        {
            var l = new List<KeyValuePair<EquipmentData, EquipState>>(stash);
            l.Sort((a, b) => ((int)a.Key.slot).CompareTo((int)b.Key.slot)); return l;
        }

        // ---------- ティア強化 ----------
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
            tier[slot] = next; if (!e.isStarter) save.Unlock(e, next); ClampHp(); CheckRankUp(); return true;
        }

        // ---------- 工房ランク ----------
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

        public void Upgrade(EquipSlot s)
        {
            int i = (int)s; level[i]++;
            var e = equipped[i];
            if (e != null && e.isStarter && e.evolvesTo != null && !swapped && level[i] >= e.evolveLevel) equipped[i] = e.evolvesTo;
        }

        // ---------- 遺物 ----------
        public void OfferRelics(RelicData[] pool, int n = 3)
        {
            relicOffers.Clear(); if (pool == null) return;
            var cand = new List<RelicData>();
            foreach (var p in pool) if (p != null && !relics.Contains(p)) cand.Add(p);
            for (int i = 0; i < n && cand.Count > 0; i++) { int k = Random.Range(0, cand.Count); relicOffers.Add(cand[k]); cand.RemoveAt(k); }
        }
        public bool TakeRelic(RelicData r)
        {
            if (r == null || !relicOffers.Contains(r)) return false;
            relics.Add(r); relicOffers.Clear();
            if (r.effect == RelicEffect.MaxHp) hp += Mathf.RoundToInt(r.value);       // 最大HPが増えた分、HPも増やす
            return true;
        }
        public bool HasRelic(RelicEffect e) { foreach (var r in relics) if (r.effect == e) return true; return false; }
        public float RelicValue(RelicEffect e) { float s = 0f; foreach (var r in relics) if (r.effect == e) s += r.value; return s; }
        public float RelicValue2(RelicEffect e) { float s = 0f; foreach (var r in relics) if (r.effect == e) s += r.value2; return s; }

        public void Finish(bool win) { won = win; }
    }
}