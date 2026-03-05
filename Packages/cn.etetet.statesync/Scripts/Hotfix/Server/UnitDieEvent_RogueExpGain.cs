namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitDieEvent_RogueExpGain : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            Unit killer = a.Unit;
            Unit target = a.Target;
            RogueProgressHelper.AddKillExp(killer, target);

            await ETTask.CompletedTask;
        }
    }
}
