namespace ET.Server
{
    public static class MonsterRuntimeProfileHelper
    {
        public const string BladeCatGroupId = "blade_cat";
        public const string ScoutMonkeyGroupId = "scout_monkey";
        public const string PhantomOwlGroupId = "phantom_owl";
        public const string HeavyGatorGroupId = "heavy_gator";
        public const string BalooGroupId = "baloo";

        public const int BladeCatUnitConfigId = 1012;
        public const int ScoutMonkeyUnitConfigId = 1013;
        public const int PhantomOwlUnitConfigId = 1014;
        public const int HeavyGatorUnitConfigId = 1015;
        public const int BalooUnitConfigId = 1016;

        private const int BladeCatAiBuffId = 330101;
        private const int ScoutMonkeyAiBuffId = 330102;
        private const int PhantomOwlAiBuffId = 330103;
        private const int HeavyGatorAiBuffId = 330104;
        private const int BalooAiBuffId = 330105;

        public static bool TryResolveUnitConfigId(string groupId, out int unitConfigId)
        {
            unitConfigId = 0;
            return TryNormalizeGroupId(groupId, out string normalizedGroupId) &&
                    TryGetUnitConfigIdByGroupId(normalizedGroupId, out unitConfigId);
        }

        public static bool IsMonster(Unit unit)
        {
            return unit != null && !unit.IsDisposed && unit.UnitType == UnitType.Monster;
        }

        public static bool IsElite(Unit unit)
        {
            return TryGetProfileGroupId(unit, out string groupId) &&
                    string.Equals(groupId, HeavyGatorGroupId, System.StringComparison.Ordinal);
        }

        public static bool IsBoss(Unit unit)
        {
            return TryGetProfileGroupId(unit, out string groupId) &&
                    string.Equals(groupId, BalooGroupId, System.StringComparison.Ordinal);
        }

        public static bool IsEliteOrBoss(Unit unit)
        {
            return IsElite(unit) || IsBoss(unit);
        }

        public static bool MatchesCombatFilter(Unit unit, int filter)
        {
            return filter switch
            {
                RogueUnitFilterType.None => true,
                RogueUnitFilterType.Monster => IsMonster(unit),
                RogueUnitFilterType.Boss => IsBoss(unit),
                RogueUnitFilterType.EliteOrBoss => IsEliteOrBoss(unit),
                RogueUnitFilterType.NonBossMonster => IsMonster(unit) && !IsBoss(unit),
                _ => true,
            };
        }

        private static bool TryNormalizeGroupId(string groupId, out string normalizedGroupId)
        {
            normalizedGroupId = null;
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return false;
            }

            switch (groupId.Trim().ToLowerInvariant())
            {
                case BladeCatGroupId:
                case "影刃猫":
                    normalizedGroupId = BladeCatGroupId;
                    return true;
                case ScoutMonkeyGroupId:
                case "侦察猴":
                case "捣蛋侦察猴":
                    normalizedGroupId = ScoutMonkeyGroupId;
                    return true;
                case PhantomOwlGroupId:
                case "幻影猫头鹰":
                    normalizedGroupId = PhantomOwlGroupId;
                    return true;
                case HeavyGatorGroupId:
                case "重装鳄鱼":
                    normalizedGroupId = HeavyGatorGroupId;
                    return true;
                case BalooGroupId:
                case "巴鲁":
                case "机械巨熊巴鲁":
                case "机械巨熊·巴鲁":
                    normalizedGroupId = BalooGroupId;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetProfileGroupId(Unit unit, out string groupId)
        {
            groupId = null;
            if (!IsMonster(unit))
            {
                return false;
            }

            int aiBuffId = unit.NumericComponent?.GetAsInt(NumericType.AI) ?? 0;
            if (TryGetGroupIdByAIBuffId(aiBuffId, out groupId))
            {
                return true;
            }

            if (TryGetGroupIdByUnitConfigId(unit.ConfigId, out groupId))
            {
                return true;
            }

            UnitConfig config = UnitConfigCategory.Instance.GetOrDefault(unit.ConfigId);
            return config != null && TryNormalizeGroupId(config.Name, out groupId);
        }

        private static bool TryGetUnitConfigIdByGroupId(string normalizedGroupId, out int unitConfigId)
        {
            unitConfigId = 0;
            switch (normalizedGroupId)
            {
                case BladeCatGroupId:
                    unitConfigId = BladeCatUnitConfigId;
                    return true;
                case ScoutMonkeyGroupId:
                    unitConfigId = ScoutMonkeyUnitConfigId;
                    return true;
                case PhantomOwlGroupId:
                    unitConfigId = PhantomOwlUnitConfigId;
                    return true;
                case HeavyGatorGroupId:
                    unitConfigId = HeavyGatorUnitConfigId;
                    return true;
                case BalooGroupId:
                    unitConfigId = BalooUnitConfigId;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetGroupIdByUnitConfigId(int unitConfigId, out string groupId)
        {
            groupId = null;
            switch (unitConfigId)
            {
                case BladeCatUnitConfigId:
                    groupId = BladeCatGroupId;
                    return true;
                case ScoutMonkeyUnitConfigId:
                    groupId = ScoutMonkeyGroupId;
                    return true;
                case PhantomOwlUnitConfigId:
                    groupId = PhantomOwlGroupId;
                    return true;
                case HeavyGatorUnitConfigId:
                    groupId = HeavyGatorGroupId;
                    return true;
                case BalooUnitConfigId:
                    groupId = BalooGroupId;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetGroupIdByAIBuffId(int aiBuffId, out string groupId)
        {
            groupId = null;
            switch (aiBuffId)
            {
                case BladeCatAiBuffId:
                    groupId = BladeCatGroupId;
                    return true;
                case ScoutMonkeyAiBuffId:
                    groupId = ScoutMonkeyGroupId;
                    return true;
                case PhantomOwlAiBuffId:
                    groupId = PhantomOwlGroupId;
                    return true;
                case HeavyGatorAiBuffId:
                    groupId = HeavyGatorGroupId;
                    return true;
                case BalooAiBuffId:
                    groupId = BalooGroupId;
                    return true;
                default:
                    return false;
            }
        }
    }
}
