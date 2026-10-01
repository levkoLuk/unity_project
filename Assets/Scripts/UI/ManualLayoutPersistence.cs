using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.UI
{
    public sealed class ManualLayoutPersistence : MonoBehaviour
    {
        [Serializable]
        private sealed class LayoutFile { public List<LayoutEntry> entries = new(); }

        [Serializable]
        private sealed class LayoutEntry
        {
            public string path;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector3 localScale;
            public Vector3 localEulerAngles;

            public bool hasImage;
            public Color imageColor;
            public int imageType;
            public bool imagePreserveAspect;
            public bool imageFillCenter;
            public bool imageHadSprite;
            public string imageSpriteResourcePath;
            public Vector4 imageSpriteBorder;
            public float imagePixelsPerUnitMultiplier;

            public bool hasText;
            public int textFontSize;
            public Color textColor;
            public int textAlignment;
            public int textFontStyle;
            public float textLineSpacing;
            public bool textBestFit;
            public int textBestFitMin;
            public int textBestFitMax;

            public bool hasLayoutElement;
            public bool layoutIgnore;
            public float layoutMinWidth;
            public float layoutMinHeight;
            public float layoutPreferredWidth;
            public float layoutPreferredHeight;
            public float layoutFlexibleWidth;
            public float layoutFlexibleHeight;

            public bool hasHorizontalOrVerticalLayout;
            public bool horizontalOrVerticalLayoutEnabled;
            public RectOffsetData layoutPadding;
            public float layoutSpacing;
            public int layoutChildAlignment;
            public bool layoutControlWidth;
            public bool layoutControlHeight;
            public bool layoutExpandWidth;
            public bool layoutExpandHeight;

            public bool hasGridLayout;
            public bool gridLayoutEnabled;
            public Vector2 gridCellSize;
            public Vector2 gridSpacing;
            public int gridConstraint;
            public int gridConstraintCount;

            public bool hasOutline;
            public Color outlineColor;
            public Vector2 outlineDistance;
            public bool outlineUseGraphicAlpha;

            public bool hasContentSizeFitter;
            public bool contentSizeFitterEnabled;
            public int horizontalFit;
            public int verticalFit;
        }

        [Serializable]
        private struct RectOffsetData
        {
            public int left, right, top, bottom;
            public RectOffsetData(RectOffset source)
            {
                left = source.left; right = source.right; top = source.top; bottom = source.bottom;
            }
            public RectOffset ToRectOffset() => new(left, right, top, bottom);
        }

        private readonly Dictionary<string, LayoutEntry> saved = new();
        private readonly HashSet<int> appliedInstances = new();
        private int knownTransformCount;
        private int applyFramesRemaining;
        private float nextHierarchyScanTime;

        public static string LayoutPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/ModeratorManualLayoutStableV3.json"));

        private IEnumerator Start()
        {
            LoadFile();
            yield return new WaitForEndOfFrame();
            ScheduleApply();
        }

        private void LateUpdate()
        {
            // A full hierarchy walk every frame made the dynamic UI noticeably stutter.
            // Explicit rebuild notifications handle page swaps; this slow fallback only
            // catches miscellaneous objects created outside those flows.
            if (Time.unscaledTime >= nextHierarchyScanTime)
            {
                nextHierarchyScanTime = Time.unscaledTime + .5f;
                var currentCount = GetComponentsInChildren<RectTransform>(true).Length;
                if (currentCount != knownTransformCount)
                {
                    knownTransformCount = currentCount;
                    ScheduleApply();
                }
            }
            if (applyFramesRemaining <= 0) return;
            Canvas.ForceUpdateCanvases();
            ApplySavedLayoutInternal(false);
            applyFramesRemaining = 0;
        }

        public void SaveCurrentLayout()
        {
            // Pages are rebuilt dynamically. Merge the currently visible hierarchy into the
            // existing file so editing one page never deletes saved edits for another page.
            CaptureCurrentIntoSaved();
            var file = new LayoutFile();
            file.entries.AddRange(saved.Values.OrderBy(entry => entry.path));
            File.WriteAllText(LayoutPath, JsonUtility.ToJson(file, true));
            LoadFile();
            Debug.Log("[Manual UI] Layout saved: " + LayoutPath);
        }

        public void ApplySavedLayout() => ApplySavedLayoutInternal(true);

        private void ApplySavedLayoutInternal(bool forceExisting)
        {
            if (saved.Count == 0) return;
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                var identity = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(rect);
                if (!forceExisting && appliedInstances.Contains(identity)) continue;
                var path = BuildPath(rect);
                var legacyPath = BuildLegacyPath(rect);
                if (!saved.TryGetValue(path, out var entry) && !saved.TryGetValue(legacyPath, out entry)) continue;

                // Browser tabs are rebuilt whenever navigation changes. Their width, position,
                // label and active/inactive artwork belong to BrowserApp's live state rather
                // than to the manually persisted static window layout.
                if (path.Contains("/TabStrip[0]/BrowserTab_") || RuntimeOwnsLayout(rect))
                {
                    appliedInstances.Add(identity);
                    continue;
                }
                rect.anchorMin = entry.anchorMin;
                rect.anchorMax = entry.anchorMax;
                rect.pivot = entry.pivot;
                rect.anchoredPosition = entry.anchoredPosition;
                rect.sizeDelta = entry.sizeDelta;
                rect.localScale = entry.localScale;
                rect.localEulerAngles = entry.localEulerAngles;
                ApplyVisualState(rect, entry);
                appliedInstances.Add(identity);
            }
        }

        private static bool RuntimeOwnsLayout(RectTransform rect)
        {
            if (rect.name == "ScrollableContent" && rect.parent != null && rect.parent.name == "PageViewport")
                return true;
            return rect.GetComponentInParent<Moderator.Browser.WrappedInlineText>() != null;
        }

        public static void ClearSavedLayout()
        {
            if (File.Exists(LayoutPath)) File.Delete(LayoutPath);
            Debug.Log("[Manual UI] Saved layout cleared.");
        }

        public static void RequestReapply(Component rebuiltArea)
        {
            var persistence = rebuiltArea != null ? rebuiltArea.GetComponentInParent<ManualLayoutPersistence>() : null;
            if (persistence != null) persistence.ScheduleApply();
        }

        public static void CaptureBeforeRebuild(Component rebuiltArea)
        {
            var persistence = rebuiltArea != null ? rebuiltArea.GetComponentInParent<ManualLayoutPersistence>() : null;
            if (persistence != null) persistence.CaptureCurrentIntoSaved();
        }

        private void LoadFile()
        {
            saved.Clear();
            if (!File.Exists(LayoutPath)) return;
            var file = JsonUtility.FromJson<LayoutFile>(File.ReadAllText(LayoutPath));
            if (file?.entries == null) return;
            foreach (var entry in file.entries) saved[entry.path] = entry;
        }

        private void ScheduleApply() => applyFramesRemaining = 1;

        private void CaptureCurrentIntoSaved()
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == transform || rect.name.StartsWith("Disposed_")) continue;
                var entry = Capture(rect);
                saved[entry.path] = entry;
            }
        }

        private LayoutEntry Capture(RectTransform rect)
        {
            var entry = new LayoutEntry
            {
                path = BuildPath(rect), anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition, sizeDelta = rect.sizeDelta, localScale = rect.localScale,
                localEulerAngles = rect.localEulerAngles
            };

            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                entry.hasImage = true; entry.imageColor = image.color; entry.imageType = (int)image.type;
                entry.imagePreserveAspect = image.preserveAspect; entry.imageFillCenter = image.fillCenter;
                entry.imageHadSprite = image.sprite != null; entry.imageSpriteResourcePath = SpriteResourcePath(image.sprite);
                entry.imageSpriteBorder = image.sprite != null ? image.sprite.border : Vector4.zero;
                entry.imagePixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
            }
            var text = rect.GetComponent<Text>();
            if (text != null)
            {
                entry.hasText = true; entry.textFontSize = text.fontSize; entry.textColor = text.color;
                entry.textAlignment = (int)text.alignment; entry.textFontStyle = (int)text.fontStyle;
                entry.textLineSpacing = text.lineSpacing; entry.textBestFit = text.resizeTextForBestFit;
                entry.textBestFitMin = text.resizeTextMinSize; entry.textBestFitMax = text.resizeTextMaxSize;
            }
            var element = rect.GetComponent<LayoutElement>();
            if (element != null)
            {
                entry.hasLayoutElement = true; entry.layoutIgnore = element.ignoreLayout;
                entry.layoutMinWidth = element.minWidth; entry.layoutMinHeight = element.minHeight;
                entry.layoutPreferredWidth = element.preferredWidth; entry.layoutPreferredHeight = element.preferredHeight;
                entry.layoutFlexibleWidth = element.flexibleWidth; entry.layoutFlexibleHeight = element.flexibleHeight;
            }
            var group = rect.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group != null)
            {
                entry.hasHorizontalOrVerticalLayout = true; entry.layoutPadding = new RectOffsetData(group.padding);
                entry.horizontalOrVerticalLayoutEnabled = group.enabled;
                entry.layoutSpacing = group.spacing; entry.layoutChildAlignment = (int)group.childAlignment;
                entry.layoutControlWidth = group.childControlWidth; entry.layoutControlHeight = group.childControlHeight;
                entry.layoutExpandWidth = group.childForceExpandWidth; entry.layoutExpandHeight = group.childForceExpandHeight;
            }
            var grid = rect.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                entry.hasGridLayout = true; entry.layoutPadding = new RectOffsetData(grid.padding);
                entry.gridLayoutEnabled = grid.enabled;
                entry.layoutChildAlignment = (int)grid.childAlignment; entry.gridCellSize = grid.cellSize;
                entry.gridSpacing = grid.spacing; entry.gridConstraint = (int)grid.constraint;
                entry.gridConstraintCount = grid.constraintCount;
            }
            var outline = rect.GetComponent<Outline>();
            if (outline != null)
            {
                entry.hasOutline = true; entry.outlineColor = outline.effectColor;
                entry.outlineDistance = outline.effectDistance; entry.outlineUseGraphicAlpha = outline.useGraphicAlpha;
            }
            var fitter = rect.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                entry.hasContentSizeFitter = true; entry.contentSizeFitterEnabled = fitter.enabled;
                entry.horizontalFit = (int)fitter.horizontalFit; entry.verticalFit = (int)fitter.verticalFit;
            }
            return entry;
        }

        private static void ApplyVisualState(RectTransform rect, LayoutEntry entry)
        {
            var image = rect.GetComponent<Image>();
            if (entry.hasImage && image != null)
            {
                // A post's red/white background represents its live moderation state and must
                // never be restored from a previous manual-layout capture.
                var runtimePostState = rect.name == "ProfilePost" || rect.name == "PostCard";
                if (!runtimePostState) image.color = entry.imageColor;
                image.type = (Image.Type)entry.imageType;
                image.preserveAspect = entry.imagePreserveAspect; image.fillCenter = entry.imageFillCenter;
                // Portrait geometry is editable, but its sprite is live user data. Restoring a
                // saved sprite here would make every reused profile/post show the same person.
                var runtimePortrait = rect.name == "Portrait";
                if (!runtimePortrait && !entry.imageHadSprite) image.sprite = null;
                else if (!runtimePortrait && !string.IsNullOrEmpty(entry.imageSpriteResourcePath))
                {
                    if ((Image.Type)entry.imageType == Image.Type.Sliced)
                    {
                        // V3 saves did not contain sprite borders. Keep the correctly constructed
                        // runtime sprite instead of replacing it with a borderless one.
                        if (entry.imageSpriteBorder.sqrMagnitude > .01f)
                            image.sprite = UIFactory.LoadSlicedSprite(entry.imageSpriteResourcePath, entry.imageSpriteBorder);
                    }
                    else image.sprite = UIFactory.LoadSprite(entry.imageSpriteResourcePath);
                }
                if (entry.imagePixelsPerUnitMultiplier > .01f)
                    image.pixelsPerUnitMultiplier = entry.imagePixelsPerUnitMultiplier;
            }
            var text = rect.GetComponent<Text>();
            if (entry.hasText && text != null)
            {
                text.fontSize = entry.textFontSize; text.color = entry.textColor;
                text.alignment = (TextAnchor)entry.textAlignment; text.fontStyle = (FontStyle)entry.textFontStyle;
                text.lineSpacing = entry.textLineSpacing; text.resizeTextForBestFit = entry.textBestFit;
                text.resizeTextMinSize = entry.textBestFitMin; text.resizeTextMaxSize = entry.textBestFitMax;
            }
            var element = rect.GetComponent<LayoutElement>();
            if (entry.hasLayoutElement && element != null)
            {
                element.ignoreLayout = entry.layoutIgnore; element.minWidth = entry.layoutMinWidth;
                element.minHeight = entry.layoutMinHeight; element.preferredWidth = entry.layoutPreferredWidth;
                element.preferredHeight = entry.layoutPreferredHeight; element.flexibleWidth = entry.layoutFlexibleWidth;
                element.flexibleHeight = entry.layoutFlexibleHeight;
            }
            var group = rect.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (entry.hasHorizontalOrVerticalLayout && group != null)
            {
                group.padding = entry.layoutPadding.ToRectOffset(); group.spacing = entry.layoutSpacing;
                group.enabled = entry.horizontalOrVerticalLayoutEnabled;
                group.childAlignment = (TextAnchor)entry.layoutChildAlignment;
                group.childControlWidth = entry.layoutControlWidth; group.childControlHeight = entry.layoutControlHeight;
                group.childForceExpandWidth = entry.layoutExpandWidth; group.childForceExpandHeight = entry.layoutExpandHeight;
            }
            var grid = rect.GetComponent<GridLayoutGroup>();
            if (entry.hasGridLayout && grid != null)
            {
                grid.padding = entry.layoutPadding.ToRectOffset(); grid.childAlignment = (TextAnchor)entry.layoutChildAlignment;
                grid.enabled = entry.gridLayoutEnabled;
                grid.cellSize = entry.gridCellSize; grid.spacing = entry.gridSpacing;
                grid.constraint = (GridLayoutGroup.Constraint)entry.gridConstraint; grid.constraintCount = entry.gridConstraintCount;
            }
            var outline = rect.GetComponent<Outline>();
            if (entry.hasOutline && outline != null)
            {
                outline.effectColor = entry.outlineColor; outline.effectDistance = entry.outlineDistance;
                outline.useGraphicAlpha = entry.outlineUseGraphicAlpha;
            }
            var fitter = rect.GetComponent<ContentSizeFitter>();
            if (entry.hasContentSizeFitter && fitter != null)
            {
                fitter.enabled = entry.contentSizeFitterEnabled;
                fitter.horizontalFit = (ContentSizeFitter.FitMode)entry.horizontalFit;
                fitter.verticalFit = (ContentSizeFitter.FitMode)entry.verticalFit;
            }
        }

        private static string SpriteResourcePath(Sprite sprite)
        {
            if (sprite == null) return string.Empty;
            if (sprite.name.Contains("/")) return sprite.name;
#if UNITY_EDITOR
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(assetPath) && sprite.texture != null)
                assetPath = UnityEditor.AssetDatabase.GetAssetPath(sprite.texture);
            const string marker = "/Resources/";
            var markerIndex = assetPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var resourcePath = assetPath.Substring(markerIndex + marker.Length).Replace('\\', '/');
                var extensionIndex = resourcePath.LastIndexOf('.');
                return extensionIndex > 0 ? resourcePath.Substring(0, extensionIndex) : resourcePath;
            }
#endif
            return string.Empty;
        }

        private string BuildPath(Transform target)
        {
            var parts = new List<string>();
            while (target != null && target != transform)
            {
                var occurrence = 0;
                for (var index = 0; index < target.GetSiblingIndex(); index++)
                    if (target.parent.GetChild(index).name == target.name) occurrence++;
                parts.Add(target.name + "[" + occurrence + "]");
                target = target.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private string BuildLegacyPath(Transform target)
        {
            var parts = new List<string>();
            while (target != null && target != transform)
            {
                parts.Add(target.name + "[" + target.GetSiblingIndex() + "]");
                target = target.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
