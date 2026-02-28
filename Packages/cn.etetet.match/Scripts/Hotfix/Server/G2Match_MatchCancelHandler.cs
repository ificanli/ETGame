namespace ET.Server
{
    [MessageHandler(SceneType.Match)]
    public class G2Match_MatchCancelHandler : MessageHandler<Scene, G2Match_MatchCancel, Match2G_MatchCancel>
    {
        protected override async ETTask Run(Scene root, G2Match_MatchCancel request, Match2G_MatchCancel response)
        {
            MatchQueueComponent matchQueue = root.GetComponent<MatchQueueComponent>();
            if (matchQueue == null)
            {
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "MatchQueueComponent not found";
                return;
            }

            // 取消匹配
            bool success = matchQueue.Cancel(request.RequestId);
            
            if (!success)
            {
                response.Error = 110000; // ERR_MyErrorCode
                response.Message = "Match request not found";
                return;
            }

            await ETTask.CompletedTask;
        }
    }
}
