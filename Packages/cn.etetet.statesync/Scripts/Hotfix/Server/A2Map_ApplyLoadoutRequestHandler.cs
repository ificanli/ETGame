namespace ET.Server
{
    /// <summary>
    /// 匹配成功后 Gate 通知 Unit 应用起装数据
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class A2Map_ApplyLoadoutRequestHandler : MessageLocationHandler<Unit, A2Map_ApplyLoadoutRequest, A2Map_ApplyLoadoutResponse>
    {
        protected override async ETTask Run(Unit unit, A2Map_ApplyLoadoutRequest request, A2Map_ApplyLoadoutResponse response)
        {
            Log.Info($"A2Map_ApplyLoadout: unit={unit.Id}, hero={request.HeroConfigId}, mainWeapon={request.MainWeaponConfigId}, subWeapon={request.SubWeaponConfigId}");

            LoadoutHelper.ApplyLoadout(unit, request.MainWeaponConfigId, request.SubWeaponConfigId, request.ArmorConfigId);

            WeaponInitHelper.InitializeWeaponsFromUnit(unit);

            await ETTask.CompletedTask;
        }
    }
}
