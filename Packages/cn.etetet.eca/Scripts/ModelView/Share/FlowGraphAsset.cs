using UnityEngine;
using ET;

namespace ET.Client
{
    [CreateAssetMenu(fileName = "FlowGraph", menuName = "ET/ECA Flow Graph")]
    [EnableClass]
    public class FlowGraphAsset : ScriptableObject
    {
        public FlowGraphData Graph = new();
    }
}
