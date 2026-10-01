using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class WorldTerminalController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used to render the player view. If empty, Camera.main will be used.")]
    public Camera playerCamera;

    [Tooltip("Distance for the interaction raycast")]
    public float maxDistance = 3f;

    [Tooltip("UI hint to show when looking at terminal (assign a GameObject, e.g. world-space canvas)")]
    public GameObject hintUI;

    [Tooltip("Optional fallback root to enable when opening desktop (if no Moderator Prototype found)")]
    public GameObject desktopRoot;

    [Header("Look control")]
    [Tooltip("Names of possible look-behaviour components to disable while in terminal")]
    public string[] lookComponentNames = new string[] { "PlayerLook", "MouseLook", "CameraController" };

    bool isLooking = false;
    bool isInTerminal = false;

    Collider _collider;

    void Start()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError("WorldTerminalController requires a Collider on the same GameObject.");
            enabled = false;
            return;
        }
        if (playerCamera == null) playerCamera = Camera.main;
        if (hintUI != null) hintUI.SetActive(false);
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
        // Enter: E
        bool pressedE = false;
        bool pressedEsc = false;

        if (Keyboard.current != null)
        {
            pressedE = Keyboard.current.eKey.wasPressedThisFrame;
            pressedEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        else
        {
            // Fallback to old Input API
            pressedE = UnityEngine.Input.GetKeyDown(KeyCode.E);
            pressedEsc = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }

        if (!isInTerminal && isLooking && pressedE)
        {
            EnterTerminal();
        }

        if (isInTerminal && pressedEsc)
        {
            // First try to close top window
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

        // Set cursor for UI
        GameCursorBridge.SetCursorForUI();

        // Open desktop (try ModeratorBridge first; fallback to desktopRoot)
        bool opened = ModeratorBootstrapBridge.OpenDesktop();
        if (!opened && desktopRoot != null)
        {
            desktopRoot.SetActive(true);
        }

        // Disable common look scripts if present
        DisableLookComponents(true);
    }

    void ExitTerminal()
    {
        if (!isInTerminal) return;
        isInTerminal = false;
        Debug.Log("WorldTerminal: ExitTerminal called");

        // restore cursor
        GameCursorBridge.SetCursorForWorld();

        // Hide desktop
        bool hid = ModeratorBootstrapBridge.HideDesktop();
        if (!hid && desktopRoot != null)
        {
            desktopRoot.SetActive(false);
        }

        // Re-enable look scripts
        DisableLookComponents(false);
    }

    void DisableLookComponents(bool disable)
    {
        if (playerCamera == null) return;
        // Look for components on the camera or on player's root that match common names
        foreach (var name in lookComponentNames)
        {
            // try camera
            var comp = playerCamera.GetComponent(name);
            if (comp != null && comp is Behaviour b1) b1.enabled = !disable;

            // try on parent objects
            var parent = playerCamera.transform.root;
            var comp2 = parent.GetComponent(name);
            if (comp2 != null && comp2 is Behaviour b2) b2.enabled = !disable;
        }
    }

    // Optional: public API to programmatically force enter/exit
    public void ForceEnter() => EnterTerminal();
    public void ForceExit() => ExitTerminal();
}
