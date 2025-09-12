namespace ET.Server
{
    public static class GateMapFactory
    {
        public static async ETTask<Scene> Create(Entity parent, long id, string name)
        {
            Scene scene = EntitySceneFactory.CreateScene(parent, id, SceneType.Map, name);
            scene.AddComponent<UnitComponent>();
            scene.AddComponent<AOIManagerComponent>();
            scene.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            
            await ETTask.CompletedTask;
            return scene;
        }
        
    }
}