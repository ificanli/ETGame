using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 目标选择辅助工具类，提供自动索敌逻辑（服务端）。
    /// 选择优先级：手动目标 > 自动（距离权重0.7 + 血量权重0.3 + 攻击自己加成）
    /// </summary>
    public static class TargetSelectorHelper
    {
        // 距离权重
        private const float DistanceWeight = 0.7f;
        // 血量权重
        private const float HpWeight = 0.3f;
        // 正在攻击自己的优先级加成（分数乘以此系数，越小越优先）
        private const float AttackMeBonus = 0.5f;

        /// <summary>
        /// 选择目标，有缓存机制避免频繁计算
        /// </summary>
        public static Unit SelectTarget(this TargetSelectorComponent self)
        {
            Unit owner = self.GetParent<Unit>();

            // 检查选择间隔
            long now = TimeInfo.Instance.ServerNow();
            if (now - self.LastSelectTime < self.SelectIntervalMs && self.CurrentTargetId != 0)
            {
                return owner.Scene().GetComponent<UnitComponent>().Get(self.CurrentTargetId);
            }

            self.LastSelectTime = now;

            // 优先使用手动指定的目标
            if (self.ManualTargetId != 0)
            {
                Unit manualTarget = owner.Scene().GetComponent<UnitComponent>().Get(self.ManualTargetId);
                if (IsValidTarget(owner, manualTarget, self.MaxRange))
                {
                    self.CurrentTargetId = self.ManualTargetId;
                    return manualTarget;
                }

                self.ManualTargetId = 0;
            }

            // 自动选择最优目标
            Unit bestTarget = FindBestTarget(owner, self.MaxRange);
            if (bestTarget != null)
            {
                self.CurrentTargetId = bestTarget.Id;
                return bestTarget;
            }

            self.CurrentTargetId = 0;
            return null;
        }

        private static Unit FindBestTarget(Unit owner, float maxRange)
        {
            List<Unit> enemies = GetEnemiesInRange(owner, maxRange);
            if (enemies.Count == 0)
            {
                return null;
            }

            Unit bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (Unit enemy in enemies)
            {
                float distance = math.distance(owner.Position, enemy.Position);
                float score = CalculateScore(owner, enemy, distance, maxRange);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private static float CalculateScore(Unit owner, Unit enemy, float distance, float maxRange)
        {
            float hpPercent = GetHpPercent(enemy);
            float score = distance * DistanceWeight + hpPercent * maxRange * HpWeight;

            if (IsAttackingMe(enemy, owner))
            {
                score *= AttackMeBonus;
            }

            return score;
        }

        private static List<Unit> GetEnemiesInRange(Unit owner, float range)
        {
            List<Unit> result = new List<Unit>();

            AOIEntity ownerAoi = owner.GetComponent<AOIEntity>();
            if (ownerAoi == null)
            {
                return result;
            }

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = ownerAoi.GetSeeUnits();

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                if (aoiEntity == null)
                {
                    continue;
                }

                Unit target = aoiEntity.Unit;
                if (target == null || target.IsDisposed)
                {
                    continue;
                }

                if (target.Id == owner.Id)
                {
                    continue;
                }

                if (!CampHelper.IsEnemy(owner, target))
                {
                    continue;
                }

                float distance = math.distance(owner.Position, target.Position);
                if (distance > range)
                {
                    continue;
                }

                if (!IsAlive(target))
                {
                    continue;
                }

                result.Add(target);
            }

            return result;
        }

        public static bool IsValidTarget(Unit owner, Unit target, float maxRange)
        {
            if (target == null || target.IsDisposed)
            {
                return false;
            }

            if (!CampHelper.IsEnemy(owner, target))
            {
                return false;
            }

            float distance = math.distance(owner.Position, target.Position);
            if (distance > maxRange)
            {
                return false;
            }

            return IsAlive(target);
        }

        private static bool IsAlive(Unit target)
        {
            NumericComponent numeric = target.NumericComponent;
            if (numeric == null)
            {
                return true;
            }

            return numeric.GetAsFloat(NumericType.HP) > 0;
        }

        private static float GetHpPercent(Unit target)
        {
            NumericComponent numeric = target.NumericComponent;
            if (numeric == null)
            {
                return 1f;
            }

            float hp = numeric.GetAsFloat(NumericType.HP);
            float maxHp = numeric.GetAsFloat(NumericType.MaxHP);
            return maxHp > 0 ? hp / maxHp : 0f;
        }

        private static bool IsAttackingMe(Unit enemy, Unit me)
        {
            TargetSelectorComponent enemySelector = enemy.GetComponent<TargetSelectorComponent>();
            if (enemySelector == null)
            {
                return false;
            }

            return enemySelector.CurrentTargetId == me.Id;
        }
    }
}
