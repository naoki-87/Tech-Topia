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
            var col = Fx.ElementColor(element);
            Fx.Ring(center, radius, col);                                                      // 効果範囲(判定と同じ半径)
            Fx.WindStreaks(center, p.Facing, radius * 2f, radius * 1.4f, new Color(col.r, col.g, col.b, 0.9f));
            Fx.Puffs(center - Vector2.right * p.Facing * radius * 0.6f, col, 6, 5f, Vector2.right * p.Facing, 0.22f, 0.4f);
            p.EmitElement(element, center, radius);
            foreach (var c in Physics2D.OverlapCircleAll(center, radius))
            {
                if (!c.TryGetComponent(out EnemyBase e)) continue;
                e.Push(new Vector2(p.Facing, 0.2f).normalized, force, stun);
                if (c.TryGetComponent(out Health h)) { h.Damage(Mathf.Max(1, Mathf.RoundToInt(p.CurrentAttack * damageMul))); Fx.Hit(c.bounds.center); }
            }
        }
    }
}