namespace ET
{
    public abstract class AIRoot: BTRoot
    {
        [BTOutput(typeof(AIComponent))]
        public string AIComponent;
    }
}