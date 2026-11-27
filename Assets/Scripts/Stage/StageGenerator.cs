using System.Collections.Generic;
using UnityEngine;

namespace SteamEngineer
{
    public static class StageGenerator
    {
        public static GameObject TryGenerate(StageGenerationProfile profile, int seed)
        {
            if (profile == null || profile.startChunks == null || profile.middleChunks == null || profile.endChunks == null)
                return null;
            if (profile.minMiddleChunks < 0 || profile.maxMiddleChunks < profile.minMiddleChunks)
                return null;

            var randomState = Random.state;
            Random.InitState(seed);
            try
            {
                var selected = new List<StageChunk>();
                var used = new HashSet<StageChunk>();
                var first = Pick(profile.startChunks, null, used);
                if (first == null) return Fail(seed, "開始部屋が未設定か、入口・出口が未設定です");
                selected.Add(first); used.Add(first);
                string socket = first.exitSocket;

                int count = Random.Range(profile.minMiddleChunks, profile.maxMiddleChunks + 1);
                for (int i = 0; i < count; i++)
                {
                    var chunk = Pick(profile.middleChunks, socket, profile.avoidRepeatingChunks ? used : null);
                    if (chunk == null) return Fail(seed, "中間部屋を接続できません");
                    selected.Add(chunk); used.Add(chunk); socket = chunk.exitSocket;
                }

                var last = Pick(profile.endChunks, socket, used);
                if (last == null) return Fail(seed, "終了部屋を接続できません");
                selected.Add(last);
                if (!HasPlayerSpawn(first.gameObject) || last.GetComponentInChildren<StageGoal>(true) == null)
                    return Fail(seed, "開始部屋に PlayerSpawn、または終了部屋に StageGoal がありません");

                var root = new GameObject("GeneratedStage");
                Vector3 cursor = Vector3.zero;
                for (int i = 0; i < selected.Count; i++)
                {
                    var source = selected[i];
                    var instance = Object.Instantiate(source.gameObject, root.transform, false);
                    var chunk = instance.GetComponent<StageChunk>();
                    if (chunk == null || !chunk.IsValid)
                    {
                        Object.Destroy(root);
                        return Fail(seed, $"部屋 {source.name} の入口または出口が未設定です");
                    }
                    instance.transform.position += cursor - chunk.entry.position;
                    cursor = chunk.exit.position;
                }

                if (!PlaceParts(root, profile.parts, seed))
                {
                    Object.Destroy(root);
                    return null;
                }

                Debug.Log($"[ステージ生成] seed={seed}, sections={selected.Count}, parts={root.GetComponentsInChildren<StagePart>(true).Length}");
                return root;
            }
            finally { Random.state = randomState; }
        }

        static StageChunk Pick(StageChunk[] choices, string requiredEntry, HashSet<StageChunk> excluded)
        {
            if (choices == null || choices.Length == 0) return null;
            var valid = new List<StageChunk>();
            foreach (var c in choices)
            {
                if (c == null || !c.IsValid || (excluded != null && excluded.Contains(c))) continue;
                if (requiredEntry != null && c.entrySocket != requiredEntry) continue;
                valid.Add(c);
            }
            return valid.Count == 0 ? null : valid[Random.Range(0, valid.Count)];
        }

        static bool HasPlayerSpawn(GameObject room)
        {
            foreach (var t in room.GetComponentsInChildren<Transform>(true))
                if (t.name == "PlayerSpawn") return true;
            return false;
        }

        static bool PlaceParts(GameObject root, StagePart[] parts, int seed)
        {
            var sockets = root.GetComponentsInChildren<StagePartSocket>(true);
            foreach (var socket in sockets)
            {
                if (!socket.required && Random.value > socket.spawnChance) continue;
                var part = PickPart(parts, socket.kind);
                if (part == null)
                {
                    if (!socket.required) continue;
                    Debug.LogWarning($"[ステージ生成] seed={seed}: 必須部品 {socket.kind} の候補がありません。");
                    return false;
                }
                var instance = Object.Instantiate(part.gameObject, root.transform, false);
                instance.transform.SetPositionAndRotation(socket.transform.position, socket.transform.rotation);
                instance.name = $"{part.name}_Placed";
            }
            return true;
        }

        static StagePart PickPart(StagePart[] parts, StagePartKind kind)
        {
            if (parts == null || parts.Length == 0) return null;
            var valid = new List<StagePart>(); float total = 0f;
            foreach (var part in parts)
            {
                if (part == null || part.kind != kind || part.weight <= 0f) continue;
                valid.Add(part); total += part.weight;
            }
            if (valid.Count == 0) return null;
            float roll = Random.value * total;
            foreach (var part in valid)
            {
                roll -= part.weight;
                if (roll <= 0f) return part;
            }
            return valid[valid.Count - 1];
        }

        static GameObject Fail(int seed, string reason)
        {
            Debug.LogWarning($"[ステージ生成] seed={seed}: {reason}。既存Prefabを使います。");
            return null;
        }
    }
}
