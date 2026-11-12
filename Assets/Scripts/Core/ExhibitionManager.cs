using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.SceneManagement;
namespace SteamEngineer
{
    // 展示用: 無操作でタイトルへ戻る / セーブを初期化 / Escの長押しで終了 / Enterでスタート
    // Title シーンの GameManager と同じオブジェクトに追加する
    public class ExhibitionManager : MonoBehaviour
    {
        public static ExhibitionManager Instance;
        public bool exhibitionMode = true;
        public float idleSeconds = 90f;          // この秒数、無操作ならタイトルへ戻る
        public bool resetSaveOnIdle = true;      // 戻るときにセーブを初期化する(次の来場者のため)
        public int minCommonMaterials = 40;      // 短い体験でも装備作成できるよう、開始時の汎用素材の下限
        public float quitHoldSeconds = 2f;       // Escを押し続けて終了する秒数
        float idle, quitHold;

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
            if (g != null && (g.leftStick.ReadValue().sqrMagnitude > 0.1f || g.buttonSouth.isPressed)) return true;
            return false;
        }

        void Update()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            var kb = Keyboard.current;
            string scene = SceneManager.GetActiveScene().name;

            if (kb != null)
            {
                if (scene == "Title" && kb.enterKey.wasPressedThisFrame && !HelpPanel.Blocking) gm.StartRun();
                else if (scene == "Result" && kb.enterKey.wasPressedThisFrame) gm.BackToTitle();
                if (kb.escapeKey.isPressed) { quitHold += Time.unscaledDeltaTime; if (quitHold >= quitHoldSeconds) Application.Quit(); }
                else quitHold = 0f;
            }

            if (!exhibitionMode) return;
            if (AnyInput()) idle = 0f; else idle += Time.unscaledDeltaTime;
            if (idle >= idleSeconds)
            {
                idle = 0f;
                if (resetSaveOnIdle) { gm.Save = new SaveData(); gm.Save.Write(); }
                SceneManager.LoadScene("Title");
            }
        }
    }
}