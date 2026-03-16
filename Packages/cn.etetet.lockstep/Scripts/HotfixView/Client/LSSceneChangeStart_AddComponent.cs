using UnityEngine.SceneManagement;

namespace ET.Client
{
    [Event(SceneType.LockStep)]
    public class LSSceneChangeStart_AddComponent: AEvent<Scene, LSSceneChangeStart>
    {
        protected override async ETTask Run(Scene clientScene, LSSceneChangeStart args)
        {
            Room room = clientScene.GetComponent<Room>();
            ResourcesLoaderComponent resourcesLoaderComponent = room.AddComponent<ResourcesLoaderComponent>();
            
            // 创建loading界面
            
            
            // 创建房间UI
            
            // 加载场景资源
            await ETTask.CompletedTask;
        }
    }
}