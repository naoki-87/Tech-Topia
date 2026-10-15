using UnityEngine;
namespace SteamEngineer
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        public int contactDamage = 2, dropAmount = 1;
        protected Health hp; protected Transform player;
        protected virtual void Awake() { hp = GetComponent<Health>(); hp.Died += OnDied; }
        protected virtual void Start() { var p = FindFirstObjectByType<PlayerController>(); if (p) player = p.transform; }
        protected virtual void OnDied() { GameManager.Instance.Run.AddStageMaterial(dropAmount); Destroy(gameObject); }
        void OnCollisionStay2D(Collision2D c) => Touch(c.collider);
        void OnTriggerStay2D(Collider2D c) => Touch(c);
        void Touch(Collider2D c) { if (c.TryGetComponent(out PlayerController p)) p.TakeHit(contactDamage, transform.position); }
    }
}
