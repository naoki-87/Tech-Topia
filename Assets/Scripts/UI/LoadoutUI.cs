using System; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.SceneManagement; using UnityEngine.UI;
namespace SteamEngineer
{
    // 装備変更。Title: 次の周回に持ち越す装備を選ぶ / AreaSelect: 周回中の装備を切り替える。UIはコードで作る
    // 使い方: Title と AreaSelect の空のオブジェクトに付ける(Canvas が必要)
    public class LoadoutUI : MonoBehaviour
    {
        static int closedFrame = -1;
        public static bool IsOpen { get; private set; }
        public static bool Blocking => IsOpen || Time.frameCount <= closedFrame;

        public Vector2 buttonPos = new Vector2(-480f, 250f);     // 開くボタンの位置(下中央基準)
        static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        static readonly Color Cream = new Color32(0xF0, 0xDC, 0xB8, 255), Dark = new Color32(0x2B, 0x1D, 0x12, 255);
        static readonly Color Brass = new Color32(0xB8, 0x86, 0x0B, 255), BrassHi = new Color32(0xD9, 0xA5, 0x20, 255), BrassLo = new Color32(0x6B, 0x4F, 0x1D, 255);

        Canvas canvas; GameObject panel, openBtn; RectTransform content; TMP_Text hint; bool titleMode;

        void Start()
        {
            var c = GetComponentInParent<Canvas>(); if (c == null) c = FindFirstObjectByType<Canvas>();
            if (c == null) { Debug.LogError("LoadoutUI: Canvas が見つかりません"); return; }
            canvas = c.rootCanvas; titleMode = SceneManager.GetActiveScene().name == "Title";
            BuildOpenButton(); BuildPanel();
            IsOpen = false; RefreshOpenButton();
        }
        void OnDestroy() { IsOpen = false; }

        // ---------- UI部品 ----------
        static TMP_Text MakeText(string name, Transform parent, string text, int size, Color col, TextAlignmentOptions al)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = col; t.alignment = al; t.raycastTarget = false;
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

        void BuildOpenButton()
        {
            var b = MakeButton("LoadoutOpen", canvas.transform, "装備変更", 48, Brass, Dark);
            Anchor((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), buttonPos, new Vector2(360f, 110f));
            b.onClick.AddListener(Open); openBtn = b.gameObject;
        }
        void BuildPanel()
        {
            panel = new GameObject("LoadoutPanel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = new Color32(0x1A, 0x11, 0x0B, 255); Stretch((RectTransform)panel.transform, 0, 0);
            var title = MakeText("Title", panel.transform, "装備変更", 64, BrassHi, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1500, 80));
            hint = MakeText("Hint", panel.transform, "", 32, Cream, TextAlignmentOptions.Center);
            Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(1700, 50));
            var cg = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)); cg.transform.SetParent(panel.transform, false);
            content = (RectTransform)cg.transform; Anchor(content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1700, 700));
            var v = cg.GetComponent<VerticalLayoutGroup>(); v.spacing = 6; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var close = MakeButton("Close", panel.transform, "閉じる", 48, Brass, Dark);
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(420, 100));
            close.onClick.AddListener(Close);
            panel.SetActive(false);
        }

        // ---------- 開閉 ----------
        void RefreshOpenButton()
        {
            var gm = GameManager.Instance; bool show = false;
            if (gm != null) show = titleMode ? gm.Save.owned.Count > 0 : (gm.Run != null && gm.Run.stash.Count > 0);
            if (openBtn) openBtn.SetActive(show && !IsOpen);
        }
        public void Open()
        {
            panel.SetActive(true); panel.transform.SetAsLastSibling(); IsOpen = true; openBtn.SetActive(false); Rebuild();
        }
        public void Close() { panel.SetActive(false); IsOpen = false; closedFrame = Time.frameCount; RefreshOpenButton(); }
        void Update()
        {
            if (!IsOpen) return; var k = Keyboard.current; if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Close();
        }

        // ---------- 中身 ----------
        void AddOption(Transform row, string line1, string line2, bool selected, Action onClick)
        {
            var b = MakeButton("Opt", row, line1 + "\n" + line2, 32, selected ? BrassHi : BrassLo, selected ? Dark : Cream);
            var le = b.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 520; le.minWidth = 380; le.preferredHeight = 90;
            if (onClick != null) b.onClick.AddListener(() => onClick()); else b.interactable = false;
            if (!b.interactable) b.GetComponent<Image>().color = selected ? BrassHi : BrassLo;
        }
        void Rebuild()
        {
            foreach (Transform c in content) Destroy(c.gameObject);
            var gm = GameManager.Instance;
            hint.text = titleMode ? "ここで選んだ装備で、次の周回を始めます" : "押すと装備を切り替えます(ガレージでも切り替えられます)";
            for (int i = 0; i < RunState.Slots; i++)
            {
                int s = i;
                var head = MakeText("Head", content, slotNames[s], 32, Cream, TextAlignmentOptions.Left);
                head.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                var rowGo = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)); rowGo.transform.SetParent(content, false);
                rowGo.GetComponent<LayoutElement>().preferredHeight = 90;
                var h = rowGo.GetComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;

                if (titleMode)
                {
                    string cur = gm.Save.loadout[s];
                    AddOption(rowGo.transform, cur == "" ? "選択中" : "初期装備", gm.starters[s].displayName, cur == "", () => Set(s, ""));
                    foreach (var o in gm.Save.owned)
                    {
                        var e = gm.FindEquipment(o.id); if (e == null || (int)e.slot != s) continue;
                        string id = o.id; string tierText = gm.keepTierOnCarryOver ? $"[{((Tier)o.tier).Label()}]" : "";
                        AddOption(rowGo.transform, cur == id ? "選択中" : "解放済み", e.displayName + tierText, cur == id, () => Set(s, id));
                    }
                }
                else
                {
                    var run = gm.Run; var ce = run.equipped[s];
                    AddOption(rowGo.transform, "装着中", $"{(ce ? ce.displayName : "-")}[{run.tier[s].Label()}]Lv{run.level[s]}", true, null);
                    foreach (var kv in run.StashList())
                    {
                        if ((int)kv.Key.slot != s) continue; var e = kv.Key; var st = kv.Value;
                        AddOption(rowGo.transform, "持ち物", $"{e.displayName}[{st.tier.Label()}]Lv{st.level}", false, () => { run.Equip(e); Rebuild(); });
                    }
                }
            }
        }
        void Set(int slot, string id) { var sv = GameManager.Instance.Save; sv.loadout[slot] = id; sv.Write(); Rebuild(); }
    }
}