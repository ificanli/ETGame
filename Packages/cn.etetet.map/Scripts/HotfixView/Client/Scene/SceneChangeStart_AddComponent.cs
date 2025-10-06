using System;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeStart_AddComponent: AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                EntityRef<CurrentScenesComponent> currentScenesComponentRef = root.GetComponent<CurrentScenesComponent>();
                CurrentScenesComponent currentScenesComponent = currentScenesComponentRef;
                using var _ = await root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.SceneChange, 0);

                root = rootRef;
                currentScenesComponent = currentScenesComponentRef;
                Scene currentScene = currentScenesComponent.Scene;
                EntityRef<Scene> currentSceneRef = currentScene;
                // 地图资源相同,则不创建Loading界面,也不需要重新加载地图
                if (args.ChangeScene)
                {
                    await root.YIUIRoot().OpenPanelAsync<LoadingPanelComponent>();

                    currentScenesComponent = currentScenesComponentRef;
                    currentScenesComponent.Progress = 0;

                    currentScene = currentSceneRef;
                    ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();

                    MapConfig mapConfig = MapConfigCategory.Instance.GetByName(currentScene.Name.GetSceneConfigName());
                    // 加载场景资源
                    await resourcesLoaderComponent.LoadSceneAsync(mapConfig.MapResName, LoadSceneMode.Single,
                        (progress) =>
                        {
                            CurrentScenesComponent currentScenes = currentScenesComponentRef;
                            currentScenes.Progress = (int)progress * 99f;
                        });
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}