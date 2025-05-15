using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.Map)]
    public class FiberInit_Map: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            EntityRef<Scene> rootRef = root;
            
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            UnitComponent unitComponent = root.AddComponent<UnitComponent>();
            
            root.AddComponent<AOIManagerComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            
            EntityRef<UnitComponent> unitComponentRef = unitComponent;
            // 加载场景寻路数据
            await NavmeshComponent.Instance.Load(root.Name);
            
            root = rootRef;
            unitComponent = unitComponentRef;
            
            foreach ((int _, UnitConfig unitConfig) in UnitConfigCategory.Instance.GetAll())
            {
                if (unitConfig.UnitType == UnitType.Player)
                {
                    continue;
                }

                
                Unit unit = UnitFactory.Create(root, IdGenerater.Instance.GenerateId(), unitConfig.Id);
                unitComponent.Add(unit);
            }

            await ETTask.CompletedTask;
        }
    }
}