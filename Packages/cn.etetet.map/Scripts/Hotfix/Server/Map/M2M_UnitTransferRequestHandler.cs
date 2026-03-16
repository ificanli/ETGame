namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class M2M_UnitTransferRequestHandler : MessageHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
    {
        protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            string mapName = scene.Name.GetSceneConfigName();

            Unit unit = request.Unit;
            if (unit != null)
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

            MapUnitEnterHelper.EnsureMapRuntimeComponents(scene, unit);
            MapUnitEnterHelper.ApplyAssignedTeamIfNeeded(scene, unit, request.TeamId);
            if (request.ChangeScene)
            {
                MapUnitEnterHelper.ApplySpawnPointIfNeeded(scene, unit, request.TeamId);
            }

            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneId = scene.Id;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.NoticeClient(unit, m2CStartSceneChange, NoticeType.Self);

            if (request.ChangeScene)
            {
                M2C_CreateMyUnit m2CCreateUnits = M2C_CreateMyUnit.Create();
                m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
                MapMessageHelper.NoticeClient(unit, m2CCreateUnits, NoticeType.Self);
            }

            if (unit.GetComponent<AOIEntity>() == null)
            {
                unit.AddComponent<AOIEntity>();
            }

            MapUnitEnterHelper.InitializePlayerGameplay(unit);
            if (unit.UnitType == UnitType.Player && mapName == "Home")
            {
                HomeEnterHelper.OnEnterHome(unit);
            }

            if (request.ChangeScene)
            {
                MapUnitEnterHelper.SetupMatchRobotIfNeeded(scene, unit);
                MatchCopyContextHelper.OnHumanPlayerEntered(scene, unit);
            }

            response.NewActorId = unit.GetActorId();
            await ETTask.CompletedTask;
        }
    }
}
