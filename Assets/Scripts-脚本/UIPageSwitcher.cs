using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        public string name;                 // ����ע
        public RectTransform root;          // ��һҳ�ĸ�����
        [Tooltip("�����գ���Ϊ�ջ��Զ���root��ץ")]
        public Canvas canvas;
        [Tooltip("�����գ���Ϊ�ջ��Զ���root��ץ")]
        public CanvasGroup canvasGroup;
        [Tooltip("�����գ���Ϊ�ջ��Զ���root��ץ")]
        public GraphicRaycaster raycaster;
    }

    [Header("ҳ���б�ÿһҳһ������")]
    public List<Page> pages = new List<Page>();

    [Header("��������")]
    public bool useSortingOrder = true; // ���鿪������
    public int topOrder = 100;          // ��ǰҳ
    public int bottomOrder = 0;         // ��һҳ/����ҳ

    int _current = -1;
    int _previous = -1;

    void Awake()
    {
        // �Զ�ץ���
        foreach (var p in pages)
        {
            if (!p.root) continue;
            if (!p.canvas) p.canvas = p.root.GetComponent<Canvas>();
            if (!p.canvasGroup) p.canvasGroup = p.root.GetComponent<CanvasGroup>();
            if (!p.raycaster) p.raycaster = p.root.GetComponent<GraphicRaycaster>();
            if (p.canvas && useSortingOrder) p.canvas.overrideSorting = true;
        }
    }

    public void Show(int index)
    {
        if (index < 0 || index >= pages.Count) return;
        if (_current == index) return;

        _previous = _current;
        _current = index;

        // 1) �Ȱ�����ҳ��Ϊ���ײ� & ���ɽ�����
        for (int i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            if (p.root == null) continue;

            if (useSortingOrder && p.canvas) p.canvas.sortingOrder = bottomOrder;
            else p.root.SetAsFirstSibling(); // ͬһCanvas�����ֵ�˳��

            if (p.canvasGroup)
            {
                p.canvasGroup.interactable = false;
                p.canvasGroup.blocksRaycasts = false; // �ؼ����������
                // alpha �ɶ����� 1����Ҫ����ʱ�ٸ�
            }
            if (p.raycaster) p.raycaster.enabled = false;
        }

        // 2) ��ǰҳ�ö� & �ɽ���
        var cur = pages[_current];
        if (useSortingOrder && cur.canvas) cur.canvas.sortingOrder = topOrder;
        else cur.root.SetAsLastSibling();

        if (cur.canvasGroup)
        {
            cur.canvasGroup.interactable = true;
            cur.canvasGroup.blocksRaycasts = true;
        }
        if (cur.raycaster) cur.raycaster.enabled = true;
    }

    // ����� Button ֱ�Ӱ�
    public void ShowByIndex(int index) => Show(index);
    public void ShowNext() => Show(Mathf.Clamp(_current + 1, 0, pages.Count - 1));
    public void ShowPrev() => Show(Mathf.Clamp(_current - 1, 0, pages.Count - 1));
}
