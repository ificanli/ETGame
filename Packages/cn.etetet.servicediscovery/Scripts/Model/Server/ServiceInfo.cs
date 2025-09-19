using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务信息实体，记录注册到服务发现中的Fiber信息
    /// </summary>
    [ChildOf(typeof(ServiceDiscoveryComponent))]
    public class ServiceInfo : Entity, IAwake<string, int, ActorId>, IDestroy
    {
        public string SceneName;
        /// <summary>
        /// 场景类型
        /// </summary>
        public int SceneType;

        /// <summary>
        /// Actor ID
        /// </summary>
        public ActorId ActorId;

        /// <summary>
        /// 服务元数据，存储KV键值对
        /// </summary>
        public Dictionary<string, string> Metadata = new();

        /// <summary>
        /// 注册时间
        /// </summary>
        public long RegisterTime;

        /// <summary>
        /// 最后心跳时间
        /// </summary>
        public long LastHeartbeatTime;

        /// <summary>
        /// 订阅时的过滤条件，Key为SceneType，Value为过滤元数据
        /// </summary>
        public Dictionary<int, Dictionary<string, string>> SubSceneTypes = new();
    }
}