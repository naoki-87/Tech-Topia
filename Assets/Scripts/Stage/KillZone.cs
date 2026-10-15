using UnityEngine;
namespace SteamEngineer
{
    // ステージ下に置く Trigger。落ちたらダメージを与えて PlayerSpawn に戻す
    public class KillZone : MonoBehaviour
    {
        public int damage = 2;
        void OnTriggerEnter2D(Collider2D c)
        {
            if (!c.TryGetComponent(out PlayerController p)) return;
            p.TakeHit(damage, transform.position);
            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn) p.transform.position = spawn.transform.position;
            if (p.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
        }
    }
}