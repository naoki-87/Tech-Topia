using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/WindWalker")]
    public class WindWalkerPassive : PassiveBase
    {
        public float airJumpDiscount = 0.2f, windResist = 0.3f, windRegen = 4f;
        public override void Modify(ref PlayerStats s, RunState r, Tier t)
        {
            s.airJumpCostMul -= airJumpDiscount * T(t);
            s.windResist = Mathf.Min(0.9f, s.windResist + windResist * T(t));
            s.windRegen += windRegen * T(t);
        }
        public override string Describe(Tier t)
            => $"二段ジャンプのエネルギー-{airJumpDiscount * T(t) * 100f:0}%  風の阻害-{windResist * T(t) * 100f:0}%  風の中でエネルギー毎秒+{windRegen * T(t):0.#}";
    }
}