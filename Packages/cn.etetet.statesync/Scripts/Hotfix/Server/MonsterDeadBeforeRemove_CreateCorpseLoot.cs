namespace ET.Server
{
    [Event(SceneType.Map)]
    public class MonsterDeadBeforeRemove_CreateCorpseLoot : AEvent<Scene, MonsterDeadBeforeRemove>
    {
        protected override async ETTask Run(Scene scene, MonsterDeadBeforeRemove args)
        {
            Unit monster = args.Unit;
            if (monster != null && !monster.IsDisposed)
            {
                MonsterCorpseLootHelper.TryCreateCorpse(monster);
            }

            await ETTask.CompletedTask;
        }
    }
}
