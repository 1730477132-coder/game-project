using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// ��ȡ CSV��ֻ���ɡ���ǽ/��ǽ������ͬ���ھ��Զ�ѡ ֱ��/�ս�/T/ʮ��/�˵㡣
/// �ؼ���Ϊÿ���񴴽� Wrapper(ê��) �̶��ڸ����ģ�������ǽ�鵱�������壬
/// ֻ������������ת(localRotation)��λ��(localPosition) ���� �ȶ��Ƕȣ��ٷ��á�
[ExecuteAlways]
public class LevelMapBuilder_TwoWallTypes_Anchored : MonoBehaviour
{
    [Header("CSV��levelmap �� TextAsset��")]
    public TextAsset csv;

    [Header("�������")]
    public Vector2 origin = Vector2.zero; // ���½���һ������
    public float cellSize = 1f;           // һ�����絥λ��= ����/PPU��

    [Header("��/��ǽ�� CSV ����")]
    public List<int> outsideWallIds = new List<int>() { 1 };
    public List<int> insideWallIds = new List<int>() { 2 };

    [Serializable]
    public class WallSet
    {
        [Header("Prefab����ʼ�������⣬�·� offset ��У����")]
        public GameObject straight;
        public GameObject corner;
        public GameObject tJunction;
        public GameObject cross;
        public GameObject cap;

        [Header("��תƫ�ƣ��ȣ�������������ʼ����")]
        public float straightOffset = 0f;
        public float cornerOffset = 0f;
        public float tJunctionOffset = 0f;
        public float crossOffset = 0f;
        public float capOffset = 0f;

        [Header("λ��ƫ�ƣ���λ=���ԡ�δ��ת�����ҡ�Ϊ�����ᣩ")]
        public Vector2 posStraightOffset = Vector2.zero;
        public Vector2 posCornerOffset = Vector2.zero;
        public Vector2 posTJunctionOffset = Vector2.zero;
        public Vector2 posCrossOffset = Vector2.zero;
        public Vector2 posCapOffset = Vector2.zero;
    }

    [Header("��ǽ / ��ǽ Ԥ������")]
    public WallSet outsideWall;
    public WallSet insideWall;

    [Header("�����壨������Զ�������")]
    public Transform tilesParent;

    [Header("����ѡ��")]
    public bool buildOnPlay = true;
    public bool clearBeforeBuild = true;

    // ===== �ڲ����� =====
    int[,] grid; int rows, cols;

