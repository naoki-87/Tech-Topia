using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // コードでUIを作るときの共通部品
    public static class UiKit
    {
        public static readonly Color Cream = new Color32(0xF0, 0xDC, 0xB8, 255), Dark = new Color32(0x2B, 0x1D, 0x12, 255);
        public static readonly Color Brass = new Color32(0xB8, 0x86, 0x0B, 255), BrassHi = new Color32(0xD9, 0xA5, 0x20, 255);
        public static readonly Color BrassLo = new Color32(0x6B, 0x4F, 0x1D, 255), Back = new Color32(0x1A, 0x11, 0x0B, 255);
        public static readonly Color Slate = new Color32(0x3A, 0x4A, 0x5C, 255);

        public static TMP_Text Txt(Transform parent, string text, int size, Color col, TextAlignmentOptions al, string name = "T")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.color = col; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        public static void Stretch(RectTransform r, float x, float y) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(x, y); r.offsetMax = new Vector2(-x, -y); }
        public static void Place(RectTransform r, Vector2 a, Vector2 pivot, Vector2 pos, Vector2 size) { r.anchorMin = r.anchorMax = a; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; }
        public static Button Btn(Transform parent, string label, int size, Color bg, Color fg, string name = "Btn")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            var t = Txt(go.transform, label, size, fg, TextAlignmentOptions.Center, "Label"); Stretch(t.rectTransform, 8, 4);
            var b = go.GetComponent<Button>(); var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f); cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f); b.colors = cb;
            return b;
        }
        public static GameObject Fullscreen(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Back; Stretch((RectTransform)go.transform, 0, 0); return go;
        }
        public static LayoutElement Size(GameObject g, float w, float h, float flexW = 0f)
        {
            var le = g.GetComponent<LayoutElement>(); if (le == null) le = g.AddComponent<LayoutElement>();
            if (w > 0f) { le.preferredWidth = w; le.minWidth = w; } if (h > 0f) { le.preferredHeight = h; le.minHeight = h; } le.flexibleWidth = flexW; return le;
        }
    }
}