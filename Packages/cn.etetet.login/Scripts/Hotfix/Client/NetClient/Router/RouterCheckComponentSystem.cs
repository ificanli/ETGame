using System;
using System.Net;

namespace ET.Client
{
    [EntitySystemOf(typeof(RouterCheckComponent))]
    public static partial class RouterCheckComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RouterCheckComponent self)
        {
            self.CheckAsync().NoContext();
        }

        private static async ETTask CheckAsync(this RouterCheckComponent self)
        {
            Session session = self.GetParent<Session>();
            Fiber fiber = self.Fiber();
            Scene root = fiber.Root;
            
            Address realAddress = session.RemoteAddress;
            NetComponent netComponent = root.GetComponent<NetComponent>();
            
            EntityRef<RouterCheckComponent> selfRef = self;
            EntityRef<Session> sessionRef = session;
            EntityRef<Scene> rootRef = root;
            EntityRef<NetComponent> netComponentRef = netComponent;
            while (true)
            {
                await fiber.Root.GetComponent<TimerComponent>().WaitAsync(1000);
                
                self = selfRef;
                session = sessionRef;
                
                if (self == null)
                {
                    return;
                }
                if (session == null)
                {
                    return;
                }

                long time = TimeInfo.Instance.ClientFrameTime();

                if (time - session.LastRecvTime < 7 * 1000)
                {
                    continue;
                }
                
                try
                {
                    long sessionId = session.Id;

                    (uint localConn, uint remoteConn) = session.AService.GetChannelConn(sessionId);
                    
                    root = rootRef;
                    netComponent = netComponentRef;
                    
                    Log.Info($"get recvLocalConn start: {root.Id} {realAddress} {localConn} {remoteConn}");

                    (uint recvLocalConn, IPEndPoint routerAddress) = await netComponent.GetRouterAddress(realAddress, localConn, remoteConn);
                    session = sessionRef;
                    root = rootRef;
                    if (recvLocalConn == 0)
                    {
                        Log.Error($"get recvLocalConn fail: {root.Id} {routerAddress} {realAddress} {localConn} {remoteConn}");
                        continue;
                    }
                    
                    Log.Info($"get recvLocalConn ok: {root.Id} {routerAddress} {realAddress} {recvLocalConn} {localConn} {remoteConn}");
                    
                    session.LastRecvTime = TimeInfo.Instance.ClientNow();
                    
                    session.AService.ChangeAddress(sessionId, routerAddress);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}