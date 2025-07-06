namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCostNumericHandler: ABTHandler<BTCostNumeric>
    {
        protected override int Run(BTCostNumeric node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            bool check = env.GetStruct<bool>(node.Check);

            NumericComponent numericComponent = caster.GetComponent<NumericComponent>();
            long a = numericComponent.Get(node.NumericType);
            
            if (check)
            {
                if (a >= node.Value)
                {
                    return 0;
                }

                switch (node.NumericType)
                {
                    case NumericType.MP:
                        return TextConstDefine.SpellCast_MPNotEnought;
                    case NumericType.HP:
                        return TextConstDefine.SpellCast_HPNotEnought;
                }
            }
            else
            {
                numericComponent.Set(node.NumericType, a - node.Value);
            }

            return 0;
        }
    }
}