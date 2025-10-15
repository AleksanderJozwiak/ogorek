using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIBlockerDebugger : MonoBehaviour
{
    void Update()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            Debug.Log("UI elements under mouse (top is blocking):");
            foreach (var res in results)
            {
                Debug.Log($"- {res.gameObject.name}");
            }
        }
    }
}
