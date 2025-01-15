namespace ET.Server
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_NumericChangeHandler: MessageHandler<Scene, M2C_NumericChange>
    {
        protected override async ETTask Run(Scene root, M2C_NumericChange message)
        {
            Unit unit = root.GetComponent<CurrentScenesComponent>().Scene.GetComponent<UnitComponent>().Get(message.UnitId);
            unit.GetComponent<NumericComponent>().Set(message.NumericType, message.Value);
            
            await ETTask.CompletedTask;
        }
    }
}