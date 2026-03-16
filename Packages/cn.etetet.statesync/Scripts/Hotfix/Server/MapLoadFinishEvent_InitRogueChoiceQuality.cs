namespace ET.Server
{
    [Event(SceneType.Map)]
    public class MapLoadFinishEvent_InitRogueChoiceQuality : AEvent<Scene, MapLoadFinishEvent>
    {
        protected override ETTask Run(Scene scene, MapLoadFinishEvent args)
        {
            RogueBuffConfigLoader.EnsureRegistered();
            return ETTask.CompletedTask;
        }
    }
}
