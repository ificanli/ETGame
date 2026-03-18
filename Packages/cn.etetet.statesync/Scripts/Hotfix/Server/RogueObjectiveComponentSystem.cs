using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(RogueObjectiveComponent))]
    public static partial class RogueObjectiveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueObjectiveComponent self)
        {
        }

        public static RogueObjective AddObjective(this RogueObjectiveComponent self, int optionId, int effectGroupId, int objectiveId, int goalValue, int rewardBuffConfigId = 0)
        {
            RogueObjective objective = self.AddChild<RogueObjective>();
            objective.OptionId = optionId;
            objective.EffectGroupId = effectGroupId;
            objective.ObjectiveId = objectiveId;
            objective.GoalValue = goalValue;
            objective.RewardBuffConfigId = rewardBuffConfigId;
            return objective;
        }

        public static RogueObjective AddObjective(this RogueObjectiveComponent self, int optionId, int effectGroupId, int objectiveId)
        {
            return self.AddObjective(optionId, effectGroupId, objectiveId, 0, 0);
        }

        public static int RemoveOneObjectiveByOptionId(this RogueObjectiveComponent self, int optionId)
        {
            long targetObjectiveId = 0;
            foreach (long childId in self.Children.Keys)
            {
                RogueObjective currentObjective = self.GetChild<RogueObjective>(childId);
                if (currentObjective == null || currentObjective.OptionId != optionId)
                {
                    continue;
                }

                if (targetObjectiveId == 0 || childId > targetObjectiveId)
                {
                    targetObjectiveId = childId;
                }
            }

            if (targetObjectiveId == 0)
            {
                return 0;
            }

            RogueObjective targetObjective = self.GetChild<RogueObjective>(targetObjectiveId);
            CleanupObjectiveReward(self, targetObjective);
            self.RemoveChild(targetObjectiveId);
            return 1;
        }

        public static void ClearObjectives(this RogueObjectiveComponent self)
        {
            foreach (long childId in self.Children.Keys.ToArray())
            {
                RogueObjective objective = self.GetChild<RogueObjective>(childId);
                CleanupObjectiveReward(self, objective);
                self.RemoveChild(childId);
            }
        }

        public static int RemoveObjectivesByEffectGroupId(this RogueObjectiveComponent self, int effectGroupId)
        {
            if (effectGroupId <= 0)
            {
                return 0;
            }

            int removedCount = 0;
            foreach (long childId in self.Children.Keys.ToArray())
            {
                RogueObjective objective = self.GetChild<RogueObjective>(childId);
                if (objective == null || objective.EffectGroupId != effectGroupId)
                {
                    continue;
                }

                CleanupObjectiveReward(self, objective);
                self.RemoveChild(childId);
                ++removedCount;
            }

            return removedCount;
        }

        private static void CleanupObjectiveReward(RogueObjectiveComponent self, RogueObjective objective)
        {
            if (self == null || self.IsDisposed || objective == null || objective.RewardBuffId <= 0)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            BuffComponent buffComponent = unit?.GetComponent<BuffComponent>();
            Buff rewardBuff = buffComponent?.GetChild<Buff>(objective.RewardBuffId);
            if (rewardBuff != null)
            {
                BuffHelper.RemoveBuff(rewardBuff, BuffFlags.NoDurationRemove);
            }

            RogueProgressComponent progress = unit?.GetComponent<RogueProgressComponent>();
            progress?.AppliedBuffIds.Remove(objective.RewardBuffId);
            objective.RewardBuffId = 0;
        }
    }
}
