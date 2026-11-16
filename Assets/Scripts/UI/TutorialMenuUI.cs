using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    // チュートリアルのレッスン選択(操作説明から開く)
    public class TutorialMenuUI : MonoBehaviour
    {
        static int closedFrame = -1;
        public static bool IsOpen { get; private set; }
        public static bool Blocking => IsOpen || Time.frameCount <= closedFrame;
        GameObject panel; RectTransform list;

        public void Init(Canvas c)
        {
            panel = UiKit.Fullscreen(c.rootCanvas.transform, "TutorialMenuPanel");
            var title = UiKit.Txt(panel.transform, "チュートリアル", 64, UiKit.BrassHi, TextAlignmentOptions.Center, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1500f, 80f));
            var go = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup)); go.transform.SetParent(panel.transform, false);
            list = (RectTransform)go.transform; UiKit.Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(1300f, 780f));
            var v = go.GetComponent<VerticalLayoutGroup>(); v.spacing = 8; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var close = UiKit.Btn(panel.transform, "閉じる", 48, UiKit.Brass, UiKit.Dark);
            UiKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(420f, 100f));
            close.onClick.AddListener(Close); panel.SetActive(false);
        }
        public void Open() { panel.SetActive(true); panel.transform.SetAsLastSibling(); IsOpen = true; Rebuild(); }
        void Close() { panel.SetActive(false); IsOpen = false; closedFrame = Time.frameCount; }
        void OnDestroy() { IsOpen = false; }
        void Update() { if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close(); }

        void Rebuild()
        {
            foreach (Transform c in list) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            var gm = GameManager.Instance; string last = null;
            for (int i = 0; i < TutorialManager.Lessons.Length; i++)
            {
                var (group, name, _) = TutorialManager.Lessons[i];
                if (group != last)
                {
                    last = group; var h = UiKit.Txt(list, group, 32, UiKit.BrassHi, TextAlignmentOptions.Left, "Group"); UiKit.Size(h.gameObject, 0f, 52f);
                }
                int id = i; bool done = gm.Save.Stat("tutorial_" + i) > 0;
                var b = UiKit.Btn(list, (done ? "[済] " : "") + name, 32, UiKit.BrassLo, UiKit.Cream, "Lesson");
                UiKit.Size(b.gameObject, 0f, 90f); b.onClick.AddListener(() => gm.StartTutorial(id));
            }
        }
    }
}