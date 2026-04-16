namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_QuickExitExtractionHandler : MessageLocationHandler<Unit, C2M_QuickExitExtraction, M2C_QuickExitExtraction>
    {
        protected override async ETTask Run(Unit unit, C2M_QuickExitExtraction request, M2C_QuickExitExtraction response)
        {
            string currentMapName = unit.Scene()?.Name.GetSceneConfigName() ?? string.Empty;
            string lobbyMapName = ECAConfig.DefaultLobbyMapName;
            if (string.IsNullOrWhiteSpace(currentMapName) || currentMapName == lobbyMapName)
            {
                response.Error = ErrorCode.ERR_QuickExitExtractionContextInvalid;
                response.Message = $"quick exit invalid in current map: {currentMapName}";
                return;
            }

            response.Message = "success";
            ProcessQuickExit(unit, lobbyMapName).Coroutine();
            await ETTask.CompletedTask;
        }

        private static async ETTask ProcessQuickExit(Unit unit, string lobbyMapName)
        {
            try
            {
                EntityRef<Unit> unitRef = unit;
                await EvacuationSettlementHelper.Settle(unit);

                unit = unitRef;
                if (unit == null || unit.IsDisposed)
                {
                    Log.Warning("[QuickExitExtraction] unit disposed after settlement");
                    return;
                }

                await TransferHelper.TransferAtFrameFinish(unit, lobbyMapName, 0);
            }
            catch (System.Exception exception)
            {
                Log.Error($"[QuickExitExtraction] process failed: {exception}");
            }
        }
    }
}
