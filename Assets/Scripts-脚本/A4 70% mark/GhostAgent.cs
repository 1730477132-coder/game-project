// GhostAgent.cs —— 仅满足70%所需
using UnityEngine;

public enum GhostMode { Normal = 0, Scared = 1, Recovering = 2, Dead = 3 }

[RequireComponent(typeof(Collider2D))]
public class GhostAgent : MonoBehaviour
{
    [Header("State")]
    public GhostMode mode = GhostMode.Normal;

    [Header("Spawn / Reset")]
    public Transform spawnPoint;       // 可不填；不填就用初始位置
    private Vector3 _spawnPos;

    [Header("Optional")]
    public Animator animator;          // 可选
    public string animatorParam = "GhostMode"; // int参数(0..3)

    void Awake()
    {
        _spawnPos = spawnPoint ? spawnPoint.position : transform.position;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;          // 70%用触发检测

        // 向全局管理器登记
        if (GhostManager.I != null) GhostManager.I.Register(this);
    }

    public void SetMode(GhostMode newMode)
    {
        mode = newMode;
        if (animator) animator.SetInteger(animatorParam, (int)mode);
    }

    public void ResetToSpawn()
    {
        transform.position = _spawnPos;
        SetMode(GhostMode.Normal);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GhostManager.I?.HandlePlayerContact(this);
    }
}

