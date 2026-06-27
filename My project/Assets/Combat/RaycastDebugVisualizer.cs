// Steel Tide: First Device — Combat / Raycast Debug Visualizer
// RaycastDebugVisualizer.cs
//
// Draws a green sphere at the raycast hit point for debugging aim alignment

using UnityEngine;

namespace SteelTide.Combat
{
    public class RaycastDebugVisualizer : MonoBehaviour
    {
        [Header("Settings")]
        public bool showDebugSphere = true;
        public Color debugColor = Color.green;
        public float sphereSize = 0.1f;
        
        [Header("References")]
        public Camera fpsCamera;
        
        private Vector3? _lastHitPoint = null;
        
        void Start()
        {
            if (fpsCamera == null)
                fpsCamera = Camera.main;
        }
        
        void Update()
        {
            if (!showDebugSphere || fpsCamera == null)
                return;
            
            // Cast ray from center of screen (crosshair position)
            Ray ray = fpsCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                _lastHitPoint = hit.point;
            }
            else
            {
                _lastHitPoint = null;
            }
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugSphere || _lastHitPoint == null)
                return;
            
            // Draw green sphere at hit point (larger and brighter)
            Gizmos.color = debugColor;
            Gizmos.DrawSphere(_lastHitPoint.Value, sphereSize);
            Gizmos.DrawWireSphere(_lastHitPoint.Value, sphereSize * 2f); // Outer ring
            
            // Draw line from camera to hit point
            if (fpsCamera != null)
            {
                Gizmos.color = debugColor;
                Gizmos.DrawLine(fpsCamera.transform.position, _lastHitPoint.Value);
            }
        }
        
        void OnGUI()
        {
            if (!showDebugSphere || _lastHitPoint == null)
                return;
            
            // Draw debug text showing hit point coordinates
            Vector3 screenPos = fpsCamera.WorldToScreenPoint(_lastHitPoint.Value);
            if (screenPos.z > 0) // In front of camera
            {
                GUI.color = debugColor;
                GUI.Label(new Rect(10, 10, 300, 20), $"Hit Point: {_lastHitPoint.Value:F2}");
                GUI.Label(new Rect(10, 30, 300, 20), $"Distance: {Vector3.Distance(fpsCamera.transform.position, _lastHitPoint.Value):F2}m");
            }
        }
    }
}
