namespace ET
{
    // 客户端挂在ClientScene上，服务端挂在Unit上
    [ComponentOf]
    public class AIComponent: Entity, IAwake<int>, IDestroy
    {
        public int AIConfigId { get; set; }
        
        public ETCancellationToken CancellationToken;

        public long Timer;

        public int Current;
    }
}