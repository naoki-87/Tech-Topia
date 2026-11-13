using UnityEngine; using UnityEngine.InputSystem;
namespace SteamEngineer
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public LayerMask groundMask, hitMask;     // hitMask: 敵 + 修理ギミック
        public Transform groundCheck, attackPoint;
        public Vector2 groundBox = new Vector2(0.5f, 0.1f), attackBox = new Vector2(1.2f, 0.9f);
        public float moveSpeed = 6f, jumpSpeed = 12f, gravity = 3f, dashSpeed = 16f, dashTime = 0.15f;
        public float coyoteTime = 0.1f, jumpBuffer = 0.12f, attackCooldown = 0.25f, hurtInvuln = 1f;
        public float dashCost = 25f, airJumpCost = 20f;
        public Health Health { get; private set; } public PlayerStats Stats { get; private set; }
        public Rigidbody2D Body => rb; public int Facing => facing;
        public int CurrentAttack => Stats.attack + GameManager.Instance.Run.AttackBonus() + LowHpBonus;
        int LowHpBonus => (Health != null && Health.Current * 2 <= Health.max) ? Mathf.RoundToInt(GameManager.Instance.Run.RelicValue(RelicEffect.LowHpAttack)) : 0;

        Rigidbody2D rb; Energy energy; Collider2D col;
        float knockUntil, knockVx;
        int facing = 1, jumpsUsed; float coyote, buffer, dashT, dashCd, atkCd, pushReadyAt;
        bool grounded, dashing;
        readonly AbilityBase[] abilities = new AbilityBase[RunState.Slots];   // 0頭(I) 1胴(O) 2脚(P) 3武器(L)
        readonly float[] abilityCd = new float[RunState.Slots];
        Vector2 windPush; float windHold, windMaxRise = 8f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>(); col = GetComponent<Collider2D>(); Health = GetComponent<Health>();
            energy = GetComponent<Energy>(); if (energy == null) energy = gameObject.AddComponent<Energy>();
            rb.gravityScale = gravity; rb.freezeRotation = true;
        }
        void Start()
        {
            var run = GameManager.Instance.Run; Stats = PlayerStats.Build(run);
            energy.Init(Stats.maxEnergy, Stats.energyRegen); energy.costMul = Stats.energyCostMul;
            for (int i = 0; i < RunState.Slots; i++) { var e = run.equipped[i]; abilities[i] = e != null ? e.ability : null; }
            Health.invulnTime = hurtInvuln; Health.Set(run.hp, Stats.maxHp);
            Health.damageModifier = d => Mathf.Max(1, Mathf.RoundToInt(d * (1f - Stats.dmgReduction)));
            Health.Died += () => { if (!TryRevive()) GameManager.Instance.OnPlayerDead(); };
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

            var cb = col.bounds;      // コライダーの下端のすぐ下を調べる(GroundCheckの位置に依存しない)
            CheckContact();
            grounded = Physics2D.OverlapBox(new Vector2(cb.center.x, cb.min.y - 0.05f), new Vector2(cb.size.x * 0.8f, 0.12f), 0, groundMask);
            if (grounded) { coyote = coyoteTime; jumpsUsed = 0; } else coyote -= dt;
            buffer = jumpDown ? jumpBuffer : buffer - dt;
            dashCd -= dt; atkCd -= dt;
            for (int i = 0; i < abilityCd.Length; i++) abilityCd[i] -= dt;
            if (windHold > 0f) windHold -= dt; else windPush = Vector2.zero;
            if (x != 0) { facing = x > 0 ? 1 : -1; transform.localScale = new Vector3(facing, 1, 1); }

            if (dash && dashCd <= 0 && !dashing && energy.TryUse(dashCost))
            { dashing = true; dashT = dashTime; dashCd = Stats.dashCooldown; rb.gravityScale = 0; Health.GrantInvuln(dashTime); AudioManager.Dash(); }

            if (dashing)
            {
                dashT -= dt; rb.linearVelocity = new Vector2(facing * dashSpeed, 0);
                if (dashT <= 0) { dashing = false; rb.gravityScale = gravity; }
            }
            else
            {
                var v = rb.linearVelocity;
                v.x = Time.time < knockUntil ? knockVx : x * moveSpeed * Stats.moveMul + windPush.x;
                if (windPush.y > 0f)                       // 上昇気流: maxRise まで上向きに加速(ジャンプの速さは削らない)
                {
                    float before = v.y;
                    v.y = Mathf.Min(v.y + windPush.y * dt, Mathf.Max(windMaxRise, before));
                }
                else if (windPush.y < 0f)                  // 下降気流
                    v.y = Mathf.Max(v.y + windPush.y * dt, -14f);
                if (buffer > 0)
                {
                    if (coyote > 0)
                    {
                        v.y = jumpSpeed * Stats.jumpMul; jumpsUsed = 1; coyote = 0; buffer = 0;
                        AudioManager.Jump();
                    }
                    else
                    {
                        if (jumpsUsed == 0) jumpsUsed = 1;
                        bool canAir = jumpsUsed < 1 + Stats.extraJumps;
                        if (canAir && energy.TryUse(airJumpCost * Stats.airJumpCostMul))
                        {
                            v.y = jumpSpeed * Stats.jumpMul; jumpsUsed++; buffer = 0;
                            AudioManager.Jump();
                        }
                        else Debug.Log($"[空中ジャンプ不可] 回数 {jumpsUsed}/{1 + Stats.extraJumps}  エネルギー {energy.Current:0}");
                    }
                }
                if (jumpUp && v.y > 0) v.y *= 0.5f;
                if (v.y < -Stats.fallCap) v.y = -Stats.fallCap;                                  // 落下速度の上限(靴の特性)
                rb.linearVelocity = v;
            }

            if (atk && atkCd <= 0) { atkCd = attackCooldown; Attack(); }

            for (int i = 0; i < RunState.Slots; i++)                                              // 各装備のアクション
            {
                var ab = abilities[i]; if (ab == null || abilityCd[i] > 0f) continue;
                var key = i == 0 ? kb.iKey : i == 1 ? kb.oKey : i == 2 ? kb.pKey : kb.lKey;
                if (key.wasPressedThisFrame && energy.TryUse(ab.energyCost)) { abilityCd[i] = ab.cooldown; ab.Activate(this); AudioManager.Ability(); }
            }
        }

        void Attack()
        {
            AudioManager.Attack();
            float m = Stats.attackBoxMul;                                                         // 武器の特性で範囲が広がる
            Vector2 box = new Vector2(attackBox.x * m, attackBox.y * Mathf.Lerp(1f, m, 0.5f));
            Vector2 center = (Vector2)attackPoint.position + Vector2.right * facing * (box.x - attackBox.x) * 0.5f;
            Fx.Slash(center, box, facing);     // 攻撃範囲の表示と斬撃
            foreach (var c in Physics2D.OverlapBoxAll(center, box, 0, hitMask))
            {
                if (c.TryGetComponent(out Repairable r)) r.Repair(this);
                else if (c.TryGetComponent(out Health h) && h != Health) { h.Damage(CurrentAttack); Fx.Hit(c.bounds.center); }
            }
        }

        public void TakeHit(int dmg, Vector2 from, bool ranged = false)
        {
            if (ranged && Stats.rangedReduction > 0f) dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * (1f - Stats.rangedReduction)));
            if (!Health.Damage(dmg)) return;
            AudioManager.Hurt();
            float dirX = transform.position.x >= from.x ? 1f : -1f;      // 敵と反対方向へ軽くのけぞる
            knockVx = dirX * 5f; knockUntil = Time.time + 0.18f;
            rb.linearVelocity = new Vector2(knockVx, 6f); SyncHp();
            if (!ranged && Stats.pushRadius > 0f && Time.time >= pushReadyAt)                    // 胴体の特性: 近接被弾で吹き飛ばし
            {
                pushReadyAt = Time.time + Stats.pushCooldown;
                foreach (var c in Physics2D.OverlapCircleAll(transform.position, Stats.pushRadius))
                    if (c.TryGetComponent(out EnemyBase e))
                        e.Push(new Vector2(Mathf.Sign(e.transform.position.x - transform.position.x), 0.3f).normalized, Stats.pushForce, Stats.pushStun);
            }
        }

        // 自分の体に重なっている敵を調べ、ダメージを受ける(物理的な衝突は不要)
        void CheckContact()
        {
            var b = col.bounds;
            var hits = Physics2D.OverlapBoxAll(b.center, b.size - new Vector3(0.1f, 0.1f, 0f), 0f);
            foreach (var h in hits)
            {
                var e = h.GetComponentInParent<EnemyBase>();
                if (e == null) continue;
                TakeHit(e.contactDamage + GameManager.Instance.EnemyDmgBonus, e.transform.position);
                if (Health.IsDead) break;
            }
        }

        public void SetWind(Vector2 w, float maxRise = 8f)
        {
            windPush = w; windMaxRise = maxRise; windHold = 0.08f;
            if (Stats.windRegen > 0f) energy.Restore(Stats.windRegen * Time.fixedDeltaTime);       // 帽子の特性: 風の中でエネルギー回復
        }
        public void SyncHp() { GameManager.Instance.Run.hp = Health.Current; }
        bool TryRevive()
        {
            var run = GameManager.Instance.Run;
            if (run.reviveUsed || !run.HasRelic(RelicEffect.Revive)) return false;
            run.reviveUsed = true;
            Health.Revive(Mathf.RoundToInt(run.RelicValue(RelicEffect.Revive))); SyncHp();
            return true;
        }
        public void EmitElement(Element el, Vector2 center, float radius)
        {
            foreach (var c in Physics2D.OverlapCircleAll(center, radius))
                if (c.TryGetComponent(out ElementGimmick g)) g.Activate(el);
        }
        public void DamageAround(Vector2 center, float radius, int dmg)
        {
            foreach (var c in Physics2D.OverlapCircleAll(center, radius))
                if (c.GetComponent<EnemyBase>() != null && c.TryGetComponent(out Health h)) { h.Damage(dmg); Fx.Hit(c.bounds.center); }
        }
        void OnDrawGizmosSelected()
        {
            if (groundCheck) Gizmos.DrawWireCube(groundCheck.position, groundBox);
            if (attackPoint) Gizmos.DrawWireCube(attackPoint.position, attackBox);
        }
    }
}