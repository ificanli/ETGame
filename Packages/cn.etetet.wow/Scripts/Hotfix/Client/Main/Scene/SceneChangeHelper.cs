namespace ET.Client
{
    public static partial class SceneChangeHelper
    {
        // 场景切换协程
        public static async ETTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
        {
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的
            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
            
            EventSystem.Instance.Publish(root, new SceneChangeStart());          // 可以订阅这个事件中创建Loading界面
                        
            await WaitUnitCreateFinish(root, currentScene);
            EventSystem.Instance.Publish(root, new SceneChangeFinish());
            // 加载场景寻路数据
            await NavmeshComponent.Instance.Load(sceneName);
            
            using CoroutineLock coroutineLock = await root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.SceneChange, 0);
            // 通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
            currentScenesComponent.Progress = 100;
        }

        private static async ETTask WaitUnitCreateFinish(Scene root, Scene currentScene)
        {
            UnitComponent unitComponent = currentScene.AddComponent<UnitComponent>();
            // 等待CreateMyUnit的消息
            Wait_CreateMyUnit waitCreateMyUnit = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
            unit.AddComponent<TargetComponent>();
            unit.AddComponent<SpellComponent>();
            unitComponent.Add(unit);
            
            EventSystem.Instance.Publish(currentScene, new AfterMyUnitCreate() {Unit = unit});
        }
    }
}