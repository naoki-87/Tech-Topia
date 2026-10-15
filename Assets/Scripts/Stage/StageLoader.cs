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
            var prefab = run.IsBossStage ? area.bossStagePrefab : area.stagePrefabs[Random.Range(0, area.stagePrefabs.Length)];
            Instantiate(prefab);
            var spawn = GameObject.Find("PlayerSpawn");
            Instantiate(playerPrefab, spawn ? spawn.transform.position : Vector3.zero, Quaternion.identity);
        }
    }
}
