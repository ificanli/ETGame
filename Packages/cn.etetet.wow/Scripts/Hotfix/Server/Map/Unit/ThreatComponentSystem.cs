using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(ThreatComponent))]
    public static partial class ThreatComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ThreatComponent self)
        {
        }
        
        public static void AddThreat(this ThreatComponent self, Unit unit, int threat)
        {
            ThreatInfo threatInfo = self.GetChild<ThreatInfo>(unit.Id);
            if (threatInfo == null)
            {
                threatInfo = self.AddChild<ThreatInfo>();
                threatInfo.Unit = unit;
                threatInfo.Threat = threat;
            }
            else
            {
                threatInfo.Threat += threat;
            }
        }

        public static void RemoveThreat(this ThreatComponent self, long unitId)
        {
            self.RemoveChild(unitId);
        }
        
        public static int GetCount(this ThreatComponent self)
        {
            return self.ChildrenCount();
        }

        public static void ClearThreat(this ThreatComponent self)
        {
            foreach (long unitId in self.Children.Keys.ToArray())
            {
                self.RemoveChild(unitId);
            }
        }

        public static int GetThreat(this ThreatComponent self, long unitId)
        {
            ThreatInfo threatInfo = self.GetChild<ThreatInfo>(unitId);
            if (threatInfo == null)
            {
                return 0;
            }
            return threatInfo.Threat;
        }

        public static ThreatInfo GetMaxThreat(this ThreatComponent self)
        {
            int max = 0;
            ThreatInfo threatInfo = null;
            foreach (var kv in self.Children)
            {
                threatInfo = kv.Value as ThreatInfo;
                if (threatInfo.Unit.Entity == null)
                {
                    threatInfo = null;
                    self.RemoveThreat(kv.Key);
                    continue;
                }

                if (threatInfo.Threat <= max)
                {
                    continue;
                }

                max = threatInfo.Threat;
            }
            return threatInfo;
        }

        public static ThreatInfo GetMinThreat(this ThreatComponent self)
        {            
            int min = 0;
            ThreatInfo threatInfo = null;
            foreach (var kv in self.Children)
            {
                threatInfo = kv.Value as ThreatInfo;
                if (threatInfo.Unit.Entity == null)
                {
                    threatInfo = null;
                    self.RemoveThreat(kv.Key);
                    continue;
                }

                if (threatInfo.Threat >= min)
                {
                    continue;
                }

                min = threatInfo.Threat;
            }
            return threatInfo;
        }

        public static ThreatInfo GetRandomThreat(this ThreatComponent self)
        {
            int index = RandomGenerator.RandomNumber(0, self.Children.Count);
            int i = 0;
            ThreatInfo threatInfo = null;
            foreach (var kv in self.Children)
            {
                if (i == index)
                {
                    threatInfo = kv.Value as ThreatInfo;
                    break;
                }

                i++;
            }
            return threatInfo;
        }

    }
}