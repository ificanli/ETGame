using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Serializable]
    [EnableClass]
    public class FlowGraphData
    {
        public int Version = 1;
        public List<FlowNodeData> Nodes = new();
        public List<FlowConnectionData> Connections = new();
        [BsonIgnore]
        public Dictionary<int, FlowNodeData> RuntimeNodeMap;
        [BsonIgnore]
        public Dictionary<int, Dictionary<string, List<int>>> RuntimeAdjacency;
        [BsonIgnore]
        public int RuntimeNodeCount;
        [BsonIgnore]
        public int RuntimeConnectionCount;
    }

    [Serializable]
    [EnableClass]
    public class FlowNodeData
    {
        // 兼容旧导出格式字段 "_id"。
        [BsonElement("_id")]
        public int LegacyId;

        [BsonElement("Id")]
        public int NodeId;
        public string NodeType;
        public string NodeKey;
        public string Title;
        public float PosX;
        public float PosY;
        public List<FlowParam> Params = new();
    }

    [Serializable]
    [EnableClass]
    public class FlowConnectionData
    {
        public int FromNodeId;
        public int ToNodeId;
        public string Branch;
    }

    [Serializable]
    [EnableClass]
    public class FlowParam
    {
        public string Key;
        public string Value;
    }
}
