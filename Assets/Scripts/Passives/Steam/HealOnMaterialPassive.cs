using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/HealOnMaterial")]
    public class HealOnMaterialPassive : PassiveBase
    {
        public int needBase = 12, minNeed = 4, healAmount = 1;
        int Need(Tier t) => Mathf.Max(minNeed, needBase - 2 * (int)t);
        public override void OnMaterialGained(RunState r, PlayerController p, Tier t, int gained)
        {
            int times = Consume(r, gained, Need(t));
            if (times <= 0 || p == null) return;
            p.Health.Heal(times * healAmount); p.SyncHp();
            Debug.Log($"[“Á«] ì‹Æ’…: HP‰ñ•œ +{times * healAmount}");
        }
        public override string Describe(Tier t) => $"‘fŞ{Need(t)}ŒÂŠl“¾‚²‚Æ‚ÉHP{healAmount}‰ñ•œ";
    }
}