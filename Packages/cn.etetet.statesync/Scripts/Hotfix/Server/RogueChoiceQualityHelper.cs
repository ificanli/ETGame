using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueChoiceQualityHelper
    {
        public const int ChoiceRoundCount = 4;
        public const int MinQuality = 1;
        public const int MaxQuality = 3;

        public static RogueChoiceQualityComponent GetOrCreate(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return null;
            }

            RogueChoiceQualityComponent component = unit.GetComponent<RogueChoiceQualityComponent>();
            if (component == null)
            {
                component = unit.AddComponent<RogueChoiceQualityComponent>();
            }

            component.InitializeIfNeeded();
            return component;
        }

        public static void Initialize(Unit unit)
        {
            GetOrCreate(unit);
        }

        public static int GetChoiceQuality(Unit unit, int choiceIndex)
        {
            RogueChoiceQualityComponent component = GetOrCreate(unit);
            if (component == null || component.ChoiceQualities.Count == 0)
            {
                return 0;
            }

            if (choiceIndex < 0)
            {
                choiceIndex = 0;
            }

            if (choiceIndex >= component.ChoiceQualities.Count)
            {
                Log.Warning($"[Rogue] choice quality index overflow: index={choiceIndex}, count={component.ChoiceQualities.Count}");
                choiceIndex = component.ChoiceQualities.Count - 1;
            }

            return component.ChoiceQualities[choiceIndex];
        }

        public static void InitializeIfNeeded(this RogueChoiceQualityComponent self)
        {
            if (self == null || self.IsDisposed || self.ChoiceQualities.Count > 0)
            {
                return;
            }

            List<int> availableQualities = GetAvailableQualities();
            if (availableQualities.Count == 0)
            {
                Log.Warning("[Rogue] init choice qualities failed: no available qualities.");
                return;
            }

            Dictionary<int, int> qualityWeights = BuildQualityWeights(availableQualities);
            for (int i = 0; i < ChoiceRoundCount; ++i)
            {
                self.ChoiceQualities.Add(RollQuality(availableQualities, qualityWeights));
            }

            ApplyExtremeQualityPity(self.ChoiceQualities, availableQualities, qualityWeights);
            Log.Info($"[Rogue] choice qualities initialized: [{string.Join(",", self.ChoiceQualities)}]");
        }

        private static List<int> GetAvailableQualities()
        {
            List<int> result = new List<int>();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                return result;
            }

            RogueBuffConfigLoader.EnsureRegistered();
            Dictionary<int, RogueOptionConfig> options = configCategory.GetOptions();
            if (options == null || options.Count == 0)
            {
                return result;
            }

            foreach (KeyValuePair<int, RogueOptionConfig> kv in options)
            {
                RogueOptionConfig optionConfig = kv.Value;
                if (optionConfig == null || optionConfig.Quality <= 0)
                {
                    continue;
                }

                if (!RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, optionConfig, out _))
                {
                    continue;
                }

                if (!result.Contains(optionConfig.Quality))
                {
                    result.Add(optionConfig.Quality);
                }
            }

            result.Sort();
            return result;
        }

        private static Dictionary<int, int> BuildQualityWeights(List<int> availableQualities)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            RogueGlobalConfigCategory globalCategory = RogueGlobalConfigCategory.Instance;
            foreach (int quality in availableQualities)
            {
                int weight = quality switch
                {
                    1 => globalCategory?.QualityRandom1 ?? 0,
                    2 => globalCategory?.QualityRandom2 ?? 0,
                    3 => globalCategory?.QualityRandom3 ?? 0,
                    _ => 0,
                };

                result[quality] = weight;
            }

            return result;
        }

        private static int RollQuality(List<int> availableQualities, Dictionary<int, int> qualityWeights)
        {
            int totalWeight = 0;
            for (int i = 0; i < availableQualities.Count; ++i)
            {
                int quality = availableQualities[i];
                if (qualityWeights.TryGetValue(quality, out int weight) && weight > 0)
                {
                    totalWeight += weight;
                }
            }

            if (totalWeight <= 0)
            {
                return availableQualities[RandomGenerator.RandomNumber(0, availableQualities.Count)];
            }

            int roll = RandomGenerator.RandomNumber(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < availableQualities.Count; ++i)
            {
                int quality = availableQualities[i];
                int weight = qualityWeights.TryGetValue(quality, out int value) && value > 0 ? value : 0;
                cursor += weight;
                if (roll < cursor)
                {
                    return quality;
                }
            }

            return availableQualities[availableQualities.Count - 1];
        }

        private static void ApplyExtremeQualityPity(List<int> qualities, List<int> availableQualities, Dictionary<int, int> qualityWeights)
        {
            if (qualities == null || qualities.Count == 0)
            {
                return;
            }

            int firstQuality = qualities[0];
            if (firstQuality != MinQuality && firstQuality != MaxQuality)
            {
                return;
            }

            for (int i = 1; i < qualities.Count; ++i)
            {
                if (qualities[i] != firstQuality)
                {
                    return;
                }
            }

            List<int> alternatives = new List<int>();
            for (int i = 0; i < availableQualities.Count; ++i)
            {
                int quality = availableQualities[i];
                if (quality != firstQuality)
                {
                    alternatives.Add(quality);
                }
            }

            if (alternatives.Count == 0)
            {
                Log.Warning($"[Rogue] choice quality pity skipped: no alternative quality for all-{firstQuality} sequence.");
                return;
            }

            int replacement = RollQuality(alternatives, qualityWeights);
            int replaceIndex = RandomGenerator.RandomNumber(0, qualities.Count);
            qualities[replaceIndex] = replacement;
        }
    }
}
