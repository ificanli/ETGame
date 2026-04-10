using System;
using System.Collections.Generic;
using ET.Client;
using ET.Server;

namespace ET.Test
{
    public static class TestHelper
    {
        public static async ETTask<Fiber> CreateRobot(Fiber fiber, string robotName)
        {
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, robotName);
            Scene root = robot.Root;
            EntityRef<Scene> rootRef = root;
            root = rootRef;
            await LoginHelper.Login(root, "127.0.0.1:10101", robotName, "");
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
            return robot;
        }
        
        public static Fiber GetMap(Fiber testFiber, Fiber robotFiber)
        {
            if (!TryGetMapFiber(testFiber, robotFiber, out Fiber mapFiber))
            {
                return null;
            }

            return mapFiber;
        }

        public static Unit GetServerUnit(Fiber testFiber, Fiber robotFiber)
        {
            if (!TryGetMapFiber(testFiber, robotFiber, out Fiber mapFiber))
            {
                return null;
            }

            Scene clientScene = robotFiber?.Root;
            ET.Client.PlayerComponent player = clientScene?.GetComponent<ET.Client.PlayerComponent>();
            if (player == null || player.MyId == 0)
            {
                return null;
            }

            UnitComponent unitComponent = mapFiber.Root?.GetComponent<UnitComponent>();
            return unitComponent?.Get(player.MyId);
        }

        private static bool TryGetMapFiber(Fiber testFiber, Fiber robotFiber, out Fiber mapFiber)
        {
            mapFiber = null;

            Scene clientScene = robotFiber?.Root;
            Scene currentScene = clientScene?.CurrentScene();
            if (currentScene == null)
            {
                return false;
            }

            Fiber mapManagerFiber = testFiber?.GetFiber("MapManager");
            MapManagerComponent mapManagerComponent = mapManagerFiber?.Root?.GetComponent<MapManagerComponent>();
            if (mapManagerComponent == null)
            {
                return false;
            }

            string mapConfigName = currentScene.Name.GetSceneConfigName();
            MapCopy mapCopy = mapManagerComponent.GetMap(mapConfigName, currentScene.Id);
            if (mapCopy == null)
            {
                return false;
            }

            mapFiber = mapManagerFiber.GetFiber(mapCopy.FiberId);
            return mapFiber != null;
        }

        public static UnitConfig FindUnitConfig(UnitType unitType)
        {
            UnitConfig fallback = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config == null)
                {
                    continue;
                }

                if (fallback == null && config.UnitType == UnitType.Player)
                {
                    fallback = config;
                }

