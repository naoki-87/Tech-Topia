using UnityEngine;
namespace SteamEngineer
{
    // Stage シーンに配置。エリアの通常ステージPrefab(ランダム)またはボスPrefabを生成し、"PlayerSpawn" にプレイヤーを出す
    public class StageLoader : MonoBehaviour
    {
        public GameObject playerPrefab;
        void Start()
        {
            var run = GameManager.Instance.Run; var area = run.current;
            if (Camera.main != null) { Camera.main.clearFlags = CameraClearFlags.SolidColor; Camera.main.backgroundColor = area.backgroundColor; }
            if (area.background != null && Camera.main != null)
            {
                var go = new GameObject("Background");
                go.transform.SetParent(Camera.main.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 10f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = area.background; sr.sortingOrder = -10;
                go.AddComponent<ParallaxBackground>();
            }
            var prefab = run.IsBossStage ? area.bossStagePrefab : area.stagePrefabs[Random.Range(0, area.stagePrefabs.Length)];
            Instantiate(prefab);
            Debug.Log($"[難易度] 装備パワー {run.EquipPower()} → 敵HP ×{GameManager.Instance.EnemyHpMul:0.00} / ダメージ +{GameManager.Instance.EnemyDmgBonus}");
            var spawn = GameObject.Find("PlayerSpawn");
            Instantiate(playerPrefab, spawn ? spawn.transform.position : Vector3.zero, Quaternion.identity);
        }
    }
}
