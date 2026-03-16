using System;
using System.Collections;
using System.Linq;
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
                self.RefreshHeroDescription(heroConfigId, null);
                return;
            }

            HeroDisplayConfig config = HeroDisplayConfigCategory.Instance.GetOrDefault(heroConfigId);
            if (config == null)
            {
                self.RefreshHeroDescription(heroConfigId, null);
                Log.Warning($"[LobbyPanel] Hero display config missing. HeroConfigId={heroConfigId}");
                return;
            }

            self.RefreshHeroDescription(heroConfigId, config);

            if (self.HeroDisplay == null)
            {
                self.InitHeroDisplay();
                if (self.HeroDisplay == null)
                {
                    return;
                }
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

        private static void RefreshHeroDescription(this LobbyPanelComponent self, int heroConfigId, HeroDisplayConfig config)
        {
            int attack = self.ResolveAttackValue(config);
            int hp = self.ResolveHpValue(heroConfigId, config);

            self.u_DataRoleAttackAbility?.SetValue($"攻击力{attack}");
            self.u_DataRoleSurviveAbility?.SetValue($"生命值{hp}");
            self.u_DataRoleSkillDes?.SetValue(self.ResolveSkillDesc(heroConfigId, config));
        }

        private static void RefreshCurrentHeroDescription(this LobbyPanelComponent self)
        {
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            if (loadout == null || loadout.SelectedHeroConfigId <= 0)
            {
                return;
            }

            self.RefreshHeroDisplay(loadout.SelectedHeroConfigId).Coroutine();
        }

        private static int ResolveAttackValue(this LobbyPanelComponent self, HeroDisplayConfig config)
        {
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            int mainWeaponId = loadout?.MainWeaponConfigId ?? 0;
            int subWeaponId = loadout?.SubWeaponConfigId ?? 0;

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(mainWeaponId)
                    ?? WeaponConfigCategory.Instance.GetOrDefault(subWeaponId);
            if (weaponConfig == null)
            {
                weaponConfig = WeaponConfigCategory.Instance.DataList.OrderBy(x => x.Id).FirstOrDefault();
            }

            if (weaponConfig != null)
            {
                return Mathf.RoundToInt(weaponConfig.Damage);
            }

            return self.ParsePropertyValue(config?.PropertyDesc, "攻击力");
        }

        private static int ResolveHpValue(this LobbyPanelComponent self, int heroConfigId, HeroDisplayConfig config)
        {
            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            UnitConfig unitConfig = UnitConfigCategory.Instance.GetOrDefault(heroConfig?.UnitConfigId ?? 0);
            if (unitConfig != null && self.TryGetUnitNumericValue(unitConfig, NumericType.MaxHPBase, out long maxHp))
            {
                return (int)maxHp;
            }

            return self.ParsePropertyValue(config?.PropertyDesc, "生命值");
        }

        private static bool TryGetUnitNumericValue(this LobbyPanelComponent self, UnitConfig unitConfig, int numericType, out long value)
        {
            value = 0;

            var kvField = typeof(UnitConfig).GetField("KV");
            if (kvField == null)
            {
                return false;
            }

            if (kvField.GetValue(unitConfig) is not IDictionary kvDict)
            {
                return false;
            }

            if (!kvDict.Contains(numericType))
            {
                return false;
            }

            object raw = kvDict[numericType];
            switch (raw)
            {
                case long longValue:
                    value = longValue;
                    return true;
                case int intValue:
                    value = intValue;
                    return true;
                case float floatValue:
                    value = (long)floatValue;
                    return true;
                case double doubleValue:
                    value = (long)doubleValue;
                    return true;
                case string stringValue when long.TryParse(stringValue, out long parsed):
                    value = parsed;
                    return true;
                default:
                    return false;
            }
        }

        private static int ParsePropertyValue(this LobbyPanelComponent self, string propertyDesc, string key)
        {
            if (string.IsNullOrWhiteSpace(propertyDesc) || string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            int keyIndex = propertyDesc.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return 0;
            }

            int numberStart = keyIndex + key.Length;
            int numberEnd = numberStart;
            while (numberEnd < propertyDesc.Length && char.IsDigit(propertyDesc[numberEnd]))
            {
                ++numberEnd;
            }

            if (numberEnd <= numberStart)
            {
                return 0;
            }

            string numberText = propertyDesc.Substring(numberStart, numberEnd - numberStart);
            return int.TryParse(numberText, out int value) ? value : 0;
        }

        private static string ResolveSkillDesc(this LobbyPanelComponent self, int heroConfigId, HeroDisplayConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config?.SkillDesc))
            {
                return config.SkillDesc;
            }

            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(heroConfig?.SkillConfigId ?? 0);
            if (spellConfig != null && !string.IsNullOrWhiteSpace(spellConfig.Desc))
            {
                return spellConfig.Desc;
            }

            return string.Empty;
        }
    }
}
