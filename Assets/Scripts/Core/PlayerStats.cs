using UnityEngine;
namespace SteamEngineer
{
    public struct PlayerStats
    {
        public int maxHp, attack, extraJumps;
        public float dmgReduction, dashCooldown, maxEnergy, energyRegen, energyCostMul;

        public static PlayerStats Build(RunState r)
        {
            var s = new PlayerStats { maxHp = 5, attack = 1, dashCooldown = 0.4f, maxEnergy = 100f, energyRegen = 20f, energyCostMul = 1f };
            for (int i = 0; i < RunState.Slots; i++)
            {
                var e = r.equipped[i]; if (e == null) continue;
                float m = 1f + 0.25f * (int)e.tier; int lv = r.level[i];      // ƒeƒBƒA”{—¦
                s.maxHp += Mathf.RoundToInt((e.hpBase + e.hpPerLevel * lv) * m);
                s.dmgReduction += (e.dmgRedBase + e.dmgRedPerLevel * lv) * m;
                s.attack += Mathf.RoundToInt((e.attackBase + e.attackPerLevel * lv) * m);
                s.extraJumps += e.extraJumps;
                s.maxEnergy += (e.energyMaxBase + e.energyMaxPerLevel * lv) * m;
                s.energyRegen += (e.regenBase + e.regenPerLevel * lv) * m;
                s.energyCostMul -= e.costReducePerLevel * lv;
            }
            s.dmgReduction = Mathf.Clamp(s.dmgReduction, 0f, 0.75f);
            s.energyCostMul = Mathf.Clamp(s.energyCostMul, 0.4f, 1f);       // Á”ï‚ÍÅ‘å‚Å‚à6Š„Œ¸‚Ü‚Å
            return s;
        }
    }
}