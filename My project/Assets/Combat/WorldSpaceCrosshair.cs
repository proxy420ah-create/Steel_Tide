// Steel Tide: First Device — Combat / World Space Crosshair
// WorldSpaceCrosshair.cs
//
// Creates a 3D crosshair that floats in front of the camera in world space

using UnityEngine;

namespace SteelTide.Combat
{
    public class WorldSpaceCrosshair : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        public Color crosshairColor = Color.white;
        public float crosshairSize = 0.02f;  // Size in world units
        public float crosshairThickness = 0.002f;
        public float distanceFromCamera = 2f;  // How far in front of camera
        
        private GameObject _crosshairObject;
        private LineRenderer _horizontalLine;
        private LineRenderer _verticalLine;
        
        void Start()
        {
            CreateWorldSpaceCrosshair();
        }
        
        void CreateWorldSpaceCrosshair()
        {
            // Create parent object
            _crosshairObject = new GameObject("World Space Crosshair");
            _crosshairObject.transform.SetParent(transform);
            _crosshairObject.transform.localPosition = new Vector3(0, 0, distanceFromCamera);
            
            // Create horizontal line
            GameObject horizontal = new GameObject("Horizontal");
            horizontal.transform.SetParent(_crosshairObject.transform, false);
            _horizontalLine = horizontal.AddComponent<LineRenderer>();
            SetupLineRenderer(_horizontalLine, true);
            
            // Create vertical line
            GameObject vertical = new GameObject("Vertical");
            vertical.transform.SetParent(_crosshairObject.transform, false);
            _verticalLine = vertical.AddComponent<LineRenderer>();
            SetupLineRenderer(_verticalLine, false);
            
            Debug.Log("[WorldSpaceCrosshair] Created 3D crosshair in world space");
        }
        
        void SetupLineRenderer(LineRenderer line, bool horizontal)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = crosshairColor;
            line.endColor = crosshairColor;
            line.startWidth = crosshairThickness;
            line.endWidth = crosshairThickness;
            line.positionCount = 2;
            line.useWorldSpace = false;
            
            if (horizontal)
            {
                line.SetPosition(0, new Vector3(-crosshairSize, 0, 0));
                line.SetPosition(1, new Vector3(crosshairSize, 0, 0));
            }
            else
            {
                line.SetPosition(0, new Vector3(0, -crosshairSize, 0));
                line.SetPosition(1, new Vector3(0, crosshairSize, 0));
            }
        }
        
        void LateUpdate()
        {
            // Keep crosshair facing camera (billboard effect)
            if (_crosshairObject != null)
            {
                _crosshairObject.transform.rotation = Quaternion.identity;
            }
        }
    }
}
