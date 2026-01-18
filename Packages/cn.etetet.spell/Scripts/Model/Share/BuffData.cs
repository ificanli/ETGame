namespace ET
{
    // buff上下文,不会被dispose
    [ComponentOf(typeof(Buff))]
    public class BuffData: Entity, IAwake
    {
        public EntityRef<BuffData> ParentData;
    }
}