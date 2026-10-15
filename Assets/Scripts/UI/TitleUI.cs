using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    // 基礎強化(永続): コアを消費して部位の基礎レベルを上げる
    public class TitleUI : MonoBehaviour
    {
        public Button startButton; public Button[] coreButtons; public TMP_Text info;   // coreButtons[i] = Head,Body,Legs,Weapon
        void Start()
        {
            var gm = GameManager.Instance; startButton.onClick.AddListener(gm.StartRun);
            for (int i = 0; i < coreButtons.Length; i++) { var s = (EquipSlot)i; coreButtons[i].onClick.AddListener(() => { gm.Save.SpendCore(s); gm.Save.Write(); Refresh(); }); }
            Refresh();
        }
        void Refresh() { var s = GameManager.Instance.Save; info.text = $"コア {s.cores}  基礎Lv 帽{s.baseLevel[0]} 服{s.baseLevel[1]} 靴{s.baseLevel[2]} 武{s.baseLevel[3]}"; }
    }
}
