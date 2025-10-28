using UnityEngine;
using TMPro;

public class GhostLabelSpawner : MonoBehaviour
{
    public GameObject labelPrefab;                // GhostLabelCanvas Ԥ��
    public string ghostNamePrefix = "ghost";
    public Vector3 localOffset = new Vector3(0f, 0.25f, 0f);
    public Vector2 canvasSize = new Vector2(0.4f, 0.2f);
    public string sortingLayer = "UI-World";
    public int orderInLayer = 200;
    public int fontSize = 18;

    void Start()
    {

        // �ҵ����й�
        var all = FindObjectsOfType<Transform>();
        int id = 0;
        foreach (var t in all)
        {
            if (!t.name.StartsWith(ghostNamePrefix)) continue;
            id++;

            // �����ɱ�ǩ
            var old = t.Find("Label");
            if (old) Destroy(old.gameObject);

            // ���ɲ���Ϊ������
            var inst = Instantiate(labelPrefab, t);
            inst.name = "Label";
            inst.transform.localPosition = localOffset;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            // ǿ������ Canvas
            var c = inst.GetComponent<Canvas>();
            if (c)
            {
                c.renderMode = RenderMode.WorldSpace;
                c.overrideSorting = true;
                c.sortingLayerName = sortingLayer;
                c.sortingOrder = orderInLayer;
                var rt = (RectTransform)c.transform;
                rt.sizeDelta = canvasSize;
            }

            // д����
            var tmp = inst.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp)
            {
                tmp.text = id.ToString();
                tmp.fontSize = fontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
            }
        }
    }
}

