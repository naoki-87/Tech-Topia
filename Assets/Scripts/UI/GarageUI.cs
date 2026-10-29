using System.Text; using TMPro; using UnityEngine; using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif
namespace SteamEngineer
{
    // ステージ間ガレージ: 強化(1回) / 修理 / 素材合成 / 装備のティア強化 / 装備作成 / 出発
    public class GarageUI : MonoBehaviour
    {
        public Button[] upgradeButtons; public TMP_Text[] upgradeLabels;
        public Button repairButton, departButton; public TMP_Text status;
        public Transform listRoot; public Button listButtonPrefab;
        bool used; string notice = "";
        static readonly string[] slotNames = { "帽子", "作業着", "安全靴", "レンチ" };
        static readonly string[] rankNames = { "素人", "職人", "達人", "工房", "伝説" };

        void Start()
        {
            var gm = GameManager.Instance; var run = gm.Run;
            var slots = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < upgradeButtons.Length && slots.Count > 0; i++)
            {
                int s = slots[UnityEngine.Random.Range(0, slots.Count)]; slots.Remove(s);
                var eq = run.equipped[s];
                upgradeLabels[i].text = $"{slotNames[s]} {(eq ? eq.displayName : "")}\nLv{run.level[s]}→{run.level[s] + 1}";
                upgradeButtons[i].onClick.AddListener(() => { if (used) return; used = true; run.Upgrade((EquipSlot)s); Heal(1); Lock(); });
            }
            repairButton.onClick.AddListener(() => { if (used) return; used = true; Heal(3); Lock(); });
            departButton.onClick.AddListener(gm.AfterGarage);
            Refresh();
        }

        void Heal(int n) { var r = GameManager.Instance.Run; r.hp = Mathf.Min(PlayerStats.Build(r).maxHp, r.hp + n); Refresh(); }
        void Lock() { foreach (var b in upgradeButtons) b.interactable = false; repairButton.interactable = false; }

        Button Add(string label, bool interactable, System.Action onClick)
        {
            var b = Instantiate(listButtonPrefab, listRoot);
            b.GetComponentInChildren<TMP_Text>().text = label;
            b.interactable = interactable;
            if (onClick != null) b.onClick.AddListener(() => { onClick(); Refresh(); });
            return b;
        }
        void Notice(int rankBefore)
        {
            int r = GameManager.Instance.Save.rank;
            notice = r > rankBefore ? $"★ 工房ランクが上がった！ → {rankNames[r]}" : "";
        }
        void ApplyUpgrade(int slot) { var gm = GameManager.Instance; int b = gm.Save.rank; if (gm.Run.UpgradeTier(slot)) Notice(b); }
        void ApplyCraft(EquipmentData e, Tier t) { var gm = GameManager.Instance; int b = gm.Save.rank; if (gm.Run.Craft(e, t)) Notice(b); }

        void Refresh()
        {
            var gm = GameManager.Instance; var run = gm.Run; var save = gm.Save; var st = PlayerStats.Build(run);

            // ---- 状況表示 ----
            var sb = new StringBuilder();
            sb.AppendLine($"工房ランク {rankNames[save.rank]}  /  HP {run.hp}/{st.maxHp}  /  エネルギー {st.maxEnergy:0}  /  汎用素材 {save.commonMaterials}");
            for (int s = 0; s < RunState.Slots; s++)
            {
                var e = run.equipped[s];
                sb.Append($"{slotNames[s]}:{(e ? e.displayName : "-")}[{run.tier[s].Label()}]Lv{run.level[s]}  ");
            }
            sb.AppendLine();
            if (st.setBonus) sb.AppendLine("装備統一ボーナス発動中");
            foreach (var a in gm.allAreas)
            {
                sb.Append(a.materialName + ": ");
                bool any = false;
                for (int t = 0; t < 5; t++) { int n = run.Mat(a, (Tier)t); if (n > 0) { sb.Append($"{((Tier)t).Label()}x{n} "); any = true; } }
                if (!any) sb.Append("なし");
                sb.AppendLine();
            }
            if (notice != "") sb.AppendLine(notice);
            status.text = sb.ToString();

            // ---- 操作リスト ----
            foreach (Transform c in listRoot) Destroy(c.gameObject);
            int count = 0;

            // 素材の合成
            foreach (var a in gm.allAreas)
                for (int t = 0; t < 4; t++)
                {
                    var area = a; var tier = (Tier)t;
                    if (run.Mat(area, tier) < 3) continue;
                    Add($"合成 {area.materialName}[{tier.Label()}]x3 → [{((Tier)(t + 1)).Label()}]", tier < run.MaxCraftTier, () => run.Fuse(area, tier)); count++;
                }

            // 装備のティア強化
            for (int s = 0; s < RunState.Slots; s++)
            {
                var e = run.equipped[s];
                if (e == null || e.sourceArea == null || run.tier[s] == Tier.Supreme) continue;
                int slot = s; var next = run.tier[s] + 1;
                Add($"強化 {slotNames[s]} {e.displayName} [{run.tier[s].Label()}→{next.Label()}]  {e.sourceArea.materialName}[{next.Label()}]x{run.UpgradeCore(e)} + 汎用x{RunState.CommonUpgrade(e, next)}",
                    run.CanUpgradeTier(slot), () => ApplyUpgrade(slot)); count++;
            }

            // 装備の作成(素材を持っているティアだけ表示)
            foreach (var a in gm.allAreas)
            {
                if (a.craftable == null) continue;
                foreach (var e in a.craftable)
                {
                    if (e == null) continue;
                    for (int t = 0; t <= (int)run.MaxCraftTier; t++)
                    {
                        var eq = e; var tier = (Tier)t;
                        if (run.Mat(a, tier) <= 0) continue;
                        Add($"作成 {eq.displayName}[{tier.Label()}]  {a.materialName}[{tier.Label()}]x{eq.coreCost} + 汎用x{RunState.CommonCraft(eq, tier)}",
                            run.CanCraft(eq, tier), () => ApplyCraft(eq, tier)); count++;
                    }
                }
            }
            if (count == 0) Add("(今できる合成・強化・作成はありません)", false, null);
        }

#if UNITY_EDITOR
        // テスト用: エディタ上でのみ動く。F1=汎用素材+50、F2=各エリアの素材を追加
        void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;
            var gm = GameManager.Instance; var run = gm.Run;
            if (kb.f1Key.wasPressedThisFrame) { gm.Save.commonMaterials += 50; Refresh(); }
            if (kb.f2Key.wasPressedThisFrame)
            {
                foreach (var a in gm.allAreas)
                {
                    run.mats[(a, Tier.Crude)] = run.Mat(a, Tier.Crude) + 6;
                    run.mats[(a, Tier.Common)] = run.Mat(a, Tier.Common) + 3;
                }
                Refresh();
            }
        }
#endif
    }
}