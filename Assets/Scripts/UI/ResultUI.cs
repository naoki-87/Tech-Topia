using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    public class ResultUI : MonoBehaviour
    {
        public TMP_Text text; public Button titleButton;
        void Start()
        {
            var r = GameManager.Instance.Run; var s = GameManager.Instance.Save;
            string rel = r.relics.Count > 0 ? "\n遺物: " + string.Join(" / ", r.relics.ConvertAll(x => x.displayName)) : "";
            text.text = (r.won ? "クリア！" : "ゲームオーバー") + $"\nエリア {r.areasCleared}/{r.totalAreas} 攻略\n獲得コア {(r.won ? r.coresCollected : 0)}\n所持コア {s.cores} / 汎用素材 {s.commonMaterials}" + rel;
            titleButton.onClick.AddListener(GameManager.Instance.BackToTitle);
        }
    }
}
