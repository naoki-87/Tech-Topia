using System.Collections; using UnityEngine;
namespace SteamEngineer
{
    // ‰_‚Ì‘«ê: æ‚é‚Æ stayTime •bŒã‚ÉÁ‚¦ArespawnTime •bŒã‚É•œŠˆ‚·‚é
    public class CloudPlatform : MonoBehaviour
    {
        public float stayTime = 0.8f, respawnTime = 3f; public SpriteRenderer sr;
        Collider2D col; bool busy;
        void Awake() { col = GetComponent<Collider2D>(); if (!sr) sr = GetComponentInChildren<SpriteRenderer>(); }
        void OnCollisionEnter2D(Collision2D c)
        {
            if (busy || !c.collider.TryGetComponent(out PlayerController p)) return;
            if (p.transform.position.y > transform.position.y) StartCoroutine(Fall());   // ã‚Éæ‚Á‚½‚Æ‚«‚¾‚¯
        }
        IEnumerator Fall()
        {
            busy = true;
            float t = 0f;
            while (t < stayTime) { t += Time.deltaTime; SetAlpha(1f - 0.5f * t / stayTime); yield return null; }
            col.enabled = false; SetAlpha(0.15f);
            yield return new WaitForSeconds(respawnTime);
            col.enabled = true; SetAlpha(1f); busy = false;
        }
        void SetAlpha(float a) { if (!sr) return; var c = sr.color; c.a = a; sr.color = c; }
    }
}