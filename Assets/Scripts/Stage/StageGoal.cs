using UnityEngine;
namespace SteamEngineer
{
    public class StageGoal : MonoBehaviour
    {
        bool used;
        void OnTriggerEnter2D(Collider2D c)
        {
            if (used || !c.TryGetComponent(out PlayerController p)) return;
            used = true; AudioManager.Clear(); p.SyncHp(); GameManager.Instance.OnStageClear();
        }
    }
}
