using System.Collections.Generic;

namespace ET
{
    public partial class MapConfigCategory
    {
        private Dictionary<string, MapConfig> nameConfigs = new();

        public MapConfig GetByName(string mapName)
        {
            return this.nameConfigs[mapName];
        }
        
        public override void EndInit()
        {
            foreach (var kv in this.GetAll())
            {
                this.nameConfigs.Add(kv.Value.Name, kv.Value);
            }
        }
    }
}