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
        public Image IconImage;
        public Sprite LoadedSprite;
        public string LoadedSpriteName = string.Empty;
        public TMP_Text TagText1;
        public TMP_Text TagText2;
        public int[] DisplayTagIds = Array.Empty<int>();
        public int CurrentTagId;
        public bool IsTagDescVisible;
    }
}
