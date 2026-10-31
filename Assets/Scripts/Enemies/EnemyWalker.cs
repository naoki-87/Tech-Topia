using UnityEngine;
namespace SteamEngineer
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyWalker : EnemyBase
    {
        public float speed = 1.5f; public LayerMask groundMask; public float edgeCheck = 0.6f;
        Rigidbody2D rb; int dir = -1;
        protected override void Awake() { base.Awake(); rb = GetComponent<Rigidbody2D>(); rb.freezeRotation = true; }
        void FixedUpdate()
        {
            if (Stunned) return;
            Vector2 p = transform.position;
            bool floor = Physics2D.Raycast(p + new Vector2(dir * edgeCheck, 0), Vector2.down, 1.2f, groundMask);   // 足場の端
            bool wall = Physics2D.Raycast(p, Vector2.right * dir, edgeCheck + 0.1f, groundMask);
            if (!floor || wall) dir = -dir;
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            transform.localScale = new Vector3(-dir, 1, 1);
        }
    }
}
