using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays an interaction prompt (like "Press E") above an interactable object.
/// Attach to any GameObject with an IInteractable component.
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    [Header("Prompt Settings")]
    [Tooltip("Sprites for jiggle animation (2-3 sprites recommended)")]
    public Sprite[] bubbleSprites;
    
    [Tooltip("How fast to cycle through sprites (lower = faster)")]
    public float animationSpeed = 0.15f;
    
    [Tooltip("Offset from object position")]
    public Vector3 worldOffset = new Vector3(0, 2, 0);
    
    [Tooltip("Custom text (leave empty to use IInteractable prompt)")]
    public string customPromptText = "";

    [Header("References")]
    [Tooltip("Leave empty to auto-create")]
    public GameObject promptUI;
    
    private Canvas canvas;
    private Image bubbleImage;
    private TextMeshProUGUI promptText;
    private Transform playerTransform;
    private IInteractable interactable;
    
    private float animationTimer = 0f;
    private int currentSpriteIndex = 0;
    private bool isVisible = false;

    void Start()
    {
        // Get interactable component
        interactable = GetComponent<IInteractable>();
        if (interactable == null)
        {
            interactable = GetComponentInParent<IInteractable>();
        }

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Create or setup UI
        if (promptUI == null)
        {
            CreatePromptUI();
        }
        else
        {
            SetupExistingPromptUI();
        }

        // Start hidden
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    void CreatePromptUI()
    {
        // Find or create world space canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
        {
            GameObject canvasObj = new GameObject("InteractionPromptsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
        }

        // Create prompt UI
        promptUI = new GameObject($"Prompt_{gameObject.name}");
        promptUI.transform.SetParent(canvas.transform);
        
        RectTransform rectTransform = promptUI.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 100);

        // Add bubble image
        bubbleImage = promptUI.AddComponent<Image>();
        if (bubbleSprites != null && bubbleSprites.Length > 0)
        {
            bubbleImage.sprite = bubbleSprites[0];
        }
        bubbleImage.preserveAspect = true;

        // Add text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptUI.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 18;
        promptText.color = Color.black;
    }

    void SetupExistingPromptUI()
    {
        bubbleImage = promptUI.GetComponent<Image>();
        promptText = promptUI.GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        if (promptUI == null) return;

        // Update position to follow object
        if (isVisible)
        {
            UpdatePromptPosition();
            AnimateBubble();
        }
    }

    void UpdatePromptPosition()
    {
        Vector3 worldPos = transform.position + worldOffset;
        promptUI.transform.position = worldPos;

        // Optional: Make prompt face camera
        if (Camera.main != null)
        {
            promptUI.transform.rotation = Quaternion.LookRotation(promptUI.transform.position - Camera.main.transform.position);
        }
    }

    void AnimateBubble()
    {
        if (bubbleSprites == null || bubbleSprites.Length <= 1 || bubbleImage == null) return;

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            currentSpriteIndex = (currentSpriteIndex + 1) % bubbleSprites.Length;
            bubbleImage.sprite = bubbleSprites[currentSpriteIndex];
        }
    }

    /// <summary>
    /// Show the prompt (called by PlayerInteraction)
    /// </summary>
    public void Show()
    {
        if (promptUI == null) return;

        // Update text
        if (promptText != null)
        {
            if (!string.IsNullOrEmpty(customPromptText))
            {
                promptText.text = customPromptText;
            }
            else if (interactable != null)
            {
                promptText.text = interactable.GetInteractionPrompt();
            }
        }

        promptUI.SetActive(true);
        isVisible = true;
        UpdatePromptPosition();
    }

    /// <summary>
    /// Hide the prompt
    /// </summary>
    public void Hide()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        isVisible = false;
    }

    /// <summary>
    /// Check if player is in range to show prompt
    /// </summary>
    public bool IsPlayerInRange(float range)
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(transform.position, playerTransform.position) <= range;
    }

    void OnDestroy()
    {
        if (promptUI != null)
        {
            Destroy(promptUI);
        }
    }
}