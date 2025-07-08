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

    public struct AppStartInitFinish
    {
    }

    public struct LoginFinish
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