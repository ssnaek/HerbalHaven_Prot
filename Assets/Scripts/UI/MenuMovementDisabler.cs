using UnityEngine;

/// <summary>
/// Disables player movement when menu is open (game scenes only).
/// Attach to same GameObject as SlideMenuController.
/// </summary>
public class MenuMovementDisabler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Can player move in this scene? (Check for main menu, uncheck for game)")]
    public bool allowMovementInThisScene = true;

    [Header("References")]
    [Tooltip("Leave empty to auto-find")]
    public SlideMenuController menuController;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private PlayerController playerController;
    private bool movementCurrentlyEnabled = true;

    void Start()
    {
        // Auto-find menu controller if not assigned
        if (menuController == null)
            menuController = GetComponent<SlideMenuController>();

        // Find player's PlayerController
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            
            if (playerController == null && showDebugLogs)
                Debug.LogWarning("[MenuMovementDisabler] Player found but no PlayerController!");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[MenuMovementDisabler] No Player GameObject with 'Player' tag found!");
        }

        // Initial state
        UpdateMovementState();
    }

    void Update()
    {
        // Only manage movement in game scenes (not main menu)
        if (!allowMovementInThisScene && menuController != null)
        {
            bool menuOpen = menuController.IsOpen();
            
            // Disable movement when menu is open, enable when closed
            if (menuOpen && movementCurrentlyEnabled)
            {
                DisableMovement();
            }
            else if (!menuOpen && !movementCurrentlyEnabled)
            {
                EnableMovement();
            }
        }
    }

    void UpdateMovementState()
    {
        if (allowMovementInThisScene)
        {
            // Main menu scene - always allow movement
            EnableMovement();
        }
        else
        {
            // Game scene - check menu state
            if (menuController != null && menuController.IsOpen())
            {
                DisableMovement();
            }
            else
            {
                EnableMovement();
            }
        }
    }

    void DisableMovement()
    {
        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
            movementCurrentlyEnabled = false;
            
            if (showDebugLogs) Debug.Log("[MenuMovementDisabler] Movement disabled");
        }
    }

    void EnableMovement()
    {
        if (playerController != null && !playerController.enabled)
        {
            playerController.enabled = true;
            movementCurrentlyEnabled = true;
            
            if (showDebugLogs) Debug.Log("[MenuMovementDisabler] Movement enabled");
        }
    }
}