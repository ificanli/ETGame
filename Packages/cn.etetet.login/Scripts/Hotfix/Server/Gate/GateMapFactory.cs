namespace ET.Server
{
    public static class GateMapFactory
    {
        public static async ETTask<Scene> Create(Entity parent, long id, long instanceId, string name)
        {
            Scene scene = EntitySceneFactory.CreateScene(parent, id, instanceId, SceneType.Map, name);
            scene.AddComponent<UnitComponent>();
            scene.AddComponent<AOIManagerComponent>();
            scene.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            
            await ETTask.CompletedTask;
            return scene;
        }
        
    }
}