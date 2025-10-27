using UnityEngine;

/// 启动时实例化整张地图 Prefab，并可选整体缩放（默认 X=2, Y=2）。
public class LevelBootstrap : MonoBehaviour
{
    [Header("地图 Prefab（整张地图）")]
    public GameObject levelPrefab;          // ← 把你的 Tiles 整体做成的 Prefab 拖进来

    [Header("生成姿态")]
    public Vector3 spawnPosition = Vector3.zero;
    public Vector3 spawnEulerAngles = Vector3.zero;
    public Transform parent;                // 可选：放到某个父物体下

    [Header("生成后整体缩放")]
    public bool applyScale = true;
    public Vector2 targetScale = new Vector2(2f, 2f); // ← X=2, Y=2
    public bool uniformZ = true;                        // Z 轴保持 1

    [Header("命名")]
    public string instanceNameOverride = ""; // 为空则沿用 Prefab 名

    void Awake()
    {
        if (levelPrefab == null)
        {
            Debug.LogError("LevelBootstrap: 请拖入 levelPrefab（整张地图的 Prefab）。");
            return;
        }

        // 实例化
        var inst = Instantiate(levelPrefab,
                               spawnPosition,
                               Quaternion.Euler(spawnEulerAngles),
                               parent ? parent : null);

        // 命名
        if (!string.IsNullOrEmpty(instanceNameOverride))
            inst.name = instanceNameOverride;
        else
            inst.name = levelPrefab.name;

        // 整体缩放：只改实例根节点的 scale，避免破坏子对象对齐
        if (applyScale)
        {
            var s = inst.transform.localScale;
            inst.transform.localScale = new Vector3(targetScale.x, targetScale.y, uniformZ ? 1f : s.z);
        }
    }
}