                if (config.UnitType == unitType)
                {
                    return config;
                }
            }

            return fallback;
        }

        public static Unit CreateServerUnit(
            Scene scene,
            UnitType unitType,
            bool addBuffComponent = true,
            bool addProgress = false,
            bool addItemComponent = false,
            int campId = 0)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            UnitConfig unitConfig = FindUnitConfig(unitType);
            if (unitConfig == null)
            {
                return null;
            }

            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), unitConfig.Id);
            unit.UnitType = unitType;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in unitConfig.KV)
            {
                numeric.SetNoEvent(numericType, numericValue);
            }

            if (addBuffComponent)
            {
                unit.AddComponent<BuffComponent>();
            }

            if (addProgress)
            {
                RogueProgressHelper.EnsureProgress(unit, false);
            }

            if (addItemComponent)
            {
                unit.AddComponent<ET.Server.ItemComponent>();
            }

            if (campId > 0)
            {
                unit.AddComponent<CampComponent, int>(campId);
            }

            return unit;
        }

        public static bool TryFindExecutableRogueOption(
            RogueRuntimeConfigCategory configCategory,
            ICollection<int> excludedOptionIds,
            out int optionId,
            out RogueOptionConfig optionConfig)
        {
            optionId = 0;
            optionConfig = null;
            if (configCategory == null)
            {
                return false;
            }

            foreach ((int currentOptionId, RogueOptionConfig currentOptionConfig) in configCategory.GetOptions())
            {
                if (currentOptionConfig == null)
                {
                    continue;
                }

                if (excludedOptionIds != null && excludedOptionIds.Contains(currentOptionId))
                {
                    continue;
                }

                if (!RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, currentOptionConfig, out _))
                {
                    continue;
                }

                optionId = currentOptionId;
                optionConfig = currentOptionConfig;
                return true;
            }

            return false;
        }

        public static bool TryFindKeepableRogueOption(
            RogueRuntimeConfigCategory configCategory,
            ICollection<int> excludedOptionIds,
            out int optionId,
            out RogueOptionConfig optionConfig,
            out int buffConfigId)
        {
            optionId = 0;
            optionConfig = null;
            buffConfigId = 0;
            if (configCategory == null)
            {
                return false;
            }

            BuffConfigCategory buffCategory = BuffConfigCategory.Instance;
            if (buffCategory == null)
            {
                return false;
            }

            foreach ((int currentOptionId, RogueOptionConfig currentOptionConfig) in configCategory.GetOptions())
            {
                if (currentOptionConfig == null)
                {
                    continue;
                }

                if (excludedOptionIds != null && excludedOptionIds.Contains(currentOptionId))
                {
                    continue;
                }

                if (!RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, currentOptionConfig, out int currentBuffConfigId) ||
                    currentBuffConfigId <= 0 ||
                    !buffCategory.Contain(currentBuffConfigId))
                {
                    continue;
                }

                BuffConfig buffConfig = buffCategory.Get(currentBuffConfigId);
                if (!ShouldKeepRogueBuffForTest(buffConfig))
                {
                    continue;
                }

                optionId = currentOptionId;
                optionConfig = currentOptionConfig;
                buffConfigId = currentBuffConfigId;
                return true;
            }

            return false;
        }

        public static bool TryFindCommonShowTagWithBuff(
            RogueRuntimeConfigCategory configCategory,
            string showTagName,
            out int tagId,
            out int buffConfigId)
        {
            tagId = 0;
            buffConfigId = 0;
            if (configCategory == null)
            {
                return false;
            }

            BuffConfigCategory buffCategory = BuffConfigCategory.Instance;
            if (buffCategory == null)
            {
                return false;
            }

            foreach (RogueTagConfig tagConfig in configCategory.GetTags().Values)
            {
                if (tagConfig == null ||
                    tagConfig.TagType != 1 ||
                    tagConfig.ShowTagsBuffId == null ||
                    tagConfig.ShowTagsBuffId.Length == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(showTagName) &&
                    !string.Equals(tagConfig.ShowTagsName, showTagName, StringComparison.Ordinal))
                {
                    continue;
                }

                int currentBuffConfigId = tagConfig.ShowTagsBuffId[0];
                if (currentBuffConfigId <= 0 || !buffCategory.Contain(currentBuffConfigId))
                {
                    continue;
                }

                tagId = tagConfig.Id;
                buffConfigId = currentBuffConfigId;
                return true;
            }

            return false;
        }

        private static bool ShouldKeepRogueBuffForTest(BuffConfig buffConfig)
        {
            if (buffConfig == null)
            {
                return false;
            }

            if (buffConfig.TickTime > 0 || (buffConfig.Duration >= 0 && buffConfig.Duration < int.MaxValue))
            {
                return true;
            }

            foreach (EffectNode effect in buffConfig.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                if (effect is EffectServerBuffAdd addEffect)
                {
                    if (ContainsOnlyInstantRogueAddActionsForTest(addEffect))
                    {
                        continue;
                    }

                    return true;
                }

                if (effect is EffectRogueReplaceAllCards)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool ContainsOnlyInstantRogueAddActionsForTest(EffectServerBuffAdd addEffect)
        {
            if (addEffect?.Children == null || addEffect.Children.Count == 0)
            {
                return true;
            }

            foreach (BTNode node in addEffect.Children)
            {
                if (node is BTRogueAddGold ||
                    node is BTRogueSpawnMerchant ||
                    node is BTRogueGrantRandomCard ||
                    node is BTRogueGrantItem ||
                    node is BTRogueReplaceAllCards ||
                    node is BTRogueHealMaxHpPermille)
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
