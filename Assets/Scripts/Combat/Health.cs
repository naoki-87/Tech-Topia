using System; using UnityEngine;
namespace SteamEngineer
{
    public class Health : MonoBehaviour
    {
        public int max = 1; public float invulnTime = 0f;
        public int Current { get; private set; } public bool IsDead { get; private set; }
        public Func<int, int> damageModifier;
        public event Action<int, int> Changed; public event Action Died;
        float invUntil;
        void Awake() { Current = max; }
        public void Set(int cur, int m) { max = m; Current = Mathf.Clamp(cur, 1, m); Changed?.Invoke(Current, max); }
        public void Heal(int a) { if (IsDead) return; Current = Mathf.Min(max, Current + a); Changed?.Invoke(Current, max); }
        public void GrantInvuln(float t) { invUntil = Mathf.Max(invUntil, Time.time + t); }
        public bool Damage(int amount)
        {
            if (IsDead || Time.time < invUntil) return false;
            if (damageModifier != null) amount = damageModifier(amount);
            Current = Mathf.Max(0, Current - amount); invUntil = Time.time + invulnTime;
            Changed?.Invoke(Current, max);
            if (Current <= 0) { IsDead = true; Died?.Invoke(); }
            return true;
        }
    }
}
