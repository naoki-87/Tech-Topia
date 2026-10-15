using UnityEngine;
namespace SteamEngineer
{
    public class CameraFollow : MonoBehaviour
    {
        public float smooth = 6f; public Vector2 offset = new Vector2(0, 1.5f); Transform target;
        void LateUpdate()
        {
            if (!target) { var p = FindFirstObjectByType<PlayerController>(); if (!p) return; target = p.transform; }
            var t = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, t, 1 - Mathf.Exp(-smooth * Time.deltaTime));
        }
    }
}
