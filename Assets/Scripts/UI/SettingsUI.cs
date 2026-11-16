using System; using System.Collections.Generic; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    public class SettingsUI : MonoBehaviour
    {
        static int closedFrame = -1;
        public static bool IsOpen { get; private set; }
        public static bool Blocking => IsOpen || Time.frameCount <= closedFrame;
        GameObject panel; RectTransform rows;

        public void Init(Canvas c)
        {
            panel = UiKit.Fullscreen(c.rootCanvas.transform, "SettingsPanel");
            var title = UiKit.Txt(panel.transform, "設定", 64, UiKit.BrassHi, TextAlignmentOptions.Center, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1500f, 80f));
            var go = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup)); go.transform.SetParent(panel.transform, false);
            rows = (RectTransform)go.transform; UiKit.Place(rows, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1500f, 700f));
            var v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 18; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var close = UiKit.Btn(panel.transform, "閉じる", 48, UiKit.Brass, UiKit.Dark);
            UiKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
            close.onClick.AddListener(Close); panel.SetActive(false);
        }
        public void Open() { panel.SetActive(true); panel.transform.SetAsLastSibling(); IsOpen = true; Rebuild(); }
        void Close() { panel.SetActive(false); IsOpen = false; closedFrame = Time.frameCount; }
        void OnDestroy() { IsOpen = false; }
        void Update() { if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close(); }

        Transform Row(string label)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup)); go.transform.SetParent(rows, false);
            UiKit.Size(go, 0f, 90f);
            var h = go.GetComponent<HorizontalLayoutGroup>(); h.spacing = 12; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            var t = UiKit.Txt(go.transform, label, 32, UiKit.Cream, TextAlignmentOptions.MidlineLeft, "Label"); UiKit.Size(t.gameObject, 520f, 0f);
            return go.transform;
        }
        void Opt(Transform row, string label, bool selected, bool ok, Action act, float w = 300f)
        {
            var b = UiKit.Btn(row, label, 32, selected ? UiKit.BrassHi : UiKit.BrassLo, selected ? UiKit.Dark : UiKit.Cream);
            UiKit.Size(b.gameObject, w, 0f); b.interactable = ok; b.onClick.AddListener(() => { act(); Rebuild(); });
        }
        static string Bar(float v) { int n = Mathf.RoundToInt(v * 10f); return new string('■', n) + new string('□', 10 - n) + $"  {Mathf.RoundToInt(v * 100f)}%"; }

        void Rebuild()
        {
            foreach (Transform c in rows) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            bool full = Settings.Fullscreen;
            var r = Row("画面モード");
            Opt(r, "フルスクリーン", full, true, () => { Settings.Fullscreen = true; Settings.Apply(); });
            Opt(r, "ウィンドウ", !full, true, () => { Settings.Fullscreen = false; Settings.Apply(); });

            r = Row("ウィンドウの大きさ"); var list = Settings.Resolutions(); int idx = Settings.ResIndex(list);
            Opt(r, "<", false, !full && idx > 0, () => SetRes(list[idx - 1]), 110f);
            var t = UiKit.Txt(r, $"{list[idx].x} × {list[idx].y}", 32, full ? new Color(0.6f, 0.6f, 0.6f) : UiKit.Cream, TextAlignmentOptions.Center, "Res"); UiKit.Size(t.gameObject, 300f, 0f);
            Opt(r, ">", false, !full && idx < list.Count - 1, () => SetRes(list[idx + 1]), 110f);
            if (full) UiKit.Txt(rows, "※ ウィンドウの大きさは、「ウィンドウ」モードのときに変えられます", 32, UiKit.Cream, TextAlignmentOptions.Left, "Note");

            r = Row("BGM音量");
            Opt(r, "-", false, Settings.Bgm > 0.01f, () => Settings.Bgm -= 0.1f, 110f);
            var tb = UiKit.Txt(r, Bar(Settings.Bgm), 32, UiKit.Cream, TextAlignmentOptions.Center, "Bar"); UiKit.Size(tb.gameObject, 520f, 0f);
            Opt(r, "+", false, Settings.Bgm < 0.99f, () => Settings.Bgm += 0.1f, 110f);

            r = Row("効果音の音量");
            Opt(r, "-", false, Settings.Se > 0.01f, () => { Settings.Se -= 0.1f; AudioManager.Jump(); }, 110f);
            var ts = UiKit.Txt(r, Bar(Settings.Se), 32, UiKit.Cream, TextAlignmentOptions.Center, "Bar"); UiKit.Size(ts.gameObject, 520f, 0f);
            Opt(r, "+", false, Settings.Se < 0.99f, () => { Settings.Se += 0.1f; AudioManager.Jump(); }, 110f);

            r = Row("");
            Opt(r, "初期設定に戻す", false, true, Settings.ResetAll, 420f);
        }
        void SetRes(Vector2Int v) { Settings.WindowSize = v; Settings.Apply(); }
    }
}