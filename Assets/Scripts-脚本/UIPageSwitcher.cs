using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class Page
    {
        public string name;                 // 仅备注
        public RectTransform root;          // 这一页的根物体
        [Tooltip("可留空；若为空会自动从root上抓")]
        public Canvas canvas;
        [Tooltip("可留空；若为空会自动从root上抓")]
        public CanvasGroup canvasGroup;
        [Tooltip("可留空；若为空会自动从root上抓")]
        public GraphicRaycaster raycaster;
    }

    [Header("页面列表（每一页一个根）")]
    public List<Page> pages = new List<Page>();

    [Header("排序设置")]
    public bool useSortingOrder = true; // 建议开：最稳
    public int topOrder = 100;          // 当前页
    public int bottomOrder = 0;         // 上一页/其它页

    int _current = -1;
    int _previous = -1;

    void Awake()
    {
        // 自动抓组件
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

        // 1) 先把所有页设为“底层 & 不可交互”
        for (int i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            if (p.root == null) continue;

            if (useSortingOrder && p.canvas) p.canvas.sortingOrder = bottomOrder;
            else p.root.SetAsFirstSibling(); // 同一Canvas下用兄弟顺序

            if (p.canvasGroup)
            {
                p.canvasGroup.interactable = false;
                p.canvasGroup.blocksRaycasts = false; // 关键：不挡点击
                // alpha 可都保持 1；需要隐藏时再改
            }
            if (p.raycaster) p.raycaster.enabled = false;
        }

        // 2) 当前页置顶 & 可交互
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

    // 方便给 Button 直接绑定
    public void ShowByIndex(int index) => Show(index);
    public void ShowNext() => Show(Mathf.Clamp(_current + 1, 0, pages.Count - 1));
    public void ShowPrev() => Show(Mathf.Clamp(_current - 1, 0, pages.Count - 1));
}
