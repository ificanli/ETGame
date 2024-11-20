using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(SpellIndicatorComponent))]
    public static partial class SpellIndicatorComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpellIndicatorComponent self)
        {

        }

        [EntitySystem]
        private static void Update(this SpellIndicatorComponent self)
        {
            if (self.Current is null)
            {
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Map")))
            {
                float3 pos = hit.point;
                Unit unit = self.GetParent<Unit>();
                if (Vector3.Distance(unit.Position, pos) > self.MaxDistance / 1000f)
                {
                    pos = unit.Position + math.normalize(pos - unit.Position) * self.MaxDistance / 1000f;
                }

                self.Current.position = pos + new float3(0, 0.1f, 0);
            }
            
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector3 pos = self.Current.position;
                self.Current.gameObject.SetActive(false);
                self.Current.position = new Vector3(10000, 10000, 10000);
                self.Current.localScale = new Vector3(1, 1, 1);
                self.Current = null;
                self.Task?.SetResult(pos);
            }
        }

        public static async ETTask<Vector3> WaitSpellIndicator(this SpellIndicatorComponent self, TargetSelectorCircle targetSelectorCircle)
        {
            self.Task = ETTask<Vector3>.Create();

            GameObject go = targetSelectorCircle.SpellIndicator;
            if (!self.Cache.TryGetValue(go.GetInstanceID(), out GameObject gameObject))
            {
                gameObject = UnityEngine.Object.Instantiate(go, GameObject.Find("/Global/Unit").transform);
                self.Cache.Add(go.GetInstanceID(), gameObject);
            }

            gameObject.SetActive(true);
            gameObject.transform.localScale *= targetSelectorCircle.Radius / 1000f;
            self.Current = gameObject.transform;
            self.MaxDistance = targetSelectorCircle.MaxDistance;
            
            return await self.Task;
        }
    }
}