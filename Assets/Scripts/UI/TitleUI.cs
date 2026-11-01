using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // タイトル: スタート / 基礎強化(コア消費・永続) / 開始レベルの調整(縛りプレイ)
    public class TitleUI : MonoBehaviour
    {
        public Button startButton; public TMP_Text info;
        public Transform rowRoot; public BaseRow rowPrefab;
        readonly BaseRow[] rows = new BaseRow[RunState.Slots];
        static readonly string[] names = { "帽子", "作業着", "安全靴", "レンチ" };
        static readonly string[] rankNames = { "素人", "職人", "達人", "工房", "伝説" };

        void Start()
        {
            var gm = GameManager.Instance;
            startButton.onClick.AddListener(gm.StartRun);
            for (int i = 0; i < rows.Length; i++)
            {
                int s = i; var r = Instantiate(rowPrefab, rowRoot); rows[i] = r;
                r.spend.onClick.AddListener(() => { if (gm.Save.SpendCore((EquipSlot)s)) gm.Save.Write(); Refresh(); });
                r.minus.onClick.AddListener(() => Change(s, -1));
                r.plus.onClick.AddListener(() => Change(s, +1));
            }
            Refresh();
        }

        void Change(int slot, int delta)
        {
            var sv = GameManager.Instance.Save;
            sv.startLevel[slot] = Mathf.Clamp(sv.StartLevel(slot) + delta, 0, sv.baseLevel[slot]);
            sv.Write(); Refresh();
        }

        void Refresh()
        {
            var sv = GameManager.Instance.Save;
            info.text = $"コア {sv.cores}   工房ランク {rankNames[Mathf.Clamp(sv.rank, 0, 4)]}   汎用素材 {sv.commonMaterials}";
            for (int i = 0; i < rows.Length; i++)
            {
                int b = sv.baseLevel[i], st = sv.StartLevel(i);
                rows[i].label.text = $"{names[i]}   基礎Lv {b}   開始Lv {st}" + (st < b ? "  (縛り)" : "");
                rows[i].spend.interactable = sv.cores > 0;
                rows[i].minus.interactable = st > 0;
                rows[i].plus.interactable = st < b;
            }
        }
    }
}