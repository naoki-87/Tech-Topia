using TMPro; using UnityEngine;
namespace SteamEngineer
{
    // Stage シーンの簡易HUD: エリア名・ステージ数・HP
    public class HudUI : MonoBehaviour
    {
        public TMP_Text text; Health hp;
        void Update()
        {
            var run = GameManager.Instance.Run;
            if (!hp) { var p = FindFirstObjectByType<PlayerController>(); if (p) hp = p.Health; }
            string area = run.current ? run.current.displayName : "";
            string hpText = hp ? $"{hp.Current}/{hp.max}" : "-";
            text.text = $"{area}  Stage {run.stageIndex + 1}/{RunState.StagesPerArea}\nHP {hpText}";
        }
    }
}