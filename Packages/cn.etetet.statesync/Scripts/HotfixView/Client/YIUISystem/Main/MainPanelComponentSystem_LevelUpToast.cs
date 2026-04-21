using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        private const string LevelUpToastRootName = "LevelUpToastRoot";
        private const string LevelUpToastViewName = "LevelUpToastView";
        private const float LevelUpToastWidth = 360f;
        private const float LevelUpToastHeight = 108f;
        private const float LevelUpToastClampPadding = 28f;
        private const float LevelUpToastMoveY = 86f;
        private const float LevelUpToastStartOffsetY = -14f;
        private const float LevelUpToastStartScale = 0.82f;
        private const float LevelUpToastPeakScale = 1.08f;
        private const float LevelUpToastEndScale = 0.98f;

        public static void ShowLevelUpToast(this MainPanelComponent self, Vector3 worldPos, string content = "LEVEL UP")
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            RectTransform root = self.EnsureLevelUpToastRoot();
            RectTransform view = self.EnsureLevelUpToastView();
            TextMeshProUGUI text = self.LevelUpToastText;
            CanvasGroup canvasGroup = self.LevelUpToastCanvasGroup;
            if (root == null || view == null || text == null || canvasGroup == null)
            {
                return;
            }

            if (!self.TryResolveLevelUpToastPosition(worldPos, out Vector2 anchoredPosition))
            {
                return;
            }

            content = string.IsNullOrWhiteSpace(content) ? "LEVEL UP" : content;
            text.text = content;

            DOTween.Kill(view);
            DOTween.Kill(canvasGroup);

            view.gameObject.SetActive(true);
            root.SetAsLastSibling();

            Vector2 startPosition = anchoredPosition + new Vector2(0f, LevelUpToastStartOffsetY);
            view.anchoredPosition = startPosition;
            view.localScale = Vector3.one * LevelUpToastStartScale;
            canvasGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.SetUpdate(true);
            sequence.Append(view.DOScale(LevelUpToastPeakScale, 0.12f).SetEase(Ease.OutBack).SetTarget(view));
            sequence.Join(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.10f)
                .SetEase(Ease.OutQuad)
                .SetTarget(canvasGroup));
            sequence.Join(DOTween.To(() => view.anchoredPosition, x => view.anchoredPosition = x,
                    anchoredPosition + new Vector2(0f, LevelUpToastMoveY), 0.52f)
                .SetEase(Ease.OutCubic)
                .SetTarget(view));
            sequence.Append(view.DOScale(LevelUpToastEndScale, 0.16f).SetEase(Ease.OutQuad).SetTarget(view));
            sequence.Join(DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, 0.20f)
                .SetEase(Ease.InQuad)
                .SetTarget(canvasGroup));
            sequence.OnComplete(() =>
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                }
            });
            sequence.Play();
        }

        public static void HideLevelUpToast(this MainPanelComponent self, bool resetPosition = false)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            RectTransform view = self.LevelUpToastView;
            CanvasGroup canvasGroup = self.LevelUpToastCanvasGroup;
            if (view != null)
            {
                DOTween.Kill(view);
                if (resetPosition)
                {
                    view.anchoredPosition = Vector2.zero;
                    view.localScale = Vector3.one;
                }

                view.gameObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                DOTween.Kill(canvasGroup);
                canvasGroup.alpha = 0f;
            }
        }

        public static void ReleaseLevelUpToast(this MainPanelComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.HideLevelUpToast(true);

            if (self.LevelUpToastRoot != null)
            {
                UnityEngine.Object.Destroy(self.LevelUpToastRoot.gameObject);
            }

            self.LevelUpToastRoot = null;
            self.LevelUpToastView = null;
            self.LevelUpToastText = null;
            self.LevelUpToastCanvasGroup = null;
        }

        private static RectTransform EnsureLevelUpToastRoot(this MainPanelComponent self)
        {
            if (self.LevelUpToastRoot != null)
            {
                self.LevelUpToastRoot.SetAsLastSibling();
                return self.LevelUpToastRoot;
            }

            RectTransform ownerRoot = self.UIBase?.OwnerRectTransform;
            if (ownerRoot == null)
            {
                return null;
            }

            RectTransform existingRoot = ownerRoot.Find(LevelUpToastRootName) as RectTransform;
            if (existingRoot != null)
            {
                existingRoot.SetAsLastSibling();
                self.LevelUpToastRoot = existingRoot;
                self.LevelUpToastView = existingRoot.Find(LevelUpToastViewName) as RectTransform;
                if (self.LevelUpToastView != null)
                {
                    self.LevelUpToastText = self.LevelUpToastView.GetComponentInChildren<TextMeshProUGUI>(true);
                    self.LevelUpToastCanvasGroup = self.LevelUpToastView.GetComponent<CanvasGroup>();
                }

                return existingRoot;
            }

            GameObject rootObject = new(LevelUpToastRootName, typeof(RectTransform));
            rootObject.layer = ownerRoot.gameObject.layer;
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(ownerRoot, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.SetAsLastSibling();
            self.LevelUpToastRoot = rootRect;
            return rootRect;
        }

        private static RectTransform EnsureLevelUpToastView(this MainPanelComponent self)
        {
            RectTransform root = self.EnsureLevelUpToastRoot();
            if (root == null)
            {
                return null;
            }

            if (self.LevelUpToastView != null && self.LevelUpToastText != null && self.LevelUpToastCanvasGroup != null)
            {
                return self.LevelUpToastView;
            }

            RectTransform view = root.Find(LevelUpToastViewName) as RectTransform;
            if (view == null)
            {
                GameObject viewObject = new(LevelUpToastViewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
                viewObject.layer = root.gameObject.layer;
                view = viewObject.GetComponent<RectTransform>();
                view.SetParent(root, false);
                view.anchorMin = new Vector2(0.5f, 0.5f);
                view.anchorMax = new Vector2(0.5f, 0.5f);
                view.pivot = new Vector2(0.5f, 0.5f);
                view.sizeDelta = new Vector2(LevelUpToastWidth, LevelUpToastHeight);

                GameObject textObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Outline));
                textObject.layer = root.gameObject.layer;
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.SetParent(view, false);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.text = "LEVEL UP";
                text.fontSize = 52f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color32(255, 242, 163, 255);
                text.raycastTarget = false;
                text.enableAutoSizing = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;

                TMP_Text template = self.ResolveLevelUpToastTemplateText();
                if (template != null)
                {
                    text.font = template.font;
                    text.fontSharedMaterial = template.fontSharedMaterial;
                }

                Outline outline = textObject.GetComponent<Outline>();
                outline.effectColor = new Color32(44, 24, 8, 200);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            self.LevelUpToastView = view;
            self.LevelUpToastCanvasGroup = view.GetComponent<CanvasGroup>();
            self.LevelUpToastText = view.GetComponentInChildren<TextMeshProUGUI>(true);
            if (self.LevelUpToastCanvasGroup != null)
            {
                self.LevelUpToastCanvasGroup.blocksRaycasts = false;
                self.LevelUpToastCanvasGroup.interactable = false;
                self.LevelUpToastCanvasGroup.alpha = 0f;
            }

            if (view != null)
            {
                view.gameObject.SetActive(false);
            }

            return view;
        }

        private static TMP_Text ResolveLevelUpToastTemplateText(this MainPanelComponent self)
        {
            if (self.RogueLevelText != null)
            {
                return self.RogueLevelText;
            }

            if (self.SearchButtonText != null)
            {
                return self.SearchButtonText;
            }

            return null;
        }

        private static bool TryResolveLevelUpToastPosition(this MainPanelComponent self, Vector3 worldPos, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;
            RectTransform root = self.EnsureLevelUpToastRoot();
            Camera worldCamera = Camera.main;
            if (root == null || worldCamera == null)
            {
                return false;
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPos);
            if (screenPosition.z <= 0f)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    root,
                    new Vector2(screenPosition.x, screenPosition.y),
                    self.ResolveUICamera(),
                    out anchoredPosition))
            {
                return false;
            }

            anchoredPosition = self.ClampLevelUpToastPosition(anchoredPosition);
            return true;
        }

        private static Vector2 ClampLevelUpToastPosition(this MainPanelComponent self, Vector2 anchoredPosition)
        {
            RectTransform root = self.LevelUpToastRoot;
            if (root == null)
            {
                return anchoredPosition;
            }

            Rect rootRect = root.rect;
            float halfWidth = LevelUpToastWidth * 0.5f;
            float halfHeight = LevelUpToastHeight * 0.5f;
            float minX = rootRect.xMin + halfWidth + LevelUpToastClampPadding;
            float maxX = rootRect.xMax - halfWidth - LevelUpToastClampPadding;
            float minY = rootRect.yMin + halfHeight + LevelUpToastClampPadding;
            float maxY = rootRect.yMax - halfHeight - LevelUpToastClampPadding;

            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
            return anchoredPosition;
        }
    }
}
