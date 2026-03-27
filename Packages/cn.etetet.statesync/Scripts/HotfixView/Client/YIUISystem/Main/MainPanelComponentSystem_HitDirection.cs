using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        private const string HitDirectionRootName = "HitDirectionRoot";
        private const float HitDirectionDuration = 1.0f;
        private const float HitDirectionFadeDuration = 0.35f;
        private const float HitDirectionIconSize = 42f;
        private const float HitDirectionEdgePaddingX = 96f;
        private const float HitDirectionEdgePaddingY = 120f;
        private const int HitDirectionMaxCount = 4;

        public static void BindHitDirectionUI(this MainPanelComponent self)
        {
            self.EnsureHitDirectionRoot();
            self.ClearHitDirectionIndicators();
        }

        public static void NotifyHitDirection(this MainPanelComponent self, long attackerUnitId)
        {
            if (self == null || attackerUnitId == 0)
            {
                return;
            }

            self.EnsureHitDirectionRoot();
            if (self.HitDirectionRoot == null)
            {
                return;
            }

            if (!self.TryResolveHitDirection(attackerUnitId, out Vector2 direction))
            {
                if (!self.HitDirectionIndicatorDirections.ContainsKey(attackerUnitId))
                {
                    return;
                }

                direction = self.HitDirectionIndicatorDirections[attackerUnitId];
            }

            self.HitDirectionIndicatorDirections[attackerUnitId] = direction;
            self.HitDirectionIndicatorExpireTimes[attackerUnitId] = Time.unscaledTime + HitDirectionDuration;

            RectTransform indicatorRect = self.GetOrCreateHitDirectionIndicator(attackerUnitId);
            if (indicatorRect == null)
            {
                return;
            }

            indicatorRect.gameObject.SetActive(true);
            self.TrimHitDirectionIndicators();
        }

        public static void RefreshHitDirectionIndicators(this MainPanelComponent self)
        {
            if (self == null || self.HitDirectionRoot == null || self.HitDirectionIndicatorRects.Count == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            List<long> expiredIds = null;
            foreach ((long attackerUnitId, float expireTime) in self.HitDirectionIndicatorExpireTimes)
            {
                if (expireTime <= now)
                {
                    expiredIds ??= new List<long>();
                    expiredIds.Add(attackerUnitId);
                    continue;
                }

                if (self.TryResolveHitDirection(attackerUnitId, out Vector2 direction))
                {
                    self.HitDirectionIndicatorDirections[attackerUnitId] = direction;
                }

                if (!self.HitDirectionIndicatorDirections.TryGetValue(attackerUnitId, out direction))
                {
                    continue;
                }

                self.RefreshHitDirectionIndicatorVisual(attackerUnitId, direction, expireTime, now);
            }

            if (expiredIds == null)
            {
                return;
            }

            foreach (long attackerUnitId in expiredIds)
            {
                self.RemoveHitDirectionIndicator(attackerUnitId);
            }
        }

        private static void EnsureHitDirectionRoot(this MainPanelComponent self)
        {
            if (self.HitDirectionRoot != null)
            {
                return;
            }

            RectTransform ownerRect = self.UIBase?.OwnerRectTransform as RectTransform;
            if (ownerRect == null)
            {
                return;
            }

            Transform existing = ownerRect.Find(HitDirectionRootName);
            if (existing is RectTransform existingRect)
            {
                self.HitDirectionRoot = existingRect;
                self.HitDirectionRoot.SetAsLastSibling();
                return;
            }

            GameObject rootObject = new GameObject(HitDirectionRootName, typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(ownerRect, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.SetAsLastSibling();
            self.HitDirectionRoot = rootRect;
        }

        private static RectTransform GetOrCreateHitDirectionIndicator(this MainPanelComponent self, long attackerUnitId)
        {
            if (self.HitDirectionIndicatorRects.TryGetValue(attackerUnitId, out RectTransform indicatorRect) && indicatorRect != null)
            {
                return indicatorRect;
            }

            if (self.HitDirectionRoot == null)
            {
                return null;
            }

            GameObject indicatorObject = new GameObject($"HitDirection_{attackerUnitId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            indicatorRect = indicatorObject.GetComponent<RectTransform>();
            indicatorRect.SetParent(self.HitDirectionRoot, false);
            indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.sizeDelta = new Vector2(HitDirectionIconSize, HitDirectionIconSize);

            Image indicatorImage = indicatorObject.GetComponent<Image>();
            indicatorImage.raycastTarget = false;
            indicatorImage.sprite = self.GetHitDirectionSprite();
            indicatorImage.type = Image.Type.Simple;
            indicatorImage.preserveAspect = true;
            indicatorImage.color = new Color32(255, 96, 64, 255);

            CanvasGroup canvasGroup = indicatorObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 1f;

            self.HitDirectionIndicatorRects[attackerUnitId] = indicatorRect;
            self.HitDirectionIndicatorImages[attackerUnitId] = indicatorImage;
            self.HitDirectionIndicatorCanvasGroups[attackerUnitId] = canvasGroup;
            return indicatorRect;
        }

        private static void RefreshHitDirectionIndicatorVisual(this MainPanelComponent self, long attackerUnitId, Vector2 direction, float expireTime, float now)
        {
            if (!self.HitDirectionIndicatorRects.TryGetValue(attackerUnitId, out RectTransform indicatorRect) || indicatorRect == null)
            {
                return;
            }

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

            float halfWidth = Mathf.Max(0f, self.HitDirectionRoot.rect.width * 0.5f - HitDirectionEdgePaddingX);
            float halfHeight = Mathf.Max(0f, self.HitDirectionRoot.rect.height * 0.5f - HitDirectionEdgePaddingY);
            float scaleX = Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float scaleY = Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
            float edgeScale = Mathf.Min(scaleX, scaleY);
            Vector2 anchoredPosition = direction * edgeScale;

            indicatorRect.anchoredPosition = anchoredPosition;
            indicatorRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            indicatorRect.sizeDelta = new Vector2(HitDirectionIconSize, HitDirectionIconSize);
            if (!indicatorRect.gameObject.activeSelf)
            {
                indicatorRect.gameObject.SetActive(true);
            }

            if (self.HitDirectionIndicatorCanvasGroups.TryGetValue(attackerUnitId, out CanvasGroup canvasGroup) && canvasGroup != null)
            {
                float remainTime = expireTime - now;
                canvasGroup.alpha = remainTime > HitDirectionFadeDuration
                    ? 1f
                    : Mathf.Clamp01(remainTime / HitDirectionFadeDuration);
            }
        }

        private static void TrimHitDirectionIndicators(this MainPanelComponent self)
        {
            while (self.HitDirectionIndicatorExpireTimes.Count > HitDirectionMaxCount)
            {
                long oldestAttackerId = 0;
                float oldestExpireTime = float.MaxValue;
                foreach ((long attackerUnitId, float expireTime) in self.HitDirectionIndicatorExpireTimes)
                {
                    if (expireTime >= oldestExpireTime)
                    {
                        continue;
                    }

                    oldestExpireTime = expireTime;
                    oldestAttackerId = attackerUnitId;
                }

                if (oldestAttackerId == 0)
                {
                    return;
                }

                self.RemoveHitDirectionIndicator(oldestAttackerId);
            }
        }

        private static void RemoveHitDirectionIndicator(this MainPanelComponent self, long attackerUnitId)
        {
            if (self.HitDirectionIndicatorRects.Remove(attackerUnitId, out RectTransform indicatorRect) && indicatorRect != null)
            {
                UnityEngine.Object.Destroy(indicatorRect.gameObject);
            }

            self.HitDirectionIndicatorImages.Remove(attackerUnitId);
            self.HitDirectionIndicatorCanvasGroups.Remove(attackerUnitId);
            self.HitDirectionIndicatorExpireTimes.Remove(attackerUnitId);
            self.HitDirectionIndicatorDirections.Remove(attackerUnitId);
        }

        private static void ClearHitDirectionIndicators(this MainPanelComponent self)
        {
            foreach ((long _, RectTransform indicatorRect) in self.HitDirectionIndicatorRects)
            {
                if (indicatorRect != null)
                {
                    UnityEngine.Object.Destroy(indicatorRect.gameObject);
                }
            }

            self.HitDirectionIndicatorRects.Clear();
            self.HitDirectionIndicatorImages.Clear();
            self.HitDirectionIndicatorCanvasGroups.Clear();
            self.HitDirectionIndicatorExpireTimes.Clear();
            self.HitDirectionIndicatorDirections.Clear();
        }

        private static Sprite GetHitDirectionSprite(this MainPanelComponent self)
        {
            if (self.HitDirectionSprite != null)
            {
                return self.HitDirectionSprite;
            }

            const int textureSize = 64;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = Color.white;
            for (int y = 0; y < textureSize; ++y)
            {
                float normalizedY = y / (float)(textureSize - 1);
                float halfWidth = (1f - normalizedY) * 0.5f;
                for (int x = 0; x < textureSize; ++x)
                {
                    float normalizedX = x / (float)(textureSize - 1) - 0.5f;
                    texture.SetPixel(x, y, Mathf.Abs(normalizedX) <= halfWidth ? fill : clear);
                }
            }

            texture.Apply(false, false);
            self.HitDirectionSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return self.HitDirectionSprite;
        }

        private static bool TryResolveHitDirection(this MainPanelComponent self, long attackerUnitId, out Vector2 direction)
        {
            direction = Vector2.zero;

            Scene root = self.Root();
            Scene currentScene = root?.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            PlayerComponent playerComponent = root?.GetComponent<PlayerComponent>();
            if (unitComponent == null || playerComponent == null || playerComponent.MyId == 0)
            {
                return false;
            }

            Unit owner = unitComponent.Get(playerComponent.MyId);
            Unit attacker = unitComponent.Get(attackerUnitId);
            if (owner == null || attacker == null || owner.IsDisposed || attacker.IsDisposed)
            {
                return false;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector3 attackerPosition = self.GetHitDirectionWorldPosition(attacker);
            Vector3 screenPosition = camera.WorldToScreenPoint(attackerPosition);
            direction = new Vector2(screenPosition.x - Screen.width * 0.5f, screenPosition.y - Screen.height * 0.5f);
            if (screenPosition.z < 0f)
            {
                direction = -direction;
            }

            if (direction.sqrMagnitude > 0.01f)
            {
                direction.Normalize();
                return true;
            }

            Vector3 ownerPosition = self.GetHitDirectionWorldPosition(owner);
            Vector3 horizontalDelta = attackerPosition - ownerPosition;
            horizontalDelta.y = 0f;

            Vector3 cameraRight = camera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 cameraForward = camera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            direction = new Vector2(
                Vector3.Dot(horizontalDelta, cameraRight),
                Vector3.Dot(horizontalDelta, cameraForward));

            if (direction.sqrMagnitude <= 0.01f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        private static Vector3 GetHitDirectionWorldPosition(this MainPanelComponent self, Unit unit)
        {
            Transform transform = unit?.GetComponent<GameObjectComponent>()?.Transform;
            if (transform != null)
            {
                return transform.position;
            }

            return unit != null ? (Vector3)unit.Position : Vector3.zero;
        }
    }
}
