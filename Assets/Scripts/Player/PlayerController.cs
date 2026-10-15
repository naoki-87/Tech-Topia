using UnityEngine; using UnityEngine.InputSystem;
namespace SteamEngineer
{
    // Unity 6 + Input System。Player prefab: Rigidbody2D / Collider2D / Health / このスクリプト
    // 子オブジェクト: GroundCheck(足元), AttackPoint(前方)。Layerは Inspector で設定。
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public LayerMask groundMask, hitMask;     // hitMask: 敵 + 修理ギミック
        public Transform groundCheck, attackPoint;
        public Vector2 groundBox = new Vector2(0.5f, 0.1f), attackBox = new Vector2(1.2f, 0.9f);
        public float moveSpeed = 6f, jumpSpeed = 12f, gravity = 3f, dashSpeed = 16f, dashTime = 0.15f;
        public float coyoteTime = 0.1f, jumpBuffer = 0.12f, attackCooldown = 0.25f, hurtInvuln = 1f;
        public Health Health { get; private set; } public PlayerStats Stats { get; private set; }
        Rigidbody2D rb; int facing = 1, jumpsUsed; float coyote, buffer, dashT, dashCd, atkCd; bool grounded, dashing;

        void Awake() { rb = GetComponent<Rigidbody2D>(); Health = GetComponent<Health>(); rb.gravityScale = gravity; rb.freezeRotation = true; }
        void Start()
        {
            var run = GameManager.Instance.Run; Stats = PlayerStats.Build(run);
            Health.invulnTime = hurtInvuln; Health.Set(run.hp, Stats.maxHp);
            Health.damageModifier = d => Mathf.Max(1, Mathf.RoundToInt(d * (1f - Stats.dmgReduction)));
            Health.Died += () => GameManager.Instance.OnPlayerDead();
        }
        void Update()
        {
            var kb = Keyboard.current; if (kb == null || Health.IsDead) return;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            bool jumpDown = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            bool jumpUp = kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame;
            bool dash = kb.leftShiftKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame;
            bool atk = kb.jKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame;
            float dt = Time.deltaTime;

            grounded = Physics2D.OverlapBox(groundCheck.position, groundBox, 0, groundMask);
            if (grounded) { coyote = coyoteTime; jumpsUsed = 0; } else coyote -= dt;      // コヨーテタイム
            buffer = jumpDown ? jumpBuffer : buffer - dt;                                  // 先行入力
            dashCd -= dt; atkCd -= dt;
            if (x != 0) { facing = x > 0 ? 1 : -1; transform.localScale = new Vector3(facing, 1, 1); }

            if (dash && dashCd <= 0 && !dashing) { dashing = true; dashT = dashTime; dashCd = Stats.dashCooldown; rb.gravityScale = 0; Health.GrantInvuln(dashTime); }
            if (dashing)
            {
                dashT -= dt; rb.linearVelocity = new Vector2(facing * dashSpeed, 0);
                if (dashT <= 0) { dashing = false; rb.gravityScale = gravity; }
            }
            else
            {
                var v = rb.linearVelocity; v.x = x * moveSpeed;
                if (buffer > 0)
                {
                    if (coyote > 0) { v.y = jumpSpeed; jumpsUsed = 1; coyote = 0; buffer = 0; }
                    else
                    {
                        if (jumpsUsed == 0) jumpsUsed = 1;                  // 崖から落ちた場合は1回消費済み
                        if (jumpsUsed < 1 + Stats.extraJumps) { v.y = jumpSpeed; jumpsUsed++; buffer = 0; }
                    }
                }
                if (jumpUp && v.y > 0) v.y *= 0.5f;                         // 可変ジャンプ
                rb.linearVelocity = v;
            }
            if (atk && atkCd <= 0) { atkCd = attackCooldown; Attack(); }
        }
        void Attack()
        {
            foreach (var c in Physics2D.OverlapBoxAll(attackPoint.position, attackBox, 0, hitMask))
            {
                if (c.TryGetComponent(out Repairable r)) r.Repair(this);
                else if (c.TryGetComponent(out Health h) && h != Health) h.Damage(Stats.attack);
            }
        }
        public void TakeHit(int dmg, Vector2 from)
        {
            if (Health.Damage(dmg))
            {
                var d = ((Vector2)transform.position - from).normalized;
                rb.linearVelocity = new Vector2(d.x * 6f, 6f); SyncHp();
            }
        }
        public void SyncHp() { GameManager.Instance.Run.hp = Health.Current; }
        void OnDrawGizmosSelected()
        {
            if (groundCheck) Gizmos.DrawWireCube(groundCheck.position, groundBox);
            if (attackPoint) Gizmos.DrawWireCube(attackPoint.position, attackBox);
        }
    }
}
