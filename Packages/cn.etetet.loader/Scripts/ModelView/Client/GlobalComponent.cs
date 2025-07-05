using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class GlobalComponent: Entity, IAwake
    {
        public Transform Global;
        public Transform Unit { get; set; }

        public GlobalConfig GlobalConfig { get; set; }
    }
}