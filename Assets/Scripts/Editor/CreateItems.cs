#if UNITY_EDITOR
using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
namespace SteamEngineer.EditorTools
{
    public static class CreateItems
    {
        [MenuItem("Tools/TechTopia/補助アイテムを作成")]
        static void Create()
        {
            const string dir = "Assets/Data/Items";
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Data", "Items");
            var list = new System.Collections.Generic.List<ItemData>();
            AreaData Area(string key) { foreach (var g in AssetDatabase.FindAssets("t:AreaData")) { var a = AssetDatabase.LoadAssetAtPath<AreaData>(AssetDatabase.GUIDToAssetPath(g)); if (a != null && a.name.Contains(key)) return a; } return null; }
            void Make(string file, string name, string desc, ItemEffect fx, float v, ItemUse use, int cost, KeyCode key = KeyCode.None, AreaData area = null)
            {
                var path = $"{dir}/{file}.asset"; var it = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (it == null) { it = ScriptableObject.CreateInstance<ItemData>(); AssetDatabase.CreateAsset(it, path); }
                it.displayName = name; it.description = desc; it.effect = fx; it.value = v; it.usableIn = use; it.craftCost = cost; it.stageKey = key; it.guideArea = area;
                EditorUtility.SetDirty(it); list.Add(it);
            }
            const ItemUse G = ItemUse.Garage | ItemUse.AreaSelect;
            Make("Item_CompassWorkshop", "エリアの羅針盤・工房", "次のエリア選択で、棄てられた工房が必ず候補に出る", ItemEffect.AreaGuide, 0f, ItemUse.AreaSelect, 20, KeyCode.None, Area("Workshop"));
            Make("Item_CompassAirship", "エリアの羅針盤・飛行船", "次のエリア選択で、無人の飛行船が必ず候補に出る", ItemEffect.AreaGuide, 0f, ItemUse.AreaSelect, 20, KeyCode.None, Area("Airship"));
            Make("Item_RepairKit", "応急修理キット", "HPを3回復する", ItemEffect.Heal, 3f, ItemUse.Stage | G, 12, KeyCode.Q);
            Make("Item_RepairKitL", "大型修理キット", "HPを全回復する(ステージ中は使えない)", ItemEffect.HealFull, 0f, G, 30);
            Make("Item_EnergyCan", "エネルギー缶", "エネルギーを全回復する", ItemEffect.Energy, 0f, ItemUse.Stage, 10, KeyCode.E);
            Make("Item_PowerBolt", "強化ボルト", "この周回の間、攻撃力 +1", ItemEffect.AttackUp, 1f, G, 25);
            Make("Item_ReinforcePlate", "補強プレート", "この周回の間、最大HP +2", ItemEffect.MaxHpUp, 2f, G, 25);
            AssetDatabase.SaveAssets();
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.itemPool = list.ToArray(); EditorUtility.SetDirty(gm); EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
                Debug.Log("補助アイテムを作成し、GameManager の Item Pool に設定しました。Ctrl+S でシーンを保存してください。");
            }
            else Debug.Log("補助アイテムを作成しました。Title シーンを開いて、もう一度実行すると GameManager に設定されます。");
        }
    }
}
#endif