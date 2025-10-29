using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager I { get; private set; }

    [Header("Score & Lives")]
    public int score = 0;
    public int lives = 3;

    [Header("HUD (optional)")]
    public TMP_Text scoreText;   
    public TMP_Text livesText;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        RefreshHUD();
    }

    public void AddScore(int amount)
    {
        score += amount;
        RefreshHUD();
    }

    public void LoseLife(int amount = 1)
    {
        lives = Mathf.Max(0, lives - amount);
        RefreshHUD();
    }

    public void RefreshHUD()
    {
        if (scoreText) scoreText.text = score.ToString();
        if (livesText) livesText.text = "x " + lives;
    }
}
