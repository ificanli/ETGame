namespace ET.Server
{
    public static class RogueOptionConfigHelper
    {
        public static bool TryGetExecutableEffectGroup(
            RogueRuntimeConfigCategory configCategory,
            RogueOptionConfig optionConfig,
            out int effectGroupId,
            out RogueEffectGroupConfig groupConfig)
        {
            effectGroupId = 0;
            groupConfig = null;
            if (optionConfig == null)
            {
                return false;
            }

            RogueRuntimeConfigCategory runtimeConfigCategory = configCategory ?? RogueRuntimeConfigCategory.Instance;
            if (runtimeConfigCategory == null || !optionConfig.TryGetEffectGroupId(out effectGroupId) || effectGroupId <= 0)
            {
                return false;
            }

            RogueEffectGroupLoader.RegisterAll();
            return runtimeConfigCategory.TryGetEffectGroup(effectGroupId, out groupConfig) && groupConfig != null;
        }

        public static bool TryGetBuffConfigId(RogueOptionConfig optionConfig, out int buffConfigId)
        {
            buffConfigId = 0;
            if (optionConfig == null)
            {
                return false;
            }

            RogueBuffConfigLoader.EnsureRegistered();
            if (TryGetExecutableEffectGroup(RogueRuntimeConfigCategory.Instance, optionConfig, out int effectGroupId, out RogueEffectGroupConfig groupConfig) &&
                TryResolveBuffConfigIdByEffectGroup(effectGroupId, groupConfig, out buffConfigId))
            {
                return true;
            }

            if (optionConfig.TryGetLegacyBuffConfigId(out int legacyBuffConfigId) &&
                legacyBuffConfigId > 0 &&
                BuffConfigCategory.Instance.Contain(legacyBuffConfigId))
            {
                buffConfigId = legacyBuffConfigId;
                return true;
            }

            return false;
        }

        public static bool TryGetPreviewBuffConfigId(RogueRuntimeConfigCategory configCategory, RogueOptionConfig optionConfig, out int buffConfigId)
        {
            buffConfigId = 0;
            if (optionConfig == null)
            {
                return false;
            }

            RogueBuffConfigLoader.EnsureRegistered();
            if (TryGetExecutableEffectGroup(configCategory, optionConfig, out int effectGroupId, out RogueEffectGroupConfig groupConfig) &&
                TryResolveBuffConfigIdByEffectGroup(effectGroupId, groupConfig, out buffConfigId))
            {
                return true;
            }

            if (optionConfig.TryGetLegacyBuffConfigId(out int legacyBuffConfigId) &&
                legacyBuffConfigId > 0 &&
                BuffConfigCategory.Instance.Contain(legacyBuffConfigId))
            {
                buffConfigId = legacyBuffConfigId;
                return true;
            }

            return false;
        }

        private static bool TryResolveBuffConfigIdByEffectGroup(int effectGroupId, RogueEffectGroupConfig groupConfig, out int buffConfigId)
        {
            buffConfigId = 0;
            if (effectGroupId <= 0 || groupConfig == null)
            {
                return false;
            }

            if (groupConfig.Entries != null)
            {
                foreach (RogueEffectEntryConfig entry in groupConfig.Entries)
                {
                    if (entry == null ||
                        entry.ExecuteType != RogueEffectExecuteType.AddBuff ||
                        entry.RefId <= 0 ||
                        !BuffConfigCategory.Instance.Contain(entry.RefId))
                    {
                        continue;
                    }

                    buffConfigId = entry.RefId;
                    return true;
                }
            }

            if (BuffConfigCategory.Instance.Contain(effectGroupId))
            {
                buffConfigId = effectGroupId;
                return true;
            }

            return RogueEffectGroupSyntheticBuffHelper.TryEnsureSyntheticBuffConfig(effectGroupId, groupConfig, out buffConfigId);
        }
    }
}
