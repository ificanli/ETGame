using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;

using TMPro;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.6
    /// Desc
    /// </summary>
    public partial class RogueOptionComponent : Entity
    {
        public EntityRef<RoguePanelComponent> Panel;
        public int OptionIndex = -1;
        public bool IsChoosing;
        public bool IsRerolling;
        public Image IconImage;
        public Image OptionBgImage;
        public Button RerollButton;
        public Image RerollButtonImage;
        public Image TagBgImage1;
        public Image TagBgImage2;
        public Sprite LoadedSprite;
        public string LoadedSpriteName = string.Empty;
        public Sprite LoadedBgSprite;
        public string LoadedBgSpriteName = string.Empty;
        public Sprite LoadedTagBgSprite;
        public string LoadedTagBgSpriteName = string.Empty;
        public Sprite DefaultBgSprite;
        public Color DefaultBgColor = Color.white;
        public bool DefaultBgImageEnabled = true;
        public Color DefaultRerollButtonColor = Color.white;
        public bool DefaultRerollButtonImageEnabled = true;
        public Sprite DefaultTagBgSprite1;
        public Sprite DefaultTagBgSprite2;
        public Color DefaultTagBgColor1 = Color.white;
        public Color DefaultTagBgColor2 = Color.white;
        public bool DefaultTagBgImageEnabled1 = true;
        public bool DefaultTagBgImageEnabled2 = true;
        public TMP_Text TagText1;
        public TMP_Text TagText2;
        public int[] DisplayTagIds = Array.Empty<int>();
        public int CurrentTagId;
        public bool IsTagDescVisible;
    }
}
