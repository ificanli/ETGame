
namespace ET.Client
{
	[Event(SceneType.Client)]
	public class AppStartInitFinish_CreateLoginUI: AEvent<Scene, AppStartInitFinish>
	{
		protected override async ETTask Run(Scene root, AppStartInitFinish args)
		{
			EntityRef<Scene> rootRef = root;
			World.Instance.AddSingleton<YIUIEventComponent>();
			root.AddComponent<ResourcesLoaderComponent>();
			root.AddComponent<GlobalComponent>();
			bool result = await root.AddComponent<YIUIMgrComponent>().Initialize();
			if (!result)
			{
				Log.Error("初始化UI失败");
				return;
			}

			root = rootRef;
			await root.YIUIRoot().OpenPanelAsync<LoginPanelComponent>();
		}
	}
}
