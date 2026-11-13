using UnityEngine;
namespace SteamEngineer
{
    public struct PlayerStats
    {
        public int maxHp, attack, extraJumps;
        public float dmgReduction, dashCooldown, maxEnergy, energyRegen, energyCostMul;
        public float jumpMul, moveMul, airJumpCostMul, fallCap, attackBoxMul;      // 移動まわり
        public float windResist, windRegen;                                         // 風
        public float rangedReduction, pushRadius, pushForce, pushStun, pushCooldown; // 胴体の特性
        public bool setBonus;

        public static float TierMul(Tier t) => 1f + 0.25f * (int)t;     // 粗雑1.0 凡作1.25 洗練1.5 傑作1.75 至高2.0

        public static PlayerStats Build(RunState r)
        {
            var s = new PlayerStats
            {
                maxHp = 5, attack = 1, dashCooldown = 0.4f, maxEnergy = 100f, energyRegen = 20f, energyCostMul = 1f,
                jumpMul = 1f, moveMul = 1f, airJumpCostMul = 1f, fallCap = 99f, attackBoxMul = 1f
            };
            float levelMul = 1f + 0.2f * r.Rank;                         // 工房ランクで、強化の上がり幅が増える
            for (int i = 0; i < RunState.Slots; i++)
            {
                var e = r.equipped[i]; if (e == null) continue;
                float m = TierMul(r.tier[i]); float lv = r.level[i] * levelMul;
                s.maxHp += Mathf.RoundToInt((e.hpBase + e.hpPerLevel * lv) * m);
                s.dmgReduction += (e.dmgRedBase + e.dmgRedPerLevel * lv) * m;
                s.attack += Mathf.RoundToInt((e.attackBase + e.attackPerLevel * lv) * m);
                s.extraJumps += e.extraJumps;
                s.maxEnergy += (e.energyMaxBase + e.energyMaxPerLevel * lv) * m;
                s.energyRegen += (e.regenBase + e.regenPerLevel * lv) * m;
                s.energyCostMul -= e.costReducePerLevel * lv;
                s.jumpMul += e.jumpBonus; s.moveMul += e.moveBonus;
                if (e.passive != null) e.passive.Modify(ref s, r, r.tier[i]);
            }
            foreach (var rl in r.relics)
            switch (rl.effect)
            {
                case RelicEffect.MaxHp: s.maxHp += Mathf.RoundToInt(rl.value); break;
                case RelicEffect.MaxEnergy: s.maxEnergy += rl.value; break;
                case RelicEffect.EnergyRegen: s.energyRegen += rl.value; break;
                case RelicEffect.Attack: s.attack += Mathf.RoundToInt(rl.value); break;
                case RelicEffect.DamageReduction: s.dmgReduction += rl.value; break;
                case RelicEffect.AirJumpCost: s.airJumpCostMul -= rl.value; break;
                case RelicEffect.MoveSpeed: s.moveMul += rl.value; break;
                case RelicEffect.JumpPower: s.jumpMul += rl.value; break;
            }
            // 装備統一ボーナス: 4部位すべてが同じエリアの装備(初期装備は対象外)
            AreaData area = null; bool set = true;
            for (int i = 0; i < RunState.Slots; i++)
            {
                var e = r.equipped[i];
                if (e == null || e.isStarter || e.sourceArea == null) { set = false; break; }
                if (area == null) area = e.sourceArea; else if (e.sourceArea != area) { set = false; break; }
            }
            if (set) { s.setBonus = true; s.maxHp += 1; s.attack += 1; s.maxEnergy += 10f; s.energyRegen += 3f; }

            s.dmgReduction = Mathf.Clamp(s.dmgReduction, 0f, 0.75f);
            s.energyCostMul = Mathf.Clamp(s.energyCostMul, 0.4f, 1f);
            s.airJumpCostMul = Mathf.Clamp(s.airJumpCostMul, 0.3f, 1f);
            return s;
        }
    }
}