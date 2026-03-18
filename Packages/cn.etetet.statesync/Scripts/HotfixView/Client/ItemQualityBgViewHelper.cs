using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 根据物品品质刷新通用格子背景图。
    /// </summary>
    public static class ItemQualityBgViewHelper
    {
        public static void UpdateQualityBgByConfigId(RectTransform view, int configId)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            UpdateQualityBg(view, itemConfig?.Quality ?? 1);
        }

        public static void UpdateQualityBg(RectTransform view, int quality)
        {
            if (view == null)
            {
                return;
            }

            ItemQualityBgViewProxy proxy = view.GetComponent<ItemQualityBgViewProxy>();
            if (proxy == null)
            {
                proxy = view.gameObject.AddComponent<ItemQualityBgViewProxy>();
            }

            proxy.QualityBgImage = FindQualityBgImage(view);
            UpdateQualityBgAsync(proxy, quality).Coroutine();
        }

        private static async ETTask UpdateQualityBgAsync(ItemQualityBgViewProxy proxy, int quality)
        {
            if (proxy == null)
            {
                return;
            }

            Image qualityBgImage = proxy.QualityBgImage;
            if (qualityBgImage == null)
            {
                return;
            }

            string spriteName = GetQualityBgSpriteName(quality);
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                qualityBgImage.sprite = null;
                qualityBgImage.enabled = false;
                return;
            }

            if (proxy.LoadedSprite != null && proxy.LoadedSpriteName == spriteName)
            {
                qualityBgImage.sprite = proxy.LoadedSprite;
                qualityBgImage.enabled = true;
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = spriteName });

            if (proxy == null || proxy.QualityBgImage == null)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            if (proxy.LoadedSprite != null && proxy.LoadedSprite != sprite)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = proxy.LoadedSprite });
            }

            proxy.LoadedSprite = sprite;
            proxy.LoadedSpriteName = spriteName;
            proxy.QualityBgImage.sprite = sprite;
            proxy.QualityBgImage.enabled = sprite != null;
            proxy.QualityBgImage.color = Color.white;
        }

        private static Image FindQualityBgImage(RectTransform view)
        {
            Transform qualityBg = view.Find("QualityBg");
            if (qualityBg != null)
            {
                return qualityBg.GetComponent<Image>();
            }

            Image[] images = view.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; ++i)
            {
                Image image = images[i];
                if (image != null && image.name == "QualityBg")
                {
                    return image;
                }
            }

            return null;
        }

        private static string GetQualityBgSpriteName(int quality)
        {
            switch (Mathf.Clamp(quality, 1, 5))
            {
                case 1:
                    return "green";
                case 2:
                    return "blue";
                case 3:
                    return "purple";
                case 4:
                    return "gold";
                case 5:
                    return "red";
                default:
                    return "green";
            }
        }
    }
}
