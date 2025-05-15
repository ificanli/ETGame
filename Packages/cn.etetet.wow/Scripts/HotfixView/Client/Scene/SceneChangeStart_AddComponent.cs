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
                
                await YIUIMgrComponent.Inst.Root.OpenPanelAsync<LoadingPanelComponent>();
                root = rootRef;
                Scene currentScene = root.CurrentScene();
                EntityRef<Scene> currentSceneRef = currentScene;
                currentScenesComponent = currentScenesComponentRef;
                currentScenesComponent.Progress = 0;
                
                ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();
            
                // 加载场景资源
                await resourcesLoaderComponent.LoadSceneAsync($"Packages/cn.etetet.wow/Bundles/Scenes/{currentScene.Name}.unity", LoadSceneMode.Single,
                    (progress) =>
                    {
                        CurrentScenesComponent currentScenes = currentScenesComponentRef;
                        currentScenes.Progress = (int)progress * 99f;
                    });
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