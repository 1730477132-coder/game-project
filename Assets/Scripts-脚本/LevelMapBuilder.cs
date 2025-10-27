using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LevelMapBuilder : MonoBehaviour
{
    [Header("CSV（关卡地图，TextAsset）")]
    public TextAsset csv;

    [Header("网格参数")]
    public Vector2 origin = Vector2.zero;   // 左下角那一格的中心
    public float cellSize = 1f;             // 一格世界单位（像素/PPU）

    [Header("父物体（为空自动创建）")]
    public string defaultParentName = "Tiles";
    public Transform rootParent;

    public enum Shape { Other, Straight, Corner, TJunction, Cross, Cap }
    public enum CornerKind { UR, RD, DL, LU } // 上+右、右+下、下+左、左+上

    [Serializable]
    public class MapRule
    {
        [Tooltip("CSV 中的数字（精确匹配）")]
        public int id;

        [Tooltip("连接组（同一组的不同 id 互相连通）。例如：外墙=1，内墙=2")]
        public int connectionGroup = 0;

        [Tooltip("该 id 的形状（用于关系/传播）")]
        public Shape shape = Shape.Other;

        [Tooltip("要实例化的 Prefab")]
        public GameObject prefab;

        [Tooltip("基础角度（度）。Straight: 0=水平向右, 90=竖直向上；Corner 会被 CornerKind 覆盖")]
        public float baseRotationZ;

        [Tooltip("基础偏移（单位=格；多数保持 0,0）")]
        public Vector2 baseLocalOffset;

        [Header("仅 Straight：方向专用偏移（单位=格，填“大小”为正数）")]
        [Tooltip("水平直线：Y 分量填大小（正数），X=0；脚本按上/下自动加正负号")]
        public Vector2 straightOffsetHorizontal; // 建议 (0, d)
        [Tooltip("竖直直线：X 分量填大小（正数），Y=0；脚本按左/右自动加正负号")]
        public Vector2 straightOffsetVertical;   // 建议 (d, 0)

        [Header("仅 Corner：角类型 & 四向偏移（单位=格，带符号）")]
        public CornerKind cornerKind = CornerKind.UR;
        public Vector2 cornerOffset_UR;
        public Vector2 cornerOffset_RD;
        public Vector2 cornerOffset_DL;
        public Vector2 cornerOffset_LU;

        [Header("可选父物体名（空=默认父物体）")]
        public string parentName;
    }

    [Header("编号 → 规则（仅这些 id 会被生成）")]
    public List<MapRule> rules = new();

    [Header("生成选项")]
    public bool buildOnPlay = true;
    public bool clearExisting = true;

    // ---------- 内部 ----------
    int[,] grid; int rows, cols;
    readonly Dictionary<int, MapRule> ruleById = new();
    readonly Dictionary<string, Transform> parents = new();

    struct Plan
    {
        public int id;
        public MapRule rule;
        public float rot;         // 最终角度
        public Vector2 off;       // 最终本地偏移（单位=格）
        public string parentName;
    }
    Plan[,] plan;

    void Start()
    {
        if (Application.isPlaying && buildOnPlay) Build();
    }

#if UNITY_EDITOR
    [ContextMenu("Build Level")]
#endif
    public void Build()
    {
        if (csv == null) { Debug.LogError("LevelMapBuilder: 请拖入 CSV(TextAsset)。"); return; }
        ParseCSV(csv.text);

        // 索引规则
        ruleById.Clear();
        foreach (var r in rules) ruleById[r.id] = r;

        plan = new Plan[rows, cols];

        // 一遍：严格映射（按 id → 形状/Prefab/角度/偏移）
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int id = grid[r, c];
                if (!ruleById.TryGetValue(id, out var rule) || rule.prefab == null) continue;

                var p = new Plan
                {
                    id = id,
                    rule = rule,
                    rot = rule.baseRotationZ,
                    off = rule.baseLocalOffset,
                    parentName = string.IsNullOrWhiteSpace(rule.parentName) ? defaultParentName : rule.parentName
                };

                if (rule.shape == Shape.Straight)
                {
                    // 先按基础角度粗分方向，选用对应偏移“大小”
                    bool horizontal = IsHorizontal(p.rot);
                    p.off = horizontal ? rule.straightOffsetHorizontal : rule.straightOffsetVertical;
                }
                else if (rule.shape == Shape.Corner)
                {
                    // Corner 的角度/偏移由 CornerKind 显式决定
                    (p.rot, p.off) = CornerPoseFromKind(rule);
                }

                plan[r, c] = p;
            }

        // 二遍：Corner 的两条出边，修正邻接直线（只改直线的 rot/offset）
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var p = plan[r, c];
                if (p.rule == null || p.rule.shape != Shape.Corner) continue;

                (var A, var B) = CornerDirsFromKind(p.rule.cornerKind);
                FixNeighborStraight(r, c, A);
                FixNeighborStraight(r, c, B);
            }

        // 三遍（迭代）：让所有直线按邻居传播方向（整条线统一），并按“哪一侧”自动加正负偏移
        PropagateStraightDirections(maxIters: 8);

        // 实体生成
        var root = rootParent ? rootParent : transform;
        PrepareParents(root);

        if (clearExisting)
        {
            foreach (Transform pnode in root)
            {
                var del = new List<GameObject>();
                foreach (Transform ch in pnode)
                    if (ch.name.StartsWith("Wrapper_", StringComparison.Ordinal)) del.Add(ch.gameObject);
                foreach (var go in del) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            }
        }

        int placed = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var p = plan[r, c];
                if (p.rule == null || p.rule.prefab == null) continue;

                var parent = EnsureParent(root, p.parentName);
                Vector3 center = CellToWorld(r, c);

                // 1) 锚点
                var wrapper = new GameObject($"Wrapper_{p.id}@{r}_{c}");
                wrapper.transform.SetParent(parent, false);
                wrapper.transform.position = center;
                wrapper.transform.rotation = Quaternion.identity;
                wrapper.transform.localScale = Vector3.one;

                // 2) 子物体（先旋转，再定位）
                var child = (GameObject)Instantiate(p.rule.prefab, wrapper.transform);
                child.name = p.rule.prefab.name;
                child.transform.localRotation = Quaternion.Euler(0f, 0f, p.rot);
                child.transform.localPosition = new Vector3(p.off.x * cellSize, p.off.y * cellSize, 0f);
                child.transform.localScale = Vector3.one;

                placed++;
            }

        Debug.Log($"LevelMapBuilder: 放置 {placed} 个。地图尺寸：{rows}x{cols}");
    }

    // ====== 连接/传播逻辑 ======

    // “我”与邻居是否同组（可连）
    bool SameGroup(int r, int c, int nr, int nc)
    {
        if (!In(r, c) || !In(nr, nc)) return false;
        if (!ruleById.TryGetValue(grid[r, c], out var a)) return false;
        if (!ruleById.TryGetValue(grid[nr, nc], out var b)) return false;
        return a.connectionGroup != 0 && a.connectionGroup == b.connectionGroup;
    }

    // Corner 的两条出边 → 若邻居是 Straight 且同组，则设成横/竖并套对应偏移“大小”
    void FixNeighborStraight(int r, int c, (int dr, int dc) dir)
    {
        int nr = r + dir.dr, nc = c + dir.dc;
        if (!In(nr, nc)) return;
        if (!SameGroup(r, c, nr, nc)) return;

        var n = plan[nr, nc];
        if (n.rule == null || n.rule.shape != Shape.Straight) return;

        bool horiz = Mathf.Abs(dir.dc) == 1; // 列变化→左右→横
        n.rot = horiz ? 0f : 90f;

        // 先取偏移“大小”
        float mag = horiz ? Mathf.Abs(n.rule.straightOffsetHorizontal.y)
                          : Mathf.Abs(n.rule.straightOffsetVertical.x);

        // 根据“靠哪一侧”自动加符号
        if (horiz)
        {
            // 上靠(+)/下靠(-)：看上/下邻是否“开向我”
            bool favorUp = In(nr - 1, nc) && OpensTo(nr - 1, nc, +1, 0);
            bool favorDown = In(nr + 1, nc) && OpensTo(nr + 1, nc, -1, 0);
            int sign = (favorUp && !favorDown) ? +1 : (favorDown && !favorUp) ? -1 : +1;
            n.off = new Vector2(0f, sign * mag);
        }
        else
        {
            // 右靠(+)/左靠(-)
            bool favorRight = In(nr, nc + 1) && OpensTo(nr, nc + 1, 0, -1);
            bool favorLeft = In(nr, nc - 1) && OpensTo(nr, nc - 1, 0, +1);
            int sign = (favorRight && !favorLeft) ? +1 : (favorLeft && !favorRight) ? -1 : +1;
            n.off = new Vector2(sign * mag, 0f);
        }

        plan[nr, nc] = n;
    }

    // 用于传播：某格是否“向我”开口（按形状 + CornerKind）
    bool OpensTo(int r, int c, int drToMe, int dcToMe)
    {
        if (!In(r, c)) return false;
        var p = plan[r, c];
        if (p.rule == null) return false;

        switch (p.rule.shape)
        {
            case Shape.Straight:
                {
                    bool horiz = IsHorizontal(p.rot);
                    return horiz ? (drToMe == 0 && Math.Abs(dcToMe) == 1)
                                 : (Math.Abs(drToMe) == 1 && dcToMe == 0);
                }
            case Shape.Corner:
                {
                    var (A, B) = CornerDirsFromKind(p.rule.cornerKind);
                    return (A.dr == drToMe && A.dc == dcToMe) || (B.dr == drToMe && B.dc == dcToMe);
                }
            case Shape.TJunction:
            case Shape.Cross:
                return true; // 简化（如需更严谨可细化三/四方向）
            default:
                return false;
        }
    }

    void PropagateStraightDirections(int maxIters)
    {
        bool changed; int iters = 0;
        do
        {
            changed = false; iters++;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var p = plan[r, c];
                    if (p.rule == null || p.rule.shape != Shape.Straight) continue;

                    // 只和“同组”的邻居传播
                    bool vUp = In(r - 1, c) && SameGroup(r, c, r - 1, c) && OpensTo(r - 1, c, +1, 0);
                    bool vDown = In(r + 1, c) && SameGroup(r, c, r + 1, c) && OpensTo(r + 1, c, -1, 0);
                    bool hLeft = In(r, c - 1) && SameGroup(r, c, r, c - 1) && OpensTo(r, c - 1, 0, +1);
                    bool hRight = In(r, c + 1) && SameGroup(r, c, r, c + 1) && OpensTo(r, c + 1, 0, -1);

                    bool wantV = (vUp || vDown) && !(hLeft || hRight);
                    bool wantH = (hLeft || hRight) && !(vUp || vDown);
                    if (!wantV && !wantH) { if (vUp || vDown) wantV = true; else if (hLeft || hRight) wantH = true; }

                    float targetRot = p.rot;
                    Vector2 targetOff = p.off;

                    if (wantV)
                    {
                        targetRot = 90f;
                        bool favorRight = In(r, c + 1) && SameGroup(r, c, r, c + 1) && OpensTo(r, c + 1, 0, -1);
                        bool favorLeft = In(r, c - 1) && SameGroup(r, c, r, c - 1) && OpensTo(r, c - 1, 0, +1);
                        int sign = (favorRight && !favorLeft) ? +1 : (favorLeft && !favorRight) ? -1 : +1;
                        float mag = Mathf.Abs(p.rule.straightOffsetVertical.x);
                        targetOff = new Vector2(sign * mag, 0f);
                    }
                    else if (wantH)
                    {
                        targetRot = 0f;
                        bool favorUp = In(r - 1, c) && SameGroup(r, c, r - 1, c) && OpensTo(r - 1, c, +1, 0);
                        bool favorDown = In(r + 1, c) && SameGroup(r, c, r + 1, c) && OpensTo(r + 1, c, -1, 0);
                        int sign = (favorUp && !favorDown) ? +1 : (favorDown && !favorUp) ? -1 : +1;
                        float mag = Mathf.Abs(p.rule.straightOffsetHorizontal.y);
                        targetOff = new Vector2(0f, sign * mag);
                    }

                    if (Mathf.Abs(Mathf.DeltaAngle(targetRot, p.rot)) > 0.1f ||
                        (targetOff - p.off).sqrMagnitude > 1e-8f)
                    {
                        p.rot = targetRot;
                        p.off = targetOff;
                        plan[r, c] = p;
                        changed = true;
                    }
                }

        } while (changed && iters < maxIters);
    }

    // ====== 辅助 ======

    bool IsHorizontal(float rotDeg)
    {
        float a = Mathf.Repeat(rotDeg, 180f);
        return Mathf.Abs(a - 90f) > 45f;
    }

    (float rot, Vector2 off) CornerPoseFromKind(MapRule rule)
    {
        return rule.cornerKind switch
        {
            CornerKind.UR => (0f, rule.cornerOffset_UR),
            CornerKind.RD => (90f, rule.cornerOffset_RD),
            CornerKind.DL => (180f, rule.cornerOffset_DL),
            _ => (270f, rule.cornerOffset_LU),
        };
    }

    ((int dr, int dc) A, (int dr, int dc) B) CornerDirsFromKind(CornerKind kind)
    {
        return kind switch
        {
            CornerKind.UR => ((-1, 0), (0, +1)), // 上、右
            CornerKind.RD => ((0, +1), (+1, 0)), // 右、下
            CornerKind.DL => ((+1, 0), (0, -1)), // 下、左
            _ => ((0, -1), (-1, 0)), // 左、上
        };
    }

    void ParseCSV(string text)
    {
        var lines = text.Replace("\r", "").Split('\n');
        var valid = new List<string>();
        foreach (var l in lines) if (!string.IsNullOrWhiteSpace(l)) valid.Add(l);

        rows = valid.Count;
        cols = rows > 0 ? valid[0].Split(',').Length : 0;
        grid = new int[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            var parts = valid[r].Split(',');
            for (int c = 0; c < parts.Length; c++)
                int.TryParse(parts[c].Trim(), out grid[r, c]);
        }
    }

    Vector3 CellToWorld(int r, int c)
    {
        float x = origin.x + c * cellSize;
        float y = origin.y + (rows - 1 - r) * cellSize; // CSV 顶行映射到世界顶行
        return new Vector3(x, y, 0);
    }

    bool In(int r, int c) => r >= 0 && c >= 0 && r < rows && c < cols;

    void PrepareParents(Transform root) => parents.Clear();

    Transform EnsureParent(Transform root, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) name = defaultParentName;
        if (parents.TryGetValue(name, out var t) && t != null) return t;

        var found = root.Find(name);
        if (!found)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            found = go.transform;
        }
        parents[name] = found;
        return found;
    }
}
