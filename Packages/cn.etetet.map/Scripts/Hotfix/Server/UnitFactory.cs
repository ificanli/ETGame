using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static partial class UnitFactory
    {
        public static Unit Create(Scene scene, long id, int configId)
        {
            return Create(scene, id, configId, float3.zero, quaternion.identity, true);
        }

        public static Unit Create(Scene scene, long id, int configId, float3 initialPosition, quaternion initialRotation, bool createInitialAiBuff)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            
            Unit unit = unitComponent.AddChildWithId<Unit, int>(id, configId);
            UnitConfig unitConfig = unit.Config();
            
            unit.UnitType = unitConfig.UnitType;
            
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            unit.AddComponent<UnitDisplayLevelComponent, int>(1);
            foreach ((int k, long v) in unitConfig.KV)
            {
                numericComponent.SetNoEvent(k, v);
            }

            // 初始落点统一交给运行时控制：
            // 玩家/机器人由 SpawnPoint ECA 决定，怪物/NPC 由调用方或 MonsterSpawnPoint ECA 显式赋值。
            unit.Position = initialPosition;
            unit.Rotation = initialRotation;
            unit.AddComponent<UnitSpawnPointComponent, float3, quaternion>(unit.Position, unit.Rotation);
            
            unit.AddComponent<MoveComponent>();
            unit.AddComponent<TurnComponent>();
            // 加入aoi
            unit.AddComponent<AOIEntity>();
            unit.AddComponent<TargetComponent>();
            unit.AddComponent<SpellComponent>();
            unit.AddComponent<BuffComponent>();
            
            switch (unit.UnitType)
            {
                case UnitType.Player:
                {
                    unit.AddComponent<ItemComponent>();
                    unit.AddComponent<EquipmentComponent>();
                    unit.AddComponent<QuestComponent>();
                    unit.AddComponent<CampComponent, int>(1); // 玩家阵营ID=1
                    break;
                }
                case UnitType.Monster:
                {
                    unit.AddComponent<ThreatComponent>();
                    unit.AddComponent<PathfindingComponent, string>(scene.Name.GetSceneConfigName());
                    unit.AddComponent<CampComponent, int>(2); // 怪物阵营ID=2
                    break;
                }
                case UnitType.NPC:
                {
                    break;
                }
                case UnitType.Virtual:
                {
                    break;
                }
            }
            
            int ai = numericComponent.GetAsInt(NumericType.AI);
            if (createInitialAiBuff && ai != 0)
            {
                if (BuffConfigCategory.Instance.Contain(ai))
                {
                    BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), ai, null);
                }
                else
                {
                    Log.Warning($"[UnitFactory] skip ai buff: missing buff config, unitId={unit.Id}, unitConfigId={configId}, aiBuffConfigId={ai}");
                }
            }
            
            unitComponent.Add(unit);
            LogCampTrace(unit, "UnitFactory.Create");
            return unit;
        }

        public static Unit CreatePet(Scene scene, Unit owner, long id, int configId)
        {
            Unit pet = Create(scene, id, configId);
            pet.AddComponent<PetComponent>().OwnerId = owner.Id;

            UnitPetComponent unitPetComponent = owner.GetComponent<UnitPetComponent>() ?? owner.AddComponent<UnitPetComponent>();
            unitPetComponent.PetId = pet.Id;
            return pet;
        }

        private static void LogCampTrace(Unit unit, string stage)
        {
            CampComponent camp = unit.GetComponent<CampComponent>();
            Log.Info($"[CampTrace][{stage}] unitId={unit.Id}, unitType={unit.UnitType}, campId={camp?.CampId ?? 0}, campType={camp?.CampType.ToString() ?? "None"}");
        }
    }
}
