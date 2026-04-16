using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class RogueMissionTaskPopupComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public GameObject RootGameObject;
        public Canvas Canvas;
        public RectTransform CanvasRect;
        public Button MaskButton;
        public RectTransform WindowRect;
        public TMP_Text TitleText;
        public TMP_Text DescText;
        public TMP_Text RewardText;
        public Button AcceptButton;
        public TMP_Text AcceptButtonText;
        public Button CancelButton;
        public string CurrentPointId = string.Empty;
        public bool Visible;
    }
}
