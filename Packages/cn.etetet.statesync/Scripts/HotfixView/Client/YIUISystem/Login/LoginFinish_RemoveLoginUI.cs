
namespace ET.Client
{
	[Event(SceneType.Client)]
	public class LoginFinish_RemoveLoginUI: AEvent<Scene, LoginFinish>
	{
		protected override async ETTask Run(Scene scene, LoginFinish args)
		{
			// 登录成功后不要立刻手动关闭登录页，避免在 Loading 打开前出现留空。
			// 后续 Home/Lobby 打开时会通过面板切换逻辑自动把它隐藏。
			await ETTask.CompletedTask;
		}
	}
}
