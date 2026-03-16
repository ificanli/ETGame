using System.Collections.Generic;

namespace ET
{
    public enum TacticalItemType
    {
        None = 0,
        Ward = 1,
        Detector = 2,
    }

    public enum TacticalCastMode
    {
        Self = 0,
        Position = 1,
        MinimapPoint = 2,
    }

    public partial class TacticalItemConfigCategory
    {
        private readonly Dictionary<int, TacticalItemConfig> itemConfigIdIndex = new();
        private bool itemConfigIdIndexBuilt;

        public TacticalItemConfig GetByItemConfigId(int itemConfigId)
        {
            if (itemConfigId <= 0)
            {
                return null;
            }

            if (!this.itemConfigIdIndexBuilt)
            {
                this.BuildItemConfigIdIndex();
            }

            this.itemConfigIdIndex.TryGetValue(itemConfigId, out TacticalItemConfig config);
            return config;
        }

        private void BuildItemConfigIdIndex()
        {
            this.itemConfigIdIndex.Clear();
            foreach ((int _, TacticalItemConfig config) in this.GetAll())
            {
                if (config == null || config.ItemConfigId <= 0)
                {
                    continue;
                }

                this.itemConfigIdIndex[config.ItemConfigId] = config;
            }

            this.itemConfigIdIndexBuilt = true;
        }
    }

    public partial class TacticalItemConfig
    {
        public TacticalItemType GetTacticalItemType()
        {
            return (TacticalItemType)this.TacticalType;
        }

        public TacticalCastMode GetTacticalCastMode()
        {
            return (TacticalCastMode)this.CastMode;
        }
    }
}
