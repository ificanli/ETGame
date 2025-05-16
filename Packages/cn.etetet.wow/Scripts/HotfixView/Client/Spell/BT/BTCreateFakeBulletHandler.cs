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
            Transform targetTransform = targetGameObject.GetComponent<BindPointComponent>().BindPoints[node.TargetBindPoint];
            
            CreateEffectOnPosAsync(caster, targetTransform, node.Effect.Name, node.CasterBindPoint, node.Duration, node).NoContext();
            
            return 0;
        }
        
        private static async ETTask CreateEffectOnPosAsync(
                Unit caster, Transform target, string effectName, 
                BindPoint casterBindPoint, int duration, BTCreateFakeBullet node)
        {
            EntityRef<Unit> casterRef = caster;
            GameObject go = await caster.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(effectName);
            caster = casterRef;
            GameObject effect = EffectUnitHelper.Create(caster, casterBindPoint, go, true, duration);
            await MoveToTarget(caster.Root(), effect.transform, target, node);;
        }
        
        private static async ETTask MoveToTarget(Scene root, Transform gameObject, Transform bindPoint, BTCreateFakeBullet node)
        {
            TimerComponent timerComponent = root.GetComponent<TimerComponent>();
            EntityRef<TimerComponent> timerComponentRef = timerComponent;
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
                
                timerComponent = timerComponentRef;
                await timerComponent.WaitAsync(1);
            }
            UnityEngine.Object.Destroy(gameObject.gameObject);
        }
    }
}