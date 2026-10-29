using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        public string displayName; public Sprite icon;
        public EquipSlot slot; public Tier tier; public AreaData sourceArea;
        public bool isStarter;
        [Header("Craft cost")] public int coreCost = 3; public int commonCost = 5;
        [Header("Stats (Tier倍率: 1+0.25*tier)")]
        public int hpBase, hpPerLevel;
        public float dmgRedBase, dmgRedPerLevel;        // 0.1 = 10%軽減
        public int attackBase, attackPerLevel;
        public int extraJumps; public float dashCdReductionPerLevel;
        [Header("Energy")]
        public float energyMaxBase, energyMaxPerLevel;     // 胴: 最大エネルギー
        public float regenBase, regenPerLevel;             // 胴: 回復量
        public float costReducePerLevel;                   // 脚: 消費量の減少(0.04 = 1レベルで4%減)
        [Header("Ability")]
        public AbilityBase ability;
        [Header("Evolution (初期装備)")] public EquipmentData evolvesTo; public int evolveLevel = 8;
    }
}
