namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitEnterCombat_RogueOutOfCombatStealth : AEvent<Scene, UnitEnterCombat>
    {
        protected override async ETTask Run(Scene scene, UnitEnterCombat args)
        {
            Unit unit = args.Unit;
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueOutOfCombatStealthStateComponent stealthState = unit.GetComponent<RogueOutOfCombatStealthStateComponent>();
            stealthState?.ClearConcealment();
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Map)]
    public class UnitLeaveCombat_RogueOutOfCombatStealth : AEvent<Scene, UnitLeaveCombat>
    {
        protected override async ETTask Run(Scene scene, UnitLeaveCombat args)
        {
            Unit unit = args.Unit;
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!RogueEffectQueryHelper.HasOutOfCombatStealth(unit))
            {
                RogueOutOfCombatStealthStateComponent staleState = unit.GetComponent<RogueOutOfCombatStealthStateComponent>();
                if (staleState != null)
                {
                    staleState.ClearConcealment();
                    unit.RemoveComponent<RogueOutOfCombatStealthStateComponent>();
                }
                await ETTask.CompletedTask;
                return;
            }

            RogueOutOfCombatStealthStateComponent stealthState = unit.GetComponent<RogueOutOfCombatStealthStateComponent>() ??
                    unit.AddComponent<RogueOutOfCombatStealthStateComponent>();
            stealthState.ApplyConcealment();
            await ETTask.CompletedTask;
        }
    }
}
