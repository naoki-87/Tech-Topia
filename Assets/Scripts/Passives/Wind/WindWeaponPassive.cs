using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/WindWeapon")]
    public class WindWeaponPassive : PassiveBase
    {
        public float areaBase = 0.3f;
        public override void Modify(ref PlayerStats s, RunState r, Tier t) { s.attackBoxMul = Mathf.Max(s.attackBoxMul, 1f + areaBase * T(t)); }
        public override string Describe(Tier t) => $"•—‚ğ‚Ü‚Æ‚¢A’ÊíUŒ‚‚Ì”ÍˆÍ‚ª{areaBase * T(t) * 100f:0}%L‚ª‚é";
    }
}