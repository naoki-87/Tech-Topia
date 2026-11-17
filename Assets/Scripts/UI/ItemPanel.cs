using System.Collections.Generic; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.SceneManagement; using UnityEngine.UI;
namespace SteamEngineer
{
    public class ItemPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        GameObject panel, openBtn; RectTransform list; TMP_Text msgText; UseScene scene; Canvas canvas;

        void Start()
        {
            var c = FindFirstObjectByType<Canvas>(); if (c == null) { enabled = false; return; }
            canvas = c.rootCanvas; scene = SceneManager.GetActiveScene().name == "AreaSelect" ? UseScene.AreaSelect : UseScene.Garage;
            var b = UiKit.Btn(canvas.transform, "アイテム", 32, UiKit.Brass, UiKit.Dark, "ItemOpen");
            if (scene == UseScene.Garage)
                UiKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-225f, 40f), new Vector2(420f, 110f));
            else
                UiKit.Place((RectTransform)b.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 100f));
            b.onClick.AddListener(Open); openBtn = b.gameObject;
            panel = UiKit.Fullscreen(canvas.transform, "ItemPanel");
            var title = UiKit.Txt(panel.transform, "アイテム", 64, UiKit.BrassHi, TextAlignmentOptions.Center, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1500f, 80f));
            msgText = UiKit.Txt(panel.transform, "", 32, UiKit.Cream, TextAlignmentOptions.Center, "Msg");
            UiKit.Place(msgText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(1600f, 50f));
            var go = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup)); go.transform.SetParent(panel.transform, false);
            list = (RectTransform)go.transform; UiKit.Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1500f, 700f));
            var v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 10; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var close = UiKit.Btn(panel.transform, "閉じる", 48, UiKit.Brass, UiKit.Dark);
            UiKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f));
            close.onClick.AddListener(Close); panel.SetActive(false);
            RefreshButton();
        }
        void RefreshButton()
        {
            var run = GameManager.Instance.Run; int n = 0; if (run != null) foreach (var kv in run.ItemList()) n += kv.Value;
            var t = openBtn.GetComponentInChildren<TMP_Text>(); t.text = n > 0 ? $"アイテム ({n})" : "アイテム";
        }
        void Open() { panel.SetActive(true); panel.transform.SetAsLastSibling(); IsOpen = true; msgText.text = ""; Rebuild(); }
        void Close() { panel.SetActive(false); IsOpen = false; RefreshButton(); }
        void OnDestroy() { IsOpen = false; }
        void Update() { if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close(); }

        void Rebuild()
        {
            foreach (Transform c in list) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            var run = GameManager.Instance.Run; int n = 0;
            foreach (var kv in run.ItemList())
            {
                var it = kv.Key; if (kv.Value <= 0) continue; n++;
                string why = ItemManager.Why(it, run, scene);
                var b = UiKit.Btn(list, $"{it.displayName} x{kv.Value}\n{it.description}\n使える場面: {ItemManager.WhenText(it)}" + (why == "" ? "" : $"\n<color=#ff8080>{why}</color>"), 32,
                                  UiKit.BrassLo, UiKit.Cream, "Item");
                b.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft; UiKit.Stretch(b.GetComponentInChildren<TMP_Text>().rectTransform, 24f, 6f);
                UiKit.Size(b.gameObject, 0f, why == "" ? 150f : 190f); b.interactable = why == "";
                b.onClick.AddListener(() => { msgText.text = ItemManager.Use(it, run, scene); Rebuild(); });
            }
            if (n == 0) { var t = UiKit.Txt(list, "持ち込んだアイテムはありません。タイトルの「装備一覧 > アイテム」で作成し、持ち込みを設定します", 32, UiKit.Cream, TextAlignmentOptions.Center, "None"); UiKit.Size(t.gameObject, 0f, 120f); }
        }
    }
}