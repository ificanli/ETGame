using System.Collections.Generic;
using YIUIFramework;

namespace ET.Client
{
    [GM(EGMType.Test, 1, "召唤怪物(UnitConfigId)", "输入 UnitConfig.Id，在当前玩家前方固定距离召唤怪物")]
    public class GM_SpawnMonsterByUnitConfig : IGMCommand
    {
        public List<GMParamInfo> GetParams()
        {
            return new()
            {
                new GMParamInfo(EGMParamType.Int, "怪物UnitConfigId", "1001"),
            };
        }

        public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
        {
            int unitConfigId = paramVo.Get<int>(0);
            if (unitConfigId <= 0)
            {
                Log.Warning($"[DebugSpawnMonsterGM] invalid unitConfigId={unitConfigId}");
                await ETTask.CompletedTask;
                return false;
            }

            Scene currentScene = clientScene?.CurrentScene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                Log.Warning("[DebugSpawnMonsterGM] current scene missing");
                await ETTask.CompletedTask;
                return false;
            }

            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null || myUnit.IsDisposed)
            {
                Log.Warning("[DebugSpawnMonsterGM] current player unit missing");
                await ETTask.CompletedTask;
                return false;
            }

            ClientSenderComponent sender = clientScene.Root().GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                Log.Warning("[DebugSpawnMonsterGM] client sender missing");
                await ETTask.CompletedTask;
                return false;
            }

            C2M_DebugSpawnMonster request = C2M_DebugSpawnMonster.Create();
            request.UnitConfigId = unitConfigId;

            M2C_DebugSpawnMonster response = await sender.Call(request) as M2C_DebugSpawnMonster;
            if (response == null)
            {
                Log.Warning($"[DebugSpawnMonsterGM] null response, unitConfigId={unitConfigId}");
                return false;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[DebugSpawnMonsterGM] failed, unitConfigId={unitConfigId}, error={response.Error}, msg={response.Message}");
                return false;
            }

            Log.Info($"[DebugSpawnMonsterGM] success, unitConfigId={response.SpawnedConfigId}, monsterId={response.SpawnedUnitId}");
            return false;
        }
    }
}
