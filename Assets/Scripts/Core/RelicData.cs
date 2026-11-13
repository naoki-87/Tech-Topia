using UnityEngine;
namespace SteamEngineer
{
    public enum RelicEffect { MaxHp, MaxEnergy, EnergyRegen, Attack, DamageReduction, AirJumpCost, MoveSpeed, JumpPower, LowHpAttack, Greed, Revive, KillExplosion }

    // 遺物: その周回の間だけ有効なパッシブ。value / value2 の意味は効果ごとに決まっている
    [CreateAssetMenu(menuName = "SteamEngineer/Relic")]
    public class RelicData : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public RelicEffect effect;
        public float value, value2;
        public Sprite icon;
    }
}