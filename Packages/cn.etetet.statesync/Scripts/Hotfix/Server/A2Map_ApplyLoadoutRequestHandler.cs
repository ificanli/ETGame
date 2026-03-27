namespace ET.Server
{
    using System.Collections.Generic;
    /// <summary>
    /// 直接应用当前起装到 Unit（Home/Map 场景）
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class A2Map_ApplyLoadoutRequestHandler : MessageLocationHandler<Unit, A2Map_ApplyLoadoutRequest, A2Map_ApplyLoadoutResponse>
    {
        protected override async ETTask Run(Unit unit, A2Map_ApplyLoadoutRequest request, A2Map_ApplyLoadoutResponse response)
        {
            Log.Info($"A2Map_ApplyLoadout: unit={unit.Id}, hero={request.HeroConfigId}, mainWeapon={request.MainWeaponConfigId}, subWeapon={request.SubWeaponConfigId}");

            List<LoadoutGridItemInfo> bagItems = new();
            for (int i = 0; i < request.BagItems.Count; ++i)
            {
                LoadoutGridItemData item = request.BagItems[i];
                bagItems.Add(new LoadoutGridItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }

            LoadoutHelper.ApplyLoadout(
                unit,
                request.MainWeaponConfigId,
                request.SubWeaponConfigId,
                request.ArmorConfigId,
                request.ConsumableConfigIds,
                request.BackpackConfigId,
                request.BagWidth,
                request.BagHeight,
                bagItems);

            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
            WeaponInitHelper.InitializeHeroPassiveBuff(unit, request.HeroConfigId);

            await ETTask.CompletedTask;
        }
    }
}
