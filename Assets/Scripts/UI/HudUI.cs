using TMPro; using UnityEngine; using UnityEngine.UI;
namespace SteamEngineer
{
    public class HudUI : MonoBehaviour
    {
        public TMP_Text infoText;                    // エリア名・ステージ数
        public Image hpFill, energyFill, bossFill;   // Image Type = Filled のもの
        public GameObject bossPanel;                 // ボスHPバー全体
        Health hp, bossHp; Energy en; float findTimer;

        void Update()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null; if (run == null) return;
            findTimer -= Time.deltaTime;
            if (findTimer <= 0f)                      // 0.5秒ごとに探す(毎フレーム探さない)
            {
                findTimer = 0.5f;
                if (!hp) { var p = FindFirstObjectByType<PlayerController>(); if (p) { hp = p.Health; en = p.GetComponent<Energy>(); } }
                if (!bossHp) { var b = FindFirstObjectByType<BossController>(); if (b) bossHp = b.GetComponent<Health>(); }
            }
            if (infoText) infoText.text = (run.current ? run.current.displayName : "") + $"  エリア {run.areasCleared + 1}/{run.totalAreas}  Stage {run.stageIndex + 1}/{RunState.StagesPerArea}";
            if (hp && hpFill) hpFill.fillAmount = (float)hp.Current / hp.max;
            if (en && energyFill) energyFill.fillAmount = en.Current / en.max;
            bool boss = bossHp && !bossHp.IsDead;
            if (bossPanel) bossPanel.SetActive(boss);
            if (boss && bossFill) bossFill.fillAmount = (float)bossHp.Current / bossHp.max;
        }
    }
}