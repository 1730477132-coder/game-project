using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CherryPickup : MonoBehaviour
{
    public int points = 100;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        // 让樱桃只与玩家触发（可选）：把樱桃放到 Cherry 层，在 Layer 矩阵里只勾 Player↔Cherry
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (ScoreManager.I != null) ScoreManager.I.AddScore(points);
        Destroy(gameObject); // 被吃后立刻消失；Spawner 后续会按 5s 规则重生
    }
}
