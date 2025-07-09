using System.Net;
using System.Net.Sockets;

namespace ET
{
    public struct NetComponentOnRead
    {
        public EntityRef<Session> Session { get; set; }
        public object Message { get; set; }
    }
    
    [ComponentOf(typeof(Scene))]
    public class NetComponent: Entity, IAwake<IKcpTransport>, IDestroy, IUpdate
    {
        public AService AService { get; set; }
    }
}