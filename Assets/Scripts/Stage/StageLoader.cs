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
            if (run.IsBossStage)
                Instantiate(area.bossStagePrefab);
            else
            {
                int seed = unchecked(run.generationSeed + (run.stagesCleared + 1) * 10007 + run.stageIndex);
                var generated = area.generatedStages != null ? StageGenerator.TryGenerate(area.generatedStages, seed) : null;
                if (generated == null)
                {
                    if (area.stagePrefabs != null && area.stagePrefabs.Length > 0)
                        Instantiate(area.stagePrefabs[Random.Range(0, area.stagePrefabs.Length)]);
                    else Debug.LogError($"Area {area.name} に通常ステージPrefabも生成プロファイルもありません");
                }
            }
            Debug.Log($"[難易度] 装備パワー {run.EquipPower()} → 敵HP ×{GameManager.Instance.EnemyHpMul:0.00} / ダメージ +{GameManager.Instance.EnemyDmgBonus}");
            var spawn = GameObject.Find("PlayerSpawn");
            Instantiate(playerPrefab, spawn ? spawn.transform.position : Vector3.zero, Quaternion.identity);
        }
    }
}
