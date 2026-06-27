// Steel Tide: First Device — Combat / Crosshair UI
// CreateCrosshairUI.cs
//
// Automatically creates a UI Canvas with a crosshair for FPS gameplay.
// Attach this to the Player Camera and it will create the crosshair at startup.

using UnityEngine;
using UnityEngine.UI;  // For Canvas, CanvasScaler, GraphicRaycaster, Image

namespace SteelTide.Combat
{
    public class CreateCrosshairUI : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        public Color crosshairColor = Color.white;
        public float crosshairSize = 40f;  // Length of each line from center
        public float crosshairThickness = 4f;  // Thickness of lines
        
        void Start()
        {
            CreateCrosshair();
        }
        
        void CreateCrosshair()
        {
            // Create Canvas at root of scene (not parented to anything)
            GameObject canvasObj = new GameObject("Crosshair Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;  // Render on top of everything
            
            // Add CanvasScaler for proper scaling
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            Debug.Log($"[CreateCrosshairUI] Canvas created with RenderMode: {canvas.renderMode}");
            
            // Create horizontal line
            GameObject horizontal = new GameObject("Horizontal");
            horizontal.transform.SetParent(canvasObj.transform, false);
            RectTransform hRect = horizontal.AddComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(crosshairSize * 2, crosshairThickness);
            hRect.anchoredPosition = Vector2.zero;
            UnityEngine.UI.Image hImage = horizontal.AddComponent<UnityEngine.UI.Image>();
            hImage.sprite = null; // Use solid color, no sprite needed
            hImage.color = crosshairColor;
            
            // Create vertical line
            GameObject vertical = new GameObject("Vertical");
            vertical.transform.SetParent(canvasObj.transform, false);
            RectTransform vRect = vertical.AddComponent<RectTransform>();
            vRect.sizeDelta = new Vector2(crosshairThickness, crosshairSize * 2);
            vRect.anchoredPosition = Vector2.zero;
            UnityEngine.UI.Image vImage = vertical.AddComponent<UnityEngine.UI.Image>();
            vImage.sprite = null; // Use solid color, no sprite needed
            vImage.color = crosshairColor;
            
            Debug.Log("[CreateCrosshairUI] Crosshair UI created");
        }
    }
}
