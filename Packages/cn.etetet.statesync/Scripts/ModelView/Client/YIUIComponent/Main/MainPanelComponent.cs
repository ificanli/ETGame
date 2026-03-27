using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    public partial class MainPanelComponent : Entity, ILateUpdate
    {
        public bool LastSearchButtonShow;
        public bool LastOpenDoorButtonShow;
        public string LastFocusPointId;
        public string LastOpenContainerPointId;
        public int LastFocusButtonTextId = int.MinValue;
        public bool LastFocusCanInteract;
        public string LastOpenDoorText;
        public int DebugProbeFrame;
        public Button SearchButton;
        public TMP_Text SearchButtonText;
        public Button OpenDoorButton;
        public TMP_Text FpsCounterText;
        public float FpsAccumulatedTime;
        public int FpsAccumulatedFrames;
        public float SmoothedFps;
        public int LastDisplayedFps = int.MinValue;
        public Slider RogueLevelSlider;
        public TMP_Text RogueLevelText;
        public int LastRogueLevel = int.MinValue;
        public int LastRogueCurrentExp = int.MinValue;
        public int LastRogueNeedExp = int.MinValue;
        public RectTransform RogueEffectRoot;
        public RectTransform RogueEffectTextRect;
        public TMP_Text RogueEffectText;
        public List<Button> RogueEffectButtons = new();
        public List<Image> RogueEffectButtonImages = new();
        public List<Sprite> RogueEffectButtonSprites = new();
        public List<string> RogueEffectButtonSpriteNames = new();
        public List<string> RogueEffectButtonDesiredSpriteNames = new();
        public List<GameObject> RogueEffectDynamicButtons = new();
        public int LastRogueEffectSignature = int.MinValue;
        public int RogueEffectPreviewIndex = -1;
        public RectTransform MinimapRoot;
        public RectTransform MinimapBackground;
        public RectTransform MinimapMask;
        public RawImage MinimapTexture;
        public RawImage MinimapFogOverlay;
        public Texture2D MinimapFogTexture;
        public Image MinimapArrow;
        public TMP_Text MinimapNameText;
        public RectTransform MinimapMarkerLayer;
        public Sprite MinimapMarkerSprite;
        public Dictionary<long, RectTransform> MinimapMarkerRects = new();
        public Dictionary<long, Image> MinimapMarkerImages = new();
        public Dictionary<long, string> MinimapMarkerDesiredSpriteNames = new();
        public Dictionary<string, Sprite> MinimapMarkerLoadedSprites = new();
        public HashSet<string> MinimapMarkerLoadingSpriteNames = new();
        public EntityRef<EvacuateTipsComponent> EvacuateTipsViewRef;
        public EvacuateTipsComponent EvacuateTipsView => this.EvacuateTipsViewRef;
        public bool IsEvacuateTipsOpening;
        public bool LastEvacuateTipsVisible;
        public string LastEvacuationPointId;
        public int LastEvacuateRemainSeconds = int.MinValue;
    }
}
