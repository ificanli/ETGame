using System;
using System.Collections.Generic;

namespace ET.Server
{
    public static class MatchRobotRuntimeHelper
    {
        private const int DefaultMatchRobotAIBuffConfigId = 300011;
        private const int DefaultAutoChooseDelayMinMs = 600;
        private const int DefaultAutoChooseDelayMaxMs = 1200;
        private const char ValueSeparator = '|';
        private const int HeroSeedSalt = 0x4D525448;
        private const int WeaponSeedSalt = 0x57504E01;
        private const int ChoiceDelaySeedSalt = 0x43484F49;
        private const int ChoiceOptionSeedSalt = 0x4F505401;

        public static bool TryBuildSpawnProfile(
            string mapName,
            int gameMode,
            long robotPlayerId,
            out int matchRobotConfigId,
            out int heroConfigId,
            out int unitConfigId,
            out int mainWeaponConfigId,
            out int aiBuffConfigId,
            out int autoChooseDelayMinMs,
            out int autoChooseDelayMaxMs)
        {
            matchRobotConfigId = 0;
            heroConfigId = 0;
            unitConfigId = 0;
            mainWeaponConfigId = 0;
            aiBuffConfigId = 0;
            autoChooseDelayMinMs = 0;
            autoChooseDelayMaxMs = 0;

            MatchRobotConfig matchRobotConfig = ResolveConfig(mapName, gameMode);
            HeroConfig heroConfig = ResolveHeroConfig(matchRobotConfig, mapName, gameMode, robotPlayerId);
            if (heroConfig == null || heroConfig.UnitConfigId <= 0)
            {
                Log.Error($"[MatchRobot] build profile failed: invalid hero, map={mapName}, gameMode={gameMode}, robotId={robotPlayerId}");
                return false;
            }

            UnitConfig unitConfig = UnitConfigCategory.Instance.GetOrDefault(heroConfig.UnitConfigId);
            if (unitConfig == null || unitConfig.UnitType != UnitType.Player)
            {
                Log.Error($"[MatchRobot] build profile failed: invalid hero unit config, heroConfigId={heroConfig.Id}, unitConfigId={heroConfig.UnitConfigId}");
                return false;
            }

            mainWeaponConfigId = ResolveMainWeaponConfigId(matchRobotConfig, mapName, gameMode, robotPlayerId);
            if (mainWeaponConfigId <= 0)
            {
                Log.Error($"[MatchRobot] build profile failed: invalid main weapon, map={mapName}, gameMode={gameMode}, robotId={robotPlayerId}");
                return false;
            }

            matchRobotConfigId = matchRobotConfig?.Id ?? 0;
            heroConfigId = heroConfig.Id;
            unitConfigId = heroConfig.UnitConfigId;
            aiBuffConfigId = ResolveAIBuffConfigId(matchRobotConfig);
            ResolveAutoChooseDelay(matchRobotConfig, out autoChooseDelayMinMs, out autoChooseDelayMaxMs);
            return true;
        }

        public static int ResolveMatchRobotAIBuffConfigId(string mapName, int gameMode, int configuredAIBuffConfigId = 0)
        {
            int aiBuffConfigId = ValidateAIBuffConfigId(configuredAIBuffConfigId);
            if (aiBuffConfigId > 0)
            {
                return aiBuffConfigId;
            }

            return ResolveAIBuffConfigId(ResolveConfig(mapName, gameMode));
        }

        public static void ApplyLoadout(Unit unit, MatchRobotComponent matchRobot)
        {
            if (unit == null || unit.IsDisposed || matchRobot == null)
            {
                return;
            }

            int mainWeaponConfigId = ValidateMainWeaponConfigId(matchRobot.MainWeaponConfigId);
            if (mainWeaponConfigId <= 0)
            {
                Log.Warning($"[MatchRobot] skip loadout apply: invalid main weapon, unitId={unit.Id}, weaponConfigId={matchRobot.MainWeaponConfigId}");
                return;
            }

            LoadoutHelper.ApplyLoadout(unit, mainWeaponConfigId, 0, 0);
        }

        public static int ResolveAutoChoiceDelayMs(long unitId, long choiceSerial, int minDelayMs, int maxDelayMs)
        {
            return ResolveChoiceDelayMs(unitId, choiceSerial, minDelayMs, maxDelayMs);
        }

