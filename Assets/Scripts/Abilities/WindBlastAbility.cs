using UnityEngine;
namespace SteamEngineer
{
    // ウィンド・ブラスト: 前方に風を起こして敵を吹き飛ばす。風ギミックを起動する
    [CreateAssetMenu(menuName = "SteamEngineer/Ability/WindBlast")]
    public class WindBlastAbility : AbilityBase
    {
        public float reach = 2f, radius = 3f, force = 12f, stun = 0.5f, damageMul = 0.5f;
        public override void Activate(PlayerController p)
        {
            Vector2 center = (Vector2)p.attackPoint.position + Vector2.right * p.Facing * reach;
            p.EmitElement(element, center, radius);
            foreach (var c in Physics2D.OverlapCircleAll(center, radius))
            {
                if (!c.TryGetComponent(out EnemyBase e)) continue;
                e.Push(new Vector2(p.Facing, 0.2f).normalized, force, stun);
                if (c.TryGetComponent(out Health h)) h.Damage(Mathf.Max(1, Mathf.RoundToInt(p.CurrentAttack * damageMul)));
            }
        }
    }
}