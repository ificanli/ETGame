using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.11
    /// Desc
    /// </summary>
    public partial class MapWorldPanelComponent : Entity, ILateUpdate
    {
        public RectTransform MapRoot;
        public RectTransform MapFrame;
        public RectTransform MapMask;
        public RectTransform MarkerLayer;
        public RectTransform PingLayer;
        public RawImage MapTexture;
        public RawImage FogOverlay;
        public Image PlayerArrow;
        public TMP_Text TitleText;
        public TMP_Text MapNameText;
        public Texture2D FogTexture;
        public Sprite MarkerSprite;
        public Dictionary<long, RectTransform> MarkerRects = new();
        public Dictionary<long, Image> MarkerImages = new();
        public Dictionary<long, string> MarkerDesiredSpriteNames = new();
        public Dictionary<string, Sprite> MarkerLoadedSprites = new();
        public HashSet<string> MarkerLoadingSpriteNames = new();
    }
}
