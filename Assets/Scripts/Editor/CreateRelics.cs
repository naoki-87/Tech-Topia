#if UNITY_EDITOR
using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
namespace SteamEngineer.EditorTools
{
    public static class CreateRelics
    {
        [MenuItem("Tools/TechTopia/遺物アセットを作成")]
        static void Create()
        {
            const string dir = "Assets/Data/Relics";
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Data", "Relics");
            var list = new System.Collections.Generic.List<RelicData>();
            void Make(string file, string name, string desc, RelicEffect fx, float v, float v2 = 0f)
            {
                var path = $"{dir}/{file}.asset"; var r = AssetDatabase.LoadAssetAtPath<RelicData>(path);
                if (r == null) { r = ScriptableObject.CreateInstance<RelicData>(); AssetDatabase.CreateAsset(r, path); }
                r.displayName = name; r.description = desc; r.effect = fx; r.value = v; r.value2 = v2;
                EditorUtility.SetDirty(r); list.Add(r);
            }
            Make("Relic_Tank", "圧縮タンク", "エネルギー最大値 +30", RelicEffect.MaxEnergy, 30f);
            Make("Relic_Frame", "補強フレーム", "最大HP +2", RelicEffect.MaxHp, 2f);
            Make("Relic_Bolt", "徹甲ボルト", "攻撃力 +1", RelicEffect.Attack, 1f);
            Make("Relic_Feather", "風切りの羽", "空中ジャンプの消費エネルギー -50%", RelicEffect.AirJumpCost, 0.5f);
            Make("Relic_Booster", "蒸気ブースター", "エネルギー回復 +8/秒", RelicEffect.EnergyRegen, 8f);
            Make("Relic_Boots", "軽量ブーツ", "移動速度 +12%", RelicEffect.MoveSpeed, 0.12f);
            Make("Relic_Spring", "跳躍バネ", "ジャンプ力 +10%", RelicEffect.JumpPower, 0.1f);
            Make("Relic_Armor", "鋼の外装", "受けるダメージ -10%", RelicEffect.DamageReduction, 0.1f);
            Make("Relic_Adversity", "逆境の歯車", "HPが半分以下の間、攻撃力 +2", RelicEffect.LowHpAttack, 2f);
            Make("Relic_Greed", "強欲な歯車", "素材 +30% / 敵のHP +15%", RelicEffect.Greed, 0.3f, 0.15f);
            Make("Relic_Kit", "緊急修理キット", "HPが0になったとき、1回だけHP3で復活", RelicEffect.Revive, 3f);
            Make("Relic_Blast", "蒸気爆発", "倒した敵が爆発し、周囲の敵にダメージ2", RelicEffect.KillExplosion, 2f);
            AssetDatabase.SaveAssets();
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.relicPool = list.ToArray(); EditorUtility.SetDirty(gm);
                EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
                Debug.Log("遺物を作成し、GameManager の Relic Pool に設定しました。Ctrl+S でシーンを保存してください。");
            }
            else Debug.Log("遺物を作成しました。Title シーンを開いて、もう一度実行すると GameManager に設定されます(手動で Relic Pool にドラッグしても構いません)。");
        }
    }
}
#endif