using UnityEngine;

namespace ET.Client
{
    public class BTCreateFakeBulletHandler: ABTHandler<BTCreateFakeBullet>
    {
        protected override int Run(BTCreateFakeBullet node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);

            GameObject targetGameObject = target.GetComponent<GameObjectComponent>().GameObject;
            
            GameObject gameObject = EffectUnitHelper.Create(caster, node.CasterBindPoint, node.Effect, true, 0);

            Transform bindPoint = targetGameObject.GetComponent<BindPointComponent>().BindPoints[node.TargetBindPoint];

            MoveToTarget(caster.Root(), gameObject.transform, bindPoint, node).NoContext();;
            return 0;
        }
        
        private static async ETTask MoveToTarget(Scene root, Transform gameObject, Transform bindPoint, BTCreateFakeBullet node)
        {
            TimerComponent timerComponent = root.GetComponent<TimerComponent>();
            float startTime = Time.time;
            while (Time.time - startTime < node.Duration)
            {
                Vector3 v = bindPoint.position - gameObject.position;
                
                gameObject.position += v.normalized * node.Speed / 1000f * Time.deltaTime;
                
                v = bindPoint.position - gameObject.position;
                if (v.magnitude < 0.3f)
                {
                    break;
                }
                
                gameObject.forward = v;
                
                await timerComponent.WaitAsync(1);
            }
            UnityEngine.Object.Destroy(gameObject.gameObject);
        }
    }
}