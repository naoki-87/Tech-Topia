#if UNITY_EDITOR
using TMPro; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine; using UnityEngine.SceneManagement;
namespace SteamEngineer.EditorTools
{
    // メニュー Tools/TechTopia から、全TMPテキストを DotGothic16 Raster・16の倍数サイズに揃える
    public static class ApplyPixelFont
    {
        static TMP_FontAsset Load()
        {
            foreach (var g in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("DotGothic16") && p.Contains("Raster")) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            }
            Debug.LogError("「DotGothic16 Raster」のフォントアセットが見つかりません。名前を確認してください。");
            return null;
        }
        static float Snap(float s) => Mathf.Max(16f, Mathf.Round(s / 16f) * 16f);
        static int Apply(GameObject root, TMP_FontAsset f)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                t.font = f; t.enableAutoSizing = false; t.fontSize = Snap(t.fontSize);
                EditorUtility.SetDirty(t); n++;
            }
            return n;
        }

        [MenuItem("Tools/TechTopia/ピクセルフォントを開いているシーンに適用")]
        static void ApplyScene()
        {
            var f = Load(); if (f == null) return; int n = 0;
            foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects()) n += Apply(r, f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"{n} 個のテキストに適用しました。Ctrl+S でシーンを保存してください。");
        }

        [MenuItem("Tools/TechTopia/ピクセルフォントを全Prefabに適用")]
        static void ApplyPrefabs()
        {
            var f = Load(); if (f == null) return; int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var root = PrefabUtility.LoadPrefabContents(path);
                int c = Apply(root, f);
                if (c > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); n += c; }
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Prefab内の {n} 個のテキストに適用しました。");
        }
    }
}
#endif