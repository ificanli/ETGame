using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangePosition_SyncGameObjectPos: AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }

            Transform transform = gameObjectComponent.Transform;

            transform.position = unit.Position;
            
            // 贴地
            GameObjectPosHelper.OnTerrain(transform);

            // 摄像机跟随
            CinemachineComponent cinemachineComponent = unit.GetComponent<CinemachineComponent>();
            if (cinemachineComponent != null)
            {
                cinemachineComponent.Follow.position = gameObjectComponent.GameObject.Get<GameObject>("Follow").transform.position;
            }
            
            await ETTask.CompletedTask;
        }
    }
}