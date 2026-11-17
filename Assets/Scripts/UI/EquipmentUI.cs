using System; using System.Collections.Generic; using System.Text; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    // 装備一覧: 装備 / 作成 / ティア / 基礎強化 / 素材。清算済みの素材(在庫)を使う
    public class EquipmentUI : MonoBehaviour
    {
        static int closedFrame = -1;
        public static bool IsOpen { get; private set; }
        public static bool Blocking => IsOpen || Time.frameCount <= closedFrame;
        const int MaxBase = 10;
        static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        static readonly string[] keys = { "I", "O", "P", "L" };
        static readonly string[] rankNames = { "素人", "職人", "達人", "工房", "伝説" };
        static readonly string[] tabNames = { "装備", "作成", "ティア", "基礎強化", "素材", "アイテム" };
        static readonly Color Cream = new Color32(0xF0, 0xDC, 0xB8, 255), Dark = new Color32(0x2B, 0x1D, 0x12, 255);
        static readonly Color Brass = new Color32(0xB8, 0x86, 0x0B, 255), BrassHi = new Color32(0xD9, 0xA5, 0x20, 255), BrassLo = new Color32(0x6B, 0x4F, 0x1D, 255);

        Canvas canvas; GameObject panel; TMP_Text infoText, detailText;
        RectTransform scrollRT, content; ScrollRect scroll;
        readonly Button[] tabBtn = new Button[6];
        int tab; string notice = ""; EquipmentData detailEquip;
        GameManager G => GameManager.Instance; SaveData S => GameManager.Instance.Save;
        Tier MaxTier => S.MaxTier;

        // ---------- UI部品 ----------
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

        public void Init(Canvas c)
        {
            canvas = c.rootCanvas;
            panel = new GameObject("EquipmentPanel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = new Color32(0x1A, 0x11, 0x0B, 255); Stretch((RectTransform)panel.transform, 0, 0);
            var title = MakeText("Title", panel.transform, "装備一覧", 64, BrassHi, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1500f, 70f));
            infoText = MakeText("Info", panel.transform, "", 32, Cream, TextAlignmentOptions.Center);
            Anchor(infoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(1800f, 80f));
            var bar = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)); bar.transform.SetParent(panel.transform, false);
            Anchor((RectTransform)bar.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -182f), new Vector2(1800f, 70f));
            var h = bar.GetComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            for (int i = 0; i < 6; i++)
            {
                int id = i; tabBtn[i] = MakeButton("Tab" + i, bar.transform, tabNames[i], 32, BrassLo, Cream);
                tabBtn[i].onClick.AddListener(() => { tab = id; scroll.verticalNormalizedPosition = 1f; Rebuild(); });
            }
            BuildScroll(panel.transform);
            detailText = MakeText("Detail", panel.transform, "", 32, Cream, TextAlignmentOptions.TopLeft);
            Anchor(detailText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -265f), new Vector2(680f, 740f));
            var close = MakeButton("Close", panel.transform, "閉じる", 48, Brass, Dark);
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
            close.onClick.AddListener(Close);
            panel.SetActive(false);
        }
        void BuildScroll(Transform parent)
        {
            var sgo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image)); sgo.transform.SetParent(parent, false);
            sgo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f); scrollRT = (RectTransform)sgo.transform;
            scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); vp.transform.SetParent(sgo.transform, false); Stretch((RectTransform)vp.transform, 0, 0);
            var ct = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); ct.transform.SetParent(vp.transform, false);
            content = (RectTransform)ct.transform; content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var v = ct.GetComponent<VerticalLayoutGroup>(); v.spacing = 8; v.padding = new RectOffset(10, 10, 10, 10);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            ct.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = sgo.GetComponent<ScrollRect>(); scroll.viewport = (RectTransform)vp.transform; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;
        }

        // ---------- 項目の追加 ----------
        void AddHeader(string text, float h = 56f)
        {
            var t = MakeText("Header", content, text, 32, BrassHi, TextAlignmentOptions.Left);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
        }
        Transform AddRow(float h)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)); go.transform.SetParent(content, false);
            go.GetComponent<LayoutElement>().preferredHeight = h;
            var hl = go.GetComponent<HorizontalLayoutGroup>(); hl.spacing = 10;
            hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            return go.transform;
        }
        Button AddBtn(Transform parent, string label, bool ok, bool sel, Action act, float minW = 240f)
        {
            var b = MakeButton("B", parent, label, 32, sel ? BrassHi : BrassLo, sel ? Dark : Cream);
            var le = b.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1f; le.minWidth = minW;
            var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f); cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f); b.colors = cb;
            b.interactable = ok; if (act != null) b.onClick.AddListener(() => { act(); Rebuild(); });
            return b;
        }
        void AddEntry(string label, bool ok, Action act)
        {
            var b = AddBtn(content, label, ok, false, act, 300f);
            var t = b.GetComponentInChildren<TMP_Text>(); t.alignment = TextAlignmentOptions.MidlineLeft; Stretch(t.rectTransform, 20f, 6f);
            b.GetComponent<LayoutElement>().preferredHeight = 96f;
        }

        // ---------- 開閉 ----------
        public void Open() { tab = 0; detailEquip = null; notice = ""; panel.SetActive(true); panel.transform.SetAsLastSibling(); IsOpen = true; scroll.verticalNormalizedPosition = 1f; Rebuild(); }
        void Close() { panel.SetActive(false); IsOpen = false; closedFrame = Time.frameCount; S.Write(); }
        void OnDestroy() { IsOpen = false; }
        void Update()
        {
            if (!IsOpen) return; var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) Close();
