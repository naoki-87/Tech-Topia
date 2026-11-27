using UnityEngine;

namespace SteamEngineer
{
    public enum StagePartKind { Platform, Wall, Obstacle, Gimmick, Enemy }

    // Attach to a prefab that can be inserted into a matching StagePartSocket.
    public class StagePart : MonoBehaviour
    {
        public StagePartKind kind;
        [Min(0f)] public float weight = 1f;
    }
}
