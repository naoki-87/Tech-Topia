using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/AttackByMaterial")]
    public class AttackByMaterialPassive : PassiveBase
    {
        public float perMaterials = 15f; public int maxBonus = 6;
        public override int AttackBonus(RunState r, Tier t)
            => Mathf.Min(maxBonus, Mathf.FloorToInt(r.TotalMaterials() * T(t) / perMaterials));
        public override string Describe(Tier t)
            => $"Š‘fŞ{perMaterials / T(t):0.#}ŒÂ‚²‚Æ‚ÉUŒ‚—Í+1(Å‘å+{maxBonus})  Œ»İ+{AttackBonus(GameManager.Instance.Run, t)}";
    }
}