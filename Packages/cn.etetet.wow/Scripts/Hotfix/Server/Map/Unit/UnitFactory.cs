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
            
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            for (int i = 0; i < unitConfig.KV.Length; i += 2)
            {
                int k = unitConfig.KV[i];
                int v = unitConfig.KV[i + 1];
                if (v == 0)
                {
                    continue;
                }
                numericComponent.SetNoEvent(k, v);
            }
            
            unit.Position = new float3(numericComponent.GetAsFloat(NumericType.X), numericComponent.GetAsFloat(NumericType.Y), numericComponent.GetAsFloat(NumericType.Z));
            
            unit.AddComponent<MoveComponent>();
            unit.AddComponent<TurnComponent>();
            // 加入aoi
            unit.AddComponent<AOIEntity>();
            unit.AddComponent<TargetComponent>();
            unit.AddComponent<SpellComponent>();
            unit.AddComponent<BuffComponent>();
            
            switch ((UnitType)unitConfig.Type)
            {
                case UnitType.Player:
                {
                    break;
                }
                case UnitType.Monster:
                {
                    unit.AddComponent<ThreatComponent>();
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