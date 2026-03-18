namespace ET.Server
{
    public static class RogueObjectiveHelper
    {
        /// <summary>
        /// 击杀事件触发任务进度推进。
        /// </summary>
        public static void OnKill(Unit killer, RogueObjectiveComponent objectiveComponent, int targetUnitType)
        {
            if (killer == null || killer.IsDisposed || objectiveComponent == null)
            {
                return;
            }

            foreach (Entity entity in objectiveComponent.Children.Values)
            {
                RogueObjective objective = entity as RogueObjective;
                if (objective == null || objective.Completed)
                {
                    continue;
                }

                // ObjectiveId 暂时用作目标类型过滤：0 = 任意怪物，>0 = 指定 UnitType
                if (objective.ObjectiveId > 0 && objective.ObjectiveId != targetUnitType)
                {
                    continue;
                }

                objective.Progress += 1;

                int goalValue = GetGoalValue(objective);
                if (goalValue > 0 && objective.Progress >= goalValue && !objective.Completed)
                {
                    objective.Completed = true;
                    OnObjectiveCompleted(killer, objective);
                }
            }
        }

        private static int GetGoalValue(RogueObjective objective)
        {
            if (objective == null)
            {
                return 0;
            }

            if (objective.GoalValue > 0)
            {
                return objective.GoalValue;
            }

            RogueObjectiveComponent objectiveComponent = objective.GetParent<RogueObjectiveComponent>();
            Unit unit = objectiveComponent?.GetParent<Unit>();
            RogueEffectRuntimeComponent runtimeComponent = unit?.GetComponent<RogueEffectRuntimeComponent>();
            if (runtimeComponent == null)
            {
                return 0;
            }

            foreach (Entity entity in runtimeComponent.Children.Values)
            {
                RogueEffectRuntime runtime = entity as RogueEffectRuntime;
                if (runtime == null ||
                    runtime.EffectGroupId != objective.EffectGroupId ||
                    runtime.ExecuteType != RogueEffectExecuteType.RegisterObjective ||
                    runtime.RefId != objective.ObjectiveId ||
                    runtime.Value1 <= 0)
                {
                    continue;
                }

                return runtime.Value1;
            }

            return 0;
        }

        private static void OnObjectiveCompleted(Unit unit, RogueObjective objective)
        {
            if (unit == null || unit.IsDisposed || objective == null || objective.RewardClaimed)
            {
                return;
            }

            objective.RewardClaimed = true;

            if (objective.RewardBuffConfigId > 0 && objective.RewardBuffId == 0)
            {
                BuffConfig rewardBuffConfig = BuffConfigCategory.Instance.Get(objective.RewardBuffConfigId);
                if (rewardBuffConfig != null)
                {
                    Buff rewardBuff = BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), objective.RewardBuffConfigId, null);
                    if (rewardBuff != null)
                    {
                        objective.RewardBuffId = rewardBuff.Id;
                        RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
                        progress?.AppliedBuffIds.Add(rewardBuff.Id);
                    }
                }
                else
                {
                    Log.Warning($"[RogueObjective] reward buff config missing, unitId={unit.Id}, objectiveId={objective.ObjectiveId}, rewardBuffConfigId={objective.RewardBuffConfigId}");
                }
            }

            Log.Info($"[RogueObjective] objective completed, unitId={unit.Id}, objectiveId={objective.ObjectiveId}, progress={objective.Progress}");
        }
    }
}
