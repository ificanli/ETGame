using System.Threading;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTFakeBulletDelayHandler: ABTHandler<BTFakeBulletDelay>
    {
        protected override int Run(BTFakeBulletDelay node, BTEnv env)
        {
            BTEnv newEnv = BTEnv.Create();
            env.CopyTo(newEnv);
            Delay(node, newEnv).NoContext();
            return 0;
        }

        private async ETTask Delay(BTFakeBulletDelay node, BTEnv env)
        {
            using BTEnv _ = env;
            Spell spell = env.GetEntity<Spell>(node.Spell);
            if (spell == null)
            {
                return;
            }
            
            Unit target = env.GetEntity<Unit>(node.Target);
            if (target == null)
            {
                return;
            }
            Unit caster = spell.Caster;
            float distance = math.distance(caster.Position, target.Position);

            float time = distance / (node.Speed / 1000f);

            await caster.Root().GetComponent<TimerComponent>().WaitAsync((int)(time * 1000));
            spell = env.GetEntity<Spell>(node.Spell);
            if (spell == null)
            {
                return;
            }
            target = env.GetEntity<Unit>(node.Target);
            if (target == null)
            {
                return;
            }

            foreach (BTNode child in node.Children)
            {
                BTDispatcher.Instance.Handle(child, env);
            }
        }
    }
}