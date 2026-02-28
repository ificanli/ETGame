using System;
using System.Collections.Generic;

namespace ET
{
    [Serializable]
    [EnableClass]
    public class FlowGraphData
    {
        public int Version = 1;
        public List<FlowNodeData> Nodes = new();
        public List<FlowConnectionData> Connections = new();
    }

    [Serializable]
    [EnableClass]
    public class FlowNodeData
    {
        public int Id;
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
