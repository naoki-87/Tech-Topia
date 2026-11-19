using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // タイトル: スタート / 装備一覧 / 実績 を縦に、その下に 操作説明 / 設定 を横に並べる
    public class TitleUI : MonoBehaviour
    {
        public Button startButton; public TMP_Text info;
                    // 旧UI(使わない。割り当てはそのままでよい)
        static readonly string[] rankNames = { "素人", "職人", "達人", "工房", "伝説" };
        static Color Dark => UiKit.Dark; static Color Brass => UiKit.Brass; static Color Cream => UiKit.Cream;
        RectTransform menu; TMP_Text toast; float toastUntil; EquipmentUI equip; HelpPanel help; AchievementUI ach; SettingsUI set; TutorialMenuUI tut;

        void Start()
        {
            if (startButton) startButton.gameObject.SetActive(false);
            
            var canvas = info.canvas.rootCanvas;
            equip = gameObject.AddComponent<EquipmentUI>(); equip.Init(canvas);
            ach = gameObject.AddComponent<AchievementUI>(); ach.Init(canvas);
            set = gameObject.AddComponent<SettingsUI>(); set.Init(canvas);
            tut = gameObject.AddComponent<TutorialMenuUI>(); tut.Init(canvas);
            help = FindFirstObjectByType<HelpPanel>();
            if (help) help.tutorialAction = () => tut.Open();

            var go = new GameObject("TitleMenu", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
            menu = (RectTransform)go.transform; menu.anchorMin = Vector2.zero; menu.anchorMax = Vector2.one; menu.offsetMin = menu.offsetMax = Vector2.zero;
            Big("スタート", -430f, () => GameManager.Instance.StartRun());
            Big("装備一覧", -570f, () => equip.Open());
            Big("実績", -710f, () => ach.Open());
            Small("操作説明", -145f, -870f, () => { if (help) help.Open(); else Toast("操作説明が見つかりません"); });
            Small("設定", 145f, -870f, () => set.Open());

            var t = new GameObject("Toast", typeof(RectTransform), typeof(TextMeshProUGUI)); t.transform.SetParent(canvas.transform, false);
            toast = t.GetComponent<TextMeshProUGUI>(); toast.fontSize = 32; toast.color = Cream; toast.alignment = TextAlignmentOptions.Center; toast.raycastTarget = false;
            var r = toast.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.pivot = new Vector2(0.5f, 0f); r.anchoredPosition = new Vector2(0f, 60f); r.sizeDelta = new Vector2(1500f, 50f);
            toast.text = "";
        }

        void Add(string label, float x, float y, float w, float h, int size, UnityEngine.Events.UnityAction act)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(menu, false);
            go.GetComponent<Image>().color = Brass;
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
            var tg = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); tg.transform.SetParent(go.transform, false);
            var t = tg.GetComponent<TextMeshProUGUI>(); t.text = label; t.fontSize = size; t.color = Dark; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            var lr = t.rectTransform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(8f, 4f); lr.offsetMax = new Vector2(-8f, -4f);
            go.GetComponent<Button>().onClick.AddListener(act);
        }
        void Big(string label, float y, UnityEngine.Events.UnityAction act) => Add(label, 0f, y, 560f, 120f, 48, act);
        void Small(string label, float x, float y, UnityEngine.Events.UnityAction act) => Add(label, x, y, 270f, 100f, 32, act);
        void Toast(string s) { toast.text = s; toastUntil = Time.unscaledTime + 2.5f; }

        void Update()
        {
            var sv = GameManager.Instance.Save;
            info.text = $"コア {sv.cores}   工房ランク {rankNames[Mathf.Clamp(sv.rank, 0, 4)]}   汎用素材 {sv.commonMaterials}";
            menu.gameObject.SetActive(!HelpPanel.IsOpen && !EquipmentUI.IsOpen && !AchievementUI.IsOpen && !SettingsUI.IsOpen && !TutorialMenuUI.IsOpen);
            if (toast.text != "" && Time.unscaledTime > toastUntil) toast.text = "";
        }
    }
}