using System.Collections.Generic; using System.Text; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    // ガレージ: 遺物カード(あれば)+ 強化カード。下部に「装備詳細 / 回復 / 出発」
    public class GarageUI : MonoBehaviour
    {
        public Button[] upgradeButtons; public TMP_Text[] upgradeLabels;      // 旧UI(自動で隠れる。割り当てはそのままでよい)
        public Button repairButton, departButton; public TMP_Text status;
        public Transform listRoot; public Button listButtonPrefab;            // 旧UI(使わない)

        static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        static readonly string[] keys = { "I", "O", "P", "L" };
        static readonly string[] rankNames = { "素人", "職人", "達人", "工房", "伝説" };
        static readonly Color Cream = new Color32(0xF0, 0xDC, 0xB8, 255), Dark = new Color32(0x2B, 0x1D, 0x12, 255);
        static readonly Color Brass = new Color32(0xB8, 0x86, 0x0B, 255), BrassHi = new Color32(0xD9, 0xA5, 0x20, 255), BrassLo = new Color32(0x6B, 0x4F, 0x1D, 255);
        static readonly Color Slate = new Color32(0x3A, 0x4A, 0x5C, 255);

        bool used; string notice = ""; int detailTab; int[] upSlots;
        RectTransform main; GameObject detailPanel; TMP_Text detailText;
        readonly Button[] dBtn = new Button[5];

        // ---------- UI部品(コードで作る) ----------
        static TMP_Text MakeText(string name, Transform parent, string text, int size, Color col, TextAlignmentOptions al)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.color = col; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        static void Stretch(RectTransform r, float x, float y) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(x, y); r.offsetMax = new Vector2(-x, -y); }
        static void Anchor(RectTransform r, Vector2 a, Vector2 pivot, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = a; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; }
        static Button MakeButton(string name, Transform parent, string label, int size, Color bg, Color fg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            var t = MakeText("Label", go.transform, label, size, fg, TextAlignmentOptions.Center); Stretch(t.rectTransform, 8, 4);
            return go.GetComponent<Button>();
        }
        Transform Root => status.canvas.rootCanvas.transform;

        void Start()
        {
            foreach (var b in upgradeButtons) if (b) b.gameObject.SetActive(false);
            var scroll = listRoot ? listRoot.GetComponentInParent<ScrollRect>() : null; if (scroll) scroll.gameObject.SetActive(false);
            var all = new List<int> { 0, 1, 2, 3 }; upSlots = new int[3];
            for (int i = 0; i < 3; i++) { int k = Random.Range(0, all.Count); upSlots[i] = all[k]; all.RemoveAt(k); }
            LayoutStatus(); BuildMain(); BuildBottom(); BuildDetail(); Refresh();
        }

        void LayoutStatus()
        {
            Anchor((RectTransform)status.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -30f), new Vector2(1800f, 130f));
            status.enableAutoSizing = false; status.fontSize = 32; status.alignment = TextAlignmentOptions.TopLeft;
            status.textWrappingMode = TextWrappingModes.Normal; status.overflowMode = TextOverflowModes.Truncate; status.raycastTarget = false;
        }
        void BuildMain()
        {
            var go = new GameObject("Main", typeof(RectTransform), typeof(VerticalLayoutGroup)); go.transform.SetParent(Root, false);
            main = (RectTransform)go.transform; main.anchorMin = Vector2.zero; main.anchorMax = Vector2.one;
            main.offsetMin = new Vector2(60f, 200f); main.offsetMax = new Vector2(-60f, -170f);
            var v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 10; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        }
        void PlaceBottom(RectTransform r, float x) { Anchor(r, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 40f), new Vector2(420f, 110f)); }
        void BuildBottom()
        {
            PlaceBottom((RectTransform)repairButton.transform, 225f);     // 回復
            PlaceBottom((RectTransform)departButton.transform, 675f);     // 出発
            var t = repairButton.GetComponentInChildren<TMP_Text>();
            if (t) { t.enableAutoSizing = false; t.fontSize = 32; t.text = "回復して休む\nHP +3"; t.textWrappingMode = TextWrappingModes.Normal; }
            repairButton.onClick.RemoveAllListeners(); repairButton.onClick.AddListener(DoHeal);
            departButton.onClick.RemoveAllListeners(); departButton.onClick.AddListener(GameManager.Instance.AfterGarage);
            var d = MakeButton("DetailBtn", Root, "装備詳細", 32, Brass, Dark);
            PlaceBottom((RectTransform)d.transform, -675f); d.onClick.AddListener(OpenDetail);   // 装備詳細
        }

        // ---------- カード ----------
        Transform AddRow(int h)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)); go.transform.SetParent(main, false);
            go.GetComponent<LayoutElement>().preferredHeight = h;
            var hl = go.GetComponent<HorizontalLayoutGroup>(); hl.spacing = 20;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            return go.transform;
        }
        void AddLabel(string text)
        {
            var t = MakeText("Label", main, text, 32, Cream, TextAlignmentOptions.Left);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
        }
        void AddCard(Transform row, string text, bool interactable, Color bg, System.Action onClick)
        {
            var b = MakeButton("Card", row, text, 32, bg, Cream);
            var t = b.GetComponentInChildren<TMP_Text>(); t.alignment = TextAlignmentOptions.TopLeft; Stretch(t.rectTransform, 24f, 18f);
            var le = b.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1f; le.minWidth = 300f;
            var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f); cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f); b.colors = cb;
            b.interactable = interactable; b.onClick.AddListener(() => onClick());
        }

        // 強化で、どのステータスがどれだけ上がるかを、実際の計算の差から出す
        static string UpgradeInfo(RunState run, int s)
        {
            var before = PlayerStats.Build(run);
            run.level[s]++; var after = PlayerStats.Build(run); run.level[s]--;
            var l = new List<string>();
            if (after.maxHp != before.maxHp) l.Add($"最大HP +{after.maxHp - before.maxHp}");
            if (after.attack != before.attack) l.Add($"攻撃力 +{after.attack - before.attack}");
            float dr = (after.dmgReduction - before.dmgReduction) * 100f; if (dr > 0.05f) l.Add($"受けるダメージ -{dr:0.#}%");
            float en = after.maxEnergy - before.maxEnergy; if (en > 0.05f) l.Add($"エネルギー最大 +{en:0}");
            float rg = after.energyRegen - before.energyRegen; if (rg > 0.05f) l.Add($"エネルギー回復 +{rg:0.#}/秒");
            float cm = (before.energyCostMul - after.energyCostMul) * 100f; if (cm > 0.05f) l.Add($"ダッシュ等の消費 -{cm:0.#}%");
            return l.Count == 0 ? "数値の変化はわずか(重ねると効いてくる)" : string.Join("\n", l);
        }
        string UpgradeCardText(RunState run, int s, bool compact)
        {
            var e = run.equipped[s]; string name = e ? e.displayName : "-";
            string lv = $"Lv{run.level[s]} → Lv{run.level[s] + 1}";
            if (compact) return $"<color=#FFD060>強化 / {slotNames[s]}</color>  <color=#FFE9A8>{lv}</color>\n{name}\n{UpgradeInfo(run, s)}";
            return $"<color=#FFD060>強化 / {slotNames[s]}</color>\n<size=48>{name}</size>\n<size=48><color=#FFE9A8>{lv}</color></size>\n{UpgradeInfo(run, s)}\n<color=#9FD8FF>選ぶとHPが1回復</color>";
        }

        void DoUpgrade(int s)
        {
            if (used) return; used = true; var run = GameManager.Instance.Run;
            run.Upgrade((EquipSlot)s); AddHp(1); notice = $"{slotNames[s]}を強化した (HP +1)"; Refresh();
        }
        void DoHeal() { if (used) return; used = true; AddHp(3); notice = "休んで回復した: HP +3"; Refresh(); }
        void AddHp(int n) { var r = GameManager.Instance.Run; r.hp = Mathf.Min(r.hp + n, PlayerStats.Build(r).maxHp); }

        void Refresh()
        {
            var gm = GameManager.Instance; var run = gm.Run; var save = gm.Save; var st = PlayerStats.Build(run);
            var sb = new StringBuilder();
            sb.AppendLine($"工房ランク {rankNames[Mathf.Clamp(save.rank, 0, 4)]}   HP {run.hp}/{st.maxHp}   攻撃力 {st.attack + run.AttackBonus()}   エネルギー {st.maxEnergy:0}   汎用素材 {save.commonMaterials}");
            sb.AppendLine($"敵の強さ HP×{gm.EnemyHpMul:0.00} / ダメージ+{gm.EnemyDmgBonus}   装備パワー {run.EquipPower()}" + (st.setBonus ? "   ★統一ボーナス" : ""));
            if (notice != "") sb.AppendLine(notice);
            else if (run.relics.Count > 0) sb.AppendLine("遺物: " + string.Join(" / ", run.relics.ConvertAll(x => x.displayName)));
            status.text = sb.ToString();

            foreach (Transform c in main) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            bool relic = run.relicOffers.Count > 0; int h = relic ? 300 : 440;
            if (relic)
            {
                AddLabel("遺物を1つ選んでください(選ぶまで出発できません)");
                var row = AddRow(h);
                foreach (var rl in new List<RelicData>(run.relicOffers))
                {
                    var r = rl;
                    AddCard(row, $"<color=#9FD8FF>遺物</color>\n<size=48>{r.displayName}</size>\n{r.description}", true, Slate, () => { run.TakeRelic(r); Refresh(); });
                }
            }
            AddLabel(used ? "今回の整備は済みました" : "強化を1つ選んでください(強化しない場合は、下の「回復して休む」)");
            var row2 = AddRow(h);
            foreach (int slot in upSlots) { int s = slot; AddCard(row2, UpgradeCardText(run, s, relic), !used, BrassLo, () => DoUpgrade(s)); }
            repairButton.interactable = !used; departButton.interactable = !relic;
        }

        // ---------- 装備詳細パネル ----------
        void BuildDetail()
        {
            detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image)); detailPanel.transform.SetParent(Root, false);
            detailPanel.GetComponent<Image>().color = new Color32(0x1A, 0x11, 0x0B, 255); Stretch((RectTransform)detailPanel.transform, 0, 0);
            var title = MakeText("Title", detailPanel.transform, "装備詳細", 64, BrassHi, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1500f, 80f));
            var bar = new GameObject("DTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)); bar.transform.SetParent(detailPanel.transform, false);
            Anchor((RectTransform)bar.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(1700f, 80f));
            var h = bar.GetComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            string[] names = { "全体", "帽子", "作業着", "安全靴", "レンチ" };
            for (int i = 0; i < 5; i++)
            {
                int id = i; dBtn[i] = MakeButton("D" + i, bar.transform, names[i], 32, BrassLo, Cream);
                dBtn[i].onClick.AddListener(() => { detailTab = id; RefreshDetail(); });
            }
            detailText = MakeText("Body", detailPanel.transform, "", 32, Cream, TextAlignmentOptions.TopLeft);
            Anchor(detailText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(1700f, 700f));
            detailText.overflowMode = TextOverflowModes.Overflow;
            var close = MakeButton("Close", detailPanel.transform, "閉じる", 48, Brass, Dark);
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
            close.onClick.AddListener(CloseDetail); detailPanel.SetActive(false);
        }
        void OpenDetail() { detailTab = 0; detailPanel.SetActive(true); detailPanel.transform.SetAsLastSibling(); RefreshDetail(); }
        void CloseDetail() { detailPanel.SetActive(false); }

        static string EquipStats(RunState run, int s)
        {
            var e = run.equipped[s]; if (e == null) return "";
            float m = PlayerStats.TierMul(run.tier[s]); float lv = run.level[s] * (1f + 0.2f * run.Rank);
            var l = new List<string>();
            int hp = Mathf.RoundToInt((e.hpBase + e.hpPerLevel * lv) * m); if (hp != 0) l.Add($"HP+{hp}");
            float dr = (e.dmgRedBase + e.dmgRedPerLevel * lv) * m; if (dr > 0.0001f) l.Add($"軽減+{dr * 100f:0}%");
            int at = Mathf.RoundToInt((e.attackBase + e.attackPerLevel * lv) * m); if (at != 0) l.Add($"攻撃+{at}");
            float en = (e.energyMaxBase + e.energyMaxPerLevel * lv) * m; if (en > 0.01f) l.Add($"EN最大+{en:0}");
            float rg = (e.regenBase + e.regenPerLevel * lv) * m; if (rg > 0.01f) l.Add($"EN回復+{rg:0.#}/秒");
            float cr = e.costReducePerLevel * lv; if (cr > 0.0001f) l.Add($"消費-{cr * 100f:0}%");
            if (e.extraJumps > 0) l.Add($"空中ジャンプ+{e.extraJumps}");
            if (e.jumpBonus > 0.0001f) l.Add($"ジャンプ+{e.jumpBonus * 100f:0}%");
            if (e.moveBonus > 0.0001f) l.Add($"移動+{e.moveBonus * 100f:0}%");
            return l.Count == 0 ? "(数値の強化なし)" : string.Join("  ", l);
        }
        void RefreshDetail()
        {
            var gm = GameManager.Instance; var run = gm.Run; var st = PlayerStats.Build(run); var sb = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                dBtn[i].GetComponent<Image>().color = i == detailTab ? BrassHi : BrassLo;
                dBtn[i].GetComponentInChildren<TMP_Text>().color = i == detailTab ? Dark : Cream;
            }
            if (detailTab == 0)
            {
                sb.AppendLine("<color=#D9A520>【ステータス合計】</color>");
                sb.AppendLine($"最大HP {st.maxHp}   攻撃力 {st.attack + run.AttackBonus()}   ダメージ軽減 {st.dmgReduction * 100f:0}%");
                sb.AppendLine($"エネルギー最大 {st.maxEnergy:0}   回復 {st.energyRegen:0.#}/秒   消費 ×{st.energyCostMul:0.00}");
                sb.AppendLine($"ジャンプ ×{st.jumpMul:0.00}   移動 ×{st.moveMul:0.00}   空中ジャンプ {st.extraJumps}回");
                sb.AppendLine("装備統一ボーナス: " + (st.setBonus ? "発動中" : "なし"));
                sb.AppendLine(); sb.AppendLine("<color=#D9A520>【遺物】</color>");
                if (run.relics.Count == 0) sb.AppendLine("なし");
                foreach (var r in run.relics) sb.AppendLine($"{r.displayName}: {r.description}");
                sb.AppendLine(); sb.AppendLine("<color=#D9A520>【今回の獲得素材】(周回の終了時に清算されます)</color>");
                bool any = false;
                foreach (var a in gm.allAreas)
                {
                    var line = new StringBuilder();
                    for (int t = 0; t < 5; t++) { int n = run.Mat(a, (Tier)t); if (n > 0) line.Append($"{((Tier)t).Label()}x{n} "); }
                    if (line.Length > 0) { sb.AppendLine($"{a.materialName}: {line}"); any = true; }
                }
                if (!any) sb.AppendLine("なし");
                sb.AppendLine(); sb.AppendLine($"敵の強さ: HP×{gm.EnemyHpMul:0.00} / ダメージ+{gm.EnemyDmgBonus}   (装備パワー {run.EquipPower()})");
            }
            else
            {
                int s = detailTab - 1; var e = run.equipped[s];
                sb.AppendLine($"<color=#D9A520>【{slotNames[s]}】</color>");
                if (e == null) sb.AppendLine("装備なし");
                else
                {
                    sb.AppendLine($"{e.displayName}  [{run.tier[s].Label()}]  Lv{run.level[s]}"); sb.AppendLine();
                    sb.AppendLine("数値: " + EquipStats(run, s)); sb.AppendLine();
                    if (e.passive != null) sb.AppendLine($"特性: {e.passive.displayName}\n  {e.passive.Describe(run.tier[s])}"); else sb.AppendLine("特性: なし");
                    sb.AppendLine();
                    if (e.ability != null) sb.AppendLine($"アクション[{keys[s]}]: {e.ability.displayName}   消費EN{e.ability.energyCost:0} / CT{e.ability.cooldown:0.#}秒");
                    else sb.AppendLine("アクション: なし");
                }
                var names = new List<string>();
                foreach (var kv in run.StashList()) if ((int)kv.Key.slot == s) names.Add($"{kv.Key.displayName}[{kv.Value.tier.Label()}]Lv{kv.Value.level}");
                sb.AppendLine(); sb.AppendLine("持ち物: " + (names.Count == 0 ? "なし" : string.Join(" / ", names)));
            }
            detailText.text = sb.ToString();
        }

        void Update()
        {
            var k = Keyboard.current; if (k == null || detailPanel == null || !detailPanel.activeSelf) return;
            if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) CloseDetail();
        }
    }
}