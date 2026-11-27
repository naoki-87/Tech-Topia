using UnityEngine;

namespace SteamEngineer
{
    // Empty marker inside a StageChunk. Its transform is the part prefab's placement origin.
    public class StagePartSocket : MonoBehaviour
    {
        public StagePartKind kind;
        public bool required;
        [Range(0f, 1f)] public float spawnChance = 1f;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = kind == StagePartKind.Enemy ? Color.red
                         : kind == StagePartKind.Gimmick ? Color.yellow
                         : kind == StagePartKind.Wall ? Color.gray
                         : kind == StagePartKind.Obstacle ? new Color(1f, 0.45f, 0.1f)
                         : Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        }
    }
}
