namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class BuffTickComponent: Entity, IAwake, IDestroy
    {
        public long TimerId;
        
        public bool Override;
        
        public ETCancellationToken CancellationToken;
        public int Current;
        public int HashCode;
        public long UnitId;
    }
}

