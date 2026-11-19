using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // タイトル画面の1行分(部位ごとの基礎強化と開始レベル)
    public class BaseRow : MonoBehaviour
    {
        public TMP_Text label;
        public Button spend, minus, plus;

        static void Fix(GameObject g, float w, float h)
        {
            var le = g.GetComponent<LayoutElement>(); if (!le) le = g.AddComponent<LayoutElement>();
            le.minWidth = w; le.preferredWidth = w; le.minHeight = h; le.preferredHeight = h;
            le.flexibleWidth = 0; le.flexibleHeight = 0;
        }

        void Awake()
        {
            var h = GetComponent<HorizontalLayoutGroup>(); if (!h) h = gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            Fix(label.gameObject, 780, 56);
            Fix(spend.gameObject, 200, 56);
            Fix(minus.gameObject, 72, 56);
            Fix(plus.gameObject, 72, 56);

            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = false; label.fontSize = 32; label.raycastTarget = false;          // ラベルがクリックを邪魔しないようにする
            foreach (var b in new[] { spend, minus, plus })
            {
                var t = b.GetComponentInChildren<TMP_Text>();
                if (t) { t.enableAutoSizing = false; t.fontSize = 32; t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false; }
            }
        }
    }
}