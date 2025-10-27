using UnityEngine;

/// <summary>
/// Closes shop UI when player moves too far from NPC.
/// Attach to ShopSystem GameObject or create separate GameObject.
/// </summary>
public class ShopProximityCloser : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The shop system to close")]
    public ShopSystem shopSystem;
    
    [Tooltip("The NPC the player is shopping with")]
    public Transform shopNPC;
    
    [Tooltip("Player transform")]
    public Transform player;

    [Header("Settings")]
    [Tooltip("Distance at which shop closes")]
    public float closeDistance = 5f;
    
    [Tooltip("Key to manually close shop")]
    public KeyCode closeKey = KeyCode.E;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool shopWasOpen = false;

    void Start()
    {
        // Auto-find if not assigned
        if (shopSystem == null)
            shopSystem = ShopSystem.Instance;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (shopSystem == null)
            Debug.LogError("[ShopProximityCloser] ShopSystem not found!");
        
        if (player == null)
            Debug.LogError("[ShopProximityCloser] Player not found!");
    }

    void Update()
    {
        if (shopSystem == null || player == null || shopNPC == null) return;

        bool isShopOpen = shopSystem.IsShopOpen();

        // Shop just opened - record which NPC
        if (isShopOpen && !shopWasOpen)
        {
            shopWasOpen = true;
            if (showDebugLogs) Debug.Log("[ShopProximityCloser] Shop opened, monitoring distance");
        }

        // Shop is open - check for close conditions
        if (isShopOpen)
        {
            // Check distance
            float distance = Vector3.Distance(player.position, shopNPC.position);

            if (distance > closeDistance)
            {
                if (showDebugLogs) Debug.Log($"[ShopProximityCloser] Player moved too far ({distance:F2}m > {closeDistance}m), closing shop");
                shopSystem.CloseShop();
                shopWasOpen = false;
                return;
            }

            // Check if player pressed close key
            if (Input.GetKeyDown(closeKey))
            {
                if (showDebugLogs) Debug.Log($"[ShopProximityCloser] Player pressed {closeKey}, closing shop");
                shopSystem.CloseShop();
                shopWasOpen = false;
            }
        }
        else
        {
            shopWasOpen = false;
        }
    }

    /// <summary>
    /// Call this when player starts shopping with a specific NPC
    /// </summary>
    public void SetCurrentShopNPC(Transform npc)
    {
        shopNPC = npc;
        if (showDebugLogs) Debug.Log($"[ShopProximityCloser] Now monitoring distance from {npc.name}");
    }

    void OnDrawGizmosSelected()
    {
        if (shopNPC != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(shopNPC.position, closeDistance);
        }
    }
}