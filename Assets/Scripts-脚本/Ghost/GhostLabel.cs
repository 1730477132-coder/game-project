using UnityEngine;
using TMPro; // 如果你用的是 Text，请改为 using UnityEngine.UI;

public class GhostLabel : MonoBehaviour
{
    public TMP_Text text;       // 如果用 Text，就把类型改成 Text
    public int number = 1;
    public bool faceCamera = true;

    public void Apply()
    {
        if (text) text.text = number.ToString();
    }

    void LateUpdate()
    {
        if (faceCamera && Camera.main)
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
    }
}

