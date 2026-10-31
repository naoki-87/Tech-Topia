using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Passive/FeatherFall")]
    public class FeatherFallPassive : PassiveBase
    {
        public float baseCap = 7f, minCap = 2.5f;       // 落下速度の上限(マス/秒)。小さいほどゆっくり落ちる
        float Cap(Tier t) => Mathf.Max(minCap, baseCap - (int)t);
        public override void Modify(ref PlayerStats s, RunState r, Tier t) { s.fallCap = Mathf.Min(s.fallCap, Cap(t)); }
        public override string Describe(Tier t) => $"風を受けて落下速度が最大{Cap(t):0.#}に抑えられる";
    }
}