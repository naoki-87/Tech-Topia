using UnityEngine;
namespace SteamEngineer
{
    public enum ItemEffect { Heal, HealFull, Energy, AttackUp, MaxHpUp, AreaGuide }
    [System.Flags] public enum ItemUse { None = 0, Stage = 1, Garage = 2, AreaSelect = 4 }

    // 補助アイテム: 汎用素材で作成 → 周回に持ち込み → 使えるタイミングで使う
    [CreateAssetMenu(menuName = "SteamEngineer/Item")]
    public class ItemData : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public ItemEffect effect;
        public float value;                    // 回復量 / 上昇量など
        public AreaData guideArea;             // AreaGuide のときに、必ず候補へ出すエリア
        public ItemUse usableIn = ItemUse.Garage | ItemUse.AreaSelect;
        public int craftCost = 15;             // 汎用素材
        public KeyCode stageKey = KeyCode.None;
        public string requireUnlock = "";      // 作成に必要な解放フラグ(空なら最初から)
        public Sprite icon;
    }
}