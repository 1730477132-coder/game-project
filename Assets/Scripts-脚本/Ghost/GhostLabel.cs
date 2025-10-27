using UnityEngine;
using TMPro; // ������õ��� Text�����Ϊ using UnityEngine.UI;

public class GhostLabel : MonoBehaviour
{
    public TMP_Text text;       // ����� Text���Ͱ����͸ĳ� Text
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

