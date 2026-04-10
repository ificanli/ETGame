namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_DebugSpawnMonsterHandler : MessageLocationHandler<Unit, C2M_DebugSpawnMonster, M2C_DebugSpawnMonster>
    {
        protected override async ETTask Run(Unit unit, C2M_DebugSpawnMonster request, M2C_DebugSpawnMonster response)
        {
            response.SpawnedConfigId = request.UnitConfigId;

            int error = DebugSpawnMonsterHelper.TrySpawn(unit, request.UnitConfigId, out Unit monster, out string errorMessage);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = errorMessage;
                return;
            }

            response.SpawnedUnitId = monster.Id;
            response.SpawnedConfigId = request.UnitConfigId;
            response.Message = "success";
            await ETTask.CompletedTask;
        }
    }
}
