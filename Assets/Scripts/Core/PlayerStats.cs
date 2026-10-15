using UnityEngine;
namespace SteamEngineer
{
    public struct PlayerStats
    {
        public int maxHp, attack, extraJumps; public float dmgReduction, dashCooldown;
        public static PlayerStats Build(RunState r)
        {
            var s = new PlayerStats { maxHp = 5, attack = 1, dashCooldown = 0.8f };
            for (int i = 0; i < RunState.Slots; i++)
            {
                var e = r.equipped[i]; if (e == null) continue;
                float m = 1f + 0.25f * (int)e.tier; int lv = r.level[i];
                s.maxHp += Mathf.RoundToInt((e.hpBase + e.hpPerLevel * lv) * m);
                s.dmgReduction += (e.dmgRedBase + e.dmgRedPerLevel * lv) * m;
                s.attack += Mathf.RoundToInt((e.attackBase + e.attackPerLevel * lv) * m);
                s.extraJumps += e.extraJumps;
                s.dashCooldown -= e.dashCdReductionPerLevel * lv;
            }
            s.dmgReduction = Mathf.Clamp(s.dmgReduction, 0f, 0.75f);
            s.dashCooldown = Mathf.Max(0.25f, s.dashCooldown);
            return s;
        }
    }
}
