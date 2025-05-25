using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class RobotManagerComponent: Entity, IAwake, IDestroy
    {
        public Dictionary<string, int> robots = new();
    }
}