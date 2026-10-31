using System.Collections; using UnityEngine;
namespace SteamEngineer
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossController : EnemyBase
    {
        public Projectile steamShot; public Transform muzzle; public float leapSpeed = 5f, leapPower = 12f;
        Rigidbody2D rb;
        protected override void Awake() { base.Awake(); rb = GetComponent<Rigidbody2D>(); rb.freezeRotation = true; }
        protected override void Start() { base.Start(); StartCoroutine(Brain()); }
        IEnumerator Brain()
        {
            yield return new WaitForSeconds(1f);
            while (!hp.IsDead)
            {
                bool enraged = hp.Current <= hp.max / 2;           // HP半分で行動が速く・弾数増加
                float rest = enraged ? 0.4f : 0.9f;
                float dir = player && player.position.x < transform.position.x ? -1 : 1;
                rb.linearVelocity = new Vector2(dir * leapSpeed, leapPower);           // ジャンプ攻撃
                yield return new WaitForSeconds(1.1f);
                rb.linearVelocity = Vector2.zero; yield return new WaitForSeconds(rest);
                int n = enraged ? 5 : 3;                                                // 蒸気弾の扇状射撃
                for (int i = 0; i < n; i++)
                {
                    var b = Instantiate(steamShot, muzzle.position, Quaternion.identity);
                    float a = (i - (n - 1) / 2f) * 0.25f;
                    b.Launch(new Vector2(dir * Mathf.Cos(a), Mathf.Sin(a)) * 5f);
                }
                yield return new WaitForSeconds(rest + 0.4f);
            }
        }
        protected override void OnDied()
        {
            var gm = GameManager.Instance; gm.Run.AddStageMaterial(dropAmount * 5); gm.Run.coresCollected++;
            gm.Invoke(nameof(GameManager.OnStageClear), 1.5f); Destroy(gameObject);
        }
        public override void Push(Vector2 dir, float force, float stun) { }
    }
}
