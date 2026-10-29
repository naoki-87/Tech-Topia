using System; using UnityEngine;
namespace SteamEngineer
{
    // エネルギー: 消費してダッシュ・空中ジャンプ・固有能力を使う。使わないと自然回復する。
    public class Energy : MonoBehaviour
    {
        public float max = 100f, regenPerSec = 20f, regenDelay = 0.6f;
        public float costMul = 1f;     // 消費量の倍率(脚装備で減少、砂漠で増加)
        public float regenMul = 1f;    // 回復量の倍率(氷河で低下)
        public float Current { get; private set; }
        public event Action<float, float> Changed;
        float lastUse;

        void Awake() { Current = max; }
        public void Init(float newMax, float regen)
        {
            max = newMax; regenPerSec = regen; Current = max; Changed?.Invoke(Current, max);
        }
        public bool TryUse(float baseCost)
        {
            float c = baseCost * costMul;
            if (Current < c) return false;
            Current -= c; lastUse = Time.time; Changed?.Invoke(Current, max); return true;
        }
        public void Restore(float amount)
        {
            Current = Mathf.Min(max, Current + amount); Changed?.Invoke(Current, max);
        }
        void Update()
        {
            if (Current >= max || Time.time - lastUse < regenDelay) return;   // 使った直後は回復しない
            Current = Mathf.Min(max, Current + regenPerSec * regenMul * Time.deltaTime);
            Changed?.Invoke(Current, max);
        }
    }
}