using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.11
    /// Desc
    /// </summary>
    [FriendOf(typeof(MapWorldPanelComponent))]
    public static partial class MapWorldPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this MapWorldPanelComponent self)
        {
            self.BindWorldMap();
            self.SetRuntimeDisplayMode(MinimapDisplayMode.Expanded);
            self.RefreshWorldMap(true);
        }

        [EntitySystem]
        private static void Destroy(this MapWorldPanelComponent self)
        {
            self.SetRuntimeDisplayMode(MinimapDisplayMode.Compact);
            self.ReleaseAllWorldMarkerSprites();
            self.ClearWorldMapMarkers();
            if (self.MarkerSprite != null)
            {
                UnityEngine.Object.Destroy(self.MarkerSprite);
            }

            if (self.FogTexture != null)
            {
                UnityEngine.Object.Destroy(self.FogTexture);
            }

            self.MapRoot = null;
            self.MapFrame = null;
            self.MapMask = null;
            self.MarkerLayer = null;
            self.PingLayer = null;
            self.MapTexture = null;
            self.FogOverlay = null;
            self.PlayerArrow = null;
            self.TitleText = null;
            self.MapNameText = null;
            self.FogTexture = null;
            self.MarkerSprite = null;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MapWorldPanelComponent self)
        {
            self.BindWorldMap();
            self.SetRuntimeDisplayMode(MinimapDisplayMode.Expanded);
            self.RefreshWorldMap(true);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void LateUpdate(this MapWorldPanelComponent self)
        {
            self.RefreshWorldMap();
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(MapWorldPanelComponent.OnEventClickCloseBgInvoke)]
        private static async ETTask OnEventClickCloseBgInvoke(this MapWorldPanelComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                self.SetRuntimeDisplayMode(MinimapDisplayMode.Compact);
                await root.YIUIMgr().ClosePanelAsync<MapWorldPanelComponent>();
                return;
            }

            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束

        private static void BindWorldMap(this MapWorldPanelComponent self)
        {
            if (self.MapRoot != null &&
                self.MapMask != null &&
                self.MapTexture != null &&
                self.FogOverlay != null &&
                self.PlayerArrow != null &&
                self.MarkerLayer != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            self.MapRoot = rootTransform.Find("MapRoot") as RectTransform;
            self.MapFrame = rootTransform.Find("MapRoot/MapFrame") as RectTransform;
            self.MapMask = rootTransform.Find("MapRoot/MapFrame/MapMask") as RectTransform;
            self.MapTexture = rootTransform.Find("MapRoot/MapFrame/MapMask/MapTexture")?.GetComponent<RawImage>();
            self.FogOverlay = rootTransform.Find("MapRoot/MapFrame/MapMask/FogOverlay")?.GetComponent<RawImage>();
            self.MarkerLayer = rootTransform.Find("MapRoot/MapFrame/MapMask/MarkerLayer") as RectTransform;
            self.PingLayer = rootTransform.Find("MapRoot/MapFrame/MapMask/PingLayer") as RectTransform;
            self.PlayerArrow = rootTransform.Find("MapRoot/MapFrame/PlayerArrow")?.GetComponent<Image>();
            self.TitleText = rootTransform.Find("TopBar/TitleText")?.GetComponent<TMP_Text>();
            self.MapNameText = rootTransform.Find("MapRoot/MapNameText")?.GetComponent<TMP_Text>();

            if (self.MapMask == null)
            {
                return;
            }

            MinimapDisplayHelper.StretchToFillParent(self.MapTexture?.rectTransform);
            MinimapDisplayHelper.StretchToFillParent(self.FogOverlay?.rectTransform);
            MinimapDisplayHelper.StretchToFillParent(self.MarkerLayer);
            MinimapDisplayHelper.StretchToFillParent(self.PingLayer);

            if (self.FogOverlay == null)
            {
                Transform fogTransform = self.MapMask.Find("FogOverlay");
                GameObject fogObject = fogTransform != null
                    ? fogTransform.gameObject
                    : new GameObject("FogOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                RectTransform fogRect = fogObject.GetComponent<RectTransform>();
                fogRect.SetParent(self.MapMask, false);
                fogRect.anchorMin = Vector2.zero;
                fogRect.anchorMax = Vector2.one;
                fogRect.offsetMin = Vector2.zero;
                fogRect.offsetMax = Vector2.zero;
                if (self.MapTexture != null)
                {
                    fogRect.SetSiblingIndex(self.MapTexture.rectTransform.GetSiblingIndex() + 1);
                }

                RawImage fogImage = fogObject.GetComponent<RawImage>() ?? fogObject.AddComponent<RawImage>();
                fogImage.raycastTarget = false;
                self.FogOverlay = fogImage;
            }

            if (self.MarkerLayer == null)
            {
                GameObject markerObject = new GameObject("MarkerLayer", typeof(RectTransform));
                RectTransform markerRect = markerObject.GetComponent<RectTransform>();
                markerRect.SetParent(self.MapMask, false);
                markerRect.anchorMin = Vector2.zero;
                markerRect.anchorMax = Vector2.one;
                markerRect.offsetMin = Vector2.zero;
                markerRect.offsetMax = Vector2.zero;
                markerRect.SetSiblingIndex(self.MapMask.childCount - 1);
                self.MarkerLayer = markerRect;
            }

            MinimapDisplayHelper.StretchToFillParent(self.MapTexture?.rectTransform);
            MinimapDisplayHelper.StretchToFillParent(self.FogOverlay?.rectTransform);
            MinimapDisplayHelper.StretchToFillParent(self.MarkerLayer);
            MinimapDisplayHelper.StretchToFillParent(self.PingLayer);

            if (self.PlayerArrow != null && self.PlayerArrow.sprite == null)
            {
                MainPanelComponent mainPanel = self.Root()?.YIUIMgr()?.GetPanel<MainPanelComponent>();
                if (mainPanel?.MinimapArrow != null)
                {
                    self.PlayerArrow.sprite = mainPanel.MinimapArrow.sprite;
                }
            }
        }

        private static void RefreshWorldMap(this MapWorldPanelComponent self, bool force = false)
        {
            self.BindWorldMap();
            if (self.MapMask == null || self.MapTexture == null || self.MarkerLayer == null)
            {
                return;
            }

            MinimapRuntimeComponent runtime = self.Root()?.CurrentScene()?.GetComponent<MinimapRuntimeComponent>();
            if (runtime == null)
            {
                self.ClearWorldMapMarkers();
                self.HideWorldFog();
                return;
            }

            self.SetRuntimeDisplayMode(MinimapDisplayMode.Expanded);

            if (self.TitleText != null && (force || self.TitleText.text != "Map"))
            {
                self.TitleText.text = "Map";
            }

            if (self.MapNameText != null && (force || self.MapNameText.text != runtime.MapName))
            {
                self.MapNameText.text = runtime.MapName;
            }

            if (!runtime.TryGetMyPosition(out float3 myPosition))
            {
                self.ClearWorldMapMarkers();
                self.HideWorldFog();
                return;
            }

            self.RefreshWorldMapTexture(runtime);
            self.RefreshWorldMapFog(runtime);
            self.RefreshWorldMapArrow(runtime, myPosition);
            self.RefreshWorldMapMarkers(runtime);
        }

        private static void RefreshWorldMapTexture(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime)
        {
            if (self.MapTexture == null)
            {
                return;
            }

            MainPanelComponent mainPanel = self.Root()?.YIUIMgr()?.GetPanel<MainPanelComponent>();
            if (mainPanel?.MinimapTexture?.texture != null)
            {
                self.MapTexture.texture = mainPanel.MinimapTexture.texture;
            }

            self.MapTexture.uvRect = MinimapDisplayHelper.GetExpandedBaseUvRect(runtime, self.MapTexture.texture);
            if (self.FogOverlay != null)
            {
                self.FogOverlay.uvRect = MinimapDisplayHelper.GetExpandedFogUvRect();
            }
        }

        private static void RefreshWorldMapArrow(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime, float3 myPosition)
        {
            if (self.PlayerArrow == null || self.MapMask == null)
            {
                return;
            }

            string colorText = global::ET.MinimapConstConfigHelper.GetString(global::ET.MinimapConstKey.MarkerColorSelf, "#FFFFFF");
            if (ColorUtility.TryParseHtmlString(colorText, out Color arrowColor))
            {
                self.PlayerArrow.color = arrowColor;
            }

            Vector2 anchoredPosition = self.WorldToExpandedAnchoredPosition(runtime, myPosition);
            self.PlayerArrow.rectTransform.anchoredPosition = anchoredPosition;

            Unit myUnit = runtime.GetMyUnit();
            if (myUnit == null || myUnit.IsDisposed)
            {
                return;
            }

            Vector3 forward = myUnit.Forward;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            self.PlayerArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        private static void RefreshWorldMapFog(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime)
        {
            if (self.FogOverlay == null)
            {
                return;
            }

            if (!runtime.TryGetFogGridSize(out int gridWidth, out int gridHeight))
            {
                self.HideWorldFog();
                return;
            }

            self.EnsureWorldFogTexture(gridWidth, gridHeight);
            if (self.FogTexture == null)
            {
                self.HideWorldFog();
                return;
            }

            Color32 visibleColor = self.ResolveWorldFogColor(global::ET.MinimapConstKey.FogColorVisible, "#00000000");
            Color32 exploredColor = self.ResolveWorldFogColor(global::ET.MinimapConstKey.FogColorExplored, "#09131E88");
            Color32 unexploredColor = self.ResolveWorldFogColor(global::ET.MinimapConstKey.FogColorUnexplored, "#09131EE8");
            Color32[] colors = new Color32[gridWidth * gridHeight];
            for (int i = 0; i < colors.Length; ++i)
            {
                if (runtime.CurrentVisibleCells.Contains(i))
                {
                    colors[i] = visibleColor;
                    continue;
                }

                colors[i] = runtime.ExploredCells.Contains(i) ? exploredColor : unexploredColor;
            }

            self.FogTexture.SetPixels32(colors);
            self.FogTexture.Apply(false, false);
            self.FogOverlay.texture = self.FogTexture;
            self.FogOverlay.color = Color.white;
            self.FogOverlay.gameObject.SetActive(true);
        }

        private static void HideWorldFog(this MapWorldPanelComponent self)
        {
            if (self.FogOverlay == null)
            {
                return;
            }

            self.FogOverlay.texture = null;
            self.FogOverlay.gameObject.SetActive(false);
        }

        private static void EnsureWorldFogTexture(this MapWorldPanelComponent self, int width, int height)
        {
            if (self.FogTexture != null && self.FogTexture.width == width && self.FogTexture.height == height)
            {
                return;
            }

            if (self.FogTexture != null)
            {
                UnityEngine.Object.Destroy(self.FogTexture);
            }

            self.FogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        private static void RefreshWorldMapMarkers(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime)
        {
            if (self.MapMask == null)
            {
                return;
            }

            HashSet<long> activeMarkerIds = new HashSet<long>();
            float markerSize = Mathf.Max(global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.MarkerSize, 10f), 4f);

            foreach (KeyValuePair<long, MinimapMarkerRuntime> pair in runtime.GetMarkers())
            {
                MinimapMarkerRuntime marker = pair.Value;
                if (marker.UnitId == runtime.MyUnitId || !MinimapRuntimeMarkerHelper.ShouldDisplayMarker(runtime, marker))
                {
                    continue;
                }

                RectTransform markerRect = self.GetOrCreateWorldMarker(marker.UnitId);
                if (markerRect == null)
                {
                    continue;
                }

                activeMarkerIds.Add(marker.UnitId);
                markerRect.gameObject.SetActive(true);
                markerRect.sizeDelta = new Vector2(markerSize, markerSize);
                markerRect.anchoredPosition = self.WorldToExpandedAnchoredPosition(runtime, marker.Position);

                if (self.MarkerImages.TryGetValue(marker.UnitId, out Image markerImage) && markerImage != null)
                {
                    self.RefreshWorldMarkerVisual(runtime, marker, markerImage);
                }
            }

            foreach (KeyValuePair<long, RectTransform> pair in self.MarkerRects)
            {
                if (!activeMarkerIds.Contains(pair.Key) && pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    self.MarkerDesiredSpriteNames.Remove(pair.Key);
                }
            }
        }

        private static Vector2 WorldToExpandedAnchoredPosition(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime, float3 worldPosition)
        {
            float2 normalized = runtime.WorldToNormalizedPosition(worldPosition);
            float width = self.MapMask.rect.width;
            float height = self.MapMask.rect.height;
            return new Vector2((normalized.x - 0.5f) * width, (normalized.y - 0.5f) * height);
        }

        private static RectTransform GetOrCreateWorldMarker(this MapWorldPanelComponent self, long unitId)
        {
            if (self.MarkerRects.TryGetValue(unitId, out RectTransform markerRect) && markerRect != null)
            {
                return markerRect;
            }

            if (self.MarkerLayer == null)
            {
                return null;
            }

            GameObject markerObject = new GameObject($"WorldMarker_{unitId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.SetParent(self.MarkerLayer, false);
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);

            Image markerImage = markerObject.GetComponent<Image>();
            markerImage.raycastTarget = false;
            markerImage.sprite = self.GetWorldMarkerSprite();
            markerImage.type = Image.Type.Simple;
            markerImage.preserveAspect = false;

            self.MarkerRects[unitId] = markerRect;
            self.MarkerImages[unitId] = markerImage;
            return markerRect;
        }

        private static void ClearWorldMapMarkers(this MapWorldPanelComponent self)
        {
            foreach (KeyValuePair<long, RectTransform> pair in self.MarkerRects)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }
            }

            self.MarkerRects.Clear();
            self.MarkerImages.Clear();
            self.MarkerDesiredSpriteNames.Clear();
        }

        private static Sprite GetWorldMarkerSprite(this MapWorldPanelComponent self)
        {
            if (self.MarkerSprite != null)
            {
                return self.MarkerSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            self.MarkerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            return self.MarkerSprite;
        }

        private static Color ResolveWorldMarkerColor(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker)
        {
            string key = global::ET.MinimapConstKey.MarkerColorOther;
            switch (marker.UnitType)
            {
                case UnitType.Player:
                    key = self.ResolveWorldPlayerMarkerColorKey(runtime, marker);
                    break;
                case UnitType.Monster:
                    key = global::ET.MinimapConstKey.MarkerColorMonster;
                    break;
            }

            string colorText = global::ET.MinimapConstConfigHelper.GetString(key, "#FFFFFF");
            if (ColorUtility.TryParseHtmlString(colorText, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        private static void RefreshWorldMarkerVisual(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker, Image markerImage)
        {
            if (markerImage == null)
            {
                return;
            }

            string iconName = MinimapMarkerIconHelper.ResolveIconName(marker);
            self.MarkerDesiredSpriteNames[marker.UnitId] = iconName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(iconName))
            {
                if (self.MarkerLoadedSprites.TryGetValue(iconName, out Sprite customSprite) && customSprite != null)
                {
                    markerImage.sprite = customSprite;
                    markerImage.color = Color.white;
                    markerImage.preserveAspect = true;
                    return;
                }

                markerImage.sprite = self.GetWorldMarkerSprite();
                markerImage.color = self.ResolveWorldMarkerColor(runtime, marker);
                markerImage.preserveAspect = false;
                self.RequestWorldMarkerSprite(iconName);
                return;
            }

            markerImage.sprite = self.GetWorldMarkerSprite();
            markerImage.color = self.ResolveWorldMarkerColor(runtime, marker);
            markerImage.preserveAspect = false;
        }

        private static void RequestWorldMarkerSprite(this MapWorldPanelComponent self, string spriteName)
        {
            if (self == null || self.IsDisposed || string.IsNullOrWhiteSpace(spriteName))
            {
                return;
            }

            if (self.MarkerLoadedSprites.ContainsKey(spriteName) || !self.MarkerLoadingSpriteNames.Add(spriteName))
            {
                return;
            }

            self.LoadWorldMarkerSpriteAsync(spriteName).Coroutine();
        }

        private static async ETTask LoadWorldMarkerSpriteAsync(this MapWorldPanelComponent self, string spriteName)
        {
            EntityRef<MapWorldPanelComponent> selfRef = self;
            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = spriteName });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            self.MarkerLoadingSpriteNames.Remove(spriteName);
            if (sprite == null)
            {
                return;
            }

            if (self.MarkerLoadedSprites.TryGetValue(spriteName, out Sprite cachedSprite) && cachedSprite != null)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                return;
            }

            self.MarkerLoadedSprites[spriteName] = sprite;
            foreach (KeyValuePair<long, Image> pair in self.MarkerImages)
            {
                if (!self.MarkerDesiredSpriteNames.TryGetValue(pair.Key, out string desiredSpriteName) ||
                    desiredSpriteName != spriteName ||
                    pair.Value == null)
                {
                    continue;
                }

                pair.Value.sprite = sprite;
                pair.Value.color = Color.white;
                pair.Value.preserveAspect = true;
            }
        }

        private static void ReleaseAllWorldMarkerSprites(this MapWorldPanelComponent self)
        {
            foreach (Sprite sprite in self.MarkerLoadedSprites.Values)
            {
                if (sprite == null)
                {
                    continue;
                }

                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
            }

            self.MarkerLoadedSprites.Clear();
            self.MarkerLoadingSpriteNames.Clear();
            self.MarkerDesiredSpriteNames.Clear();
        }

        private static string ResolveWorldPlayerMarkerColorKey(this MapWorldPanelComponent self, MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker)
        {
            Unit myUnit = runtime?.GetMyUnit();
            if (myUnit == null || myUnit.IsDisposed)
            {
                return global::ET.MinimapConstKey.MarkerColorPlayer;
            }

            Scene scene = runtime.GetParent<Scene>();
            Unit targetUnit = scene?.GetComponent<UnitComponent>()?.Get(marker.UnitId);
            if (targetUnit == null || targetUnit.IsDisposed)
            {
                return global::ET.MinimapConstKey.MarkerColorPlayer;
            }

            CampRelation relation = CampHelper.GetRelation(myUnit, targetUnit);
            switch (relation)
            {
                case CampRelation.Friendly:
                    return global::ET.MinimapConstKey.MarkerColorFriendlyPlayer;
                case CampRelation.Enemy:
                    return global::ET.MinimapConstKey.MarkerColorEnemyPlayer;
                default:
                    return global::ET.MinimapConstKey.MarkerColorPlayer;
            }
        }

        private static Color32 ResolveWorldFogColor(this MapWorldPanelComponent self, string key, string defaultColor)
        {
            string colorText = global::ET.MinimapConstConfigHelper.GetString(key, defaultColor);
            if (ColorUtility.TryParseHtmlString(colorText, out Color color))
            {
                return color;
            }

            return Color.clear;
        }

        private static void SetRuntimeDisplayMode(this MapWorldPanelComponent self, MinimapDisplayMode displayMode)
        {
            self.Root()?.CurrentScene()?.GetComponent<MinimapRuntimeComponent>()?.SetDisplayMode(displayMode);
        }
    }
}
