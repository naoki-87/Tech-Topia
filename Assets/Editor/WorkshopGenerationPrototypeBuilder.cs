#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SteamEngineer.EditorTools
{
    public static class WorkshopGenerationPrototypeBuilder
    {
        const string Folder = "Assets/Prefabs/Stages/Generated";
        const string ProfilePath = "Assets/Data/Areas/WorkshopGenerationProfile.asset";
        const string WorkshopPath = "Assets/Data/Areas/Area_Workshop.asset";

        [MenuItem("Tools/TechTopia/Build Workshop Generation Prototype")]
        public static void Build()
        {
            var area = AssetDatabase.LoadAssetAtPath<AreaData>(WorkshopPath);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Square.asset");
            var goal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Other/Goal.prefab");
            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/prototype/Enemy_Walker.prefab");
            var repair = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gimmicks/prototype/Machine_Repair.prefab");
            if (area == null || tile == null || goal == null || enemy == null || repair == null)
                throw new InvalidOperationException("Workshop area, Square tile, Goal, Walker, or repair machine prefab is missing.");

            EnsureFolder("Assets/Prefabs/Stages", "Generated");
            var walkerPart = BuildWrapperPart("WorkshopPart_Walker", enemy, StagePartKind.Enemy);
            var repairPart = BuildWrapperPart("WorkshopPart_RepairMachine", repair, StagePartKind.Gimmick);
            var blockPart = BuildTilePart("WorkshopPart_Block", tile, StagePartKind.Obstacle, new[] { new Vector3Int(0, 0, 0) });
            var platformPart = BuildTilePart("WorkshopPart_RaisedPlatform", tile, StagePartKind.Platform, new[]
            {
                new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0)
            });
            var wallPart = BuildTilePart("WorkshopPart_Wall", tile, StagePartKind.Wall, new[]
            {
                new Vector3Int(0, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, 2, 0)
            });

            var start = BuildChunk("Workshop_Start", tile, goal, ChunkKind.Start, 0);
            var middleA = BuildChunk("Workshop_Middle_A", tile, goal, ChunkKind.Middle, 0);
            var middleB = BuildChunk("Workshop_Middle_B", tile, goal, ChunkKind.Middle, 1);
            var middleC = BuildChunk("Workshop_Middle_C", tile, goal, ChunkKind.Middle, 2);
            var end = BuildChunk("Workshop_End", tile, goal, ChunkKind.End, 0);

            var profile = AssetDatabase.LoadAssetAtPath<StageGenerationProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<StageGenerationProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            profile.startChunks = new[] { start.GetComponent<StageChunk>() };
            profile.middleChunks = new[] { middleA.GetComponent<StageChunk>(), middleB.GetComponent<StageChunk>(), middleC.GetComponent<StageChunk>() };
            profile.endChunks = new[] { end.GetComponent<StageChunk>() };
            profile.parts = new[]
            {
                walkerPart.GetComponent<StagePart>(), repairPart.GetComponent<StagePart>(), blockPart.GetComponent<StagePart>(),
                platformPart.GetComponent<StagePart>(), wallPart.GetComponent<StagePart>()
            };
            profile.minMiddleChunks = 2;
            profile.maxMiddleChunks = 3;
            profile.avoidRepeatingChunks = true;
            area.generatedStages = profile;
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(area);
            AssetDatabase.SaveAssets();
            Debug.Log("[ステージ生成] Workshop section and modular part prefabs/profile created.");
        }

        enum ChunkKind { Start, Middle, End }

        static GameObject BuildChunk(string name, TileBase tile, GameObject goalPrefab, ChunkKind kind, int layout)
        {
            string path = $"{Folder}/{name}.prefab";
            var root = new GameObject(name);
            try
            {
                var chunk = root.AddComponent<StageChunk>();
                chunk.entry = Marker(root.transform, "Entry", new Vector3(0f, 0f, 0f));
                chunk.exit = Marker(root.transform, "Exit", new Vector3(12f, 0f, 0f));
                chunk.entrySocket = "Ground";
                chunk.exitSocket = "Ground";
                chunk.difficulty = layout;

                var gridRoot = new GameObject("Grid", typeof(Grid));
                gridRoot.transform.SetParent(root.transform, false);
                var ground = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
                ground.layer = 6; // Existing PlayerController groundMask targets the Ground layer.
                ground.transform.SetParent(gridRoot.transform, false);
                var map = ground.GetComponent<Tilemap>();
                map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
                var renderer = ground.GetComponent<TilemapRenderer>();
                renderer.sortingOrder = 0;

                var body = ground.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                var tileCollider = ground.AddComponent<TilemapCollider2D>();
                var composite = ground.AddComponent<CompositeCollider2D>();
                tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
                composite.geometryType = CompositeCollider2D.GeometryType.Outlines;

                for (int x = 0; x < 12; x++)
                {
                    bool gap = kind == ChunkKind.Middle && layout == 1 && (x == 5 || x == 6);
                    if (!gap) map.SetTile(new Vector3Int(x, -1, 0), tile);
                }
                if (kind == ChunkKind.Middle && layout == 1)
                {
                    map.SetTile(new Vector3Int(5, 0, 0), tile);
                    map.SetTile(new Vector3Int(6, 0, 0), tile);
                }

                var killObject = new GameObject("KillZone", typeof(BoxCollider2D), typeof(KillZone));
                killObject.transform.SetParent(root.transform, false);
                killObject.transform.localPosition = new Vector3(6f, -5f, 0f);
                var killCollider = killObject.GetComponent<BoxCollider2D>();
                killCollider.isTrigger = true;
                killCollider.size = new Vector2(12f, 1f);

                if (kind == ChunkKind.Start)
                    Marker(root.transform, "PlayerSpawn", new Vector3(1.5f, 1f, 0f));
                if (kind == ChunkKind.End)
                {
                    var goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
                    goal.transform.SetParent(root.transform, false);
                    goal.transform.localPosition = new Vector3(10.5f, 3f, 0f);
                }
                if (kind == ChunkKind.Middle)
                {
                    PartSocket(root.transform, "EnemySocket", StagePartKind.Enemy, new Vector3(8f, 1f, 0f), true, 1f);
                    if (layout == 0)
                    {
                        PartSocket(root.transform, "PlatformSocket", StagePartKind.Platform, new Vector3(4f, 0f, 0f), false, 0.65f);
                        PartSocket(root.transform, "ObstacleSocket", StagePartKind.Obstacle, new Vector3(9f, 0f, 0f), false, 0.6f);
                        PartSocket(root.transform, "WallSocket", StagePartKind.Wall, new Vector3(2f, 4f, 0f), false, 0.75f);
                    }
                    else if (layout == 1)
                        PartSocket(root.transform, "ObstacleSocket", StagePartKind.Obstacle, new Vector3(3f, 0f, 0f), false, 0.5f);
                    else
                    {
                        PartSocket(root.transform, "PlatformSocket", StagePartKind.Platform, new Vector3(3f, 0f, 0f), false, 0.7f);
                        PartSocket(root.transform, "GimmickSocket", StagePartKind.Gimmick, new Vector3(5f, 0f, 0f), false, 0.55f);
                        PartSocket(root.transform, "ObstacleSocket", StagePartKind.Obstacle, new Vector3(10f, 0f, 0f), false, 0.45f);
                    }
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new InvalidOperationException("Failed to save room prefab: " + path);
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static GameObject BuildWrapperPart(string name, GameObject source, StagePartKind kind)
        {
            var root = new GameObject(name);
            try
            {
                root.AddComponent<StagePart>().kind = kind;
                var child = (GameObject)PrefabUtility.InstantiatePrefab(source);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = Vector3.one;
                var saved = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab");
                if (saved == null) throw new InvalidOperationException("Failed to save part prefab: " + name);
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static GameObject BuildTilePart(string name, TileBase tile, StagePartKind kind, Vector3Int[] cells)
        {
            var root = new GameObject(name);
            try
            {
                root.AddComponent<StagePart>().kind = kind;
                var gridRoot = new GameObject("Grid", typeof(Grid));
                gridRoot.transform.SetParent(root.transform, false);
                var ground = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
                ground.layer = 6;
                ground.transform.SetParent(gridRoot.transform, false);
                var map = ground.GetComponent<Tilemap>();
                map.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
                foreach (var cell in cells) map.SetTile(cell, tile);
                ground.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                var tileCollider = ground.AddComponent<TilemapCollider2D>();
                ground.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Outlines;
                tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
                var saved = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab");
                if (saved == null) throw new InvalidOperationException("Failed to save part prefab: " + name);
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void PartSocket(Transform parent, string name, StagePartKind kind, Vector3 position, bool required, float chance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var socket = go.AddComponent<StagePartSocket>();
            socket.kind = kind; socket.required = required; socket.spawnChance = chance;
        }

        static Transform Marker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
            return marker.transform;
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
