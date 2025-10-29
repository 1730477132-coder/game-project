using UnityEngine;
using TMPro;

public class FrightUI : MonoBehaviour
{
    public TMP_Text display;      // 拖 Canvas/HUDRoot/FrightBox 的文本
    public Color normalColor = Color.white;
    public Color warningColor = new Color(1f, .5f, .5f);
    float remain; bool visible;

    void Awake() { Hide(); }

    public void Show(float seconds)
    {
        remain = Mathf.Max(0f, seconds);
        visible = true;
        if (display) display.gameObject.SetActive(true);
        UpdateText();
    }

    public void Hide()
    {
        visible = false;
        if (display) display.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!visible) return;
        remain -= Time.deltaTime;
        if (remain <= 0f) { Hide(); return; }
        UpdateText();
    }

    void UpdateText()
    {
        if (!display) return;
        display.color = remain <= 3f ? warningColor : normalColor;
        display.text = remain.ToString("00.00");
    }

    // 供外部（GhostStateController）刷新剩余时间
    public void SetRemaining(float seconds)
    {
        remain = Mathf.Max(0f, seconds);
        if (!visible && remain > 0f) Show(remain);
    }
}
