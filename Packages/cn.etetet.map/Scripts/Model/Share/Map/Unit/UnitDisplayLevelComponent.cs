namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class UnitDisplayLevelComponent : Entity, IAwake<int>, ITransfer
    {
        public int Level;
    }
}
