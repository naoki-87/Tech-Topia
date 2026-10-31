using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/EnergyOnMaterial")]
    public class EnergyOnMaterialPassive : PassiveBase
    {
        public int needBase = 6, minNeed = 2; public float restore = 20f;
        int Need(Tier t) => Mathf.Max(minNeed, needBase - (int)t);
        public override void OnMaterialGained(RunState r, PlayerController p, Tier t, int gained)
        {
            int times = Consume(r, gained, Need(t));
            if (times <= 0 || p == null) return;
            var en = p.GetComponent<Energy>(); if (en == null) return;
            en.Restore(times * restore * T(t));
            Debug.Log($"[“Á«] ˆÀ‘SŒC: ƒGƒlƒ‹ƒM[‰ñ•œ +{times * restore * T(t):0}");
        }
        public override string Describe(Tier t) => $"‘fŞ{Need(t)}ŒÂŠl“¾‚²‚Æ‚ÉƒGƒlƒ‹ƒM[{restore * T(t):0}‰ñ•œ";
    }
}