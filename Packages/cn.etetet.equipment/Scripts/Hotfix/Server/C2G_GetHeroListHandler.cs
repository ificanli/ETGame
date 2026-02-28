namespace ET.Server
{
    /// <summary>
    /// 获取可用英雄列表（Gate 服务器处理）
    /// 从 HeroConfigCategory 读取所有英雄配置并返回给客户端
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_GetHeroListHandler : MessageSessionHandler<C2G_GetHeroList, G2C_GetHeroList>
    {
        protected override async ETTask Run(Session session, C2G_GetHeroList request, G2C_GetHeroList response)
        {
            HeroConfigCategory heroConfigs = HeroConfigCategory.Instance;

            foreach (HeroConfig heroConfig in heroConfigs.DataList)
            {
                HeroInfoData heroInfo = HeroInfoData.Create();
                heroInfo.HeroConfigId = heroConfig.Id;
                heroInfo.Name = heroConfig.Name;
                heroInfo.UnitConfigId = heroConfig.UnitConfigId;
                response.Heroes.Add(heroInfo);
            }

            await ETTask.CompletedTask;
        }
    }
}
