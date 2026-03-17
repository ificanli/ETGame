using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [EnableClass]
    public class LoadoutGridItemViewProxy : MonoBehaviour
    {
        public EntityRef<LobbyPanelComponent> PanelRef;
        public bool IsWarehouse;
        public long ItemUid;
        public int AreaType;
        public int FixedSlotType;
        public int AnchorSlotIndex;
        public int ConfigId;
        public Image IconImage;
        public TMP_Text[] TmpTexts;
        public Text[] Texts;
        public Sprite LoadedSprite;
        public string LoadedIconName = string.Empty;

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
