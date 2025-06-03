namespace ET.Server
{
    [NumericWatcher(SceneType.Map, (int)NumericType.Phase)]
    public class NumericChange_PhaseChange: INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            // Phase改变，将unit从aoi中移除，再重新加入aoi
            unit.RemoveComponent<AOIEntity>();
            unit.AddComponent<AOIEntity>();
        }
    }
}