namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitDieEvent_PlayerCorpseLoot : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie args)
        {
            Unit target = args.Target;
            if (target != null && !target.IsDisposed)
            {
                PlayerCorpseLootHelper.TryCreateCorpse(target);
            }

            await ETTask.CompletedTask;
        }
    }
}