        public static int ResolveAutoChoiceOptionId(Unit unit, RogueProgressComponent progress)
        {
            if (unit == null || unit.IsDisposed || progress == null || progress.PendingOptionIds.Count == 0)
            {
                return 0;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            Random random = CreateRandom(unit.Id, progress.ChoiceSerial, ChoiceOptionSeedSalt);
            List<int> validOptionIds = new(progress.PendingOptionIds.Count);
            List<int> validWeights = new(progress.PendingOptionIds.Count);

            foreach (int optionId in progress.PendingOptionIds)
            {
                if (optionId <= 0)
                {
                    continue;
                }

                validOptionIds.Add(optionId);
                if (configCategory != null && configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) && optionConfig != null && optionConfig.Weight > 0)
                {
                    validWeights.Add(optionConfig.Weight);
                }
                else
                {
                    validWeights.Add(0);
                }
            }

            if (validOptionIds.Count == 0)
            {
                return 0;
            }

            return RollWeightedValue(validOptionIds, validWeights, random);
        }

        private static MatchRobotConfig ResolveConfig(string mapName, int gameMode)
        {
            MatchRobotConfigCategory category = MatchRobotConfigCategory.Instance;
            if (category == null || category.DataList == null || category.DataList.Count == 0)
            {
                return null;
            }

            mapName = NormalizeMapName(mapName);
            MatchRobotConfig bestConfig = null;
            int bestScore = int.MinValue;

            foreach (MatchRobotConfig config in category.DataList)
            {
                if (config == null)
                {
                    continue;
                }

                string configMapName = NormalizeMapName(config.MapName);
                bool mapMatched = string.IsNullOrEmpty(configMapName) || string.Equals(configMapName, mapName, StringComparison.OrdinalIgnoreCase);
                bool gameModeMatched = config.GameMode <= 0 || config.GameMode == gameMode;
                if (!mapMatched || !gameModeMatched)
                {
                    continue;
                }

                int score = 0;
                score += string.IsNullOrEmpty(configMapName) ? 1 : 4;
                score += config.GameMode <= 0 ? 1 : 2;

                if (score > bestScore || (score == bestScore && (bestConfig == null || config.Id < bestConfig.Id)))
                {
                    bestConfig = config;
                    bestScore = score;
                }
            }

            return bestConfig;
        }

        private static HeroConfig ResolveHeroConfig(MatchRobotConfig config, string mapName, int gameMode, long robotPlayerId)
        {
            int heroConfigId = RollConfigId(
                config?.HeroConfigIds,
                config?.HeroWeights,
                CreateRandom(mapName, gameMode, robotPlayerId, HeroSeedSalt),
                ValidateHeroConfigId);

            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            if (heroConfig != null && heroConfig.UnitConfigId > 0)
            {
                return heroConfig;
            }

            if (HeroConfigCategory.Instance?.DataList == null)
            {
                return null;
            }

            foreach (HeroConfig fallbackHeroConfig in HeroConfigCategory.Instance.DataList)
            {
                if (ValidateHeroConfigId(fallbackHeroConfig?.Id ?? 0) <= 0)
                {
                    continue;
                }

                return fallbackHeroConfig;
            }

            return null;
        }

        private static int ResolveMainWeaponConfigId(MatchRobotConfig config, string mapName, int gameMode, long robotPlayerId)
        {
            int mainWeaponConfigId = RollConfigId(
                config?.MainWeaponConfigIds,
                config?.MainWeaponWeights,
                CreateRandom(mapName, gameMode, robotPlayerId, WeaponSeedSalt),
                ValidateMainWeaponConfigId);

            if (mainWeaponConfigId > 0)
            {
                return mainWeaponConfigId;
            }

            if (WeaponConfigCategory.Instance?.DataList == null)
            {
                return 0;
            }

            foreach (WeaponConfig weaponConfig in WeaponConfigCategory.Instance.DataList)
            {
                if (ValidateMainWeaponConfigId(weaponConfig?.Id ?? 0) <= 0)
                {
                    continue;
                }

                return weaponConfig.Id;
            }

            return 0;
        }

        private static int ResolveAIBuffConfigId(MatchRobotConfig config)
        {
            return ValidateAIBuffConfigId(config?.AIBuffConfigId ?? 0);
        }

        private static void ResolveAutoChooseDelay(MatchRobotConfig config, out int minDelayMs, out int maxDelayMs)
        {
            minDelayMs = Math.Max(0, config?.AutoChooseDelayMinMs ?? 0);
            maxDelayMs = Math.Max(0, config?.AutoChooseDelayMaxMs ?? 0);

            if (minDelayMs <= 0)
            {
                minDelayMs = DefaultAutoChooseDelayMinMs;
            }

            if (maxDelayMs <= 0)
            {
                maxDelayMs = Math.Max(minDelayMs, DefaultAutoChooseDelayMaxMs);
            }

            if (maxDelayMs < minDelayMs)
            {
                maxDelayMs = minDelayMs;
            }
        }

