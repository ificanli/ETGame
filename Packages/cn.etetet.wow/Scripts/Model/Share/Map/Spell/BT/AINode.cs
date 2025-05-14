namespace ET
{
    public abstract class AINode: BTAction
    {
        [BTInput(typeof(AIComponent))]
        public string AIComponent = "AIComponent";
    }
}