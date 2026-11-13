using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.SceneManagement;
namespace SteamEngineer
{
    // 展示用: F9長押しで手動リセット / Esc長押しで終了 / Enterでスタート
    // Title シーンの GameManager と同じオブジェクトに追加する
    public class ExhibitionManager : MonoBehaviour
    {
        public static ExhibitionManager Instance;
        public bool exhibitionMode = true;
        public float resetHoldSeconds = 2f;       // F9を押し続ける秒数
        public float quitHoldSeconds = 2f;        // Escを押し続ける秒数
        public float idleReturnSeconds = 0f;      // 無操作でTitleに戻る秒数(0で無効。セーブは消さない)
        public int minCommonMaterials = 40;       // 周回開始時の汎用素材の下限
        float idle, quitHold, resetHold, toastUntil;

        void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 1;
        }

        bool AnyInput()
        {
            var k = Keyboard.current; if (k != null && k.anyKey.isPressed) return true;
            var m = Mouse.current;
            if (m != null && (m.leftButton.isPressed || m.delta.ReadValue().sqrMagnitude > 0.5f)) return true;
            var g = Gamepad.current;
            return g != null && (g.leftStick.ReadValue().sqrMagnitude > 0.1f || g.buttonSouth.isPressed);
        }

        public void ResetAll()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            gm.Save = new SaveData(); gm.Save.Write(); gm.Run = null;
            toastUntil = Time.unscaledTime + 2f;
            SceneManager.LoadScene("Title");
        }

        void Update()
        {
            var gm = GameManager.Instance; var kb = Keyboard.current;
            string scene = SceneManager.GetActiveScene().name;
            if (kb != null)
            {
                if (gm != null)
                {
                    if (scene == "Title" && kb.enterKey.wasPressedThisFrame && !HelpPanel.Blocking) gm.StartRun();
                    else if (scene == "Result" && kb.enterKey.wasPressedThisFrame) gm.BackToTitle();
                }
                if (kb.escapeKey.isPressed) { quitHold += Time.unscaledDeltaTime; if (quitHold >= quitHoldSeconds) Application.Quit(); }
                else quitHold = 0f;
                if (kb.f9Key.isPressed)
                {
                    resetHold += Time.unscaledDeltaTime;
                    if (resetHold >= resetHoldSeconds) { resetHold = -999f; ResetAll(); }
                }
                else resetHold = 0f;
            }
            if (exhibitionMode && idleReturnSeconds > 0f)
            {
                if (AnyInput()) idle = 0f; else idle += Time.unscaledDeltaTime;
                if (idle >= idleReturnSeconds) { idle = 0f; if (scene != "Title") SceneManager.LoadScene("Title"); }
            }
        }

        void OnGUI()   // 日本語フォントに依存しないよう英字で表示する
        {
            if (resetHold > 0.15f)
            {
                float t = Mathf.Clamp01(resetHold / resetHoldSeconds);
                GUI.color = new Color(0f, 0f, 0f, 0.7f); GUI.DrawTexture(new Rect(16, 16, 320, 44), Texture2D.whiteTexture);
                GUI.color = new Color(0.85f, 0.65f, 0.12f); GUI.DrawTexture(new Rect(24, 40, 304 * t, 14), Texture2D.whiteTexture);
                GUI.color = Color.white; GUI.Label(new Rect(24, 20, 300, 20), "RESET... (hold F9)");
            }
            else if (Time.unscaledTime < toastUntil)
            {
                GUI.color = Color.white; GUI.Label(new Rect(24, 20, 300, 20), "RESET DONE");
            }
        }
    }
}