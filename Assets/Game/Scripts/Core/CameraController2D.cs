using UnityEngine;
using Witherspoon.Game.Map;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Handles perspective camera panning and zooming (distance) over the playfield.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GridManager grid;

        [Header("Orientation")]
        [SerializeField] private float yawDegrees = 35f;
        [SerializeField] private float pitchDegrees = 55f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 18f;
        [SerializeField] private float dragPanSpeed = 0.6f;
        [SerializeField] private KeyCode fastPanModifier = KeyCode.LeftShift;
        [SerializeField] private float fastPanMultiplier = 1.6f;

        [Header("Zoom (Distance)")]
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 26f;
        [SerializeField] private float zoomSpeed = 25f;
        [SerializeField] private float zoomLerpSpeed = 10f;
        [SerializeField] private bool invertScroll;

        [Header("Smoothing")]
        [SerializeField] private float positionLerp = 10f;

        private Vector3 _focusPoint;
        private float _targetDistance;
        private Quaternion _targetRotation;

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

            targetCamera.orthographic = false;

            _targetRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            _focusPoint = ResolveInitialFocus();
            _targetDistance = Mathf.Clamp(Vector3.Distance(targetCamera.transform.position, _focusPoint), minDistance, maxDistance);
            SnapTo(_focusPoint);
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
            float modifier = Input.GetKey(fastPanModifier) ? fastPanMultiplier : 1f;
            float baseSpeed = panSpeed * modifier;

            Vector2 moveInput = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 planarRight = GetPlanarDirection(targetCamera.transform.right);
                Vector3 planarForward = GetPlanarDirection(targetCamera.transform.forward);
                Vector3 desired = (planarRight * moveInput.x + planarForward * moveInput.y).normalized;
                _focusPoint += desired * baseSpeed * Time.unscaledDeltaTime;
            }

            if (Input.GetMouseButton(2))
            {
                Vector2 dragDelta = new(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
                Vector3 drag = (GetPlanarDirection(targetCamera.transform.right) * dragDelta.x + GetPlanarDirection(targetCamera.transform.forward) * dragDelta.y) * (dragPanSpeed * modifier);
                _focusPoint += drag;
            }

            ClampFocusToGrid(ref _focusPoint);
        }

        private void HandleZoomInput()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) <= 0.01f) return;

            if (invertScroll) scroll = -scroll;

            _targetDistance -= scroll * zoomSpeed * Time.unscaledDeltaTime;
            _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
            ClampFocusToGrid(ref _focusPoint);
        }

        private void ApplyCameraTargets()
        {
            _targetRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);

            Vector3 desiredPosition = _focusPoint - (_targetRotation * Vector3.forward) * _targetDistance;
            Vector3 currentPosition = targetCamera.transform.position;
            Vector3 smoothedPosition = Vector3.Lerp(currentPosition, desiredPosition, positionLerp * Time.unscaledDeltaTime);

            targetCamera.transform.position = smoothedPosition;
            targetCamera.transform.rotation = Quaternion.Slerp(targetCamera.transform.rotation, _targetRotation, positionLerp * Time.unscaledDeltaTime);
        }

        private Vector3 GetPlanarDirection(Vector3 vector)
        {
            vector.z = 0f;
            if (vector == Vector3.zero) return Vector3.zero;
            return vector.normalized;
        }

        private void ClampFocusToGrid(ref Vector3 focus)
        {
            if (grid == null) return;

            Vector3 minWorld = grid.OriginPosition;
            Vector3 maxWorld = minWorld + new Vector3(grid.Dimensions.x * grid.CellSize, grid.Dimensions.y * grid.CellSize, 0f);

            float radiusX = Mathf.Tan(Mathf.Deg2Rad * targetCamera.fieldOfView * 0.5f) * _targetDistance * targetCamera.aspect;
            float radiusY = Mathf.Tan(Mathf.Deg2Rad * targetCamera.fieldOfView * 0.5f) * _targetDistance;

            float minX = minWorld.x + radiusX;
            float maxX = maxWorld.x - radiusX;
            float minY = minWorld.y + radiusY;
            float maxY = maxWorld.y - radiusY;

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

            focus.x = Mathf.Clamp(focus.x, minX, maxX);
            focus.y = Mathf.Clamp(focus.y, minY, maxY);
            focus.z = 0f;
        }

        private Vector3 ResolveInitialFocus()
        {
            if (grid == null)
            {
                return new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 0f);
            }

            Vector3 minWorld = grid.OriginPosition;
            Vector3 maxWorld = minWorld + new Vector3(grid.Dimensions.x * grid.CellSize, grid.Dimensions.y * grid.CellSize, 0f);
            Vector3 center = Vector3.Lerp(minWorld, maxWorld, 0.5f);
            center.z = 0f;
            return center;
        }

        public void SnapTo(Vector3 worldPosition)
        {
            _focusPoint = new Vector3(worldPosition.x, worldPosition.y, 0f);
            ClampFocusToGrid(ref _focusPoint);
            Vector3 desiredPosition = _focusPoint - (_targetRotation * Vector3.forward) * _targetDistance;
            targetCamera.transform.position = desiredPosition;
            targetCamera.transform.rotation = _targetRotation;
        }
    }
}
