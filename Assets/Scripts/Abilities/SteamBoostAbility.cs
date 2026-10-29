using UnityEngine;
namespace SteamEngineer
{
    // 蒸気ブースト: 足元から蒸気を噴き出して跳ね上がる。足元の蒸気ギミックも起動する
    [CreateAssetMenu(menuName = "SteamEngineer/Ability/SteamBoost")]
    public class SteamBoostAbility : AbilityBase
    {
        public float boostSpeed = 14f, radius = 1.5f;
        public override void Activate(PlayerController p)
        {
            var v = p.Body.linearVelocity; v.y = boostSpeed; p.Body.linearVelocity = v;
            p.EmitElement(element, p.groundCheck.position, radius);
        }
    }
}