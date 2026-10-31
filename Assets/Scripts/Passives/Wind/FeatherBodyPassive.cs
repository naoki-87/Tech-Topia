using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/FeatherBody")]
    public class FeatherBodyPassive : PassiveBase
    {
        public float rangedReduction = 0.25f, pushRadius = 2.5f, pushForce = 9f, pushStun = 0.6f, cooldown = 6f;
        public override void Modify(ref PlayerStats s, RunState r, Tier t)
        {
            s.rangedReduction = Mathf.Min(0.8f, s.rangedReduction + rangedReduction * T(t));
            s.pushRadius = pushRadius; s.pushForce = pushForce; s.pushStun = pushStun;
            s.pushCooldown = Mathf.Max(1.5f, cooldown - (int)t);
        }
        public override string Describe(Tier t)
            => $"‰“‹——£UŒ‚‚Ìƒ_ƒ[ƒW-{rangedReduction * T(t) * 100f:0}%  ‹ßÚ‚Å”í’e‚·‚é‚ÆüˆÍ‚Ì“G‚ğ‚«”ò‚Î‚·(CT{Mathf.Max(1.5f, cooldown - (int)t):0.#}•b)";
    }
}