using UnityEngine;
namespace SteamEngineer
{
    public class EnemyFlyer : EnemyBase
    {
        public float amplitude = 2f, freq = 1.2f, chaseRange = 6f, chaseSpeed = 2f;
        Vector3 home; float t;
        protected override void Start() { base.Start(); home = transform.position; t = Random.value * 6f; }
        void Update()
        {
            t += Time.deltaTime;
            if (player && Vector2.Distance(player.position, transform.position) < chaseRange)
                transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
            else transform.position = home + new Vector3(Mathf.Sin(t * freq) * amplitude, Mathf.Cos(t * freq * 2f) * 0.4f);
        }
    }
}
