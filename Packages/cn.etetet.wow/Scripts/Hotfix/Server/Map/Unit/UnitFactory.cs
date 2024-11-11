using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static partial class UnitFactory
    {
        public static Unit Create(Scene scene, long id, int configId)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            
            Unit unit = unitComponent.AddChildWithId<Unit, int>(id, configId);
            UnitConfig unitConfig = unit.Config();
            unit.Position = new float3(unitConfig.Position[0], unitConfig.Position[1], unitConfig.Position[2]) / 1000f;
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();

            for (int i = 0; i < unitConfig.KV.Length; i += 2)
            {
                numericComponent.Set(unitConfig.KV[i], unitConfig.KV[i + 1]);
            }
            
            unit.AddComponent<MoveComponent>();
            // 加入aoi
            unit.AddComponent<AOIEntity>();
            unit.AddComponent<TargetComponent>();
            
            switch (unitConfig.Type)
            {
                case UnitType.Player:
                {
                    break;
                }
                case UnitType.Monster:
                {
                    unit.AddComponent<SpellComponent>();
                    unit.AddComponent<PathfindingComponent, string>(scene.Name);
                    
                    int ai = numericComponent.GetAsInt(NumericType.AI);
                    if (ai != 0)
                    {
                        unit.AddComponent<AIComponent, int>(ai);
                    }
                    break;
                }
            }
            unitComponent.Add(unit);
            return unit;
        }
    }
}