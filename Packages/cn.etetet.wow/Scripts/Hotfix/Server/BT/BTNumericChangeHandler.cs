namespace ET.Server
{
    public class BTNumericChangeHandler: ABTHandler<BTNumericChange>
    {
        protected override int Run(BTNumericChange node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            long value = numericComponent.GetAsLong(node.NumericType);
            numericComponent.Set(node.NumericType, value + node.Value);
            
            
            if ((int)node.NumericType < NumericComponentSystem.Max)
            {
                return 0;
            }
            
            
            Buff buff = env.GetEntity<Buff>(node.Buff);
            // buff删除的时候会还原数值
            BuffChangeNumericRecordComponent buffChangeNumericRecordComponent = 
                    buff.GetComponent<BuffChangeNumericRecordComponent>() ??
                    buff.AddComponent<BuffChangeNumericRecordComponent>();
            buffChangeNumericRecordComponent.Add(node.NumericType, node.Value);
            
            return 0;
        }
    }
}