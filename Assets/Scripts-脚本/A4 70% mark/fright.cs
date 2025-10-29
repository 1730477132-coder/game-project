using UnityEngine;
using TMPro;

public class FrightUI : MonoBehaviour
{
    public TMP_Text display;      // �� Canvas/HUDRoot/FrightBox ���ı�
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

    // ���ⲿ��GhostStateController��ˢ��ʣ��ʱ��
    public void SetRemaining(float seconds)
    {
        remain = Mathf.Max(0f, seconds);
        if (!visible && remain > 0f) Show(remain);
    }
}
