using UnityEngine;
namespace SteamEngineer
{
    // 有効になってから seconds 秒後に自分を無効にする(スイッチで起動した風を、一定時間で止めるのに使う)
    public class AutoDisable : MonoBehaviour
    {
        public float seconds = 8f;
        void OnEnable() { CancelInvoke(); Invoke(nameof(Off), seconds); }
        void Off() { gameObject.SetActive(false); }
    }
}