namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class Match2Map_InitMatchCopyRequestHandler : MessageHandler<Scene, Match2Map_InitMatchCopyRequest, Map2Match_InitMatchCopyResponse>
    {
        protected override async ETTask Run(Scene scene, Match2Map_InitMatchCopyRequest request, Map2Match_InitMatchCopyResponse response)
        {
            MatchCopyContextComponent context = scene.GetComponent<MatchCopyContextComponent>();
            if (context == null)
            {
                context = scene.AddComponent<MatchCopyContextComponent>();
            }

            MatchCopyContextHelper.Initialize(scene, context, request);
            await ETTask.CompletedTask;
        }
    }
}
