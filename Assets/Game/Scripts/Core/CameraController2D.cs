using UnityEngine;
using Witherspoon.Game.Map;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Handles orthographic camera panning and zooming for the main playfield.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GridManager grid;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 8f;
        [SerializeField] private float dragPanSpeed = 0.35f;
        [SerializeField] private KeyCode fastPanModifier = KeyCode.LeftShift;
        [SerializeField] private float fastPanMultiplier = 1.8f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 14f;
        [SerializeField] private float zoomLerpSpeed = 12f;
        [SerializeField] private bool invertScroll = false;

        private Vector3 _targetPosition;
        private float _targetZoom;

        private void Reset()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            _targetPosition = targetCamera.transform.position;
            _targetZoom = targetCamera.orthographicSize;
        }

        private void Update()
        {
            if (targetCamera == null) return;

            HandlePanInput();
            HandleZoomInput();
            ApplyCameraTargets();
        }

        private void HandlePanInput()
        {
            Vector3 move = Vector3.zero;

            float modifier = Input.GetKey(fastPanModifier) ? fastPanMultiplier : 1f;
            float baseSpeed = panSpeed * modifier;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                move += new Vector3(horizontal, vertical, 0f).normalized * baseSpeed;
            }

            if (Input.GetMouseButton(2))
            {
                float dragX = -Input.GetAxis("Mouse X") * dragPanSpeed * modifier;
                float dragY = -Input.GetAxis("Mouse Y") * dragPanSpeed * modifier;
                move += new Vector3(dragX * 60f, dragY * 60f, 0f);
            }

            _targetPosition += move * Time.unscaledDeltaTime;
            ClampToGridBounds(ref _targetPosition);
        }

        private void HandleZoomInput()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) <= 0.01f) return;

            if (invertScroll) scroll = -scroll;

            _targetZoom -= scroll * zoomSpeed * Time.unscaledDeltaTime * 10f;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }

        private void ApplyCameraTargets()
        {
            if (!Mathf.Approximately(targetCamera.orthographicSize, _targetZoom))
            {
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, _targetZoom, zoomLerpSpeed * Time.unscaledDeltaTime);
            }
            else
            {
                targetCamera.orthographicSize = _targetZoom;
            }

            Vector3 current = targetCamera.transform.position;
            Vector3 next = Vector3.Lerp(current, _targetPosition, 10f * Time.unscaledDeltaTime);
            targetCamera.transform.position = new Vector3(next.x, next.y, current.z);
        }

        private void ClampToGridBounds(ref Vector3 position)
        {
            if (grid == null) return;

            float halfHeight = _targetZoom;
            float halfWidth = halfHeight * targetCamera.aspect;

            Vector3 minWorld = grid.OriginPosition;
            Vector3 maxWorld = minWorld + new Vector3(grid.Dimensions.x * grid.CellSize, grid.Dimensions.y * grid.CellSize, 0f);

            float minX = minWorld.x + halfWidth;
            float maxX = maxWorld.x - halfWidth;
            float minY = minWorld.y + halfHeight;
            float maxY = maxWorld.y - halfHeight;

            if (minX > maxX)
            {
                float center = (minX + maxX) * 0.5f;
                minX = maxX = center;
            }
            if (minY > maxY)
            {
                float center = (minY + maxY) * 0.5f;
                minY = maxY = center;
            }

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        public void SnapTo(Vector3 worldPosition)
        {
            _targetPosition = new Vector3(worldPosition.x, worldPosition.y, targetCamera.transform.position.z);
            ClampToGridBounds(ref _targetPosition);
            targetCamera.transform.position = _targetPosition;
        }
    }
}
