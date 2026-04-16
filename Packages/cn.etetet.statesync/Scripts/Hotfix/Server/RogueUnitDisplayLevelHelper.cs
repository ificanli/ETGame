using System;

namespace ET.Server
{
    /// <summary>
    /// 统一维护单位头顶显示等级。
    /// 玩家显示局内等级，怪物显示当前存活玩家平均等级。
    /// </summary>
    public static class RogueUnitDisplayLevelHelper
    {
        public static void RefreshPlayerDisplayLevel(Unit player, RogueProgressComponent progress, bool refreshMonsterLevels, bool notifyClients = true)
        {
            if (player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            progress ??= player.GetComponent<RogueProgressComponent>();
            int displayLevel = progress?.Level ?? 1;
            SetUnitDisplayLevel(player, displayLevel, notifyClients);

            if (refreshMonsterLevels)
            {
                RefreshMonsterDisplayLevels(player.Scene(), true);
            }
        }

        public static void RefreshMonsterDisplayLevel(Unit monster, bool notifyClients)
        {
            if (monster == null || monster.IsDisposed || monster.UnitType != UnitType.Monster)
            {
                return;
            }

            int averageLevel = GetAlivePlayerAverageLevel(monster.Scene());
            SetUnitDisplayLevel(monster, averageLevel, notifyClients);
        }

        public static void RefreshMonsterDisplayLevels(Scene scene, bool notifyClients)
        {
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            int averageLevel = GetAlivePlayerAverageLevel(scene);
            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Monster)
                {
                    continue;
                }

                SetUnitDisplayLevel(unit, averageLevel, notifyClients);
            }
        }

        private static int GetAlivePlayerAverageLevel(Scene scene)
        {
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return 1;
            }

            int alivePlayerCount = 0;
            int totalLevel = 0;

            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
                {
                    continue;
                }

                NumericComponent numeric = unit.NumericComponent;
                if (numeric == null || numeric.GetAsLong(NumericType.HP) <= 0)
                {
                    continue;
                }

                RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
                int level = progress?.Level ?? unit.GetComponent<UnitDisplayLevelComponent>()?.Level ?? 1;
                totalLevel += Math.Max(level, 1);
                ++alivePlayerCount;
            }

            if (alivePlayerCount <= 0)
            {
                return 1;
            }

            return Math.Max(totalLevel / alivePlayerCount, 1);
        }

        /// <summary>
        /// 怪物等级变化时，按 RogueLevelNumericEntry (UnitType=2) 应用属性增量。
        /// 支持升级（正向累加）和降级（反向回退）。
        /// </summary>
        private static void ApplyMonsterLevelNumerics(Unit unit, int oldLevel, int newLevel)
        {
            if (unit == null || unit.IsDisposed || oldLevel == newLevel)
            {
                return;
            }

            NumericComponent numeric = unit.NumericComponent;
            if (numeric == null)
            {
                return;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                return;
            }

            int monsterUnitType = (int)UnitType.Monster;

            if (newLevel > oldLevel)
            {
                // 升级：累加 oldLevel+1 ~ newLevel 的增量
                for (int lv = oldLevel + 1; lv <= newLevel; lv++)
                {
                    if (!configCategory.TryGetLevel(lv, out RogueLevelConfig levelConfig) || levelConfig.NumericDeltas == null)
                    {
                        continue;
                    }

                    foreach (RogueNumericConfig delta in levelConfig.NumericDeltas)
                    {
                        if (delta == null || delta.NumericType <= 0 || delta.Value == 0)
                        {
                            continue;
                        }

                        if (delta.UnitType != 0 && delta.UnitType != monsterUnitType)
                        {
                            continue;
                        }

                        long current = numeric.GetAsLong(delta.NumericType);
                        numeric.Set(delta.NumericType, current + delta.Value);
                    }
                }
            }
            else
            {
                // 降级：回退 oldLevel ~ newLevel+1 的增量
                for (int lv = oldLevel; lv > newLevel; lv--)
                {
                    if (!configCategory.TryGetLevel(lv, out RogueLevelConfig levelConfig) || levelConfig.NumericDeltas == null)
                    {
                        continue;
                    }

                    foreach (RogueNumericConfig delta in levelConfig.NumericDeltas)
                    {
                        if (delta == null || delta.NumericType <= 0 || delta.Value == 0)
                        {
                            continue;
                        }

                        if (delta.UnitType != 0 && delta.UnitType != monsterUnitType)
                        {
                            continue;
                        }

                        long current = numeric.GetAsLong(delta.NumericType);
                        numeric.Set(delta.NumericType, current - delta.Value);
                    }
                }
            }

            Log.Info($"[RogueMonsterGrowth] applied, unitId={unit.Id}, oldLevel={oldLevel}, newLevel={newLevel}");
        }

        private static void SetUnitDisplayLevel(Unit unit, int displayLevel, bool notifyClients)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            displayLevel = Math.Max(displayLevel, 1);

            UnitDisplayLevelComponent displayLevelComponent = unit.GetComponent<UnitDisplayLevelComponent>();
            int oldLevel = displayLevelComponent?.Level ?? 0;
            if (displayLevelComponent == null)
            {
                displayLevelComponent = unit.AddComponent<UnitDisplayLevelComponent, int>(displayLevel);
            }
            else if (oldLevel != displayLevel)
            {
                displayLevelComponent.SetLevel(displayLevel);
            }

            if (oldLevel == displayLevel)
            {
                return;
            }

            // 怪物等级变化时应用属性成长
            if (unit.UnitType == UnitType.Monster)
            {
                ApplyMonsterLevelNumerics(unit, oldLevel, displayLevel);
            }

            if (!notifyClients)
            {
                return;
            }

            M2C_UnitDisplayLevelChange message = M2C_UnitDisplayLevelChange.Create();
            message.UnitId = unit.Id;
            message.DisplayLevel = displayLevel;
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Broadcast);
        }
    }
}
