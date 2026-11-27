using UnityEngine;

namespace SteamEngineer
{
    [CreateAssetMenu(menuName = "SteamEngineer/Stage Generation Profile")]
    public class StageGenerationProfile : ScriptableObject
    {
        public StageChunk[] startChunks;
        public StageChunk[] middleChunks;
        public StageChunk[] endChunks;
        public StagePart[] parts;
        [Min(0)] public int minMiddleChunks = 2;
        [Min(0)] public int maxMiddleChunks = 4;
        public bool avoidRepeatingChunks = true;
    }
}
