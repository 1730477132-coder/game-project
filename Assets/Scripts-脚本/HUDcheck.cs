using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastProbe : MonoBehaviour
{
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    void Reset()
    {
        if (!raycaster) raycaster = GetComponentInParent<Canvas>()?.GetComponent<GraphicRaycaster>();
        if (!eventSystem) eventSystem = EventSystem.current;
    }

    void Update()
    {
        if (!raycaster || !eventSystem) return;

        var ped = new PointerEventData(eventSystem) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);
        if (results.Count > 0)
        {
            // 层级最上面的第一个就是挡住点击的那个
            Debug.Log($"Top UI under cursor: {results[0].gameObject.name}");
        }
    }
}