        private static int ResolveChoiceDelayMs(long unitId, long choiceSerial, int minDelayMs, int maxDelayMs)
        {
            minDelayMs = Math.Max(0, minDelayMs);
            maxDelayMs = Math.Max(minDelayMs, maxDelayMs);
            if (maxDelayMs <= minDelayMs)
            {
                return minDelayMs;
            }

            Random random = CreateRandom(unitId, choiceSerial, ChoiceDelaySeedSalt);
            return random.Next(minDelayMs, maxDelayMs + 1);
        }

        private static int RollConfigId(string valuePool, string weightPool, Random random, Func<int, int> validator)
        {
            List<int> values = ParseIntPool(valuePool);
            if (values.Count == 0)
            {
                return 0;
            }

            List<int> sourceWeights = ParseIntPool(weightPool);
            List<int> validValues = new(values.Count);
            List<int> validWeights = new(values.Count);
            for (int i = 0; i < values.Count; ++i)
            {
                int validatedValue = validator(values[i]);
                if (validatedValue > 0)
                {
                    validValues.Add(validatedValue);
                    validWeights.Add(i < sourceWeights.Count ? sourceWeights[i] : 0);
                }
            }

            if (validValues.Count == 0)
            {
                return 0;
            }

            return RollWeightedValue(validValues, validWeights, random);
        }

        private static int RollWeightedValue(List<int> values, List<int> weights, Random random)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            int totalWeight = 0;
            for (int i = 0; i < values.Count; ++i)
            {
                totalWeight += GetWeight(weights, i);
            }

            if (totalWeight <= 0)
            {
                return values[random.Next(0, values.Count)];
            }

            int roll = random.Next(0, totalWeight);
            int accumulatedWeight = 0;
            for (int i = 0; i < values.Count; ++i)
            {
                accumulatedWeight += GetWeight(weights, i);
                if (roll < accumulatedWeight)
                {
                    return values[i];
                }
            }

            return values[values.Count - 1];
        }

        private static int GetWeight(List<int> weights, int index)
        {
            if (weights == null || index < 0 || index >= weights.Count)
            {
                return 0;
            }

            return Math.Max(0, weights[index]);
        }

        private static List<int> ParseIntPool(string text)
        {
            List<int> result = new();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            string[] values = text.Split(ValueSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string valueText in values)
            {
                if (!int.TryParse(valueText.Trim(), out int value))
                {
                    continue;
                }

                result.Add(value);
            }

            return result;
        }

        private static int ValidateHeroConfigId(int heroConfigId)
        {
            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            if (heroConfig == null || heroConfig.UnitConfigId <= 0)
            {
                return 0;
            }

            UnitConfig unitConfig = UnitConfigCategory.Instance.GetOrDefault(heroConfig.UnitConfigId);
            if (unitConfig == null || unitConfig.UnitType != UnitType.Player)
            {
                return 0;
            }

            return heroConfigId;
        }

        private static int ValidateMainWeaponConfigId(int weaponConfigId)
        {
            return WeaponConfigCategory.Instance.GetOrDefault(weaponConfigId) != null ? weaponConfigId : 0;
        }

        private static int ValidateAIBuffConfigId(int aiBuffConfigId)
        {
            if (BuffConfigCategory.Instance.Contain(aiBuffConfigId))
            {
                return aiBuffConfigId;
            }

            return BuffConfigCategory.Instance.Contain(DefaultMatchRobotAIBuffConfigId) ? DefaultMatchRobotAIBuffConfigId : 0;
        }

        private static string NormalizeMapName(string mapName)
        {
            return string.IsNullOrWhiteSpace(mapName) ? string.Empty : mapName.Trim();
        }

        private static Random CreateRandom(string mapName, int gameMode, long robotPlayerId, int salt)
        {
            ulong seed = 1469598103934665603UL;
            MixSeed(ref seed, (ulong)(uint)salt);
            MixSeed(ref seed, (ulong)(uint)gameMode);
            MixSeed(ref seed, (ulong)robotPlayerId);

            mapName = NormalizeMapName(mapName);
            for (int i = 0; i < mapName.Length; ++i)
            {
                MixSeed(ref seed, mapName[i]);
            }

            return new Random((int)(seed ^ (seed >> 32)));
        }

        private static Random CreateRandom(long unitId, long serial, int salt)
        {
            ulong seed = 1469598103934665603UL;
            MixSeed(ref seed, (ulong)(uint)salt);
            MixSeed(ref seed, (ulong)unitId);
            MixSeed(ref seed, (ulong)serial);
            return new Random((int)(seed ^ (seed >> 32)));
        }

        private static void MixSeed(ref ulong seed, ulong value)
        {
            seed ^= value;
            seed *= 1099511628211UL;
        }
    }
}
