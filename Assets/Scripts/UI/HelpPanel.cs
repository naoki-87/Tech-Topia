using System; using TMPro; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.UI;
namespace SteamEngineer
{
    // タイトル画面の説明パネル。このスクリプトは「常に有効なオブジェクト」に付け、panel に本体を指定する
    public class HelpPanel : MonoBehaviour
    {
        [Serializable] public class Entry { public Sprite sprite; public string title; [TextArea(1, 3)] public string desc; }

        static int closedFrame = -1;
        public System.Action tutorialAction; GameObject tutorialBtn;
        public static bool IsOpen { get; private set; }
        public static bool Blocking => IsOpen || Time.frameCount <= closedFrame;   // 閉じた同じフレームのEnterでスタートしないため

        public GameObject panel;
        public Button openButton, closeButton, prevButton, nextButton;
        public GameObject[] hideWhileOpen;      // 説明を開いている間、隠すUI(スタートボタン、BaseRows など)
        public GameObject[] pages;                                     // 0:操作 1:ギミック 2:敵
        public string[] pageTitles = { "操作方法", "ギミック", "登場する敵" };
        public TMP_Text titleText, pageLabel;
        public HelpCard cardPrefab; public Transform gimmickRoot, enemyRoot;
        public Entry[] gimmicks, enemies;
        int page;

        void Start()
        {
            Build(gimmickRoot, gimmicks); Build(enemyRoot, enemies);
            if (openButton) openButton.onClick.AddListener(Open); closeButton.onClick.AddListener(Close);
            prevButton.onClick.AddListener(() => Go(-1)); nextButton.onClick.AddListener(() => Go(1));
            panel.SetActive(false); IsOpen = false; SetBackUi(true);
        }
        void OnDestroy() { IsOpen = false; }
        void Build(Transform root, Entry[] list)
        {
            if (root == null || list == null) return;
            foreach (var e in list) Instantiate(cardPrefab, root).Set(e.sprite, e.title, e.desc);
        }
        public void Open() { page = 0; panel.SetActive(true); IsOpen = true; SetBackUi(false); EnsureTutorialButton(); Show(); }
        void EnsureTutorialButton()
        {
            if (tutorialAction == null || tutorialBtn != null) return;
            var go = new GameObject("TutorialBtn", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(panel.transform, false);
            go.GetComponent<Image>().color = new Color32(0xB8, 0x86, 0x0B, 255);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f); r.anchoredPosition = new Vector2(60f, -50f); r.sizeDelta = new Vector2(380f, 90f);
            var tg = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI)); tg.transform.SetParent(go.transform, false);
            var tm = tg.GetComponent<TextMeshProUGUI>(); tm.text = "チュートリアル"; tm.fontSize = 32; tm.color = new Color32(0x2B, 0x1D, 0x12, 255); tm.alignment = TextAlignmentOptions.Center; tm.raycastTarget = false;
            var tr = tm.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
            go.GetComponent<Button>().onClick.AddListener(() => { Close(); tutorialAction(); });
            tutorialBtn = go;
        }
        public void Close() { panel.SetActive(false); IsOpen = false; closedFrame = Time.frameCount; SetBackUi(true); }
        void SetBackUi(bool visible)
        {
            if (openButton) openButton.gameObject.SetActive(false);    // タイトルのメニューから開くので、旧ボタンは常に隠す
            if (hideWhileOpen != null) foreach (var g in hideWhileOpen) if (g) g.SetActive(visible);
        }
        void Go(int d) { page = (page + d + pages.Length) % pages.Length; Show(); }
        void Show()
        {
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == page);
            if (titleText) titleText.text = pageTitles[page];
            if (pageLabel) pageLabel.text = $"{page + 1}/{pages.Length}";
        }
        void Update()
        {
            if (!IsOpen) return; var k = Keyboard.current; if (k == null) return;
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Go(-1);
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Go(1);
            if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Close();
        }
    }
}