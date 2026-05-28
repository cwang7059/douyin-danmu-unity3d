using UnityEngine;
using UnityEngine.EventSystems;

public sealed class OrbitTouchCamera : MonoBehaviour
{
    public Transform target;
    public float yaw = 0f;
    public float pitch = 18f;
    public float distance = 40f;
    public float minPitch = 12f;
    public float maxPitch = 64f;
    public float minDistance = 42f;
    public float maxDistance = 112f;
    public float rotateSensitivity = 0.11f;
    public float panSensitivity = 0.018f;
    public float pinchSensitivity = 0.035f;
    public float mouseZoomSensitivity = 2.2f;
    public Vector2 panXBounds = new Vector2(-7.5f, 7.5f);
    public Vector2 panZBounds = new Vector2(-10.5f, 11.5f);
    public Vector3 shakeOffset;

    private float previousPinchDistance;

    private void Update()
    {
        HandleTouch();
        HandleMouse();
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void LateUpdate()
    {
        if (!target)
        {
            return;
        }

        ClampTarget();
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var offset = rotation * new Vector3(0f, 0f, -distance);
        transform.position = target.position + offset + shakeOffset;
        transform.LookAt(target.position + Vector3.up * 0.65f);
    }

    private void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved && !IsPointerOverUi(touch.fingerId))
            {
                Pan(touch.deltaPosition);
            }
            previousPinchDistance = 0f;
            return;
        }

        if (Input.touchCount < 2)
        {
            previousPinchDistance = 0f;
            return;
        }

        var first = Input.GetTouch(0).position;
        var second = Input.GetTouch(1).position;
        var currentDistance = Vector2.Distance(first, second);
        if (previousPinchDistance > 0f)
        {
            distance -= (currentDistance - previousPinchDistance) * pinchSensitivity;
        }
        previousPinchDistance = currentDistance;

        var firstDelta = Input.GetTouch(0).deltaPosition;
        var secondDelta = Input.GetTouch(1).deltaPosition;
        Pan((firstDelta + secondDelta) * 0.5f);
    }

    private void HandleMouse()
    {
        if (Input.touchCount > 0)
        {
            return;
        }

        if (Input.GetMouseButton(0) && !IsPointerOverUi(-1))
        {
            Pan(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 18f);
        }

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 3.4f;
            pitch -= Input.GetAxis("Mouse Y") * 2.6f;
        }

        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * mouseZoomSensitivity;
        }
    }

    private void Pan(Vector2 delta)
    {
        if (!target || delta.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float scale = panSensitivity * Mathf.Lerp(0.7f, 1.4f, Mathf.InverseLerp(minDistance, maxDistance, distance));
        target.position += (-right * delta.x - forward * delta.y) * scale;
        ClampTarget();
    }

    private void ClampTarget()
    {
        var position = target.position;
        position.x = Mathf.Clamp(position.x, panXBounds.x, panXBounds.y);
        position.z = Mathf.Clamp(position.z, panZBounds.x, panZBounds.y);
        target.position = position;
    }

    private bool IsPointerOverUi(int fingerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
