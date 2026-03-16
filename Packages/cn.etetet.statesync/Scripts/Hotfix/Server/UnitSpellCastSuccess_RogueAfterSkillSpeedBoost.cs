namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitSpellCastSuccess_RogueAfterSkillSpeedBoost : AEvent<Scene, UnitSpellCastSuccess>
    {
        protected override async ETTask Run(Scene scene, UnitSpellCastSuccess args)
        {
            Unit unit = args.Unit;
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueEffectQueryHelper.GetAfterSkillSpeedBoostData(unit, out int totalSpeedPct, out int maxDurationMs);

            if (totalSpeedPct <= 0 || maxDurationMs <= 0)
            {
                RogueAfterSkillSpeedBoostComponent stale = unit.GetComponent<RogueAfterSkillSpeedBoostComponent>();
                if (stale != null)
                {
                    unit.RemoveComponent<RogueAfterSkillSpeedBoostComponent>();
                }

                await ETTask.CompletedTask;
                return;
            }

            RogueAfterSkillSpeedBoostComponent speedBoost = unit.GetComponent<RogueAfterSkillSpeedBoostComponent>() ??
                    unit.AddComponent<RogueAfterSkillSpeedBoostComponent>();
            speedBoost.RefreshSpeedBoost(totalSpeedPct, maxDurationMs);
            await ETTask.CompletedTask;
        }
    }
}
