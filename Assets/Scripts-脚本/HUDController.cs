using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class HUDController : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text scoreValue;
    public TMP_Text timerValue;
    public TMP_Text frightValue;
    public TMP_Text levelName;

    [Header("Lives")]
    public GameObject life1;
    public GameObject life2;
    public GameObject life3;

    [Header("Config")]
    public string startSceneName = "StartScene";

    int mockScore = 0;     // 仅用于40%占位显示
    float mockTime = 0f;   // 秒
    float mockFright = 0f; // 秒（<=0时你可以先手动隐藏）

    void Update()
    {
        // 仅演示：运行时让数字在动，证明布局OK（提交时可保持为0）
        mockTime += Time.deltaTime;
        if (timerValue) timerValue.text = FormatTime(mockTime);

        if (scoreValue) scoreValue.text = mockScore.ToString("D6");

        if (frightValue)
            frightValue.text = mockFright > 0 ? mockFright.ToString("00.00") : "00.00";
    }

    string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int m = total / 60;
        int s = total % 60;
        int centi = Mathf.FloorToInt((seconds - total) * 100f);
        return $"{m:00}:{s:00}:{centi:00}";
    }

    // 退出按钮绑定这个
    public void OnClickExit()
    {
        SceneManager.LoadScene(startSceneName);
    }
}

