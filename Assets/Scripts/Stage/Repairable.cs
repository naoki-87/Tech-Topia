using UnityEngine; using UnityEngine.Events;
namespace SteamEngineer
{
    // 壊れた機械: レンチで修理 → 素材+HP回復 + 任意ギミック(扉・足場の起動など)
    public class Repairable : MonoBehaviour
    {
        public int rewardMaterials = 2, heal = 1; public SpriteRenderer sr; public Sprite fixedSprite; public UnityEvent onRepaired;
        bool done;
        public void Repair(PlayerController p)
        {
            if (done) return; done = true;
            AudioManager.Repair();
            GameManager.Instance.Run.AddStageMaterial(rewardMaterials); p.Health.Heal(heal); p.SyncHp();
            if (sr && fixedSprite) sr.sprite = fixedSprite; onRepaired?.Invoke();
        }
    }
}
