using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private static void InitHeroDisplay(this LobbyPanelComponent self)
        {
            if (self.HeroDisplay != null)
            {
                return;
            }

            YIUIChild uiBase = self.UIBase;
            if (uiBase == null || uiBase.OwnerGameObject == null)
            {
                Log.Warning("[LobbyPanel] InitHeroDisplay failed: UIBase missing.");
                return;
            }

            UI3DDisplay[] displays = uiBase.OwnerGameObject.GetComponentsInChildren<UI3DDisplay>(true);
            foreach (UI3DDisplay display in displays)
            {
                if (display == null || display.gameObject.name != "YIUI3DDisplay")
                {
                    continue;
                }

                self.m_HeroDisplay = self.AddChild<YIUI3DDisplayChild, UI3DDisplay>(display);
                return;
            }

            Log.Warning("[LobbyPanel] InitHeroDisplay failed: YIUI3DDisplay not found.");
        }

        private static async ETTask RefreshHeroDisplay(this LobbyPanelComponent self, int heroConfigId)
        {
            if (heroConfigId <= 0)
            {
                return;
            }

            if (self.HeroDisplay == null)
            {
                self.InitHeroDisplay();
                if (self.HeroDisplay == null)
                {
                    return;
                }
            }

            HeroDisplayConfig config = HeroDisplayConfigCategory.Instance.GetOrDefault(heroConfigId);
            if (config == null)
            {
                Log.Warning($"[LobbyPanel] Hero display config missing. HeroConfigId={heroConfigId}");
                return;
            }
            if (string.IsNullOrEmpty(config.ModelResName))
            {
                Log.Warning($"[LobbyPanel] Hero display model res name empty. HeroConfigId={heroConfigId}");
                return;
            }

            if (self.CurrentHeroDisplayResName == config.ModelResName && self.CurrentHeroDisplayCameraName == config.CameraName)
            {
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            await self.HeroDisplay.ShowAsync(config.ModelResName, config.CameraName);

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.CurrentHeroDisplayResName = config.ModelResName;
            self.CurrentHeroDisplayCameraName = config.CameraName;
        }
    }
}