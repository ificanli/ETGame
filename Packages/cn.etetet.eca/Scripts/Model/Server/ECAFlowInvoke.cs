using ET;

namespace ET.Server
{
    public struct ECAFlowConditionInvoke
    {
        public string Key;
        public FlowNodeData Node;
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
    }

    public struct ECAFlowActionInvoke
    {
        public string Key;
        public FlowNodeData Node;
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
    }
}
