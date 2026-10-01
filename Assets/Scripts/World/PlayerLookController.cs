using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minimal fallback look controller.
/// Use when project does not already provide a look behaviour.
/// Reads Mouse delta (Input System if available, otherwise legacy Input).
/// Applies yaw around Y and clamped pitch around X.
/// Safe: non-invasive — only one component needed on Camera.
/// </summary>
[DisallowMultipleComponent]
public class PlayerLookController : MonoBehaviour
{
    [Tooltip("Mouse sensitivity multiplier")]
    public float sensitivity = 1.8f;

    [Tooltip("Minimum pitch (deg)")]
    public float minPitch = -80f;

    [Tooltip("Maximum pitch (deg)")]
    public float maxPitch = 80f;

    float yaw = 0f;
    float pitch = 0f;

    void Start()
    {
        Vector3 e = transform.localEulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void LateUpdate()
    {
        Vector2 delta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            // scale to be similar to legacy Input axes
            delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue() * 0.02f * sensitivity;
        }
        else
#endif
        {
            delta.x = Input.GetAxis("Mouse X") * sensitivity;
            delta.y = Input.GetAxis("Mouse Y") * sensitivity;
        }

        yaw += delta.x;
        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
