using Unity.Mathematics;

namespace ET.Server
{
    public class AI_MonsterReturnCheckHandler: ABTHandler<AI_MonsterReturnCheck>
    {
        protected override int Run(AI_MonsterReturnCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            NumericComponent numericComponent = unit.NumericComponent;
            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X),
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            if (math.distance(unit.Position, birthPos) < 30f)
            {
                return 1;
            }
            return 0;
        }
    }
}
