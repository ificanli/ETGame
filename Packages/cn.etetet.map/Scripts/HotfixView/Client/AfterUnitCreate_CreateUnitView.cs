using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            if (scene.Name.GetSceneConfigName() == "Home")
            {
                return;
            }

            EntityRef<Scene> sceneRef = scene;
            Unit unit = args.Unit;
            EntityRef<Unit> unitRef = unit;
            // Unit View层
            string assetsName = $"Packages/cn.etetet.map/Bundles/Units/{unit.Config().Name}.prefab";
            GameObject bundleGameObject = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);

            scene = sceneRef;
            GlobalComponent globalComponent = scene.Root().GetComponent<GlobalComponent>();
            GameObject go = UnityEngine.Object.Instantiate(bundleGameObject, globalComponent.Unit, true);
            unit = unitRef;
            go.AddComponent<GameObjectEntityRef>().Entity = unit;
            go.transform.position = unit.Position;
            go.transform.rotation = unit.Rotation;

            {
                using var _ = await scene.Root().CoroutineLockComponent.Wait(CoroutineLockType.SceneChange, 0);
                GameObjectPosHelper.OnTerrain(go.transform);
            }

            GameObjectComponent gameObjectComponent = unit.AddComponent<GameObjectComponent>();
            gameObjectComponent.GameObject = go;
            gameObjectComponent.CacheRenderers();
            unit.AddComponent<UnitViewInterpolationComponent>();
            unit.AddComponent<AnimatorComponent>();
            gameObjectComponent.RefreshLocalConcealmentVisual(unit, go.transform.position);
            
            if (scene.Root().GetComponent<PlayerComponent>().MyId == unit.Id)
            {
                if (unit.GetComponent<PathfindingComponent>() == null)
                {
                    unit.AddComponent<PathfindingComponent, string>(scene.Name.GetSceneConfigName());
                }

                unit.AddComponent<CinemachineComponent>();
                unit.AddComponent<InputSystemComponent>();
                unit.AddComponent<SpellIndicatorComponent>();
            }

            // 头顶血条需要对所有战斗单位生效，不能排除本地玩家单位；
            // 但 Virtual Unit（如尸体盒、交互点）不应创建血条，否则会在搜索阶段错误显示。
            //await YIUIFactory.InstantiateAsync<HPView3DComponent>(unit, go.transform);
            int campId = unit.GetComponent<CampComponent>()?.CampId ?? 0;
            bool shouldShowHpView = unit.UnitType != UnitType.Virtual &&
                !(unit.UnitType == UnitType.NPC && campId <= 0);
            if (shouldShowHpView)
            {
                await YIUIFactory.InstantiateAsync<HPViewComponent>(scene, unit, scene.YIUIMgr().UICache);
            }
            
            await ETTask.CompletedTask;
        }
    }
}
