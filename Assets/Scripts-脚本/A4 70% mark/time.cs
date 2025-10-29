using UnityEngine;
using TMPro;

public class GameClock : MonoBehaviour
{
    public TMP_Text display;    
    public bool autoStart = true;

    float elapsed; bool running;

    void Start() { if (autoStart) StartClock(); UpdateText(); }
    void Update() { if (!running) return; elapsed += Time.deltaTime; UpdateText(); }

    public void StartClock() { running = true; }
    public void StopClock() { running = false; }
    public void ResetClock() { elapsed = 0f; UpdateText(); }

    public float ElapsedSeconds => elapsed;

    void UpdateText()
    {
        if (!display) return;
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        int centis = (int)((elapsed - Mathf.Floor(elapsed)) * 100f);
        display.text = $"{minutes:00}:{seconds:00}:{centis:00}";
    }
}
