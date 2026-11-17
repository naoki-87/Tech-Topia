using System.Collections.Generic; using System.Text; using TMPro; using UnityEngine; using UnityEngine.InputSystem;
namespace SteamEngineer
{
    // ステージ中のアイテム欄(右下)。持ち込んだアイテムのうち、ステージ中に使えるものにキーを割り当てる
    public class ItemBar : MonoBehaviour
    {
        TMP_Text text; PlayerController player; Energy energy; float msgUntil; string msg = ""; Canvas canvas;

        void Start()
        {
            var c = FindFirstObjectByType<Canvas>(); if (c == null) { enabled = false; return; }
            canvas = c.rootCanvas;
            text = UiKit.Txt(canvas.transform, "", 32, UiKit.Cream, TextAlignmentOptions.BottomRight, "ItemBar");
            UiKit.Place(text.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(700f, 260f));
        }
        void Update()
        {
            var gm = GameManager.Instance; if (gm == null || gm.Run == null || text == null) return;
            if (player == null) { player = FindFirstObjectByType<PlayerController>(); if (player != null) energy = player.GetComponent<Energy>(); }
            var run = gm.Run; var k = Keyboard.current; var sb = new StringBuilder();
            foreach (var kv in run.ItemList())
            {
                var it = kv.Key; if ((it.usableIn & ItemUse.Stage) == 0 || kv.Value <= 0) continue;
                string key = it.stageKey == KeyCode.None ? "-" : it.stageKey.ToString();
                sb.AppendLine($"[{key}] {it.displayName} x{kv.Value}");
                if (k != null && player != null && !player.Health.IsDead && KeyDown(k, it.stageKey))
                { msg = ItemManager.Use(it, run, UseScene.Stage, player); msgUntil = Time.unscaledTime + 2.5f; }
            }
            if (Time.unscaledTime < msgUntil) sb.AppendLine($"<color=#FFD060>{msg}</color>");
            text.text = sb.ToString();
        }
        static bool KeyDown(Keyboard k, KeyCode c)
        {
            switch (c) { case KeyCode.Q: return k.qKey.wasPressedThisFrame; case KeyCode.E: return k.eKey.wasPressedThisFrame; case KeyCode.R: return k.rKey.wasPressedThisFrame; default: return false; }
        }
    }
}