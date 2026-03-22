using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.2.28
    /// Desc
    /// </summary>
    public partial class HeroSelectItemComponent : Entity
    {
        public readonly List<Image> m_HeroIconImages = new();
        public readonly List<RectTransform> m_VisualRects = new();
        public readonly List<Vector2> m_VisualBaseAnchoredPositions = new();
        public string m_LastHeroIconName = string.Empty;
        public Sprite m_LastHeroSprite;
    }
}
