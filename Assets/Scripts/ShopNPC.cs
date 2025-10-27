using UnityEngine;

public class ShopNPC : MonoBehaviour, IInteractable
{
    [Header("Shop Reference")]
    [Tooltip("Reference to the ShopSystem (will find automatically if empty)")]
    public ShopSystem shopSystem;

    [Header("Prompt Settings")]
    public string npcName = "Shopkeeper";
    public string interactionPrompt = "Open Shop";

    [Header("Range Settings")]
    public float interactionRange = 3f;

    [Header("Debug")]
    public bool showDebugLogs = false;
    

    private Transform player;
    private bool shopIsOpen = false;

    void Start()
    {
        // Auto-find ShopSystem if not assigned
        if (shopSystem == null)
        {
            shopSystem = FindObjectOfType<ShopSystem>();
        }

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Ensure NPC has collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add capsule collider if missing
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1, 0);
            
            if (showDebugLogs) Debug.Log("[ShopNPC] Added CapsuleCollider to NPC");
        }
    }

    void Update()
    {
        // Check if shop is open and player moved away
        if (shopIsOpen && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > interactionRange)
            {
                CloseShop();
            }
        }
    }

    public bool CanInteract()
    {
        return shopSystem != null;
    }

    public void Interact()
    {
        if (shopSystem == null) 
        {
            Debug.LogError("[ShopNPC] ShopSystem not found!");
            return;
        }

        // Toggle shop
        if (shopIsOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    void OpenShop()
    {
        if (showDebugLogs) Debug.Log($"[ShopNPC] Opening shop from {npcName}");
        shopSystem.OpenShop();
        shopIsOpen = true;
    }

    void CloseShop()
    {
        if (showDebugLogs) Debug.Log($"[ShopNPC] Closing shop");
        shopSystem.CloseShop();
        shopIsOpen = false;
    }

    public string GetInteractionPrompt()
    {
        if (shopIsOpen)
            return "Close Shop";
        else
            return interactionPrompt;
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}