namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_SpellInterruptHandler: MessageHandler<Scene, M2C_SpellInterrupt>
    {
        protected override async ETTask Run(Scene root, M2C_SpellInterrupt message)
        {
            await ETTask.CompletedTask;
        }
    }
}