// GhostAgent.cs ���� ������70%����
using UnityEngine;

public enum GhostMode { Normal = 0, Scared = 1, Recovering = 2, Dead = 3 }

[RequireComponent(typeof(Collider2D))]
public class GhostAgent : MonoBehaviour
{
    [Header("State")]
    public GhostMode mode = GhostMode.Normal;

    [Header("Spawn / Reset")]
    public Transform spawnPoint;       // �ɲ��������ó�ʼλ��
    private Vector3 _spawnPos;

    [Header("Optional")]
    public Animator animator;          // ��ѡ
    public string animatorParam = "GhostMode"; // int����(0..3)

    void Awake()
    {
        _spawnPos = spawnPoint ? spawnPoint.position : transform.position;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;          // 70%�ô������

        // ��ȫ�ֹ������Ǽ�
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

