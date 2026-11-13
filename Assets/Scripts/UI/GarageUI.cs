using System.Collections.Generic; using System.Text; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    // ステージ間ガレージ: 上部に状態、タブで画面を切り替え(遺物 / 整備 / 作成・強化 / 素材 / 切替)+ 装備詳細パネル
    public class GarageUI : MonoBehaviour
    {
        public Button[] upgradeButtons; public TMP_Text[] upgradeLabels;
        public Button repairButton, departButton; public TMP_Text status;
        public Transform listRoot; public Button listButtonPrefab;

        class Entry { public string label; public bool ok; public System.Action act; }
        const int TRelic = 0, TPrep = 1, TCraft = 2, TMat = 3, TSwitch = 4;
        static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        static readonly string[] keys = { "I", "O", "P", "L" };
        static readonly string[] rankNames = { "素人", "職人", "達人", "工房", "伝説" };
        static readonly Color Cream = new Color32(0xF0, 0xDC, 0xB8, 255), Dark = new Color32(0x2B, 0x1D, 0x12, 255);
        static readonly Color Brass = new Color32(0xB8, 0x86, 0x0B, 255), BrassHi = new Color32(0xD9, 0xA5, 0x20, 255), BrassLo = new Color32(0x6B, 0x4F, 0x1D, 255);

        bool used; string notice = ""; int tab = TPrep, detailTab;
        GameObject scrollGo, detailPanel; TMP_Text detailText;
        readonly Button[] tabBtn = new Button[5]; readonly TMP_Text[] tabTxt = new TMP_Text[5]; readonly Button[] dBtn = new Button[5];
        readonly List<Entry>[] lists = new List<Entry>[5];

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

        void Start()
        {
            var gm = GameManager.Instance; var run = gm.Run;
            for (int i = 0; i < lists.Length; i++) lists[i] = new List<Entry>();
            SetupLayout(); BuildTabBar(); BuildDetail();

            // 強化の3択(1回のガレージで1つ)
            var slots = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < upgradeButtons.Length && slots.Count > 0; i++)
            {
                int s = slots[Random.Range(0, slots.Count)]; slots.Remove(s);
                var eq = run.equipped[s];
                upgradeLabels[i].text = $"{slotNames[s]} {(eq ? eq.displayName : "")}\nLv{run.level[s]}→{run.level[s] + 1}";
                upgradeButtons[i].onClick.AddListener(() =>
                { if (used) return; used = true; run.Upgrade((EquipSlot)s); notice = $"{slotNames[s]}を強化した"; Heal(1); Lock(); });
            }
            repairButton.onClick.AddListener(() => { if (used) return; used = true; notice = "修理した: HP +3"; Heal(3); Lock(); });
            departButton.onClick.AddListener(gm.AfterGarage);
            tab = run.relicOffers.Count > 0 ? TRelic : TPrep;
            Refresh();
        }

        void SetupLayout()
        {
            Anchor((RectTransform)status.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -30f), new Vector2(1800f, 190f));
            status.enableAutoSizing = false; status.fontSize = 32; status.alignment = TextAlignmentOptions.TopLeft;
            status.textWrappingMode = TextWrappingModes.Normal; status.overflowMode = TextOverflowModes.Truncate; status.raycastTarget = false;
            var scroll = listRoot.GetComponentInParent<ScrollRect>(); scrollGo = scroll != null ? scroll.gameObject : null;
            if (scrollGo != null)
            {
                var r = (RectTransform)scrollGo.transform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(60f, 170f); r.offsetMax = new Vector2(-60f, -340f);
            }
        }

        void BuildTabBar()
        {
            var bar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup)); bar.transform.SetParent(status.canvas.rootCanvas.transform, false);
            Anchor((RectTransform)bar.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1800f, 90f));
            var h = bar.GetComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            string[] names = { "遺物を選ぶ", "整備", "作成・強化", "素材", "切替" };
            for (int i = 0; i < 5; i++)
            {
                int id = i; var b = MakeButton("Tab" + i, bar.transform, names[i], 32, BrassLo, Cream);
                tabBtn[i] = b; tabTxt[i] = b.GetComponentInChildren<TMP_Text>();
                b.onClick.AddListener(() => { tab = id; Refresh(); });
            }
            var d = MakeButton("TabDetail", bar.transform, "装備詳細", 32, Brass, Dark); d.onClick.AddListener(OpenDetail);
        }

        // ---------- 装備詳細パネル ----------
        void BuildDetail()
        {
            var root = status.canvas.rootCanvas.transform;
            detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image)); detailPanel.transform.SetParent(root, false);
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
            close.onClick.AddListener(CloseDetail);
            detailPanel.SetActive(false);
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
                sb.AppendLine();
                sb.AppendLine("<color=#D9A520>【遺物】</color>");
                if (run.relics.Count == 0) sb.AppendLine("なし");
                foreach (var r in run.relics) sb.AppendLine($"{r.displayName}: {r.description}");
                sb.AppendLine();
                sb.AppendLine("<color=#D9A520>【所持素材】</color>");
                bool any = false;
                foreach (var a in gm.allAreas)
                {
                    var line = new StringBuilder();
                    for (int t = 0; t < 5; t++) { int n = run.Mat(a, (Tier)t); if (n > 0) line.Append($"{((Tier)t).Label()}x{n} "); }
                    if (line.Length > 0) { sb.AppendLine($"{a.materialName}: {line}"); any = true; }
                }
                if (!any) sb.AppendLine("なし");
                sb.AppendLine();
                sb.AppendLine($"敵の強さ: HP×{gm.EnemyHpMul:0.00} / ダメージ+{gm.EnemyDmgBonus}   (装備パワー {run.EquipPower()})");
            }
            else
            {
                int s = detailTab - 1; var e = run.equipped[s];
                sb.AppendLine($"<color=#D9A520>【{slotNames[s]}】</color>");
                if (e == null) sb.AppendLine("装備なし");
                else
                {
                    sb.AppendLine($"{e.displayName}  [{run.tier[s].Label()}]  Lv{run.level[s]}");
                    sb.AppendLine();
                    sb.AppendLine("数値: " + EquipStats(run, s));
                    sb.AppendLine();
                    if (e.passive != null) sb.AppendLine($"特性: {e.passive.displayName}\n  {e.passive.Describe(run.tier[s])}"); else sb.AppendLine("特性: なし");
                    sb.AppendLine();
                    if (e.ability != null) sb.AppendLine($"アクション[{keys[s]}]: {e.ability.displayName}   消費EN{e.ability.energyCost:0} / CT{e.ability.cooldown:0.#}秒");
                    else sb.AppendLine("アクション: なし");
                }
                var names = new List<string>();
                foreach (var kv in run.StashList()) if ((int)kv.Key.slot == s) names.Add($"{kv.Key.displayName}[{kv.Value.tier.Label()}]Lv{kv.Value.level}");
                sb.AppendLine();
                sb.AppendLine("持ち物: " + (names.Count == 0 ? "なし" : string.Join(" / ", names)));
            }
            detailText.text = sb.ToString();
        }

        // ---------- ガレージ本体 ----------
        void Heal(int n) { var r = GameManager.Instance.Run; r.hp = Mathf.Min(r.hp + n, PlayerStats.Build(r).maxHp); Refresh(); }
        void Lock() { foreach (var b in upgradeButtons) b.interactable = false; repairButton.interactable = false; }
        void RankNotice(int before) { int r = GameManager.Instance.Save.rank; notice = r > before ? $"★ 工房ランクが上がった！ → {rankNames[r]}" : ""; }
        void ApplyUpgrade(int slot) { var gm = GameManager.Instance; int b = gm.Save.rank; if (gm.Run.UpgradeTier(slot)) RankNotice(b); }
        void ApplyCraft(EquipmentData e, Tier t) { var gm = GameManager.Instance; int b = gm.Save.rank; if (gm.Run.Craft(e, t)) RankNotice(b); }

        static string CraftWhy(RunState run, SaveData save, AreaData listed, EquipmentData e, Tier t)
        {
            if (e.sourceArea == null) return "装備の Source Area が未設定";
            if (e.sourceArea != listed) return $"Source Area が {e.sourceArea.name} になっている(要: {listed.name})";
            if (t > run.MaxCraftTier) return "工房ランク不足";
            int have = run.Mat(e.sourceArea, t); if (have < e.coreCost) return $"素材不足 {have}/{e.coreCost}";
            int need = RunState.CommonCraft(e, t); if (save.commonMaterials < need) return $"汎用素材不足 {save.commonMaterials}/{need}";
            return run.CanCraft(e, t) ? "" : "条件を満たしていません";
        }

        void Refresh()
        {
            var gm = GameManager.Instance; var run = gm.Run; var save = gm.Save; var st = PlayerStats.Build(run);
            if (tab == TRelic && run.relicOffers.Count == 0) tab = TPrep;

            // ---- 状態表示(短く) ----
            var sb = new StringBuilder();
            sb.AppendLine($"工房ランク {rankNames[Mathf.Clamp(save.rank, 0, 4)]}   HP {run.hp}/{st.maxHp}   攻撃力 {st.attack + run.AttackBonus()}   エネルギー {st.maxEnergy:0}   汎用素材 {save.commonMaterials}");
            sb.AppendLine($"敵の強さ HP×{gm.EnemyHpMul:0.00} / ダメージ+{gm.EnemyDmgBonus}   装備パワー {run.EquipPower()}" + (st.setBonus ? "   ★統一ボーナス" : ""));
            if (run.relics.Count > 0) sb.AppendLine("遺物: " + string.Join(" / ", run.relics.ConvertAll(x => x.displayName)));
            if (notice != "") sb.AppendLine(notice);
            status.text = sb.ToString();

            // ---- タブごとの項目 ----
            foreach (var l in lists) l.Clear();
            foreach (var rl in run.relicOffers) { var relic = rl; lists[TRelic].Add(new Entry { label = $"【遺物を1つ選ぶ】{relic.displayName}\n{relic.description}", ok = true, act = () => run.TakeRelic(relic) }); }

            for (int s = 0; s < RunState.Slots; s++)                           // 装備のティア強化
            {
                var e = run.equipped[s];
                if (e == null || e.sourceArea == null || run.tier[s] == Tier.Supreme) continue;
                int slot = s; var next = run.tier[s] + 1;
                string why = next > run.MaxCraftTier ? "\n<color=#ff8080>工房ランク不足</color>" : "";
                lists[TCraft].Add(new Entry
                {
                    label = $"強化 {slotNames[s]} {e.displayName} [{run.tier[s].Label()}→{next.Label()}]  {e.sourceArea.materialName}[{next.Label()}]x{run.UpgradeCore(e)} + 汎用x{RunState.CommonUpgrade(e, next)}" + why,
                    ok = run.CanUpgradeTier(slot), act = () => ApplyUpgrade(slot)
                });
            }
            foreach (var a in gm.allAreas)                                      // 装備の作成
            {
                if (a.craftable == null) continue;
                foreach (var e in a.craftable)
                {
                    if (e == null || run.Owns(e)) continue;
                    for (int t = 0; t <= (int)run.MaxCraftTier; t++)
                    {
                        var eq = e; var tr = (Tier)t; if (run.Mat(a, tr) <= 0) continue;
                        string why = CraftWhy(run, save, a, eq, tr);
                        lists[TCraft].Add(new Entry
                        {
                            label = $"作成 {eq.displayName}[{tr.Label()}]  {a.materialName}[{tr.Label()}]x{eq.coreCost} + 汎用x{RunState.CommonCraft(eq, tr)}" + (why == "" ? "" : "\n<color=#ff8080>" + why + "</color>"),
                            ok = run.CanCraft(eq, tr), act = () => ApplyCraft(eq, tr)
                        });
                    }
                }
            }
            foreach (var a in gm.allAreas)                                      // 合成と分解
                for (int t = 0; t < 5; t++)
                {
                    var area = a; var tier = (Tier)t; int have = run.Mat(area, tier);
                    if (have >= 3 && t < 4)
                        lists[TMat].Add(new Entry { label = $"合成 {area.materialName}[{tier.Label()}]x3 → [{((Tier)(t + 1)).Label()}]", ok = tier < run.MaxCraftTier, act = () => run.Fuse(area, tier) });
                    if (have <= 0) continue;
                    int y = RunState.DisassembleYield(tier, gm.disassembleBase);
                    lists[TMat].Add(new Entry { label = $"分解 {area.materialName}[{tier.Label()}]x1 → 汎用素材 +{y}", ok = true, act = () => { run.Disassemble(area, tier, 1, gm.disassembleBase); notice = $"分解した: 汎用素材 +{y}"; } });
                    if (have >= 2)
                        lists[TMat].Add(new Entry { label = $"分解 {area.materialName}[{tier.Label()}]x{have}(すべて) → 汎用素材 +{y * have}", ok = true, act = () => { int g = run.Disassemble(area, tier, 999, gm.disassembleBase); notice = $"分解した: 汎用素材 +{g}"; } });
                }
            foreach (var kv in run.StashList())                                 // 装備の切り替え
            {
                var e = kv.Key; var info = kv.Value; int slot = (int)e.slot; var cur = run.equipped[slot];
                lists[TSwitch].Add(new Entry { label = $"切替 {slotNames[slot]}: {(cur ? cur.displayName : "-")} → {e.displayName}[{info.tier.Label()}]Lv{info.level}", ok = true, act = () => run.Equip(e) });
            }

            // ---- タブの表示 ----
            int okCraft = 0; foreach (var en in lists[TCraft]) if (en.ok) okCraft++;
            tabBtn[TRelic].gameObject.SetActive(run.relicOffers.Count > 0);
            tabTxt[TCraft].text = okCraft > 0 ? $"作成・強化 ({okCraft})" : "作成・強化";
            tabTxt[TMat].text = lists[TMat].Count > 0 ? $"素材 ({lists[TMat].Count})" : "素材";
            tabTxt[TSwitch].text = lists[TSwitch].Count > 0 ? $"切替 ({lists[TSwitch].Count})" : "切替";
            for (int i = 0; i < 5; i++) { tabBtn[i].GetComponent<Image>().color = i == tab ? BrassHi : BrassLo; tabTxt[i].color = i == tab ? Dark : Cream; }

            bool prep = tab == TPrep;
            foreach (var b in upgradeButtons) b.gameObject.SetActive(prep);
            repairButton.gameObject.SetActive(prep);
            if (scrollGo != null) scrollGo.SetActive(!prep);

            if (!prep)
            {
                foreach (Transform c in listRoot) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
                var list = lists[tab];
                if (list.Count == 0) { var b = Instantiate(listButtonPrefab, listRoot); b.GetComponentInChildren<TMP_Text>().text = "(ここでできることはありません)"; b.interactable = false; }
                foreach (var en in list)
                {
                    var b = Instantiate(listButtonPrefab, listRoot); b.GetComponentInChildren<TMP_Text>().text = en.label; b.interactable = en.ok;
                    var act = en.act; b.onClick.AddListener(() => { notice = ""; act(); Refresh(); });
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)listRoot);
            }
            departButton.interactable = run.relicOffers.Count == 0;             // 遺物を選ぶまで出発できない
        }

        void CycleTab(int d)
        {
            var run = GameManager.Instance.Run; int n = tab;
            do { n = (n + d + 5) % 5; } while (n == TRelic && run.relicOffers.Count == 0);
            tab = n; Refresh();
        }

        void Update()
        {
            var k = Keyboard.current; if (k == null) return;
            if (detailPanel != null && detailPanel.activeSelf)
            { if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) CloseDetail(); }
            else
            {
                int d = k.rightArrowKey.wasPressedThisFrame ? 1 : k.leftArrowKey.wasPressedThisFrame ? -1 : 0;
                if (d != 0) CycleTab(d);
            }
#if UNITY_EDITOR
            // テスト用: エディタ上でのみ動く。F1=汎用素材+50、F2=各エリアの素材を追加
            var gm = GameManager.Instance; var run = gm.Run;
            if (k.f1Key.wasPressedThisFrame) { gm.Save.commonMaterials += 50; Refresh(); }
            if (k.f2Key.wasPressedThisFrame)
            {
                foreach (var a in gm.allAreas)
                {
                    run.mats[(a, Tier.Crude)] = run.Mat(a, Tier.Crude) + 6;
                    run.mats[(a, Tier.Common)] = run.Mat(a, Tier.Common) + 3;
                }
                Refresh();
            }
#endif
        }
    }
}