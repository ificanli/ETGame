using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    public enum NetworkProtocol
    {
        TCP,
        KCP,
        Websocket,
        UDP,
    }
}