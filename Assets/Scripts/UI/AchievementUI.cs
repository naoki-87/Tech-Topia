using System; using System.Text; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    // 実績画面: メインルート / サブルート / コレクション(装備・遺物)
    public class AchievementUI : MonoBehaviour
    {
        static int closedFrame = -1;
        public static bool IsOpen { get; private set; }
        public static bool Blocking => IsOpen || Time.frameCount <= closedFrame;
        static readonly string[] tabNames = { "メインルート", "サブルート", "コレクション" };
        static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        static Color Cream => UiKit.Cream; static Color Dark => UiKit.Dark;
        static Color Brass => UiKit.Brass; static Color BrassHi => UiKit.BrassHi; static Color BrassLo => UiKit.BrassLo;

        GameObject panel; TMP_Text infoText; ScrollRect scroll; RectTransform content;
        readonly Button[] tabBtn = new Button[3]; int tab;
        GameManager G => GameManager.Instance; SaveData S => GameManager.Instance.Save;

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
            var canvas = c.rootCanvas;
            panel = new GameObject("AchievementPanel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = new Color32(0x1A, 0x11, 0x0B, 255); Stretch((RectTransform)panel.transform, 0, 0);
            var title = MakeText("Title", panel.transform, "実績", 64, BrassHi, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1500f, 70f));
            infoText = MakeText("Info", panel.transform, "", 32, Cream, TextAlignmentOptions.Center);
            Anchor(infoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(1800f, 50f));
            var bar = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)); bar.transform.SetParent(panel.transform, false);
            Anchor((RectTransform)bar.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1400f, 70f));
            var h = bar.GetComponent<HorizontalLayoutGroup>(); h.spacing = 10; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            for (int i = 0; i < 3; i++)
            {
                int id = i; tabBtn[i] = MakeButton("Tab" + i, bar.transform, tabNames[i], 32, BrassLo, Cream);
                tabBtn[i].onClick.AddListener(() => { tab = id; scroll.verticalNormalizedPosition = 1f; Rebuild(); });
            }
            var sgo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image)); sgo.transform.SetParent(panel.transform, false);
            sgo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            var srt = (RectTransform)sgo.transform; srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = new Vector2(120f, 170f); srt.offsetMax = new Vector2(-120f, -245f);
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); vp.transform.SetParent(sgo.transform, false); Stretch((RectTransform)vp.transform, 0, 0);
            var ct = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); ct.transform.SetParent(vp.transform, false);
            content = (RectTransform)ct.transform; content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var v = ct.GetComponent<VerticalLayoutGroup>(); v.spacing = 10; v.padding = new RectOffset(10, 10, 10, 10);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            ct.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = sgo.GetComponent<ScrollRect>(); scroll.viewport = (RectTransform)vp.transform; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;
            var close = MakeButton("Close", panel.transform, "閉じる", 48, Brass, Dark);
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
            close.onClick.AddListener(Close); panel.SetActive(false);
        }

        public void Open() { tab = 0; panel.SetActive(true); panel.transform.SetAsLastSibling(); IsOpen = true; Rebuild(); scroll.verticalNormalizedPosition = 1f; }
        void Close() { panel.SetActive(false); IsOpen = false; closedFrame = Time.frameCount; }
        void OnDestroy() { IsOpen = false; }
        void Update()
        {
            if (!IsOpen) return; var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame) Close();
#if UNITY_EDITOR
            // テスト用(エディタのみ): F4 = 周回クリア数 +1 して実績を判定
            if (k.f4Key.wasPressedThisFrame) { S.AddStat("runs_cleared", 1); Achievements.CheckAll(S); Rebuild(); }
#endif
        }

        void AddHeader(string text) { var t = MakeText("H", content, text, 32, BrassHi, TextAlignmentOptions.Left); t.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f; }
        void AddCard(string text, bool highlight)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup)); go.transform.SetParent(content, false);
            go.GetComponent<Image>().color = highlight ? new Color32(0x4A, 0x3A, 0x16, 255) : new Color32(0x3A, 0x2A, 0x1C, 255);
            var v = go.GetComponent<VerticalLayoutGroup>(); v.padding = new RectOffset(24, 24, 14, 14);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            MakeText("T", go.transform, text, 32, Cream, TextAlignmentOptions.TopLeft);
        }

        void Rebuild()
        {
            foreach (Transform c in content) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            int done = 0, total = Achievements.All.Count; foreach (var d in Achievements.All) if (S.IsAchieved(d.id)) done++;
            infoText.text = $"達成 {done}/{total}";
            for (int i = 0; i < 3; i++)
            {
                tabBtn[i].GetComponent<Image>().color = i == tab ? BrassHi : BrassLo;
                tabBtn[i].GetComponentInChildren<TMP_Text>().color = i == tab ? Dark : Cream;
            }
            if (tab == 2) BuildCollection(); else BuildRoute(tab == 0 ? AchRoute.Main : AchRoute.Sub);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
        void BuildRoute(AchRoute r)
        {
            int n = 0;
            foreach (var d in Achievements.All) { if (d.route != r) continue; AddCard(Achievements.Describe(d, S), S.IsAchieved(d.id)); n++; }
            if (n == 0) AddHeader("このルートの実績は、準備中です");
        }
        void BuildCollection()
        {
            int have = 0, all = 0; var sb = new StringBuilder();
            foreach (var s in G.starters) if (s != null) { all++; have++; }
            foreach (var a in G.allAreas) if (a != null && a.craftable != null) foreach (var e in a.craftable) if (e != null) { all++; if (Owned(e)) have++; }
            AddHeader($"装備  {have}/{all}");
            foreach (var s in G.starters) if (s != null) AddCard($"◆ {s.displayName}  ({slotNames[(int)s.slot]})  [初期装備 / 最高 {TierColors.Name(S.GetTier(s.name))}]", true);
            foreach (var a in G.allAreas)
            {
                if (a == null || a.craftable == null) continue;
                foreach (var e in a.craftable)
                {
                    if (e == null) continue;
                    if (Owned(e)) AddCard($"◆ {e.displayName}  ({slotNames[(int)e.slot]})  [最高 {TierColors.Name(S.GetTier(e.name))}]\n{e.description}", true);
                    else AddCard($"◇ ???  ({slotNames[(int)e.slot]})  {a.displayName}の素材で作れる", false);
                }
            }
            int rh = 0, ra = 0;
            if (G.relicPool != null) foreach (var r in G.relicPool) if (r != null) { ra++; if (S.discovered.Contains("relic:" + r.name)) rh++; }
            AddHeader($"遺物  {rh}/{ra}");
            if (G.relicPool != null)
                foreach (var r in G.relicPool)
                {
                    if (r == null) continue;
                    if (S.discovered.Contains("relic:" + r.name)) AddCard($"◆ {r.displayName}\n{r.description}", true); else AddCard("◇ ???", false);
                }
        }
        bool Owned(EquipmentData e) { foreach (var o in S.owned) if (o.id == e.name) return true; return false; }
    }
}