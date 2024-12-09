using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ThreatComponent))]
    public static partial class ThreatComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ThreatComponent self)
        {
        }
        
        public static void AddThreat(this ThreatComponent self, long unitId, int threat)
        {
            if (self.Threats.TryGetValue(unitId, out int value))
            {
                self.Threats[unitId] = value + threat;
            }
            else
            {
                self.Threats.Add(unitId, threat);
            }
        }

        public static void RemoveThreat(this ThreatComponent self, long unitId)
        {
            self.Threats.Remove(unitId);
        }

        public static void ClearThreat(this ThreatComponent self)
        {
            self.Threats.Clear();
        }

        public static void SetThreat(this ThreatComponent self, long unitId, int threat)
        {
            self.Threats[unitId] = threat;
        }

        public static int GetThreat(this ThreatComponent self, long unitId)
        {
            return self.Threats.GetValueOrDefault(unitId, 0);
        }

        public static KeyValuePair<long, int> GetMaxThreat(this ThreatComponent self)
        {
            KeyValuePair<long, int> max = new KeyValuePair<long, int>(0, 0);
            foreach (var threat in self.Threats)
            {
                if (threat.Value > max.Value)
                {
                    max = threat;
                }
            }

            return max;
        }

        public static KeyValuePair<long, int> GetMinThreat(this ThreatComponent self)
        {
            KeyValuePair<long, int> min = new KeyValuePair<long, int>(0, int.MaxValue);
            foreach (var threat in self.Threats)
            {
                if (threat.Value < min.Value)
                {
                    min = threat;
                }
            }

            return min;
        }

        public static KeyValuePair<long, int> GetRandomThreat(this ThreatComponent self)
        {
            int index = RandomGenerator.RandomNumber(0, self.Threats.Count);
            int i = 0;
            foreach (var threat in self.Threats)
            {
                if (i == index)
                {
                    return threat;
                }

                i++;
            }

            return new KeyValuePair<long, int>(0, 0);
        }

    }
}