using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Canvas-based interaction prompt. Shows at fixed position on screen.
/// Singleton - only one prompt at a time.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Parent panel containing the bubble and text")]
    public GameObject promptPanel;
    
    [Tooltip("Image component for the bubble animation")]
    public Image bubbleImage;
    
    [Tooltip("Text for interaction message")]
    public TextMeshProUGUI messageText;
    
    [Tooltip("Text for key display (e.g., 'E')")]
    public TextMeshProUGUI keyText;

    [Header("Animation")]
    [Tooltip("Bubble sprite frames for animation (3 sprites recommended)")]
    public Sprite[] bubbleSprites;
    
    [Tooltip("Time between frames (lower = faster)")]
    public float animationSpeed = 0.2f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float animationTimer = 0f;
    private int currentSpriteIndex = 0;
    private bool isVisible = false;
    private string currentKey = "E";

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Start hidden
        Hide();
    }

    void Update()
    {
        if (isVisible)
        {
            AnimateBubble();
        }
    }

    void AnimateBubble()
    {
        if (bubbleSprites == null || bubbleSprites.Length == 0 || bubbleImage == null) return;

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            currentSpriteIndex = (currentSpriteIndex + 1) % bubbleSprites.Length;
            bubbleImage.sprite = bubbleSprites[currentSpriteIndex];
        }
    }

    /// <summary>
    /// Show prompt with message
    /// </summary>
    public void Show(string message, string interactionKey = "E")
    {
        if (promptPanel == null) return;

        currentKey = interactionKey;

        if (messageText != null)
        {
            messageText.text = message;
        }

        if (keyText != null)
        {
            keyText.text = interactionKey;
        }

        promptPanel.SetActive(true);
        isVisible = true;

        // Reset animation
        animationTimer = 0f;
        currentSpriteIndex = 0;
        if (bubbleImage != null && bubbleSprites != null && bubbleSprites.Length > 0)
        {
            bubbleImage.sprite = bubbleSprites[0];
        }

        if (showDebugLogs) Debug.Log($"[PromptUI] Showing: {message}");
    }

    /// <summary>
    /// Hide the prompt
    /// </summary>
    public void Hide()
    {
        if (promptPanel == null) return;

        promptPanel.SetActive(false);
        isVisible = false;

        if (showDebugLogs) Debug.Log("[PromptUI] Hidden");
    }

    /// <summary>
    /// Check if prompt is currently visible
    /// </summary>
    public bool IsVisible() => isVisible;
}