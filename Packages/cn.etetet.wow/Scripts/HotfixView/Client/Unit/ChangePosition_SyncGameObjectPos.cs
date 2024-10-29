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

            Vector3 unitPos = unit.Position;
            
            Ray ray = new(new Vector3(unitPos.x, unitPos.y + 100, unitPos.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                transform.position = hit.point;    
            }
            else
            {
                transform.position = unit.Position;
            }
            
            await ETTask.CompletedTask;
        }
    }
}