using UnityEngine;
namespace SteamEngineer
{
    // 風の一撃: 前方に風を起こす。帆や風車などの風ギミックを起動する
    [CreateAssetMenu(menuName = "SteamEngineer/Ability/WindBlast")]
    public class WindBlastAbility : AbilityBase
    {
        public float reach = 1.5f, radius = 2.5f;
        public override void Activate(PlayerController p)
        {
            Vector2 center = (Vector2)p.attackPoint.position + Vector2.right * p.Facing * reach;
            p.EmitElement(element, center, radius);
        }
    }
}