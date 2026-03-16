namespace ET.Server
{
    public class BTApplyDetectorHandler : ABTHandler<BTApplyDetector>
    {
        protected override int Run(BTApplyDetector node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return 1;
            }

            if (node.RevealRadius <= 0f)
            {
                return 1;
            }

            DetectorComponent detectorComponent = buff.GetComponent<DetectorComponent>() ?? buff.AddComponent<DetectorComponent>();
            detectorComponent.OwnerUnitId = unit.Id;
            detectorComponent.RevealRadius = node.RevealRadius;
            return 0;
        }
    }
}
