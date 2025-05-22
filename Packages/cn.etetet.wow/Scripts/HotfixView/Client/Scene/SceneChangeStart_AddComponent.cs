using System;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    [Event(SceneType.WOW)]
    public class SceneChangeStart_AddComponent: AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                EntityRef<CurrentScenesComponent> currentScenesComponentRef = root.GetComponent<CurrentScenesComponent>();
                using CoroutineLock coroutineLock = await root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.SceneChange, 0);

                root = rootRef;
                CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
                Scene currentScene = root.CurrentScene();
                EntityRef<Scene> currentSceneRef = currentScene;
                MapConfig preMapConfig = null;
                if (args.PreSceneName != null)
                {
                    preMapConfig = MapConfigCategory.Instance.GetByName(args.PreSceneName);
                }
                 
                MapConfig mapConfig = MapConfigCategory.Instance.GetByName(currentScene.Name);
                
                // 地图资源相同,则不创建Loading界面,也不需要重新加载地图
                if (mapConfig.MapResName != preMapConfig?.MapResName)
                {
                    await YIUIMgrComponent.Inst.Root.OpenPanelAsync<LoadingPanelComponent>();
                    
                    currentScenesComponent = currentScenesComponentRef;
                    currentScenesComponent.Progress = 0;
                
                    ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();
            
                    // 加载场景资源
                    await resourcesLoaderComponent.LoadSceneAsync(mapConfig.MapResName, LoadSceneMode.Single,
                        (progress) =>
                        {
                            CurrentScenesComponent currentScenes = currentScenesComponentRef;
                            currentScenes.Progress = (int)progress * 99f;
                        });
                }
                
                // 切换到map场景
                currentScene = currentSceneRef;
                currentScene.AddComponent<OperaComponent>();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}