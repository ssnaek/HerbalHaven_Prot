using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates a tiled background pattern by scrolling its UV coordinates.
/// Perfect for polka dot patterns, stripes, etc.
/// Attach to a UI Image with a tiled material.
/// </summary>
public class AnimatedTiledBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Scroll direction and speed (X and Y)")]
    public Vector2 scrollSpeed = new Vector2(0.1f, -0.1f); // Diagonal down-right
    
    [Tooltip("Tile scale (how many times pattern repeats)")]
    public Vector2 tileScale = new Vector2(5f, 5f);

    [Header("References")]
    [Tooltip("The UI Image component (auto-finds if empty)")]
    public Image backgroundImage;

    private Material backgroundMaterial;
    private Vector2 offset = Vector2.zero;

    void Start()
    {
        // Auto-find Image component
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            Debug.LogError("[AnimatedTiledBackground] No Image component found!");
            enabled = false;
            return;
        }

        // Create material instance so we don't modify the original
        if (backgroundImage.material != null)
        {
            backgroundMaterial = new Material(backgroundImage.material);
            backgroundImage.material = backgroundMaterial;
        }
        else
        {
            Debug.LogError("[AnimatedTiledBackground] Image has no material assigned!");
            enabled = false;
            return;
        }

        // Set initial tiling
        backgroundMaterial.mainTextureScale = tileScale;
    }

    void Update()
    {
        // Scroll the texture offset
        offset += scrollSpeed * Time.deltaTime;
        
        // Wrap offset to prevent float precision issues
        offset.x = offset.x % 1f;
        offset.y = offset.y % 1f;

        // Apply to material
        if (backgroundMaterial != null)
        {
            backgroundMaterial.mainTextureOffset = offset;
        }
    }

    void OnDestroy()
    {
        // Clean up material instance
        if (backgroundMaterial != null)
        {
            Destroy(backgroundMaterial);
        }
    }
}