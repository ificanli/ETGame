using System;
using System.Net;

namespace ET.Server
{
    [EntitySystemOf(typeof(ProcessOuterSender))]
    public static partial class ProcessOuterSenderSystem
    {
        [EntitySystem]
        private static void Awake(this ProcessOuterSender self, IPEndPoint address)
        {
            switch (self.InnerProtocol)
            {
                case NetworkProtocol.TCP:
                {
                    self.AService = new TService(address, ServiceType.Inner);
                    break;
                }
                case NetworkProtocol.KCP:
                {
                    self.AService = new KService(new UdpTransport(address), ServiceType.Inner);
                    break;
                }
            }

            // 进程的真实ip port
            AddressSingleton.Instance.InnerAddress = self.AService.GetBindPoint();
            
            self.AService.AcceptCallback = self.OnAccept;
            self.AService.ReadCallback = self.OnRead;
            self.AService.ErrorCallback = self.OnError;
        }
        
        
        [EntitySystem]
        private static void Update(this ProcessOuterSender self)
        {
            self.AService.Update();
        }

        [EntitySystem]
        private static void Destroy(this ProcessOuterSender self)
        {
            self.AService.Dispose();
        }

        private static void OnRead(this ProcessOuterSender self, long channelId, MemoryBuffer memoryBuffer)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                return;
            }
            session.LastRecvTime = TimeInfo.Instance.ClientNow();

            (FiberInstanceId fiberInstanceId, object message) = MessageSerializeHelper.ToMessage(self.AService, memoryBuffer);
            LogMsg.Instance.Recv(self.Fiber(), message);
            
            self.AService.Recycle(memoryBuffer);
            if (message is IResponse response)
            {
                self.HandleIActorResponse(response);
                return;
            }
            
            Fiber fiber = self.Fiber();
            
            EntityRef<ProcessOuterSender> selfRef = self;

            switch (message)
            {
                case ILocationRequest:
                case IRequest:
                {
                    Address remoteAddress = session.RemoteAddress;
                    CallInner().NoContext();
                    break;
                    
                    async ETTask CallInner()
                    {
                        IRequest req = (IRequest)message;
                        int rpcId = req.RpcId;
                        // 注意这里都不能抛异常，因为这里只是中转消息
                        IResponse res = await fiber.Root.GetComponent<ProcessInnerSender>().Call(fiberInstanceId, req, false);
                        // 注意这里的response会在该协程执行完之后由ProcessInnerSender dispose。
                        res.RpcId = rpcId;
                        self = selfRef;
                        self.Send(new ActorId(remoteAddress, new FiberInstanceId(0, 0)), res);
                        ((MessageObject)res).Dispose();
                    }
                }
                default:
                {
                    fiber.Root.GetComponent<ProcessInnerSender>().Send(fiberInstanceId, (IMessage)message);
                    break;
                }
            }
        }

        private static void OnError(this ProcessOuterSender self, long channelId, int error)
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
        private static void OnAccept(this ProcessOuterSender self, long channelId, IPEndPoint ipEndPoint)
        {
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = ipEndPoint;
            //session.AddComponent<SessionIdleCheckerComponent, int, int, int>(NetThreadComponent.checkInteral, NetThreadComponent.recvMaxIdleTime, NetThreadComponent.sendMaxIdleTime);
        }

        private static Session CreateInner(this ProcessOuterSender self, Address address)
        {
            int channelId;
            while (true)
            {
                channelId = RandomGenerator.RandInt32();
                if (self.GetChild<Session>(channelId) == null)
                {
                    break;
                }
            }
            
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = address;
            
            self.AddressSessions[address] = session;
            self.AService.Create(channelId, session.RemoteAddress);

            //session.AddComponent<InnerPingComponent>();
            //session.AddComponent<SessionIdleCheckerComponent, int, int, int>(NetThreadComponent.checkInteral, NetThreadComponent.recvMaxIdleTime, NetThreadComponent.sendMaxIdleTime);

            return session;
        }

        // 内网actor session
        private static Session Get(this ProcessOuterSender self, Address address)
        {
            EntityRef<Session> sessionRef;
            self.AddressSessions.TryGetValue(address, out sessionRef);
            Session session = sessionRef;
            if (session != null)
            {
                return session;
            }
            
            session = self.CreateInner(address);
            return session;
        }

        private static void HandleIActorResponse(this ProcessOuterSender self, IResponse response)
        {
            if (!self.requestCallback.Remove(response.RpcId, out MessageSenderStruct actorMessageSender))
            {
                return;
            }
            Run(actorMessageSender, response);
        }

        private static void Run(MessageSenderStruct self, IResponse response)
        {
            if (response.Error == ErrorCode.ERR_MessageTimeout)
            {
                self.SetException(new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {self.ActorId} {self.RequestType.FullName}, response: {response}"));
                return;
            }

            if (self.NeedException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                self.SetException(new RpcException(response.Error, $"Rpc error: actorId: {self.ActorId} request: {self.RequestType.FullName}, response: {response}"));
                return;
            }

            self.SetResult(response);
        }

        public static void Send(this ProcessOuterSender self, ActorId actorId, IMessage message)
        {
            self.SendInner(actorId, message as MessageObject);
        }

        private static void SendInner(this ProcessOuterSender self, ActorId actorId, MessageObject message)
        {
            if (actorId == default)
            {
                throw new Exception($"actor id is 0: {message}");
            }
            
            if (message == null)
            {
                throw new Exception($"message is null");
            }

            // 如果发向同一个进程，则报错
            if (actorId.Address == AddressSingleton.Instance.InnerAddress)
            {
                throw new Exception($"actor is the same process: {actorId}");
            }
            Session session = self.Get(actorId.Address);
            session.Send(actorId.FiberInstanceId, message);
        }

        private static int GetRpcId(this ProcessOuterSender self)
        {
            return ++self.RpcId;
        }

        public static async ETTask<IResponse> Call(this ProcessOuterSender self, ActorId actorId, IRequest iRequest, bool needException = true)
        {
            if (actorId == default)
            {
                throw new Exception($"actor id is 0: {iRequest}");
            }
            Fiber fiber = self.Fiber();
            
            int rpcId = self.GetRpcId();

            iRequest.RpcId = rpcId;

            Type requestType = iRequest.GetType();
            MessageSenderStruct messageSenderStruct = new(actorId, requestType, needException);
            self.requestCallback.Add(rpcId, messageSenderStruct);
            self.SendInner(actorId, iRequest as MessageObject);
            EntityRef<ProcessOuterSender> selfRef = self;

            async ETTask Timeout()
            {
                await fiber.Root.GetComponent<TimerComponent>().WaitAsync(ProcessOuterSender.TIMEOUT_TIME);
                self = selfRef;
                if (!self.requestCallback.Remove(rpcId, out MessageSenderStruct action))
                {
                    return;
                }
                
                if (needException)
                {
                    action.SetException(new Exception($"actor sender timeout: {requestType.FullName}"));
                }
                else
                {
                    IResponse response = MessageHelper.CreateResponse(requestType, rpcId, ErrorCode.ERR_Timeout);
                    action.SetResult(response);
                }
            }

            Timeout().NoContext();

            long beginTime = TimeInfo.Instance.ServerNow();

            IResponse response = await messageSenderStruct.Wait();

            long endTime = TimeInfo.Instance.ServerNow();

            long costTime = endTime - beginTime;
            if (costTime > 200)
            {
                Log.Warning($"actor rpc time > 200: {costTime} {requestType.FullName}");
            }

            return response;
        }
    }
}