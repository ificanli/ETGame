using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(RealmGateInfoComponent))]
    public static partial class RealmGateInfoComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RealmGateInfoComponent self)
        {
            
        }
        
        [EntitySystem]
        private static void Destroy(this RealmGateInfoComponent self)
        {

        }
        
        public static void AddGate(this RealmGateInfoComponent self, int zone, string gateName)
        {
            self.ZoneGates.Add(zone, gateName);
        }
        
        public static void RemoveGate(this RealmGateInfoComponent self, int zone, string gateName)
        {
            self.ZoneGates.Remove(zone, gateName);
        }

        public static string[] GetGatesByZone(this RealmGateInfoComponent self, int zone)
        {
            return self.ZoneGates.GetAll(zone);
        }
    }
}