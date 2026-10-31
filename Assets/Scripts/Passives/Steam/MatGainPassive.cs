using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/MaterialGain")]
    public class MatGainPassive : PassiveBase
    {
        public float bonus = 0.15f;                                    // ‘eG‚Å‚Ì‘‰Á—¦
        public override float MatGainBonus(Tier t) => bonus * T(t);
        public override string Describe(Tier t) => $"‘fŞŠl“¾—Ê +{MatGainBonus(t) * 100f:0}%";
    }
}