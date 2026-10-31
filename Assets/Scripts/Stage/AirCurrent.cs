using UnityEngine;
namespace SteamEngineer
{
    // 気流: 範囲内のプレイヤーに風を当てる。BoxCollider2D(Is Trigger オン)と一緒に使う
    public class AirCurrent : MonoBehaviour
    {
        public Vector2 push = new Vector2(-3f, 0f);   // x: 横に加わる速度(マス/秒)  y: 上向きの加速度(上昇気流)
        public float period = 0f, onTime = 2f;        // 突風: period秒ごとにonTime秒だけ吹く(0なら常時)
        public SpriteRenderer sr; public float visibleAlpha = 0.35f;
        bool Active => period <= 0f || Time.time % period < onTime;

        void Update()
        {
            if (!sr) return; var c = sr.color; c.a = Active ? visibleAlpha : 0.06f; sr.color = c;
        }
        void OnTriggerStay2D(Collider2D other)
        {
            if (!Active || !other.TryGetComponent(out PlayerController p)) return;
            var w = push; w.x *= 1f - p.Stats.windResist;      // 帽子の特性で横風を軽減(上昇気流は軽減しない)
            p.SetWind(w);
        }
    }
}