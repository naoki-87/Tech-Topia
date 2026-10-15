using UnityEngine;
namespace SteamEngineer
{
    // 編集用: Scene ビューにマス目の線と座標の数字を表示する(ゲームには影響しない)
    // 数字は「そのマスのセル番号」。x は下の行、y は左の列に出る。
    [ExecuteAlways]
    public class GridRuler : MonoBehaviour
    {
#if UNITY_EDITOR
        public int xMin = -5, xMax = 45, yMin = -10, yMax = 12;
        public int labelEvery = 5;          // 何マスごとに数字を出すか
        public float xLabelRow = -5f;       // x の数字を出す高さ(セルの y)
        public float yLabelCol = -3f;       // y の数字を出す位置(セルの x)

        void OnDrawGizmos()
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Gizmos.color = x == 0 ? new Color(1f, 0.3f, 0.3f, 0.9f)
                             : x % labelEvery == 0 ? new Color(1f, 1f, 1f, 0.35f)
                             : new Color(1f, 1f, 1f, 0.12f);
                Gizmos.DrawLine(new Vector3(x, yMin, 0), new Vector3(x, yMax, 0));
                if (x % labelEvery == 0)
                    UnityEditor.Handles.Label(new Vector3(x + 0.15f, xLabelRow + 0.8f, 0), "x" + x);
            }
            for (int y = yMin; y <= yMax; y++)
            {
                Gizmos.color = y == 0 ? new Color(0.3f, 1f, 0.3f, 0.95f)       // y=0 は緑(地面の上面)
                             : y % labelEvery == 0 ? new Color(1f, 1f, 1f, 0.35f)
                             : new Color(1f, 1f, 1f, 0.12f);
                Gizmos.DrawLine(new Vector3(xMin, y, 0), new Vector3(xMax, y, 0));
                if (y % labelEvery == 0)
                    UnityEditor.Handles.Label(new Vector3(yLabelCol - 0.4f, y + 0.5f, 0), "y" + y);
            }
        }
#endif
    }
}