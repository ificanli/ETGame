using System;
using System.Net;
using System.Net.Sockets;

namespace ET
{
    [EntitySystemOf(typeof(NetComponent))]
    public static partial class NetComponentSystem
    {
        [EntitySystem]
        private static void Awake(this NetComponent self, IKcpTransport kcpTransport)
        {
            self.AService = new KService(kcpTransport, ServiceType.Outer);
            self.AService.AcceptCallback = self.OnAccept;
            self.AService.ReadCallback = self.OnRead;
            self.AService.ErrorCallback = self.OnError;
        }
        
        [EntitySystem]
        private static void Update(this NetComponent self)
        {
            self.AService.Update();
        }

        [EntitySystem]
        private static void Destroy(this NetComponent self)
        {
            self.AService.Dispose();
        }

        private static void OnError(this NetComponent self, long channelId, int error)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                return;
            }

            session.Error = error;
            session.Dispose();
        }

        // 这个channelId是由CreateAcceptChannelId生成的
        private static void OnAccept(this NetComponent self, long channelId, IPEndPoint ipEndPoint)
        {
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = ipEndPoint;

            // 挂上这个组件，5秒就会删除session，所以客户端验证完成要删除这个组件。该组件的作用就是防止外挂一直连接不发消息也不进行权限验证
            session.AddComponent<SessionAcceptTimeoutComponent>();
            // 客户端连接，2秒检查一次recv消息，10秒没有消息则断开
            session.AddComponent<SessionIdleCheckerComponent>();
        }
        
        private static void OnRead(this NetComponent self, long channelId, MemoryBuffer memoryBuffer)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                return;
            }
            session.LastRecvTime = TimeInfo.Instance.ClientNow();
            
            (ActorId _, object message) = MessageSerializeHelper.ToMessage(self.AService, memoryBuffer);
            self.AService.Recycle(memoryBuffer);

            // 外网消息是10000~20000
            ushort opcode = OpcodeType.Instance.GetOpcode(message.GetType());
            if (opcode is > 20000 or < 10000)
            {
                Log.Error($"client message must in (10000, 20000), opcode: {opcode}");
                return;
            }

            // 机器人消息只在RobotCase场景中处理
            if (message is IRobotCaseMessage)
            {
                if (Options.Instance.SceneName != "RobotCase")
                {
                    Log.Error($"RobotCase message received in non-RobotCase scene: {message.GetType().Name}");
                    return;
                }
            }

            EventSystem.Instance.Invoke(self.IScene.SceneType, new NetComponentOnRead() {Session = session, Message = message});
        }
        
        public static Session Create(this NetComponent self, IPEndPoint realIPEndPoint)
        {
            long channelId = NetServices.Instance.CreateConnectChannelId();
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = realIPEndPoint;
            session.AddComponent<SessionIdleCheckerComponent>();
            
            self.AService.Create(session.Id, session.RemoteAddress);

            return session;
        }

        public static Session Create(this NetComponent self, IPEndPoint routerIPEndPoint, IPEndPoint realIPEndPoint, uint localConn)
        {
            long channelId = localConn;
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = realIPEndPoint;
            session.AddComponent<SessionIdleCheckerComponent>();
            self.AService.Create(session.Id, routerIPEndPoint);
            return session;
        }
    }
}