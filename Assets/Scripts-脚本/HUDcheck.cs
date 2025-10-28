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
            // �㼶������ĵ�һ�����ǵ�ס������Ǹ�
        }
    }
}

