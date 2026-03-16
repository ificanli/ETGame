namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ECAPointPlayerInteractEvent_RoguePointReward : AEvent<Scene, ECAPointPlayerInteractEvent>
    {
        protected override async ETTask Run(Scene scene, ECAPointPlayerInteractEvent a)
        {
            ECAPointComponent point = a.Point;
            Unit player = a.Player;
            RoguePointRewardHelper.TryGrantInteractGold(point, player);
            await ETTask.CompletedTask;
        }
    }
}
