namespace ET.Client
{
    public struct SceneChangeStart
    {
        public bool ChangeScene;
    }
    
    public struct SceneChangeFinish
    {
    }
    
    public struct AfterCreateClientScene
    {
    }
    
    public struct AfterCreateCurrentScene
    {
    }
    
    public struct AppStartInit
    {
    }

    public struct AppStartInitFinish
    {
    }
    
    public struct EnterMapFinish
    {
    }

    public struct AfterUnitCreate
    {
        public EntityRef<Unit> Unit;
    }
    
    public struct AfterMyUnitCreate
    {
        public EntityRef<Unit> Unit;
    }
}