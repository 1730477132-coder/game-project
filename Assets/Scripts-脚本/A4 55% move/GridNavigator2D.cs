using UnityEngine;

public class GridNavigator2D : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("һ������絥λ���ȣ�����ؿ� Grid/Tiles ���롣")]
    public float cellSize = 1f;

    [Header("Collision")]
    [Tooltip("ǽ���ڵ� LayerMask������ Wall����")]
    public LayerMask wallMask;

    [Tooltip("������ĵ��ݲ�")]
    [Range(0.001f, 0.1f)] public float centerEpsilon = 0.01f;

    /// ������������������������ĸ���
    public Vector3 SnapToCell(Vector3 worldPos)
    {
        float x = Mathf.Round(worldPos.x / cellSize) * cellSize;
        float y = Mathf.Round(worldPos.y / cellSize) * cellSize;
        return new Vector3(x, y, worldPos.z);
    }

    /// �����Ƿ������ڸ��ģ��ݲ��ڣ�
    public bool IsAtCellCenter()
    {
        return (transform.position - SnapToCell(transform.position)).sqrMagnitude <= centerEpsilon * centerEpsilon;
    }

    /// ��һ���Ƿ�ǽ��ס���ӵ�ǰ���ĵ�Ŀ������� Linecast��
    public bool IsBlocked(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return true;

        Vector3 from = SnapToCell(transform.position);
        Vector3 to = from + (Vector3)((Vector2)dir * cellSize);

        // �������� wallMask ����Ϊ����
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
