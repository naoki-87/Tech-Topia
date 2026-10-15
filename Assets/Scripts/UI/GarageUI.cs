using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // ステージ間ガレージ: 強化3択(どれか1つ) / 修理 / 素材合成 / 装備作成 / 出発
    public class GarageUI : MonoBehaviour
    {
        public Button[] upgradeButtons; public TMP_Text[] upgradeLabels;
        public Button repairButton, departButton; public TMP_Text status;
        public Transform listRoot; public Button listButtonPrefab;      // 合成・作成ボタンの動的生成用
        bool used; static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        void Start()
        {
            var gm = GameManager.Instance; var run = gm.Run;
            var slots = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < upgradeButtons.Length && slots.Count > 0; i++)
            {
                int s = slots[Random.Range(0, slots.Count)]; slots.Remove(s);
                var eq = run.equipped[s]; upgradeLabels[i].text = $"{slotNames[s]} {(eq ? eq.displayName : "")}\nLv{run.level[s]}→{run.level[s] + 1}";
                upgradeButtons[i].onClick.AddListener(() => { if (used) return; used = true; run.Upgrade((EquipSlot)s); Heal(1); Lock(); });
            }
            repairButton.onClick.AddListener(() => { if (used) return; used = true; Heal(3); Lock(); });
            departButton.onClick.AddListener(gm.AfterGarage);
            Refresh();
        }
        void Heal(int n) { var r = GameManager.Instance.Run; r.hp = Mathf.Min(PlayerStats.Build(r).maxHp, r.hp + n); Refresh(); }
        void Lock() { foreach (var b in upgradeButtons) b.interactable = false; repairButton.interactable = false; }
        void Refresh()
        {
            var gm = GameManager.Instance; var run = gm.Run;
            status.text = $"HP {run.hp}/{PlayerStats.Build(run).maxHp}   汎用素材 {gm.Save.commonMaterials}";
            foreach (Transform c in listRoot) Destroy(c.gameObject);
            foreach (var a in gm.allAreas)
            {
                for (int t = 0; t < 4; t++)     // 合成
                {
                    var area = a; var tier = (Tier)t; if (run.Mat(area, tier) < 3) continue;
                    var b = Instantiate(listButtonPrefab, listRoot);
                    b.GetComponentInChildren<TMP_Text>().text = $"合成 {area.materialName}[{tier.Label()}]x3→[{((Tier)(t + 1)).Label()}]";
                    b.onClick.AddListener(() => { run.Fuse(area, tier); Refresh(); });
                }
                if (a.craftable == null) continue;
                foreach (var e in a.craftable)  // 作成
                {
                    if (!run.CanCraft(e)) continue; var eq = e;
                    var b = Instantiate(listButtonPrefab, listRoot);
                    b.GetComponentInChildren<TMP_Text>().text = $"作成 {eq.displayName}[{eq.tier.Label()}]";
                    b.onClick.AddListener(() => { run.Craft(eq); Refresh(); });
                }
            }
        }
    }
}
