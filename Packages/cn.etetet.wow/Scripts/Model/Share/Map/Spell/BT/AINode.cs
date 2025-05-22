namespace ET
{
    public abstract class AINode: BTAction
    {
        [BTInput(typeof(Unit))]
        public string Unit = "Unit";
    }
}