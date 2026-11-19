using UnityEngine;
namespace SteamEngineer
{
    // ティアの色: 粗雑=白 → 凡作=緑 → 洗練=青 → 傑作=紫 → 至高=金(将来 赤)
    // Text: 暗い背景の上の文字 / Back: 背景や枠 / Deep: 明るい背景(選択中のボタンなど)の上の文字
    public static class TierColors
    {
        static readonly Color32[] text = { new Color32(0xE8, 0xE8, 0xE8, 255), new Color32(0x7B, 0xE0, 0x7B, 255), new Color32(0x6F, 0xB4, 0xFF, 255), new Color32(0xC0, 0x8B, 0xFF, 255), new Color32(0xFF, 0xD0, 0x60, 255) };
        static readonly Color32[] back = { new Color32(0x4A, 0x4A, 0x4A, 255), new Color32(0x2F, 0x5A, 0x2F, 255), new Color32(0x2A, 0x4F, 0x8A, 255), new Color32(0x5A, 0x3A, 0x8A, 255), new Color32(0x8A, 0x6A, 0x10, 255) };
        static readonly Color32[] deep = { new Color32(0x4A, 0x4A, 0x4A, 255), new Color32(0x1F, 0x6A, 0x1F, 255), new Color32(0x1F, 0x4F, 0xA0, 255), new Color32(0x5A, 0x2A, 0x9A, 255), new Color32(0x5A, 0x3F, 0x00, 255) };
        static int I(Tier t) { return Mathf.Clamp((int)t, 0, text.Length - 1); }      // 赤を足すときは、3つの配列に1つずつ足す

        public static Color Text(Tier t) { return text[I(t)]; }
        public static Color Back(Tier t) { return back[I(t)]; }
        public static Color Deep(Tier t) { return deep[I(t)]; }
        public static string Hex(Tier t, bool onLight = false) { return "#" + ColorUtility.ToHtmlStringRGB(onLight ? Deep(t) : Text(t)); }
        public static string Rich(Tier t, string s, bool onLight = false) { return "<color=" + Hex(t, onLight) + ">" + s + "</color>"; }
        public static string Name(Tier t, bool onLight = false) { return Rich(t, t.Label(), onLight); }          // 色付きの「凡作」
        public static string Tag(Tier t, bool onLight = false) { return Rich(t, "[" + t.Label() + "]", onLight); } // 色付きの「[凡作]」
    }
}
