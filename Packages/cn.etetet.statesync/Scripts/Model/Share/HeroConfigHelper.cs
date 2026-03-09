namespace ET
{
    public static class HeroConfigHelper
    {
        public static HeroConfig GetDefaultHeroConfig()
        {
            foreach (HeroConfig heroConfig in HeroConfigCategory.Instance.DataList)
            {
                return heroConfig;
            }

            return null;
        }

        public static int GetDefaultHeroConfigId()
        {
            return GetDefaultHeroConfig()?.Id ?? 0;
        }

        public static int GetDefaultUnitConfigId()
        {
            return GetDefaultHeroConfig()?.UnitConfigId ?? 0;
        }
    }
}
