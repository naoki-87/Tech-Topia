using UnityEngine;
namespace SteamEngineer
{
    // スチーム・ジャンプ: 足元から蒸気を噴射して跳び上がる。近くの敵にダメージ、蒸気ギミックを起動する
    [CreateAssetMenu(menuName = "SteamEngineer/Ability/SteamBoost")]
    public class SteamBoostAbility : AbilityBase
    {
        public float boostSpeed = 14f, radius = 2f, damageMul = 1f;
        public override void Activate(PlayerController p)
        {
            var v = p.Body.linearVelocity; v.y = boostSpeed; p.Body.linearVelocity = v;
            Vector2 foot = p.groundCheck.position;
            p.EmitElement(element, foot, radius);
            p.DamageAround(foot, radius, Mathf.Max(1, Mathf.RoundToInt(p.CurrentAttack * damageMul)));
        }
    }
}