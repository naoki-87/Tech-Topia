using UnityEngine;
namespace SteamEngineer
{
    // ’¹: ã‹ó‚Å‘Ò‹@ ¨ k‚¦‚Ä—\”õ“®ì ¨ ƒvƒŒƒCƒ„[‚ÌˆÊ’u‚Ö‹}~‰º ¨ Œ³‚ÌˆÊ’u‚É–ß‚é
    public class EnemyDiver : EnemyBase
    {
        public float hoverAmp = 0.5f, hoverFreq = 2f, detectRange = 7f, diveSpeed = 9f, windup = 0.5f, returnSpeed = 3f, cooldown = 2f;
        Vector3 home, target; float t, timer; int state;   // 0‘Ò‹@ 1—\”õ“®ì 2‹}~‰º 3–ß‚è

        protected override void Start() { base.Start(); home = transform.position; t = Random.value * 6f; timer = 1f; }
        void Update()
        {
            if (Stunned) return;
            t += Time.deltaTime;
            switch (state)
            {
                case 0:
                    transform.position = home + Vector3.up * Mathf.Sin(t * hoverFreq) * hoverAmp;
                    timer -= Time.deltaTime;
                    if (player && timer <= 0f && Vector2.Distance(player.position, transform.position) < detectRange) { state = 1; timer = windup; }
                    break;
                case 1:
                    transform.position += (Vector3)(Random.insideUnitCircle * 0.03f);
                    timer -= Time.deltaTime;
                    if (timer <= 0f) { target = player ? player.position : transform.position; state = 2; }
                    break;
                case 2:
                    transform.position = Vector3.MoveTowards(transform.position, target, diveSpeed * Time.deltaTime);
                    if ((transform.position - target).sqrMagnitude < 0.05f) state = 3;
                    break;
                case 3:
                    transform.position = Vector3.MoveTowards(transform.position, home, returnSpeed * Time.deltaTime);
                    if ((transform.position - home).sqrMagnitude < 0.05f) { state = 0; timer = cooldown; }
                    break;
            }
        }
        public override void Push(Vector2 dir, float force, float stun)
        {
            base.Push(dir, force, stun); home += (Vector3)(dir * force * stun * 0.5f); state = 3;
        }
    }
}