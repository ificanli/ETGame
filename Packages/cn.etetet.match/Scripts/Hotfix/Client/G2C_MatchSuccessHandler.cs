namespace ET.Client
{
    [MessageHandler(SceneType.NetClient)]
    public class G2C_MatchSuccessHandler : MessageHandler<Scene, G2C_MatchSuccess>
    {
        protected override async ETTask Run(Scene root, G2C_MatchSuccess message)
        {
            Log.Info($"匹配成功！GameMode: {message.GameMode}, MapName: {message.MapName}, MapId: {message.MapId}, 玩家数: {message.PlayerIds.Count}");

            // 发布匹配成功事件，让UI层处理
            root.GetComponent<ObjectWait>().Notify(new Wait_MatchSuccess()
            {
                GameMode = message.GameMode,
                MapName = message.MapName,
                MapId = message.MapId,
                PlayerIds = message.PlayerIds
            });

            await ETTask.CompletedTask;
        }
    }
}
