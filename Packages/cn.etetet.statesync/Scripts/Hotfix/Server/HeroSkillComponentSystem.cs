using Unity.Mathematics;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(HeroSkillComponent))]
    public static partial class HeroSkillComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HeroSkillComponent self, int heroConfigId)
        {
            self.HeroConfigId = heroConfigId;

            // 根据英雄ID初始化技能参数
            // TODO: 从配置表读取，这里先硬编码桃子的技能参数
            if (heroConfigId == 1001) // 假设桃子的 HeroConfigId 是 1001
            {
                self.HealRange = 5f; // 5米范围
                self.HealIntervalMs = 1000; // 每秒治疗一次
                self.HealAmount = 10f; // 每次治疗10点
            }

            // 启动技能定时器
            self.StartSkillTimer();
        }

        [EntitySystem]
        private static void Destroy(this HeroSkillComponent self)
        {
            self.StopSkillTimer();
        }

        /// <summary>
        /// 启动技能定时器
        /// </summary>
        private static void StartSkillTimer(this HeroSkillComponent self)
        {
            if (self.HealIntervalMs <= 0) return;

            self.SkillTimerId = self.Root().TimerComponent.NewRepeatedTimer(
                self.HealIntervalMs,
                TimerInvokeType.HeroSkillTimer,
                self
            );
        }

        /// <summary>
        /// 停止技能定时器
        /// </summary>
        private static void StopSkillTimer(this HeroSkillComponent self)
        {
            if (self.SkillTimerId != 0)
            {
                self.Root()?.TimerComponent?.Remove(ref self.SkillTimerId);
            }
        }

        /// <summary>
        /// 重启技能定时器（用于 Unit 跨场景传送后重新注册 Timer）
        /// </summary>
        public static void RestartSkillTimer(this HeroSkillComponent self)
        {
            self.StopSkillTimer();
            self.StartSkillTimer();
        }

        /// <summary>
        /// 技能定时器触发（每秒执行一次）
        /// </summary>
        public static void OnSkillTick(this HeroSkillComponent self)
        {
            Unit owner = self.GetParent<Unit>();
            if (owner == null || owner.IsDisposed)
            {
                return;
            }

            // 获取范围内的友方单位
            var friendlyUnits = self.GetFriendlyUnitsInRange(owner);

            // 对每个友方单位施加治疗
            foreach (Unit target in friendlyUnits)
            {
                self.HealTarget(target);
            }
        }

        /// <summary>
        /// 获取范围内的友方单位
        /// </summary>
        private static List<Unit> GetFriendlyUnitsInRange(this HeroSkillComponent self, Unit owner)
        {
            List<Unit> result = new List<Unit>();

            AOIEntity ownerAOI = owner.GetComponent<AOIEntity>();
            if (ownerAOI == null)
            {
                return result;
            }

            // 遍历视野内的单位
            Dictionary<long, EntityRef<AOIEntity>> seeUnits = ownerAOI.GetSeeUnits();
            foreach ((long _, EntityRef<AOIEntity> aoiEntityRef) in seeUnits)
            {
                AOIEntity aoiEntity = aoiEntityRef;
                if (aoiEntity == null) continue;

                Unit target = aoiEntity.Unit;
                if (target == null || target.IsDisposed) continue;

                // 检查是否友方
                if (!CampHelper.IsFriendly(owner, target))
                {
                    continue;
                }

                // 检查距离
                float distance = math.distance(owner.Position, target.Position);
                if (distance <= self.HealRange)
                {
                    result.Add(target);
                }
            }

            return result;
        }

        /// <summary>
        /// 治疗目标
        /// </summary>
        private static void HealTarget(this HeroSkillComponent self, Unit target)
        {
            NumericComponent targetNumeric = target.NumericComponent;
            if (targetNumeric == null)
            {
                return;
            }

            // 获取当前HP和最大HP
            float currentHp = targetNumeric.GetAsFloat(NumericType.HP);
            float maxHp = targetNumeric.GetAsFloat(NumericType.MaxHP);

            // 如果已满血，不治疗
            if (currentHp >= maxHp)
            {
                return;
            }

            // 增加HP
            float newHp = math.min(maxHp, currentHp + self.HealAmount);
            targetNumeric.Set(NumericType.HP, (long)newHp);

            Log.Debug($"HeroSkill: Healed unit {target.Id} for {self.HealAmount}, HP: {currentHp} -> {newHp}");
        }
    }
}
