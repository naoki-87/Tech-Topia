using System.Text; using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // エリア選択: 現在の敵の強さ / 各エリアの条件(リスクと報酬)/ 周回の最初は出発ボーナスの3択
    public class AreaSelectUI : MonoBehaviour
    {
        public Button[] areaButtons; public TMP_Text[] areaLabels; public Button rerollButton; public TMP_Text info;
        TMP_Text strength; GameObject bonusPanel; string sig = "";

        void Start()
        {
            RunExtras.mods.Clear();
            var go = new GameObject("Strength", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(info.transform.parent, false);
            strength = go.GetComponent<TextMeshProUGUI>();
            strength.fontSize = 32; strength.color = UiKit.Cream; strength.alignment = TextAlignmentOptions.Center;
            strength.textWrappingMode = TextWrappingModes.Normal; strength.raycastTarget = false;
            var r = strength.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -200f); r.sizeDelta = new Vector2(1700f, 130f);
            Refresh();
            if (RunExtras.startBonusPending) ShowBonus();
        }

        static string Sig(RunState run)
        {
            var sb = new StringBuilder(); foreach (var a in run.choices) sb.Append(a.name).Append(',');
            return sb.Append(run.rerolls).ToString();
        }
        void Update()      // 羅針盤などで候補が変わったら、表示を更新する
        {
            var gm = GameManager.Instance; if (gm == null || gm.Run == null) return;
            if (Sig(gm.Run) != sig) Refresh();
        }

        void Refresh()
        {
            var gm = GameManager.Instance; var run = gm.Run; sig = Sig(run);
            RunExtras.Assign(run.choices);
            info.text = $"攻略 {run.areasCleared}/{run.totalAreas}   リロール残り {run.rerolls}";
            strength.text = $"現在の敵の強さ  HP ×{gm.EnemyHpMul:0.00} / 接触ダメージ +{gm.EnemyDmgBonus}\n" +
                            $"装備パワー {run.EquipPower()} / 攻略 {run.areasCleared}エリア / 手に入る素材のティア: {TierColors.Name(run.DropTier)}\n" +
                            "各エリアの【条件】は、そのエリアの間だけ有効です";
            for (int i = 0; i < areaButtons.Length; i++)
            {
                bool has = i < run.choices.Count; areaButtons[i].gameObject.SetActive(has); if (!has) continue;
                var a = run.choices[i]; var mod = RunExtras.ModFor(a); string visit = run.Visits(a) > 0 ? $" ({run.Visits(a) + 1}回目)" : "";
                var lbl = areaLabels[i]; lbl.enableAutoSizing = false; lbl.fontSize = 32; lbl.alignment = TextAlignmentOptions.Center;
                lbl.textWrappingMode = TextWrappingModes.Normal; lbl.overflowMode = TextOverflowModes.Truncate;
                lbl.rectTransform.offsetMin = new Vector2(16f, 12f); lbl.rectTransform.offsetMax = new Vector2(-16f, -12f);
                lbl.text = $"<size=48>{a.displayName}</size>{visit}\n素材: {a.materialName}\n{a.description}\n<color=#FFD060>【{mod.name}】</color>\n{mod.desc}";
                areaButtons[i].onClick.RemoveAllListeners(); areaButtons[i].onClick.AddListener(() => GameManager.Instance.ChooseArea(a));
            }
            rerollButton.interactable = run.rerolls > 0;
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => { run.Reroll(); RunExtras.mods.Clear(); Refresh(); });
        }

        // ---------- 出発ボーナス ----------
        void ShowBonus()
        {
            if (RunExtras.bonusOffers.Count == 0) RunExtras.RollBonuses();
            var canvas = info.canvas.rootCanvas;
            bonusPanel = UiKit.Fullscreen(canvas.transform, "StartBonusPanel"); bonusPanel.transform.SetAsLastSibling();
            var t = UiKit.Txt(bonusPanel.transform, "出発ボーナスを1つ選んでください", 64, UiKit.BrassHi, TextAlignmentOptions.Center, "Title");
            UiKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1700f, 90f));
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup)); row.transform.SetParent(bonusPanel.transform, false);
            UiKit.Place((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1700f, 480f));
            var h = row.GetComponent<HorizontalLayoutGroup>(); h.spacing = 30;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            foreach (var b in RunExtras.bonusOffers)
            {
                var bonus = b;
                var btn = UiKit.Btn(row.transform, $"<size=48>{bonus.name}</size>\n\n{bonus.desc}", 32, UiKit.BrassLo, UiKit.Cream, "Bonus");
                btn.onClick.AddListener(() => PickBonus(bonus));
            }
        }
        void PickBonus(StartBonus b)
        {
            b.apply(GameManager.Instance.Run);
            RunExtras.startBonusPending = false; RunExtras.bonusOffers.Clear();
            Destroy(bonusPanel); Refresh();
        }
    }
}
