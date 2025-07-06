using System.Collections.Generic;

namespace ET
{
    public static class StartConfigHelper
    {
        [StaticField]
        public static List<string> StartConfigs = new List<string>()
        {
            "StartMachineConfigCategory",
            "StartProcessConfigCategory",
            "StartSceneConfigCategory",
            "StartZoneConfigCategory",
        };
    }
}