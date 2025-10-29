using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Changes text color when button is hovered.
/// Attach to TextMeshPro text component inside a button.
/// </summary>
public class ButtonTextHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Colors")]
    [Tooltip("Normal text color")]
    public Color normalColor = Color.black;
    
    [Tooltip("Color when hovered")]
    public Color hoverColor = Color.white;
    
    [Header("Transition")]
    [Tooltip("Smooth color transition")]
    public bool smoothTransition = true;
    
    [Tooltip("Transition speed (if smooth enabled)")]
    public float transitionSpeed = 10f;

    private TextMeshProUGUI text;
    private Color targetColor;
    private bool isHovered = false;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        
        if (text == null)
        {
            Debug.LogError("[ButtonTextHoverColor] No TextMeshProUGUI found!");
            enabled = false;
            return;
        }
        
        // Set initial color
        text.color = normalColor;
        targetColor = normalColor;
    }

    void Update()
    {
        if (smoothTransition && text != null)
        {
            // Smooth color transition
            text.color = Color.Lerp(text.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetColor = hoverColor;
        
        if (!smoothTransition && text != null)
        {
            text.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetColor = normalColor;
        
        if (!smoothTransition && text != null)
        {
            text.color = normalColor;
        }
    }

    // Reset color if disabled
    void OnDisable()
    {
        if (text != null)
        {
            text.color = normalColor;
        }
    }
}
