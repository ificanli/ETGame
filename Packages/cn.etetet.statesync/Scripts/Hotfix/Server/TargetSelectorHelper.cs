using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 目标选择辅助工具类，提供自动索敌逻辑（服务端）。
    /// </summary>
    public static class TargetSelectorHelper
    {
        // 距离权重
        private const float DistanceWeight = 0.7f;
        // 血量权重
        private const float HpWeight = 0.3f;
        // 正在攻击自己的优先级加成（分数乘以此系数，越小越优先）
        private const float AttackMeBonus = 0.5f;
        private const long LineOfSightPassCacheMs = 120;
        private const int LineOfSightBlockedFailureThreshold = 2;

        /// <summary>
        /// 选择目标，有缓存机制避免频繁计算
        /// </summary>
        public static Unit SelectTarget(this TargetSelectorComponent self)
        {
            return SelectTarget(self, self?.MaxRange ?? 0f);
        }

        /// <summary>
        /// 在不修改 Selector.MaxRange 的前提下按指定范围选目标。
        /// </summary>
        public static Unit SelectTarget(this TargetSelectorComponent self, float maxRange)
        {
            if (self == null || self.IsDisposed)
            {
                return null;
            }

            Unit owner = self.GetParent<Unit>();
            if (owner == null || owner.IsDisposed)
            {
                return null;
            }

            // 检查选择间隔
            long now = TimeInfo.Instance.ServerNow();
            if (now - self.LastSelectTime < self.SelectIntervalMs && self.CurrentTargetId != 0)
            {
                Unit cachedTarget = owner.Scene().GetComponent<UnitComponent>().Get(self.CurrentTargetId);
                if (IsValidTarget(owner, cachedTarget, maxRange))
                {
                    return cachedTarget;
                }

                self.CurrentTargetId = 0;
            }

            self.LastSelectTime = now;

            // 自动选择最优目标
            Unit bestTarget = FindBestTarget(owner, maxRange);
            if (bestTarget != null)
            {
                self.CurrentTargetId = bestTarget.Id;
                return bestTarget;
            }

            self.CurrentTargetId = 0;
            return null;
        }

        public static Unit FindBestTarget(Unit owner, float maxRange)
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
                if (!HasDirectLineOfSight(owner, enemy))
                {
                    continue;
                }

                float distance = GetHorizontalDistance(owner.Position, enemy.Position);
                float score = CalculateScore(owner, enemy, distance, maxRange);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            if (bestTarget != null)
            {
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
                Log.Warning($"TargetSelector: unit {owner.Id} has no AOIEntity");
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

                bool isEnemy = CampHelper.IsEnemy(owner, target);

                if (!isEnemy)
                {
                    continue;
                }

                float distance = GetHorizontalDistance(owner.Position, target.Position);
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
            if (!PassesBasicTargetRules(owner, target, maxRange))
            {
                return false;
            }

            TargetSelectorComponent selector = owner.GetComponent<TargetSelectorComponent>();
            if (selector == null)
            {
                return HasDirectLineOfSight(owner, target);
            }

            long now = TimeInfo.Instance.ServerNow();
            if (selector.LastLineOfSightTargetId == target.Id &&
                selector.LastLineOfSightPassed &&
                now - selector.LastLineOfSightCheckTime < LineOfSightPassCacheMs)
            {
                return true;
            }

            bool passed = HasDirectLineOfSight(owner, target);
            if (passed)
            {
                selector.LastLineOfSightCheckTime = now;
                selector.LastLineOfSightTargetId = target.Id;
                selector.LastLineOfSightPassed = true;
                selector.ConsecutiveLineOfSightBlockedCount = 0;
                return true;
            }

            if (selector.LastLineOfSightTargetId != target.Id)
            {
                selector.ConsecutiveLineOfSightBlockedCount = 0;
            }

            selector.LastLineOfSightCheckTime = now;
            selector.LastLineOfSightTargetId = target.Id;
            selector.LastLineOfSightPassed = false;
            selector.ConsecutiveLineOfSightBlockedCount++;
            return selector.ConsecutiveLineOfSightBlockedCount < LineOfSightBlockedFailureThreshold;
        }

        private static bool PassesBasicTargetRules(Unit owner, Unit target, float maxRange)
        {
            if (owner == null || owner.IsDisposed || target == null || target.IsDisposed)
            {
                return false;
            }

            if (!IsVisibleInAoi(owner, target))
            {
                return false;
            }

            if (!CampHelper.IsEnemy(owner, target))
            {
                return false;
            }

            float distance = GetHorizontalDistance(owner.Position, target.Position);
            if (distance > maxRange)
            {
                return false;
            }

            return IsAlive(target);
        }

        private static bool HasDirectLineOfSight(Unit owner, Unit target)
        {
            if (owner == null || target == null || owner.IsDisposed || target.IsDisposed)
            {
                return false;
            }

            return VisibilityLineOfSightHelper.HasLineOfSight(owner, target);
        }

        private static bool IsVisibleInAoi(Unit owner, Unit target)
        {
            AOIEntity ownerAoi = owner.GetComponent<AOIEntity>();
            if (ownerAoi == null)
            {
                Log.Warning($"TargetSelector: unit {owner.Id} has no AOIEntity");
                return false;
            }

            return ownerAoi.GetSeeUnits().ContainsKey(target.Id);
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

        private static float GetHorizontalDistance(float3 a, float3 b)
        {
            return math.distance(new float2(a.x, a.z), new float2(b.x, b.z));
        }
    }
}
