using UnityEngine; using UnityEngine.Events;
namespace SteamEngineer
{
    // 属性ギミック: 対応する属性の能力を当てると、追加報酬が出る(帆・ボイラー・風車など)
    public class ElementGimmick : MonoBehaviour
    {
        public Element required = Element.Steam;
        public int rewardMaterials = 3;
        public SpriteRenderer sr; public Color activatedColor = new Color(0.5f, 1f, 0.6f);
        public UnityEvent onActivated;      // 扉を開ける、足場を出す、などに使える
        bool done;
        public bool Activate(Element e)
        {
            if (done || e != required) return false;
            done = true;
            GameManager.Instance.Run.AddStageMaterial(rewardMaterials);
            if (sr) sr.color = activatedColor;
            onActivated?.Invoke();
            return true;
        }
    }
}