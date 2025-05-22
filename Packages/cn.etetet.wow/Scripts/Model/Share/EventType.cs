namespace ET.Client
{
    public struct SceneChangeStart
    {
        public string PreSceneName;
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
        public Unit Unit;
    }
    
    public struct AfterMyUnitCreate
    {
        public Unit Unit;
    }
}