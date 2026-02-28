namespace ET.Server
{
    [MessageHandler(SceneType.Match)]
    public class G2Match_MatchRequestHandler : MessageHandler<Scene, G2Match_MatchRequest, Match2G_MatchRequest>
    {
        protected override async ETTask Run(Scene root, G2Match_MatchRequest request, Match2G_MatchRequest response)
        {
            MatchQueueComponent matchQueue = root.GetComponent<MatchQueueComponent>();
            if (matchQueue == null)
            {
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "MatchQueueComponent not found";
                return;
            }

            // 加入匹配队列
            long requestId = matchQueue.Enqueue(request.PlayerId, request.GameMode);
            
            if (requestId == -1)
            {
                response.Error = 110000; // ERR_MyErrorCode
                response.Message = "Player already in queue";
                return;
            }

            response.RequestId = requestId;
            await ETTask.CompletedTask;
        }
    }
}
