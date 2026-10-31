using UnityEngine;
namespace SteamEngineer
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        public int damage = 2; public float life = 4f; public LayerMask groundMask;
        public void Launch(Vector2 vel) { GetComponent<Rigidbody2D>().linearVelocity = vel; Destroy(gameObject, life); }
        void OnTriggerEnter2D(Collider2D c)
        {
            if (c.TryGetComponent(out PlayerController p)) { p.TakeHit(damage, transform.position, true); Destroy(gameObject); }
            else if (((1 << c.gameObject.layer) & groundMask) != 0) Destroy(gameObject);
        }
    }
}
