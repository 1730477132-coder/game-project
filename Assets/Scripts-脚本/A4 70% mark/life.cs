using UnityEngine;

public class LivesIcons : MonoBehaviour
{
    public GameObject[] icons; // °´Ë³ÐòÍÏ Life1, Life2, Life3
    void LateUpdate()
    {
        if (!ScoreManager.I) return;
        int lives = Mathf.Max(0, ScoreManager.I.lives);
        for (int i = 0; i < icons.Length; i++) icons[i].SetActive(i < lives);
    }
}

