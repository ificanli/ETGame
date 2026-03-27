using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 小地图显示辅助方法，统一底图有效 UV 与 UI 节点拉伸规则。
    /// </summary>
    public static class MinimapDisplayHelper
    {
        public static void StretchToFillParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        public static Rect GetExpandedBaseUvRect(MinimapRuntimeComponent runtime, Texture texture)
        {
            return GetBaseTextureWorldUvRect(runtime, texture);
        }

        public static Rect GetExpandedFogUvRect()
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        public static Rect GetCompactBaseUvRect(MinimapRuntimeComponent runtime, Texture texture, float3 centerPosition)
        {
            Rect worldUvRect = GetBaseTextureWorldUvRect(runtime, texture);
            if (!TryGetWorldSpan(runtime, out float width, out float height))
            {
                return worldUvRect;
            }

            Rect worldView = ComputeWorldViewRect(runtime, centerPosition);
            float normX = (worldView.x - runtime.WorldMinX) / width;
            float normY = (worldView.y - runtime.WorldMinZ) / height;
            float normW = worldView.width / width;
            float normH = worldView.height / height;

            float uvX = worldUvRect.x + normX * worldUvRect.width;
            float uvY = worldUvRect.y + normY * worldUvRect.height;
            float uvW = normW * worldUvRect.width;
            float uvH = normH * worldUvRect.height;

            return new Rect(uvX, uvY, uvW, uvH);
        }

        public static Rect GetCompactFogUvRect(MinimapRuntimeComponent runtime, float3 centerPosition)
        {
            if (!TryGetWorldSpan(runtime, out float width, out float height))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            Rect worldView = ComputeWorldViewRect(runtime, centerPosition);
            float uvX = (worldView.x - runtime.WorldMinX) / width;
            float uvY = (worldView.y - runtime.WorldMinZ) / height;
            float uvW = worldView.width / width;
            float uvH = worldView.height / height;

            return new Rect(uvX, uvY, uvW, uvH);
        }

        private static Rect ComputeWorldViewRect(MinimapRuntimeComponent runtime, float3 centerPosition)
        {
            float worldWidth = runtime.WorldMaxX - runtime.WorldMinX;
            float worldHeight = runtime.WorldMaxZ - runtime.WorldMinZ;
            float range = runtime.CompactRange;

            float viewMinX = centerPosition.x - range;
            float viewMaxX = centerPosition.x + range;
            float viewMinZ = centerPosition.z - range;
            float viewMaxZ = centerPosition.z + range;

            if (viewMinX < runtime.WorldMinX)
            {
                viewMaxX += runtime.WorldMinX - viewMinX;
                viewMinX = runtime.WorldMinX;
            }

            if (viewMaxX > runtime.WorldMaxX)
            {
                viewMinX -= viewMaxX - runtime.WorldMaxX;
                viewMaxX = runtime.WorldMaxX;
            }

            if (viewMinZ < runtime.WorldMinZ)
            {
                viewMaxZ += runtime.WorldMinZ - viewMinZ;
                viewMinZ = runtime.WorldMinZ;
            }

            if (viewMaxZ > runtime.WorldMaxZ)
            {
                viewMinZ -= viewMaxZ - runtime.WorldMaxZ;
                viewMaxZ = runtime.WorldMaxZ;
            }

            viewMinX = Mathf.Max(viewMinX, runtime.WorldMinX);
            viewMaxX = Mathf.Min(viewMaxX, runtime.WorldMaxX);
            viewMinZ = Mathf.Max(viewMinZ, runtime.WorldMinZ);
            viewMaxZ = Mathf.Min(viewMaxZ, runtime.WorldMaxZ);

            return new Rect(viewMinX, viewMinZ, viewMaxX - viewMinX, viewMaxZ - viewMinZ);
        }

        private static Rect GetBaseTextureWorldUvRect(MinimapRuntimeComponent runtime, Texture texture)
        {
            if (!TryGetWorldSpan(runtime, out float width, out float height) || texture == null || texture.height <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float worldAspect = width / height;
            float textureAspect = texture.width / (float)texture.height;
            if (worldAspect <= 0f || textureAspect <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (Mathf.Abs(textureAspect - worldAspect) <= 0.0001f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (textureAspect > worldAspect)
            {
                float activeWidth = worldAspect / textureAspect;
                float paddingX = (1f - activeWidth) * 0.5f;
                return new Rect(paddingX, 0f, activeWidth, 1f);
            }

            float activeHeight = textureAspect / worldAspect;
            float paddingY = (1f - activeHeight) * 0.5f;
            return new Rect(0f, paddingY, 1f, activeHeight);
        }

        /// <summary>
        /// 返回紧凑模式下经过世界边界钳制后的视野中心点。
        /// 当玩家靠近地图边缘时，视野中心会偏离玩家位置。
        /// </summary>
        public static float3 GetCompactViewCenter(MinimapRuntimeComponent runtime, float3 playerPosition)
        {
            Rect worldView = ComputeWorldViewRect(runtime, playerPosition);
            return new float3(
                worldView.x + worldView.width * 0.5f,
                playerPosition.y,
                worldView.y + worldView.height * 0.5f);
        }

        private static bool TryGetWorldSpan(MinimapRuntimeComponent runtime, out float width, out float height)
        {
            width = 0f;
            height = 0f;
            if (runtime == null)
            {
                return false;
            }

            width = runtime.WorldMaxX - runtime.WorldMinX;
            height = runtime.WorldMaxZ - runtime.WorldMinZ;
            return width > 0f && height > 0f;
        }
    }
}