#if UNITY_EDITOR
            // テスト用(エディタのみ): F1=汎用素材+50 / F2=全エリアの素材を在庫に追加 / F3=工房ランク+1
            if (k.f1Key.wasPressedThisFrame) { S.commonMaterials += 50; Rebuild(); }
            if (k.f2Key.wasPressedThisFrame)
            { foreach (var a in G.allAreas) { S.AddStock(a.name, Tier.Crude, 12); S.AddStock(a.name, Tier.Common, 9); S.AddStock(a.name, Tier.Refined, 6); } Rebuild(); }
            if (k.f3Key.wasPressedThisFrame) { S.rank = Mathf.Min(4, S.rank + 1); Rebuild(); }
#endif
        }

        void Rebuild()
        {
            float pos = scroll.verticalNormalizedPosition;
            foreach (Transform c in content) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            infoText.text = $"コア {S.cores}   汎用素材 {S.commonMaterials}   工房ランク {rankNames[Mathf.Clamp(S.rank, 0, 4)]}   作成・ティアの上限 [{MaxTier.Label()}]"
                            + (notice != "" ? "\n" + notice : "");
            for (int i = 0; i < 6; i++)
            {
                tabBtn[i].GetComponent<Image>().color = i == tab ? BrassHi : BrassLo;
                tabBtn[i].GetComponentInChildren<TMP_Text>().color = i == tab ? Dark : Cream;
            }
            tabBtn[2].GetComponentInChildren<TMP_Text>().text = S.Has(Unlocks.TierRank) ? tabNames[2] : tabNames[2] + "(未解放)";
            tabBtn[3].GetComponentInChildren<TMP_Text>().text = S.Has(Unlocks.BaseEnhance) ? tabNames[3] : tabNames[3] + "(未解放)";
            scrollRT.offsetMin = new Vector2(60f, 170f); scrollRT.offsetMax = new Vector2(tab == 0 ? -760f : -60f, -265f);
            detailText.gameObject.SetActive(tab == 0);
            switch (tab)
            {
                case 0: BuildEquip(); detailText.text = DetailOf(detailEquip); break;
                case 1: BuildCraft(); break;
                case 2: if (S.Has(Unlocks.TierRank)) BuildTier(); else BuildLocked("main_workshop"); break;
                case 3: if (S.Has(Unlocks.BaseEnhance)) BuildBase(); else BuildLocked("main_first"); break;
                case 4: BuildMat(); break;
                default: BuildItems(); break;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(content); Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(pos);
        }

        bool Owns(EquipmentData e) { foreach (var o in S.owned) if (o.id == e.name) return true; return false; }

        // ---------- タブ0: 装備(装着の変更) ----------
        void BuildEquip()
        {
            for (int s = 0; s < 4; s++)
            {
                int slot = s; var starter = G.starters[s]; string cur = S.loadout[s];
                var curEq = cur == "" ? starter : (G.FindEquipment(cur) ?? starter);
                AddHeader($"{slotNames[s]}   装着中: {curEq.displayName}[{S.GetTier(curEq.name).Label()}]   基礎Lv {S.baseLevel[s]}");
                var row = AddRow(96f);
                AddBtn(row, $"初期装備\n{starter.displayName}[{S.GetTier(starter.name).Label()}]", true, cur == "", () => Pick(slot, "", starter));
                foreach (var o in S.owned)
                {
                    var e = G.FindEquipment(o.id); if (e == null || (int)e.slot != s) continue;
                    string id = o.id; var eq = e;
                    AddBtn(row, $"解放済み\n{e.displayName}[{S.GetTier(e.name).Label()}]", true, cur == id, () => Pick(slot, id, eq));
                }
            }
        }
        void Pick(int slot, string id, EquipmentData e)
        {
            S.loadout[slot] = id; S.Write(); detailEquip = e; notice = $"{slotNames[slot]}: {e.displayName} を装着します(次の周回から)";
        }
        static string StatsText(EquipmentData e, Tier t, int level, int rank)
        {
            float m = PlayerStats.TierMul(t); float lv = level * (1f + 0.2f * rank); var l = new List<string>();
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
        string DetailOf(EquipmentData e)
        {
            if (e == null) return "<color=#D9A520>装備の説明</color>\n装備のボタンを押すと、装着して、ここに説明が出ます。";
            var t = S.GetTier(e.name); int lv = S.StartLevel((int)e.slot); var sb = new StringBuilder();
            sb.AppendLine($"<color=#D9A520>{e.displayName}</color>  [{t.Label()}]  {slotNames[(int)e.slot]}");
            sb.AppendLine(e.isStarter ? "入手: 初期装備" : $"入手: {(e.sourceArea ? e.sourceArea.displayName : "-")}の素材で作成");
            if (!string.IsNullOrEmpty(e.description)) sb.AppendLine("\n" + e.description);
            sb.AppendLine($"\n数値(開始Lv{lv}): " + StatsText(e, t, lv, S.rank));
            if (e.passive != null) sb.AppendLine($"\n特性: {e.passive.displayName}\n{e.passive.Describe(t)}");
            if (e.ability != null) sb.AppendLine($"\nアクション[{keys[(int)e.slot]}]: {e.ability.displayName}\n消費EN{e.ability.energyCost:0} / CT{e.ability.cooldown:0.#}秒");
            return sb.ToString();
        }

        // ---------- タブ1: 作成 ----------
        int CraftCommon(EquipmentData e, Tier t) => e.commonCost * ((int)t + 1);
        string CraftWhy(AreaData a, EquipmentData e, Tier t)
        {
            if (t > MaxTier) return S.Has(Unlocks.TierRank) ? "工房ランク不足" : "上位のティアは未解放(実績)";
            int have = S.Stock(a.name, t); if (have < e.coreCost) return $"素材不足 {have}/{e.coreCost}";
            int need = CraftCommon(e, t); if (S.commonMaterials < need) return $"汎用素材不足 {S.commonMaterials}/{need}";
            return "";
        }
        void BuildCraft()
        {
            AddHeader("素材と汎用素材で、新しい装備を作ります。作った装備は「装備」タブで装着できます", 90f);
            int n = 0;
            foreach (var a in G.allAreas)
            {
                if (a == null || a.craftable == null) continue;
                foreach (var e in a.craftable)
                {
                    if (e == null || Owns(e)) continue;
                    for (int t = 0; t <= (int)MaxTier; t++)
                    {
                        var tr = (Tier)t; var eq = e; var ar = a;
                        if (t > 0 && S.Stock(a.name, tr) <= 0) continue;           // 粗雑は常に表示、上位は素材があるときだけ
                        string why = CraftWhy(a, eq, tr);
                        AddEntry($"作成 {eq.displayName}[{tr.Label()}] ({slotNames[(int)eq.slot]})  {a.materialName}[{tr.Label()}]x{eq.coreCost} + 汎用x{CraftCommon(eq, tr)}"
                                 + (why == "" ? "" : "\n<color=#ff8080>" + why + "</color>"), why == "", () => DoCraft(ar, eq, tr)); n++;
                    }
                }
            }
            if (n == 0) AddHeader("作成できる装備はありません", 60f);
        }
        void DoCraft(AreaData a, EquipmentData e, Tier t)
        {
            if (CraftWhy(a, e, t) != "") return;
            S.AddStock(a.name, t, -e.coreCost); S.commonMaterials -= CraftCommon(e, t); S.Unlock(e, t);
            notice = $"作成した: {e.displayName}[{t.Label()}]"; S.Write();
        }

        // ---------- タブ2: ティア上昇 ----------
        int TierCore(EquipmentData e) => e.coreCost * Mathf.Max(1, G.tierUpCoreMul);
        int TierCommon(EquipmentData e, Tier next) => e.commonCost * ((int)next + 1) * Mathf.Max(1, G.tierUpCommonMul);
        string TierWhy(EquipmentData e, Tier next)
        {
            if (e.sourceArea == null) return "Source Area が未設定";
            if (next > MaxTier) return "工房ランク不足(実績で解放)";
            int have = S.Stock(e.sourceArea.name, next), core = TierCore(e); if (have < core) return $"素材不足 {have}/{core}";
            int com = TierCommon(e, next); if (S.commonMaterials < com) return $"汎用素材不足 {S.commonMaterials}/{com}";
            return "";
        }
        void BuildTier()
        {
            AddHeader("装備のティアを上げます。次のティアの素材が必要です(作成より重い費用)", 90f);
            var list = new List<EquipmentData>(); foreach (var s in G.starters) if (s != null) list.Add(s);
            foreach (var o in S.owned) { var e = G.FindEquipment(o.id); if (e != null) list.Add(e); }
            foreach (var e in list)
            {
                var eq = e; var t = S.GetTier(e.name); string head = $"{slotNames[(int)e.slot]} {e.displayName}";
                if (t == Tier.Supreme) { AddEntry($"{head} [{t.Label()}]  最高のティアです", false, null); continue; }
                var next = t + 1; string why = TierWhy(e, next);
                string mat = e.sourceArea ? e.sourceArea.materialName : "?";
                AddEntry($"{head} [{t.Label()}→{next.Label()}]  {mat}[{next.Label()}]x{TierCore(e)} + 汎用x{TierCommon(e, next)}"
                         + (why == "" ? "" : "\n<color=#ff8080>" + why + "</color>"), why == "", () => DoTierUp(eq, next));
            }
        }
        void DoTierUp(EquipmentData e, Tier next)
        {
            if (TierWhy(e, next) != "") return;
            S.AddStock(e.sourceArea.name, next, -TierCore(e)); S.commonMaterials -= TierCommon(e, next); S.SetTier(e.name, next);
            notice = $"{e.displayName} が [{next.Label()}] になった"; S.Write();
        }

        void BuildLocked(string achId)
        {
            AddHeader("【未解放】この機能は、実績を達成すると使えるようになります", 70f);
            var d = Achievements.Find(achId); if (d == null) return;
            var t = MakeText("Lock", content, Achievements.Describe(d, S), 32, Cream, TextAlignmentOptions.TopLeft);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 260f;
        }

        // ---------- タブ3: 基礎強化 ----------
        void BuildBase()
        {
            AddHeader("コア1つで、部位の基礎レベルが1上がります。開始レベルは基礎レベルまで下げられます(縛りプレイ)", 90f);
            for (int s = 0; s < 4; s++)
            {
                int slot = s; var row = AddRow(96f);
                var lbl = MakeText("L", row, $"{slotNames[s]}   基礎Lv {S.baseLevel[s]}/{MaxBase}   開始Lv {S.StartLevel(s)}" + (S.StartLevel(s) < S.baseLevel[s] ? "  (縛り)" : ""), 32, Cream, TextAlignmentOptions.MidlineLeft);
                var le = lbl.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 2f; le.minWidth = 560f;
                AddBtn(row, "基礎+1\nコア1", S.cores > 0 && S.baseLevel[s] < MaxBase, false, () => { S.SpendCore((EquipSlot)slot); S.Write(); notice = $"{slotNames[slot]}の基礎レベルが上がった"; }, 200f);
                AddBtn(row, "開始Lv\n-", S.StartLevel(s) > 0, false, () => { S.startLevel[slot] = Mathf.Max(0, S.StartLevel(slot) - 1); S.Write(); }, 120f);
                AddBtn(row, "開始Lv\n+", S.StartLevel(s) < S.baseLevel[s], false, () => { S.startLevel[slot] = Mathf.Min(S.baseLevel[slot], S.StartLevel(slot) + 1); S.Write(); }, 120f);
            }
        }

        // ---------- タブ5: アイテム(作成と持ち込み) ----------
        void BuildItems()
        {
            AddHeader($"補助アイテムを、汎用素材で作ります。1周に、種類ごとに最大{ItemManager.CarryPerKind}個まで持ち込めます。使い残しは、周回の終了時に消えます", 100f);
            if (G.itemPool == null || G.itemPool.Length == 0) { AddHeader("アイテムが設定されていません(Tools > TechTopia > 補助アイテムを作成)", 60f); return; }
            foreach (var it in G.itemPool)
            {
                if (it == null) continue; var item = it;
                bool open = string.IsNullOrEmpty(it.requireUnlock) || S.Has(it.requireUnlock);
                int own = S.ItemOwned(it.name), carry = S.ItemCarry(it.name);
                AddHeader($"{it.displayName}   所持 {own}   持ち込み {carry}/{ItemManager.CarryPerKind}", 48f);
                var desc = MakeText("D", content, $"{it.description}\n使える場面: {ItemManager.WhenText(it)}", 32, Cream, TextAlignmentOptions.TopLeft);
                desc.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
                var row = AddRow(96f);
                string why = !open ? "未解放(実績)" : S.commonMaterials < it.craftCost ? $"汎用素材不足 {S.commonMaterials}/{it.craftCost}" : "";
                AddBtn(row, $"作成\n汎用 x{it.craftCost}" + (why == "" ? "" : $"\n<color=#ff8080>{why}</color>"), why == "",
                       false, () => { S.commonMaterials -= item.craftCost; S.AddItem(item.name, 1); notice = $"作成した: {item.displayName}"; S.Write(); }, 300f);
                AddBtn(row, "持ち込み\n-", carry > 0, false, () => { S.SetCarry(item.name, S.ItemCarry(item.name) - 1); S.Write(); }, 200f);
                AddBtn(row, "持ち込み\n+", carry < Mathf.Min(own, ItemManager.CarryPerKind), false, () => { S.SetCarry(item.name, S.ItemCarry(item.name) + 1); S.Write(); }, 200f);
            }
        }

        // ---------- タブ4: 素材(合成・分解) ----------
        void BuildMat()
        {
            AddHeader("素材の在庫です(周回の終了時に清算された素材)。合成は3個で上位1個、分解は汎用素材になります", 90f);
            bool any = false;
            foreach (var a in G.allAreas)
                for (int t = 0; t < 5; t++)
                {
                    var area = a; var tier = (Tier)t; int n = S.Stock(a.name, tier); if (n <= 0) continue; any = true;
                    AddHeader($"{a.materialName}[{tier.Label()}]  在庫 {n}", 48f);
                    if (n >= 3 && t < 4)
                    {
                        bool ok = tier < MaxTier;
                        AddEntry($"合成 x3 → {a.materialName}[{((Tier)(t + 1)).Label()}]" + (ok ? "" : "\n<color=#ff8080>工房ランク不足</color>"), ok,
                                 () => { S.AddStock(area.name, tier, -3); S.AddStock(area.name, tier + 1, 1); notice = "合成した"; S.Write(); });
                    }
                    int y = G.disassembleBase * (t + 1);
                    AddEntry($"分解 x1 → 汎用素材 +{y}", true, () => { S.AddStock(area.name, tier, -1); S.commonMaterials += y; notice = $"分解した: 汎用素材 +{y}"; S.Write(); });
                    if (n >= 2)
                        AddEntry($"分解 x{n}(すべて) → 汎用素材 +{y * n}", true, () => { S.AddStock(area.name, tier, -n); S.commonMaterials += y * n; notice = $"分解した: 汎用素材 +{y * n}"; S.Write(); });
                }
            if (!any) AddHeader("素材はありません。周回で集めた素材は、周回の終了時にここへ入ります", 60f);
        }
    }
}