using System;
using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class M2M_UnitTransferRequestHandler: MessageHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
    {
        protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();

            Unit unit = request.Unit;
            if (unit != null)  // 黑科技，直接传送Unit对象
            {
                unitComponent.AddChild(unit);
                unitComponent.Add(unit);
            }
            else
            {
                unit = MongoHelper.Deserialize<Unit>(request.UnitBytes);

                unitComponent.AddChild(unit);
                unitComponent.Add(unit);

                foreach (byte[] bytes in request.EntityBytes)
                {
                    Entity entity = MongoHelper.Deserialize<Entity>(bytes);
                    unit.AddComponent(entity);
                }
            }

            unit.AddComponent<TurnComponent>();
            unit.AddComponent<MoveComponent>();
            string mapName = scene.Name.GetSceneConfigName();
            if (mapName != "Home")
            {
                unit.AddComponent<PathfindingComponent, string>(mapName);
            }
            unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);
            unit.AddComponent<TargetComponent>();

            // 通知客户端开始切场景
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneId = scene.Id;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.NoticeClient(unit, m2CStartSceneChange, NoticeType.Self);

            if (request.ChangeScene)
            {
                // 通知客户端创建My Unit
                M2C_CreateMyUnit m2CCreateUnits = M2C_CreateMyUnit.Create();
                m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
                MapMessageHelper.NoticeClient(unit, m2CCreateUnits, NoticeType.Self);
            }

            // 加入aoi
            unit.AddComponent<AOIEntity>();

            response.NewActorId = unit.GetActorId();
            await ETTask.CompletedTask;
        }
    }
}