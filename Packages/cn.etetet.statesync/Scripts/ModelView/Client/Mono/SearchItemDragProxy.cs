using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [EnableClass]
    public class SearchItemDragProxy : MonoBehaviour
    {
        public EntityRef<SearchPanelComponent> PanelRef;
        public long ItemId;
        public bool IsBag;
        public int ConfigId;
        public Image IconImage;
        public TMP_Text[] TmpTexts;
        public Text[] Texts;
        public Sprite LoadedSprite;
        public string LoadedIconName = string.Empty;

        public SearchPanelComponent Panel => this.PanelRef;

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
            this.LoadedIconName = string.Empty;
        }
    }
}
