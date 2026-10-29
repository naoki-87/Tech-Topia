using UnityEngine;
namespace SteamEngineer
{
    // 装備固有の能力。ScriptableObjectとして作り、EquipmentData の ability に設定する
    public abstract class AbilityBase : ScriptableObject
    {
        public string displayName;
        public Element element;
        public float energyCost = 30f, cooldown = 0.6f;
        public Sprite icon;
        public abstract void Activate(PlayerController p);
    }
}