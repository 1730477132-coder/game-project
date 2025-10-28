using UnityEngine;

public class GridNavigator2D : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("一格的世界单位长度，需与关卡 Grid/Tiles 对齐。")]
    public float cellSize = 1f;

    [Header("Collision")]
    [Tooltip("墙所在的 LayerMask（例如 Wall）。")]
    public LayerMask wallMask;

    [Tooltip("到达格心的容差")]
    [Range(0.001f, 0.1f)] public float centerEpsilon = 0.01f;

    /// 把任意世界坐标吸附到最近的格心
    public Vector3 SnapToCell(Vector3 worldPos)
    {
        float x = Mathf.Round(worldPos.x / cellSize) * cellSize;
        float y = Mathf.Round(worldPos.y / cellSize) * cellSize;
        return new Vector3(x, y, worldPos.z);
    }

    /// 现在是否正处在格心（容差内）
    public bool IsAtCellCenter()
    {
        return (transform.position - SnapToCell(transform.position)).sqrMagnitude <= centerEpsilon * centerEpsilon;
    }

    /// 下一格是否被墙挡住（从当前格心到目标格心做 Linecast）
    public bool IsBlocked(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return true;

        Vector3 from = SnapToCell(transform.position);
        Vector3 to = from + (Vector3)((Vector2)dir * cellSize);

        // 命中任意 wallMask 即视为被挡
        var hit = Physics2D.Linecast(from, to, wallMask);
        return hit.collider != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var c = SnapToCell(transform.position);
        Gizmos.DrawWireCube(c, new Vector3(cellSize * 0.95f, cellSize * 0.95f, 0.01f));
    }
#endif
}
