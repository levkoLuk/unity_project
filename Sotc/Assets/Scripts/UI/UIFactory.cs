using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moderator.UI
{
    public static class UIFactory
    {
        public static readonly Color Ink = Hex("07121D");
        public static readonly Color Panel = Hex("172633");
        public static readonly Color Raised = Hex("34204D");
        public static readonly Color Blue = Hex("0876A8");
        public static readonly Color Paper = Hex("E7D8B5");
        public static readonly Color Parchment = Hex("D7C39B");
        public static readonly Color Charcoal = Hex("1B1715");
        public static readonly Color Muted = Hex("9A8B76");
        public static readonly Color Red = Hex("A83A35");
        public static readonly Color Green = Hex("708D32");

        private static Font font;
        private static readonly Dictionary<string, Sprite> spriteCache = new();
        private static readonly Dictionary<string, Sprite> slicedSpriteCache = new();
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Sprite LoadSprite(string resourcePath)
        {
            resourcePath = HandDrawnReplacement(resourcePath);
            if (spriteCache.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            sprite.name = resourcePath;
            spriteCache[resourcePath] = sprite;
            return sprite;
        }

        public static Sprite LoadSlicedSprite(string resourcePath, Vector4 border)
        {
            resourcePath = HandDrawnReplacement(resourcePath);
            var cacheKey = resourcePath + "|" + border.x + "," + border.y + "," + border.z + "," + border.w;
            if (slicedSpriteCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = resourcePath;
            slicedSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static string HandDrawnReplacement(string path)
        {
            return path switch
            {
                "Sprites/UI/ButtonPaper" => "Sprites/UI/ButtonPaperHand",
                "Sprites/UI/ButtonBlue" => "Sprites/UI/ButtonBlueHand",
                "Sprites/UI/ButtonPurple" => "Sprites/UI/ButtonPurpleHand",
                "Sprites/UI/ButtonGray" => "Sprites/UI/ButtonGrayHand",
                "Sprites/UI/WindowClose" => "Sprites/UI/WindowCloseHand",
                "Sprites/UI/WindowMaximize" => "Sprites/UI/WindowMaximizeHand",
                "Sprites/UI/WindowMinimize" => "Sprites/UI/WindowMinimizeHand",
                "Sprites/UI/Home" => "Sprites/UI/HomeHand",
                "Sprites/UI/Friends" => "Sprites/UI/FriendsHand",
                "Sprites/UI/Bell" => "Sprites/UI/BellHand",
                "Sprites/UI/Mail" => "Sprites/UI/MailHand",
                _ => path
            };
        }

        public static Image Artwork(string name, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, bool preserveAspect = true)
        {
            var rect = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, Color.white);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform TexturedRect(string name, Transform parent, string texturePath,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color tint)
        {
            var image = Artwork(name, parent, LoadSprite(texturePath), anchorMin, anchorMax, offsetMin, offsetMax, false);
            image.color = tint;
            image.type = Image.Type.Simple;
            return image.rectTransform;
        }

        public static RectTransform SlicedTexturedRect(string name, Transform parent, string texturePath, Vector4 border,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color tint)
        {
            var texture = Resources.Load<Texture2D>(texturePath);
            var rect = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, Color.white);
            var image = rect.GetComponent<Image>();
            if (texture != null)
            {
                image.sprite = LoadSlicedSprite(texturePath, border);
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 7.5f;
                image.fillCenter = true;
            }
            image.color = tint;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return rect;
        }

        public static Outline InkOutline(GameObject target, float weight = 2f)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = Hex("17100E");
            outline.effectDistance = new Vector2(weight, -weight);
            return outline;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            if (color.HasValue)
            {
                var image = go.AddComponent<Image>();
                image.color = color.Value;
            }
            return rect;
        }

        public static RectTransform FixedRect(string name, Transform parent, Vector2 size, Vector2 position, Color? color = null)
        {
            var rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, color);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Text Text(string name, Transform parent, string value, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(string name, Transform parent, string label, int fontSize, Color background,
            Color foreground, UnityAction onClick = null)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, background);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.82f, .9f, .96f, 1f);
            colors.highlightedColor = new Color(1f, .92f, .7f, 1f);
            colors.pressedColor = new Color(.72f, .58f, .4f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            if (background.a > .05f) InkOutline(rect.gameObject, 1.5f);
            Text("Label", rect, label, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        public static Button SpriteButton(string name, Transform parent, string resourcePath, UnityAction onClick = null,
            Color? background = null, float padding = 3f)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, background ?? Color.clear);
            var button = rect.gameObject.AddComponent<Button>();
            var icon = Artwork("Icon", rect, LoadSprite(resourcePath), Vector2.zero, Vector2.one,
                new Vector2(padding, padding), new Vector2(-padding, -padding), true);
            icon.raycastTarget = false;
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .94f, .74f, 1f);
            colors.pressedColor = new Color(.72f, .62f, .48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        public static Button PaintedButton(string name, Transform parent, string label, string resourcePath,
            int fontSize, Color foreground, UnityAction onClick = null)
        {
            return StretchedPaintedButton(name, parent, label, resourcePath, fontSize, foreground, onClick);
        }

        public static Button StretchedPaintedButton(string name, Transform parent, string label, string resourcePath,
            int fontSize, Color foreground, UnityAction onClick = null)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(resourcePath);
            image.preserveAspect = false;
            image.type = Image.Type.Simple;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .94f, .74f, 1f);
            colors.pressedColor = new Color(.72f, .62f, .48f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            var labelText = Text("Label", rect, label, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            Layout(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4));
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = Mathf.Min(7, fontSize);
            labelText.resizeTextMaxSize = fontSize;
            return button;
        }

        public static void Layout(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static Shadow Shadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            return shadow;
        }
    }
}
