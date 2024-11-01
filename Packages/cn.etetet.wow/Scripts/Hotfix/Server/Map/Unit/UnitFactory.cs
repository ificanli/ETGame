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
            numericComponent.Set(NumericType.Speed, unitConfig.Speed / 1000f); // 速度是6米每秒
            numericComponent.Set(NumericType.AOI, unitConfig.AOI / 1000); // 视野15米
            unit.AddComponent<MoveComponent>();
            // 加入aoi
            unit.AddComponent<AOIEntity, int, float3>(numericComponent.GetAsInt(NumericType.AOI), unit.Position);
            
            switch (unitConfig.Type)
            {
                case UnitType.Player:
                {
                    break;
                }
                case UnitType.Monster:
                {
                    //unit.AddComponent<AIComponent, int>(unitConfig.AI);
                    break;
                }
            }
            unitComponent.Add(unit);
            return unit;
        }
    }
}