namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_UnitDisplayLevelChangeHandler : MessageHandler<Scene, M2C_UnitDisplayLevelChange>
    {
        protected override async ETTask Run(Scene root, M2C_UnitDisplayLevelChange message)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>()?.Scene;
            Unit unit = currentScene?.GetComponent<UnitComponent>()?.Get(message.UnitId);
            if (unit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            UnitDisplayLevelComponent displayLevelComponent = unit.GetComponent<UnitDisplayLevelComponent>();
            if (displayLevelComponent == null)
            {
                displayLevelComponent = unit.AddComponent<UnitDisplayLevelComponent, int>(message.DisplayLevel);
            }
            else
            {
                displayLevelComponent.SetLevel(message.DisplayLevel);
            }

            await ETTask.CompletedTask;
        }
    }
}
