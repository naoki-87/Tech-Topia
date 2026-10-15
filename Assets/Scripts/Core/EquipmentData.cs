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
        [Header("Evolution (初期装備)")] public EquipmentData evolvesTo; public int evolveLevel = 8;
    }
}
