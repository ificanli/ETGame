using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    public partial class MainPanelComponent
    {
        public RectTransform HitDirectionRoot;
        public Sprite HitDirectionSprite;
        public Dictionary<long, RectTransform> HitDirectionIndicatorRects = new();
        public Dictionary<long, Image> HitDirectionIndicatorImages = new();
        public Dictionary<long, CanvasGroup> HitDirectionIndicatorCanvasGroups = new();
        public Dictionary<long, float> HitDirectionIndicatorExpireTimes = new();
        public Dictionary<long, Vector2> HitDirectionIndicatorDirections = new();
    }
}
