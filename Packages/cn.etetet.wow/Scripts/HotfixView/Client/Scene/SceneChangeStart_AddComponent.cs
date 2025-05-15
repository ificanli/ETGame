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
                using CoroutineLock coroutineLock = await root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.SceneChange, 0);
                
                CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
                
                await YIUIMgrComponent.Inst.Root.OpenPanelAsync<LoadingPanelComponent>();
                Scene currentScene = root.CurrentScene();

                currentScenesComponent.Progress = 0;
                
                ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();
            
                // 加载场景资源
                EntityRef<CurrentScenesComponent> currentScenesComponentRef = currentScenesComponent;
                await resourcesLoaderComponent.LoadSceneAsync($"Packages/cn.etetet.wow/Bundles/Scenes/{currentScene.Name}.unity", LoadSceneMode.Single,
                    (progress) =>
                    {
                        CurrentScenesComponent currentScenes = currentScenesComponentRef;
                        currentScenes.Progress = (int)progress * 99f;
                    });
                // 切换到map场景

                currentScene.AddComponent<OperaComponent>();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}