using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Activates/deactivates UISway when button is hovered.
/// Attach to the same GameObject that has UISway.cs
/// </summary>
public class ButtonHoverSway : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("UISway component (auto-finds if empty)")]
    public UISway uiSway;
    
    [Header("Settings")]
    [Tooltip("Should sway only when hovered?")]
    public bool swayOnlyOnHover = true;

    void Start()
    {
        // Auto-find UISway
        if (uiSway == null)
        {
            uiSway = GetComponent<UISway>();
        }

        if (uiSway == null)
        {
            Debug.LogError("[ButtonHoverSway] No UISway component found!");
            enabled = false;
            return;
        }

        // Start with sway disabled if swayOnlyOnHover is true
        if (swayOnlyOnHover)
        {
            uiSway.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiSway != null)
        {
            uiSway.enabled = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiSway != null && swayOnlyOnHover)
        {
            uiSway.enabled = false;
        }
    }
}
