using System.Collections.Generic;

namespace ET
{
    public static class LubanHelper
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