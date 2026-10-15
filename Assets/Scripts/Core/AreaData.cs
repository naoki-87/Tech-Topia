using UnityEngine;
namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Area")]
    public class AreaData : ScriptableObject
    {
        public string displayName;            // 例: 歯車の工場
        public string materialName;           // 例: 歯車
        public Color themeColor = Color.white;
        public Sprite icon;
        public GameObject[] stagePrefabs;     // 通常ステージ(ランダム選択)
        public GameObject bossStagePrefab;    // 5ステージ目
        public EquipmentData[] craftable;     // このエリアの素材で作れる装備
    }
}
