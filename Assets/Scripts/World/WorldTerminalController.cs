using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WorldTerminalController — minimal Stage1 controller:
/// - shows hint when player looks at terminal
/// - Enter (E) opens Desktop (reuses existing object via ModeratorBootstrapBridge)
/// - Escape closes top window (WindowManagerBridge) or exits terminal
/// - disables player look components while in terminal and restores afterwards
/// - robust fallback to legacy Input if InputSystem absent
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WorldTerminalController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used to render the player view. If empty, Camera.main will be used.")]
    public Camera playerCamera;

    [Tooltip("Max distance for interaction raycast")]
    public float maxDistance = 3f;

    [Tooltip("World-space canvas / GameObject to show when looking at terminal")]
    public GameObject hintUI;

    [Tooltip("Optional fallback root to enable when opening desktop (if Moderator Prototype not found)")]
    public GameObject desktopRoot;

    [Header("Look control")]
    [Tooltip("Common look component type names that should be disabled while in terminal (case-insensitive)")]
    public string[] lookComponentNames = new string[] { "PlayerLook", "MouseLook", "CameraController", "PlayerLookController" };

    bool isLooking = false;
    bool isInTerminal = false;

    Collider _collider;

    void Start()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            // Safety: add BoxCollider if missing (should not happen due to RequireComponent)
            _collider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("WorldTerminalController: added missing BoxCollider.");
        }
        if (playerCamera == null) playerCamera = Camera.main;
        if (hintUI != null) hintUI.SetActive(false);

        // If there is no explicit look controller on the camera, optionally attach our fallback.
        EnsureFallbackLookController();
    }

    void Update()
    {
        if (playerCamera == null) return;

        UpdateLook();
        HandleInput();
    }

    void UpdateLook()
    {
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider == _collider)
            {
                if (hintUI != null && !hintUI.activeSelf) hintUI.SetActive(true);
                isLooking = true;
                return;
            }
        }
        if (hintUI != null && hintUI.activeSelf) hintUI.SetActive(false);
        isLooking = false;
    }

    void HandleInput()
    {
        bool pressedE = false;
        bool pressedEsc = false;

        // Prefer InputSystem if present
#if ENABLE_INPUT_SYSTEM
        try
        {
            if (Keyboard.current != null)
            {
                pressedE = Keyboard.current.eKey.wasPressedThisFrame;
                pressedEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
        }
        catch { /* fall back to legacy */ }
#endif
        if (!pressedE) pressedE = UnityEngine.Input.GetKeyDown(KeyCode.E);
        if (!pressedEsc) pressedEsc = UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        if (!isInTerminal && isLooking && pressedE)
        {
            EnterTerminal();
        }

        if (isInTerminal && pressedEsc)
        {
            // First try to close top window via WindowManagerBridge
            var closed = WindowManagerBridge.CloseTopWindow();
            if (!closed)
            {
                ExitTerminal();
            }
        }
    }

    void EnterTerminal()
    {
        if (isInTerminal) return;
        isInTerminal = true;
        Debug.Log("WorldTerminal: EnterTerminal called");

        // set UI cursor mode
        GameCursorBridge.SetCursorForUI();

        // open/reuse desktop
        bool opened = ModeratorBootstrapBridge.OpenDesktop();
        if (!opened && desktopRoot != null)
        {
            desktopRoot.SetActive(true);
        }

        // disable look behaviours
        ToggleLookComponents(true);
    }

    void ExitTerminal()
    {
        if (!isInTerminal) return;
        isInTerminal = false;
        Debug.Log("WorldTerminal: ExitTerminal called");

        // restore cursor for world
        GameCursorBridge.SetCursorForWorld();

        // hide desktop
        bool hid = ModeratorBootstrapBridge.HideDesktop();
        if (!hid && desktopRoot != null) desktopRoot.SetActive(false);

        // re-enable look
        ToggleLookComponents(false);
    }

    /// <summary>
    /// Disable or enable common look components by scanning camera root and camera object.
    /// Safe: tries to find components by type name.
    /// </summary>
    void ToggleLookComponents(bool disable)
    {
        if (playerCamera == null) return;

        // check camera and camera root
        var targets = new System.Collections.Generic.List<MonoBehaviour>();
        var cameraRoot = playerCamera.transform.root;
        // collect MonoBehaviours on camera
        targets.AddRange(playerCamera.GetComponents<MonoBehaviour>());
        // collect on root
        targets.AddRange(cameraRoot.GetComponents<MonoBehaviour>());

        foreach (var comp in targets)
        {
            if (comp == null) continue;
            var tname = comp.GetType().Name;
            foreach (var pattern in lookComponentNames)
            {
                if (string.Equals(tname, pattern, StringComparison.OrdinalIgnoreCase) ||
                    tname.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (comp is Behaviour b)
                    {
                        b.enabled = !disable;
                    }
                }
            }
        }
    }

    /// <summary>
    /// If no known look component exists on the camera, add our fallback PlayerLookController.
    /// This prevents the player being unable to look around in clean scenes.
    /// </summary>
    void EnsureFallbackLookController()
    {
        if (playerCamera == null) return;
        // If any known look type exists, do nothing
        var existing = playerCamera.GetComponents<MonoBehaviour>();
        foreach (var m in existing)
        {
            var n = m.GetType().Name.ToLower();
            if (n.Contains("look") || n.Contains("mouse") || n.Contains("camera"))
            {
                // assume existing look behaviour present
                return;
            }
        }

        // Otherwise, add fallback if not present
        if (playerCamera.GetComponent<PlayerLookController>() == null)
        {
            playerCamera.gameObject.AddComponent<PlayerLookController>();
            Debug.Log("WorldTerminalController: PlayerLookController added as fallback.");
        }
    }

    // Public API
    public void ForceEnter() => EnterTerminal();
    public void ForceExit() => ExitTerminal();
}
