using System;

namespace ET
{
    [Serializable]
    [EnableClass]
    public class ECAConfig
    {
        public string ConfigId;
        public int PointType;
        public float PosX;
        public float PosY;
        public float PosZ;
        public float InteractRange = 3f;
        public FlowGraphData FlowGraph;
    }
}