    void Start()
    {
        if (Application.isPlaying && buildOnPlay) Build();
    }

#if UNITY_EDITOR
    [ContextMenu("Build Level����/��ǽ + ê��/ƫ�ƣ�")]
    public void Build()
    {
        if (csv == null) { Debug.LogError("��� levelmap CSV(TextAsset) �ϵ� csv��"); return; }

        ParseCSV(csv.text);

        if (tilesParent == null) tilesParent = EnsureChild("Tiles");
        if (clearBeforeBuild) ClearChildren(tilesParent);

        ForEachCell((r, c, id) =>
        {
            var kind = GetWallKind(id);
            if (kind == WallKind.None) return;

            var set = (kind == WallKind.Outside) ? outsideWall : insideWall;

            // ����ͬ���ھ�
            bool up    = IsSameKind(r - 1, c, kind);
            bool down  = IsSameKind(r + 1, c, kind);
            bool left  = IsSameKind(r, c - 1, kind);
            bool right = IsSameKind(r, c + 1, kind);
            int n = (up?1:0) + (down?1:0) + (left?1:0) + (right?1:0);

            GameObject prefab = null;
            float rotZ = 0f;              // �߼���ת�Ƕȣ��ȣ�
            Vector2 localOffset = Vector2.zero; // ����ƫ�ƣ���λ=��

            if (n == 0)
            {
                prefab = set.cap != null ? set.cap : set.straight;
                rotZ   = 0f + ((prefab == set.cap) ? set.capOffset : set.straightOffset);
                localOffset = (prefab == set.cap) ? set.posCapOffset : set.posStraightOffset;
            }
            else if (n == 1)
            {
                prefab = set.cap != null ? set.cap : set.straight;
                rotZ   = DirToRot(up, right, down, left) + ((prefab == set.cap) ? set.capOffset : set.straightOffset);
                localOffset = (prefab == set.cap) ? set.posCapOffset : set.posStraightOffset;
            }
            else if (n == 2)
            {
                if ((up && down) || (left && right))
                {
                    prefab = set.straight;
                    rotZ   = ((left && right) ? 0f : 90f) + set.straightOffset; // ˮƽ0��/��ֱ90��
                    localOffset = set.posStraightOffset;
                }
                else
                {
                    prefab = set.corner;
                    // Ĭ�ϣ���ƫ��ʱ�Ļ�׼����
                    if (up && right) rotZ = 0f;
                    else if (right && down) rotZ = 270f;
                    else if (down && left) rotZ = 180f;
                    else if (left && up) rotZ = 90f;
                    rotZ += set.cornerOffset;
                    localOffset = set.posCornerOffset;
                }
            }
            else if (n == 3)
            {
                prefab = set.tJunction;
                // Ĭ�ϣ����ڳ�ȱʧ����
                if (!up) rotZ = 0f;
                else if (!right) rotZ = 270f;
                else if (!down) rotZ = 180f;
                else if (!left) rotZ = 90f;
                rotZ += set.tJunctionOffset;
                localOffset = set.posTJunctionOffset;
            }
            else // n == 4
            {
                prefab = set.cross != null ? set.cross : set.tJunction;
                rotZ   = 0f + ((prefab == set.cross) ? set.crossOffset : set.tJunctionOffset);
                localOffset = (prefab == set.cross) ? set.posCrossOffset : set.posTJunctionOffset;
            }

            if (prefab == null) return;

            // 1) ê�� Wrapper���̶��ڸ����ģ�����ת�����ţ�
            Vector3 center = CellToWorld(r, c);
            var wrapper = new GameObject($"{prefab.name}_Wrapper");
            wrapper.transform.SetParent(tilesParent, false);
            wrapper.transform.position = center;
            wrapper.transform.rotation = Quaternion.identity;
            wrapper.transform.localScale = Vector3.one;

            // 2) ��ʵǽ�� Child����Ϊ�����壬�ȶ��Ƕȣ������ñ���λ��
            var child = (GameObject)Instantiate(prefab, wrapper.transform);
            child.name = prefab.name;
            child.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            child.transform.localPosition = new Vector3(localOffset.x * cellSize, localOffset.y * cellSize, 0f);
            child.transform.localScale = Vector3.one;
        });

        Debug.Log($"Level ������ɣ�{rows}x{cols}����/��ǽ + ê�� + ƫ�ƣ�");
    }
#endif

    // ========= ���� & �ڲ��߼� =========
    enum WallKind { None, Outside, Inside }

    WallKind GetWallKind(int id)
    {
        if (outsideWallIds.Contains(id)) return WallKind.Outside;
        if (insideWallIds.Contains(id)) return WallKind.Inside;
        return WallKind.None;
    }

    bool IsSameKind(int r, int c, WallKind k)
    {
        if (r < 0 || c < 0 || r >= rows || c >= cols) return false;
        return GetWallKind(grid[r, c]) == k;
    }

    void ParseCSV(string text)
    {
        var lines = text.Replace("\r", "").Split('\n')
                        .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        rows = lines.Count;
        cols = lines[0].Split(',').Length;
        grid = new int[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            var parts = lines[r].Split(',');
            for (int c = 0; c < parts.Length; c++)
                int.TryParse(parts[c].Trim(), out grid[r, c]);
        }
    }

    void ForEachCell(Action<int, int, int> act)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                act(r, c, grid[r, c]);
    }

    // �˵�/ֱ�ߵ��ھ�ʱ�ĳ�����0�㣬��270�㣬��180�㣬��90��
    float DirToRot(bool up, bool right, bool down, bool left)
    { if (up) return 0f; if (right) return 270f; if (down) return 180f; return 90f; }

    Vector3 CellToWorld(int r, int c)
    {
        float x = origin.x + c * cellSize;
        float y = origin.y + (rows - 1 - r) * cellSize; // CSV ����ӳ�䵽����������
        return new Vector3(x, y, 0);
    }

    Transform EnsureChild(string name)
    {
        var t = transform.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            t = go.transform;
            t.localPosition = Vector3.zero;
        }
        return t;
    }

    void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        var list = new List<GameObject>();
        foreach (Transform ch in parent) list.Add(ch.gameObject);

#if UNITY_EDITOR
        if (Application.isPlaying) list.ForEach(Destroy);
        else list.ForEach(DestroyImmediate);
#else
        list.ForEach(DestroyImmediate);
#endif
    }
}

