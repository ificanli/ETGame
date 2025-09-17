using System.Collections.Generic;

namespace ET.Server
{
    public struct OnServiceChangeAddService
    {
        public int SceneType;
        public string ServiceName;
    }
    
    public struct OnServiceChangeRemoveService
    {
        public int SceneType;
        public string ServiceName;
    }
}