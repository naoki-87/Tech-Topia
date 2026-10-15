using UnityEngine;
namespace SteamEngineer
{
    // 確認用: シーンビューにワールド座標とセル座標を表示する(ゲームには影響しない)
    [ExecuteAlways]
    public class CoordLabel : MonoBehaviour
    {
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            Vector2Int cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            UnityEditor.Handles.Label(p + Vector3.up * 0.6f,
                $"{name}\nworld ({p.x:0.##}, {p.y:0.##})\ncell ({cell.x}, {cell.y})");
        }
#endif
    }
}