using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            // Unit View层
            string assetsName = $"Packages/cn.etetet.wow/Bundles/Units/{unit.Config().Name}.prefab";
            GameObject bundleGameObject = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);

            GlobalComponent globalComponent = scene.Root().GetComponent<GlobalComponent>();
            GameObject go = UnityEngine.Object.Instantiate(bundleGameObject, globalComponent.Unit, true);
            go.AddComponent<GameObjectEntityRef>().EntityRef = unit;
            go.transform.position = unit.Position;
            GameObjectPosHelper.OnTerrain(go.transform);
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<AnimatorComponent>();
            
            if (scene.Root().GetComponent<PlayerComponent>().MyId == unit.Id)
            {
                unit.AddComponent<CinemachineComponent>();
                unit.AddComponent<InputSystemComponent>();
                unit.AddComponent<SpellIndicatorComponent>();
            }
            
            await ETTask.CompletedTask;
        }
    }
}