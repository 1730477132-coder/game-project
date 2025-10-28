using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("樱桃移动速度（单位/秒）")]
    public float moveSpeed = 3f;
    
    [Header("关卡边界")]
    [Tooltip("关卡中心点")]
    public Transform levelCenter;
    
    [Tooltip("关卡边界范围")]
    public Vector2 levelBounds = new Vector2(20f, 15f);
    
    [Header("相机检测")]
    [Tooltip("主相机")]
    public Camera mainCamera;
    
    [Tooltip("超出相机范围的距离")]
    public float cameraMargin = 2f;
    
    // 移动状态
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 centerPosition;
    private float journeyLength;
    private float startTime;
    private bool isMoving = false;
    
    // 移动方向
    private Vector3 moveDirection;
    
    void OnDestroy()
    {
        isMoving = false;
    }
    
    void Update()
    {
        if (isMoving)
        {
            MoveCherry();
            CheckBounds();
        }
    }
    
    /// <summary>
    /// 初始化樱桃移动路径
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="endPos">结束位置</param>
    public void InitializeMovement(Vector3 startPos, Vector3 endPos)
    {
        startPosition = startPos;
        endPosition = endPos;
        centerPosition = (startPos + endPos) * 0.5f; // 计算中心点
        
        // 设置初始位置
        transform.position = startPosition;
        
        // 计算移动方向
        moveDirection = (endPosition - startPosition).normalized;
        
        // 计算总距离
        journeyLength = Vector3.Distance(startPosition, endPosition);
        
        // 开始移动
        startTime = Time.time;
        isMoving = true;
        
        Debug.Log($"Cherry: 开始移动从 {startPosition} 到 {endPosition}");
    }
    
    /// <summary>
    /// 樱桃移动逻辑
    /// </summary>
    void MoveCherry()
    {
        // 计算移动进度
        float distanceCovered = (Time.time - startTime) * moveSpeed;
        float fractionOfJourney = distanceCovered / journeyLength;
        
        // 使用线性插值移动
        transform.position = Vector3.Lerp(startPosition, endPosition, fractionOfJourney);
        
        // 检查是否到达终点
        if (fractionOfJourney >= 1f)
        {
            DestroyCherry();
        }
    }
    
    /// <summary>
    /// 检查是否超出边界
    /// </summary>
    void CheckBounds()
    {
        if (!mainCamera) return;
        
        // 获取相机视野范围
        Vector3 cameraPos = mainCamera.transform.position;
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        // 计算相机边界
        float leftBound = cameraPos.x - cameraWidth * 0.5f - cameraMargin;
        float rightBound = cameraPos.x + cameraWidth * 0.5f + cameraMargin;
        float bottomBound = cameraPos.y - cameraHeight * 0.5f - cameraMargin;
        float topBound = cameraPos.y + cameraHeight * 0.5f + cameraMargin;
        
        // 检查是否超出相机视野
        Vector3 currentPos = transform.position;
        if (currentPos.x < leftBound || currentPos.x > rightBound ||
            currentPos.y < bottomBound || currentPos.y > topBound)
        {
            DestroyCherry();
        }
    }
    
    /// <summary>
    /// 销毁樱桃
    /// </summary>
    void DestroyCherry()
    {
        isMoving = false;
        
        // 通知生成器樱桃被销毁
        CherrySpawner spawner = FindObjectOfType<CherrySpawner>();
        if (spawner)
        {
            spawner.OnCherryDestroyed();
        }
        
        Debug.Log("Cherry: 樱桃被销毁");
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 当被玩家收集时调用
    /// </summary>
    public void OnCollected()
    {
        Debug.Log("Cherry: 樱桃被玩家收集");
        DestroyCherry();
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 绘制移动路径
        if (isMoving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPosition, endPosition);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerPosition, 0.5f);
        }
        
        // 绘制关卡边界
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(centerPosition, new Vector3(levelBounds.x, levelBounds.y, 0.1f));
    }
#endif
}