using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    public class AreaSelectUI : MonoBehaviour
    {
        public Button[] areaButtons; public TMP_Text[] areaLabels; public Button rerollButton; public TMP_Text info;
        void Start() => Refresh();
        void Refresh()
        {
            var run = GameManager.Instance.Run;
            info.text = $"攻略 {run.areasCleared}/{run.totalAreas}   リロール残り {run.rerolls}";
            for (int i = 0; i < areaButtons.Length; i++)
            {
                bool has = i < run.choices.Count; areaButtons[i].gameObject.SetActive(has); if (!has) continue;
                var a = run.choices[i]; areaLabels[i].text = $"{a.displayName}{(run.Visits(a) > 0 ? $"  ({run.Visits(a) + 1}回目)" : "")}\n素材: {a.materialName}";
                areaButtons[i].onClick.RemoveAllListeners(); areaButtons[i].onClick.AddListener(() => GameManager.Instance.ChooseArea(a));
            }
            rerollButton.interactable = run.rerolls > 0;
            rerollButton.onClick.RemoveAllListeners(); rerollButton.onClick.AddListener(() => { run.Reroll(); Refresh(); });
        }
    }
}
