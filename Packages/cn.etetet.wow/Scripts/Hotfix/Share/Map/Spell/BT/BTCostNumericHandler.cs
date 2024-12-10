namespace ET
{
    public class BTCostNumericHandler: ABTHandler<CostNumeric>
    {
        protected override int Run(CostNumeric node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);

            NumericComponent numericComponent = caster.GetComponent<NumericComponent>();
            long a = numericComponent.Get(node.NumericType);
            if (a < node.Value)
            {
                switch (node.NumericType)
                {
                    case NumericType.MP:
                        return TextConstDefine.SpellCast_MPNotEnought;
                    case NumericType.HP:
                        return TextConstDefine.SpellCast_HPNotEnought;
                }
            }
            numericComponent.Set(node.NumericType, a - node.Value);
            return 0;
        }
    }
}