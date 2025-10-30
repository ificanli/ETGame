namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitDieEvent_TriggerKilled: AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            QuestEventHelper.OnMonsterKilled(a.Unit, a.Target.Entity.Id, 1);
            await ETTask.CompletedTask;
        }
    }
}