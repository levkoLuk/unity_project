using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.UI
{
    public sealed class AmbientBackgroundAnimator : MonoBehaviour
    {
        private sealed class Pulse
        {
            public Image image;
            public float speed;
            public float phase;
            public float minimum;
            public float maximum;
            public bool hardBlink;
        }

        private readonly List<Pulse> pulses = new();

        public void Build(RectTransform root)
        {
            // Only the remote tower is animated. Desk equipment stays dark and unobtrusive.
            AddGlow(root, "TowerWindow01", new Vector2(.921f, .735f), new Vector2(3, 5), UIFactory.Hex("DFA64A"), .52f, .1f, .02f, .74f, true);
            AddGlow(root, "TowerWindow02", new Vector2(.930f, .711f), new Vector2(3, 5), UIFactory.Hex("C98B38"), .37f, 1.7f, .02f, .55f, true);
            AddGlow(root, "TowerWindow03", new Vector2(.940f, .686f), new Vector2(3, 5), UIFactory.Hex("E8B859"), .63f, 2.4f, .01f, .7f, true);
        }

        private void AddGlow(RectTransform root, string name, Vector2 anchor, Vector2 size, Color color,
            float speed, float phase, float minimum, float maximum, bool hardBlink)
        {
            var rect = UIFactory.Rect(name, root, anchor, anchor, Vector2.zero, Vector2.zero, color);
            rect.sizeDelta = size;
            var image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            pulses.Add(new Pulse
            {
                image = image,
                speed = speed,
                phase = phase,
                minimum = minimum,
                maximum = maximum,
                hardBlink = hardBlink
            });
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            foreach (var pulse in pulses)
            {
                if (pulse.image == null) continue;
                var wave = (Mathf.Sin(time * pulse.speed + pulse.phase) + 1f) * .5f;
                if (pulse.hardBlink) wave = wave > .72f ? 1f : wave * .18f;
                var color = pulse.image.color;
                color.a = Mathf.Lerp(pulse.minimum, pulse.maximum, wave);
                pulse.image.color = color;
            }
        }
    }
}
