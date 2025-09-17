namespace ET.Server
{
    /// <summary>
    /// 服务信息系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceInfo))]
    public static partial class ServiceInfoSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceInfo self, string sceneName, int sceneType, ActorId actorId)
        {
            self.SceneName = sceneName;
            self.SceneType = sceneType;
            self.ActorId = actorId;
            self.RegisterTime = TimeInfo.Instance.ServerNow();
            self.LastHeartbeatTime = self.RegisterTime;
        }

        [EntitySystem]
        private static void Destroy(this ServiceInfo self)
        {
        }

        /// <summary>
        /// 更新心跳时间
        /// </summary>
        public static void UpdateHeartbeat(this ServiceInfo self)
        {
            self.LastHeartbeatTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 检查是否心跳超时
        /// </summary>
        public static bool IsHeartbeatTimeout(this ServiceInfo self, long timeoutMs)
        {
            long now = TimeInfo.Instance.ServerNow();
            return now - self.LastHeartbeatTime > timeoutMs;
        }

        /// <summary>
        /// 转换为Proto格式
        /// </summary>
        public static ServiceInfoProto ToProto(this ServiceInfo self)
        {
            ServiceInfoProto proto = ServiceInfoProto.Create();
            proto.SceneType = self.SceneType;
            proto.SceneName = self.SceneName;
            proto.ActorId = self.ActorId;
            return proto;
        }
    }
}