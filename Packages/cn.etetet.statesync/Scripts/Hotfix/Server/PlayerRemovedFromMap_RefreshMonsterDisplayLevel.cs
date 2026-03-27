namespace ET.Server
{
    [Event(SceneType.Map)]
    public class PlayerRemovedFromMap_RefreshMonsterDisplayLevel : AEvent<Scene, PlayerRemovedFromMap>
    {
        protected override async ETTask Run(Scene scene, PlayerRemovedFromMap args)
        {
            if (scene == null || scene.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevels(scene, true);
            await ETTask.CompletedTask;
        }
    }
}
