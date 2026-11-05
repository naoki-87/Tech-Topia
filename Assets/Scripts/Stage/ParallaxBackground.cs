using UnityEngine;
namespace SteamEngineer
{
    // 背景: カメラの子として置き、カメラの動きと逆向きに少しだけずらして奥行きを出す
    public class ParallaxBackground : MonoBehaviour
    {
        public float factor = 0.08f, limitX = 4f, limitY = 1.5f;
        Camera cam;
        void LateUpdate()
        {
            if (!cam) cam = Camera.main; if (!cam) return;
            var p = transform.localPosition;
            p.x = -Mathf.Clamp(cam.transform.position.x * factor, -limitX, limitX);
            p.y = -Mathf.Clamp(cam.transform.position.y * factor * 0.5f, -limitY, limitY);
            transform.localPosition = p;
        }
    }
}