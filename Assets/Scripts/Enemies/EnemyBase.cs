using System.Collections; using UnityEngine;
namespace SteamEngineer
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        public int contactDamage = 2, dropAmount = 1;
        protected Health hp; protected Transform player;
        float stunUntil;
        public bool Stunned => Time.time < stunUntil;

        protected virtual void Awake() { hp = GetComponent<Health>(); hp.Died += OnDied; }
        protected virtual void Start()
        {
            var p = FindFirstObjectByType<PlayerController>(); if (p) player = p.transform;
            var gm = GameManager.Instance; if (gm != null) hp.ScaleMax(gm.EnemyHpMul);
        }
        protected virtual void OnDied()
        {
            AudioManager.Kill();
            var run = GameManager.Instance.Run;
            run.AddStageMaterial(dropAmount);
            float boom = run.RelicValue(RelicEffect.KillExplosion);                    // 蒸気爆発
            if (boom > 0f)
                foreach (var c in Physics2D.OverlapCircleAll(transform.position, 2.5f))
                    if (c.gameObject != gameObject && c.GetComponent<EnemyBase>() != null && c.TryGetComponent(out Health h))
                        h.Damage(Mathf.Max(1, Mathf.RoundToInt(boom)));
            Destroy(gameObject);
        }

        // 吹き飛ばし(風・胴体の特性などで使う)。ボスは上書きして無効にする
        public virtual void Push(Vector2 dir, float force, float stun)
        {
            stunUntil = Time.time + stun;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = dir * force;
            else StartCoroutine(Slide(dir * force, stun));
        }
        IEnumerator Slide(Vector2 v, float t)
        {
            float e = 0f;
            while (e < t) { e += Time.deltaTime; transform.position += (Vector3)(v * Time.deltaTime * (1f - e / t)); yield return null; }
        }
    }
}