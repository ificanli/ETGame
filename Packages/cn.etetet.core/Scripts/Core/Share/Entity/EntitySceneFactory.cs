namespace ET
{
    public static class EntitySceneFactory
    {
        public static Scene CreateScene(Entity parent, long id, int sceneType, string name)
        {
            Scene scene = new(parent.Fiber(), id, sceneType, name);
            parent?.AddChild(scene);
            return scene;
        }
    }
}