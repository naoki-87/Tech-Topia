using UnityEngine;

namespace SteamEngineer
{
    // Reusable room prefab. Place the socket transforms at the walkable floor edge.
    public class StageChunk : MonoBehaviour
    {
        public Transform entry;
        public Transform exit;
        public string entrySocket = "Ground";
        public string exitSocket = "Ground";
        [Min(0)] public int difficulty = 1;

        public bool IsValid => entry != null && exit != null;

        void OnDrawGizmosSelected()
        {
            if (entry != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(entry.position, 0.2f);
                Gizmos.DrawLine(entry.position, entry.position + Vector3.up * 0.8f);
            }
            if (exit != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(exit.position, 0.2f);
                Gizmos.DrawLine(exit.position, exit.position + Vector3.up * 0.8f);
            }
        }
    }
}
