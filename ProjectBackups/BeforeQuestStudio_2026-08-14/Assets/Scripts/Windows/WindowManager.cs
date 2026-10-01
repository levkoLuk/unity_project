using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moderator.Windows
{
    public sealed class WindowManager : MonoBehaviour
    {
        private readonly List<WindowController> windows = new();

        public void Register(WindowController window)
        {
            if (!windows.Contains(window)) windows.Add(window);
            BringToFront(window);
        }

        public void BringToFront(WindowController window)
        {
            if (window != null && window.gameObject.activeSelf) window.transform.SetAsLastSibling();
        }

        public void CloseTopmost()
        {
            WindowController top = null;
            var topIndex = -1;
            foreach (var window in windows)
            {
                if (window == null || !window.gameObject.activeSelf) continue;
                var index = window.transform.GetSiblingIndex();
                if (index > topIndex) { top = window; topIndex = index; }
            }
            if (top != null) top.Close();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseTopmost();
        }
    }
}
