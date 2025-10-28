using UnityEngine;
using System.Collections;

public class CherrySpawner : MonoBehaviour
{
    [Header("樱桃设置")]
    [Tooltip("樱桃预制体")]
    public GameObject cherryPrefab;
    
    [Tooltip("樱桃生成间隔时间（秒）")]
    public float spawnInterval = 5f;
    
    [Header("关卡边界")]
    [Tooltip("关卡中心点")]
    public Transform levelCenter;
    
    [Tooltip("关卡边界范围")]
    public Vector2 levelBounds = new Vector2(20f, 15f);
    
    [Tooltip("生成边界偏移（在关卡边界外多远生成）")]
    public float spawnOffset = 3f;
    
    [Header("移动路径")]
    [Tooltip("樱桃移动速度")]
    public float cherryMoveSpeed = 3f;
    
    [Tooltip("移动方向变化范围（度数）")]
    public Vector2 directionRange = new Vector2(-45f, 45f);
    
    [Header("调试")]
    [Tooltip("显示生成路径")]
    public bool showDebugPaths = true;
    
    // 内部状态
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    private GameObject currentCherry;
    
    void OnDestroy()
    {
        StopSpawning();
    }
    
    void OnDisable()
    {
        StopSpawning();
    }
    
    void Start()
    {
        // 如果没有设置关卡中心，使用世界原点
        if (!levelCenter)
            levelCenter = transform;
            
        // 开始生成樱桃
        StartSpawning();
    }
    
    /// <summary>
    /// 开始生成樱桃
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        
        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnCherryCoroutine());
        
        Debug.Log("CherrySpawner: 开始生成樱桃");
    }
    
    /// <summary>
    /// 停止生成樱桃
    /// </summary>
    public void StopSpawning()
    {
        if (!isSpawning) return;
        
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        Debug.Log("CherrySpawner: 停止生成樱桃");
    }
    
    /// <summary>
    /// 樱桃生成协程
    /// </summary>
    IEnumerator SpawnCherryCoroutine()
    {
        // 等待5秒后生成第一个樱桃
        yield return new WaitForSeconds(spawnInterval);
        
        while (isSpawning)
        {
            SpawnCherry();
            
            // 等待下一个樱桃被销毁后再生成
            while (currentCherry != null && isSpawning)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            if (!isSpawning) break;
            
            // 等待5秒再生成下一个
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    /// <summary>
    /// 生成樱桃
    /// </summary>
    void SpawnCherry()
    {
        if (!cherryPrefab)
        {
            Debug.LogError("CherrySpawner: 樱桃预制体未设置！");
            return;
        }
        
        // 计算生成位置和移动路径
        Vector3 startPos, endPos;
        CalculateSpawnPath(out startPos, out endPos);
        
        // 生成樱桃
        currentCherry = Instantiate(cherryPrefab, startPos, Quaternion.identity);
        
        // 设置樱桃控制器
        CherryController cherryController = currentCherry.GetComponent<CherryController>();
        if (cherryController)
        {
            cherryController.moveSpeed = cherryMoveSpeed;
            cherryController.levelCenter = levelCenter;
            cherryController.levelBounds = levelBounds;
            cherryController.InitializeMovement(startPos, endPos);
        }
        else
        {
            Debug.LogError("CherrySpawner: 樱桃预制体缺少CherryController组件！");
        }
        
        Debug.Log($"CherrySpawner: 生成樱桃在 {startPos}，目标 {endPos}");
    }
    
    /// <summary>
    /// 计算樱桃生成路径
    /// </summary>
    void CalculateSpawnPath(out Vector3 startPos, out Vector3 endPos)
    {
        Vector3 center = levelCenter.position;
        
        // 随机选择移动方向（水平或垂直）
        bool horizontalMove = Random.Range(0f, 1f) > 0.5f;
        
        if (horizontalMove)
        {
            // 水平移动：从左侧到右侧或从右侧到左侧
            bool leftToRight = Random.Range(0f, 1f) > 0.5f;
            
            float startX = leftToRight ? 
                center.x - levelBounds.x * 0.5f - spawnOffset : 
                center.x + levelBounds.x * 0.5f + spawnOffset;
            float endX = leftToRight ? 
                center.x + levelBounds.x * 0.5f + spawnOffset : 
                center.x - levelBounds.x * 0.5f - spawnOffset;
            
            // 在关卡高度范围内随机Y位置
            float randomY = center.y + Random.Range(-levelBounds.y * 0.4f, levelBounds.y * 0.4f);
            
            startPos = new Vector3(startX, randomY, center.z);
            endPos = new Vector3(endX, randomY, center.z);
        }
        else
        {
            // 垂直移动：从下侧到上侧或从上侧到下侧
            bool bottomToTop = Random.Range(0f, 1f) > 0.5f;
            
            float startY = bottomToTop ? 
                center.y - levelBounds.y * 0.5f - spawnOffset : 
                center.y + levelBounds.y * 0.5f + spawnOffset;
            float endY = bottomToTop ? 
                center.y + levelBounds.y * 0.5f + spawnOffset : 
                center.y - levelBounds.y * 0.5f - spawnOffset;
            
            // 在关卡宽度范围内随机X位置
            float randomX = center.x + Random.Range(-levelBounds.x * 0.4f, levelBounds.x * 0.4f);
            
            startPos = new Vector3(randomX, startY, center.z);
            endPos = new Vector3(randomX, endY, center.z);
        }
        
        // 添加随机角度偏移
        Vector3 direction = (endPos - startPos).normalized;
        float angleOffset = Random.Range(directionRange.x, directionRange.y);
        Quaternion rotation = Quaternion.AngleAxis(angleOffset, Vector3.forward);
        Vector3 offsetDirection = rotation * direction;
        
        // 重新计算终点位置
        float distance = Vector3.Distance(startPos, endPos);
        endPos = startPos + offsetDirection * distance;
    }
    
    /// <summary>
    /// 当樱桃被销毁时调用
    /// </summary>
    public void OnCherryDestroyed()
    {
        currentCherry = null;
    }
    
    /// <summary>
    /// 立即生成一个樱桃（用于测试）
    /// </summary>
    [ContextMenu("立即生成樱桃")]
    public void SpawnCherryNow()
    {
        if (currentCherry != null)
        {
            DestroyImmediate(currentCherry);
            currentCherry = null;
        }
        
        SpawnCherry();
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!levelCenter) return;
        
        Vector3 center = levelCenter.position;
        
        // 绘制关卡边界
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(center, new Vector3(levelBounds.x, levelBounds.y, 0.1f));
        
        // 绘制生成边界
        Gizmos.color = Color.red;
        Vector3 spawnBounds = new Vector3(
            levelBounds.x + spawnOffset * 2f,
            levelBounds.y + spawnOffset * 2f,
            0.1f
        );
        Gizmos.DrawWireCube(center, spawnBounds);
        
        if (showDebugPaths && currentCherry)
        {
            // 绘制当前樱桃的移动路径
            CherryController cherryController = currentCherry.GetComponent<CherryController>();
            if (cherryController)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(currentCherry.transform.position, center);
            }
        }
    }
#endif
}