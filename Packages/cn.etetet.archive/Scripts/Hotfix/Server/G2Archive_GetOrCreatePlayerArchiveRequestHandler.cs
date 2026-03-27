namespace ET.Server
{
    [MessageHandler(SceneType.Archive)]
    public class G2Archive_GetOrCreatePlayerArchiveRequestHandler : MessageHandler<Scene, G2Archive_GetOrCreatePlayerArchiveRequest, Archive2G_GetOrCreatePlayerArchiveResponse>
    {
        protected override async ETTask Run(Scene root, G2Archive_GetOrCreatePlayerArchiveRequest request, Archive2G_GetOrCreatePlayerArchiveResponse response)
        {
            ArchiveManagerComponent archiveManager = root.GetComponent<ArchiveManagerComponent>();
            if (archiveManager == null)
            {
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ArchiveManagerComponent not found";
                return;
            }

            PlayerArchive archive = archiveManager.GetOrCreate(
                request.Account,
                request.TotalWealth,
                request.LastEvacuationWealth,
                request.WarehouseColumnCount,
                request.WarehouseItems,
                request.LastEvacuationItems);
            if (archive == null)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "account invalid";
                return;
            }

            archive.FillStorageSnapshot(response);
            await ETTask.CompletedTask;
        }
    }
}
