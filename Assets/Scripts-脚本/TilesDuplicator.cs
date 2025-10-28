using UnityEngine;

public class LevelBootstrap : MonoBehaviour
{
    [Header("地图 Prefab（整张地图）")]
    public GameObject levelPrefab;

    [Header("生成姿态")]
    public Vector3 spawnPosition = Vector3.zero;
    public Vector3 spawnEulerAngles = Vector3.zero;
    public Transform parent;

    [Header("生成后整体缩放")]
    public bool applyScale = true;
    public Vector2 targetScale = new Vector2(2f, 2f);
    public bool uniformZ = true;

    [Header("命名")]
    public string instanceNameOverride = "";

    [Header("地形管理")]
    [Tooltip("启用：隐藏原地形，用副本替换；关闭：仅生成一个副本，不动原地形")]
    public bool enableTerrainReplacement = false;
    [Tooltip("原始地形的父对象（例如 Level Root）")]
    public Transform originalTerrainParent;

    // 运行期
    GameObject originalTerrainInstance;
    bool originalTerrainActive = true;
    GameObject duplicatedTerrainInstance;

    void Awake()
    {
        if (!levelPrefab)
        {
            Debug.LogError("LevelBootstrap: 请拖入 levelPrefab。", this);
            enabled = false;
            return;
        }

        CreateTerrainDuplicate();

        if (enableTerrainReplacement)
            HandleOriginalTerrain();
    }

    void CreateTerrainDuplicate()
    {
        duplicatedTerrainInstance = Instantiate(
            levelPrefab,
            spawnPosition,
            Quaternion.Euler(spawnEulerAngles),
            parent ? parent : null);

        duplicatedTerrainInstance.name = string.IsNullOrEmpty(instanceNameOverride)
            ? levelPrefab.name + "_Duplicate"
            : instanceNameOverride;

        if (applyScale)
        {
            var s = duplicatedTerrainInstance.transform.localScale;
            duplicatedTerrainInstance.transform.localScale =
                new Vector3(targetScale.x, targetScale.y, uniformZ ? 1f : s.z);
        }

        Debug.Log($"LevelBootstrap: 已创建地形副本 '{duplicatedTerrainInstance.name}'", duplicatedTerrainInstance);
    }

    void HandleOriginalTerrain()
    {
        if (!originalTerrainParent)
        {
            Debug.LogWarning("LevelBootstrap: 启用了替换，但 originalTerrainParent 未设置，跳过隐藏原地形。");
            return;
        }

        originalTerrainInstance = originalTerrainParent.gameObject;
        originalTerrainActive = originalTerrainInstance.activeSelf;

        originalTerrainInstance.SetActive(false);
        Debug.Log($"LevelBootstrap: 已隐藏原始地形 '{originalTerrainInstance.name}'", originalTerrainInstance);
    }

    public void RestoreOriginalTerrain()
    {
        // 恢复原始地形
        if (enableTerrainReplacement && originalTerrainInstance)
        {
            originalTerrainInstance.SetActive(originalTerrainActive);
            Debug.Log($"LevelBootstrap: 已恢复原始地形 '{originalTerrainInstance.name}'", originalTerrainInstance);
        }

        // 销毁副本（编辑器下用 DestroyImmediate，运行时 Destroy）
        if (duplicatedTerrainInstance)
        {
            string n = duplicatedTerrainInstance.name;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(duplicatedTerrainInstance);
            else                        Destroy(duplicatedTerrainInstance);
#else
            Destroy(duplicatedTerrainInstance);
#endif
            duplicatedTerrainInstance = null;
            Debug.Log($"LevelBootstrap: 已销毁地形副本 '{n}'");
        }
    }

    void OnApplicationQuit() => RestoreOriginalTerrain();
    void OnDisable() => RestoreOriginalTerrain();  // 取消 isPlaying 限制
    void OnDestroy() => RestoreOriginalTerrain();  // 双保险

#if UNITY_EDITOR
    [ContextMenu("Rebuild Duplicate (Editor)")]
    void CM_Rebuild()
    {
        RestoreOriginalTerrain();
        CreateTerrainDuplicate();
        if (enableTerrainReplacement) HandleOriginalTerrain();
    }

    [ContextMenu("Restore Now (Editor)")]
    void CM_RestoreNow()
    {
        RestoreOriginalTerrain();
    }
#endif
}