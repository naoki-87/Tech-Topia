using System.Collections; using UnityEngine;
namespace SteamEngineer
{
    // ダメージを受けたあとの無敵時間の間、キャラクターを点滅させる
    [RequireComponent(typeof(Health))]
    public class InvincibleBlink : MonoBehaviour
    {
        public float interval = 0.07f;      // 点滅の速さ(小さいほど速い)
        SpriteRenderer[] rends; Health hp; Coroutine co;

        void Awake()
        {
            rends = GetComponentsInChildren<SpriteRenderer>();
            hp = GetComponent<Health>(); hp.Damaged += Begin;
        }
        void OnDestroy() { if (hp) hp.Damaged -= Begin; }

        void Begin()
        {
            if (hp.invulnTime <= 0.05f) return;
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(Blink(hp.invulnTime));
        }
        IEnumerator Blink(float duration)
        {
            float t = 0f; bool visible = false;
            while (t < duration)
            {
                visible = !visible; Set(visible);
                yield return new WaitForSeconds(interval); t += interval;
            }
            Set(true); co = null;
        }
        void Set(bool v) { foreach (var r in rends) if (r) r.enabled = v; }
    }
}