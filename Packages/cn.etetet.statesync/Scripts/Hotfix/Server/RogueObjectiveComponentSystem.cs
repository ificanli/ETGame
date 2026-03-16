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

        public static RogueObjective AddObjective(this RogueObjectiveComponent self, int optionId, int effectGroupId, int objectiveId, int goalValue)
        {
            RogueObjective objective = self.AddChild<RogueObjective>();
            objective.OptionId = optionId;
            objective.EffectGroupId = effectGroupId;
            objective.ObjectiveId = objectiveId;
            objective.GoalValue = goalValue;
            return objective;
        }

        public static RogueObjective AddObjective(this RogueObjectiveComponent self, int optionId, int effectGroupId, int objectiveId)
        {
            return self.AddObjective(optionId, effectGroupId, objectiveId, 0);
        }

        public static int RemoveOneObjectiveByOptionId(this RogueObjectiveComponent self, int optionId)
        {
            long targetObjectiveId = 0;
            foreach (long childId in self.Children.Keys)
            {
                RogueObjective objective = self.GetChild<RogueObjective>(childId);
                if (objective == null || objective.OptionId != optionId)
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

            self.RemoveChild(targetObjectiveId);
            return 1;
        }

        public static void ClearObjectives(this RogueObjectiveComponent self)
        {
            foreach (long childId in self.Children.Keys.ToArray())
            {
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

                self.RemoveChild(childId);
                ++removedCount;
            }

            return removedCount;
        }
    }
}
