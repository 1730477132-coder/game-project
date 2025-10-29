using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerPill : MonoBehaviour
{
    public int points = 50;
    public float frightSeconds = 10f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (ScoreManager.I) ScoreManager.I.AddScore(points);
        if (GhostManager.I) GhostManager.I.TriggerFrightened(frightSeconds);

        Destroy(gameObject);
    }
}
