using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    public class AreaSelectUI : MonoBehaviour
    {
        public Button[] areaButtons; public TMP_Text[] areaLabels; public Button rerollButton; public TMP_Text info;
        TMP_Text strength;

        void Start()
        {
            var go = new GameObject("Strength", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(info.transform.parent, false);
            strength = go.GetComponent<TextMeshProUGUI>();
            strength.fontSize = 32; strength.color = new Color32(0xF0, 0xDC, 0xB8, 255); strength.alignment = TextAlignmentOptions.Center;
            strength.textWrappingMode = TextWrappingModes.Normal; strength.raycastTarget = false;
            var r = strength.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -200f); r.sizeDelta = new Vector2(1700f, 100f);
            Refresh();
        }
        void Refresh()
        {
            var gm = GameManager.Instance; var run = gm.Run;
            info.text = $"攻略 {run.areasCleared}/{run.totalAreas}   リロール残り {run.rerolls}";
            strength.text = $"現在の敵の強さ  HP ×{gm.EnemyHpMul:0.00} / 接触ダメージ +{gm.EnemyDmgBonus}\n" +
                            $"装備パワー {run.EquipPower()} / 攻略 {run.areasCleared}エリア / 手に入る素材のティア: {TierColors.Name(run.DropTier)}";
            for (int i = 0; i < areaButtons.Length; i++)
            {
                bool has = i < run.choices.Count; areaButtons[i].gameObject.SetActive(has); if (!has) continue;
                var a = run.choices[i]; string visit = run.Visits(a) > 0 ? $" ({run.Visits(a) + 1}回目)" : "";
                var lbl = areaLabels[i]; lbl.enableAutoSizing = false; lbl.fontSize = 32; lbl.alignment = TextAlignmentOptions.Center;
                lbl.textWrappingMode = TextWrappingModes.Normal; lbl.overflowMode = TextOverflowModes.Truncate;
                lbl.rectTransform.offsetMin = new Vector2(16f, 12f); lbl.rectTransform.offsetMax = new Vector2(-16f, -12f);
                lbl.text = $"<size=48>{a.displayName}</size>{visit}\n素材: {a.materialName}\n{a.description}";
                areaButtons[i].onClick.RemoveAllListeners(); areaButtons[i].onClick.AddListener(() => GameManager.Instance.ChooseArea(a));
            }
            rerollButton.interactable = run.rerolls > 0;
            rerollButton.onClick.RemoveAllListeners(); rerollButton.onClick.AddListener(() => { run.Reroll(); Refresh(); });
        }
    }
}