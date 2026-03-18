using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [EnableClass]
    public class ItemQualityBgViewProxy : MonoBehaviour
    {
        public Image QualityBgImage;
        public Sprite LoadedSprite;
        public string LoadedSpriteName = string.Empty;

        private void OnDestroy()
        {
            if (this.LoadedSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = this.LoadedSprite });

            this.LoadedSprite = null;
            this.LoadedSpriteName = string.Empty;
        }
    }
}
