using UnityEngine;
namespace SteamEngineer
{
    // 装備の特性(パッシブ)。ScriptableObjectとして作り、EquipmentData の passive に設定する
    public abstract class PassiveBase : ScriptableObject
    {
        public string displayName;
        protected static float T(Tier t) => PlayerStats.TierMul(t);     // ティア倍率 1.0?2.0

        public virtual void Modify(ref PlayerStats s, RunState r, Tier t) { }          // ステータスを直接変える
        public virtual float MatGainBonus(Tier t) => 0f;                                // 素材獲得量の増加率
        public virtual int AttackBonus(RunState r, Tier t) => 0;                        // 攻撃力の加算
        public virtual void OnMaterialGained(RunState r, PlayerController p, Tier t, int gained) { }
        public abstract string Describe(Tier t);                                        // ガレージに表示する説明

        // カウンター: 獲得数をためて、必要数に達した回数を返す(RunStateに保存される)
        protected int Consume(RunState r, int gained, int need)
        {
            r.counters.TryGetValue(this, out int c);
            c += gained; int times = c / need; r.counters[this] = c % need; return times;
        }
    }
}