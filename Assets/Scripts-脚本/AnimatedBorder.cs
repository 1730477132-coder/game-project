using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated Border component for Start Screen UI
/// Handles scrolling texture animation on RawImage borders
/// </summary>
public class AnimatedBorder : MonoBehaviour
{
    [Header("Border Settings")]
    [SerializeField] private RawImage[] borderImages = new RawImage[4]; // Top, Bottom, Left, Right
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float thickness = 20f;
    
    private RectTransform[] borderRects;
    private float scrollOffset = 0f;
    
    void Start()
    {
        // Initialize border rectangles
        if (borderImages != null && borderImages.Length >= 4)
        {
            borderRects = new RectTransform[4];
            for (int i = 0; i < 4 && i < borderImages.Length; i++)
            {
                if (borderImages[i] != null)
                {
                    borderRects[i] = borderImages[i].GetComponent<RectTransform>();
                }
            }
        }
    }
    
    void Update()
    {
        // Update scroll offset
        scrollOffset += Time.deltaTime * scrollSpeed;
        
        // Update UV offset for each border
        for (int i = 0; i < borderImages.Length; i++)
        {
            if (borderImages[i] != null)
            {
                Rect uvRect = borderImages[i].uvRect;
                
                // Scrolling direction based on border type
                // 0 = Top, 1 = Bottom, 2 = Left, 3 = Right
                switch (i)
                {
                    case 0: // Top - scroll left to right
                        uvRect = new Rect(scrollOffset, 0, 1, 1);
                        break;
                    case 1: // Bottom - scroll right to left
                        uvRect = new Rect(-scrollOffset, 0, 1, 1);
                        break;
                    case 2: // Left - scroll top to bottom
                        uvRect = new Rect(0, -scrollOffset, 1, 1);
                        break;
                    case 3: // Right - scroll bottom to top
                        uvRect = new Rect(0, scrollOffset, 1, 1);
                        break;
                }
                
                borderImages[i].uvRect = uvRect;
            }
        }
    }
    
    /// <summary>
    /// Set border thickness
    /// </summary>
    public void SetThickness(float newThickness)
    {
        thickness = newThickness;
        
        if (borderRects != null)
        {
            // Update size of each border
            for (int i = 0; i < borderRects.Length; i++)
            {
                if (borderRects[i] != null)
                {
                    RectTransform rt = borderRects[i];
                    
                    switch (i)
                    {
                        case 0: // Top
                        case 1: // Bottom
                            rt.sizeDelta = new Vector2(0, thickness);
                            break;
                        case 2: // Left
                        case 3: // Right
                            rt.sizeDelta = new Vector2(thickness, 0);
                            break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Set scroll speed
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }
}
