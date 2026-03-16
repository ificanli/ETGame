namespace ET.Server
{
    [Event(SceneType.Map)]
    public class NumericChangeEvent_Broadcast: AEvent<Scene, NumbericChange>
    {
        protected override async ETTask Run(Scene scene, NumbericChange a)
        {
            Unit unit = a.Unit;
            if (unit == null || unit.IsDisposed || scene == null || scene.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            NumericTypeConfig numericTypeConfig = NumericTypeConfigCategory.Instance.Get(a.NumericType);
            if (numericTypeConfig == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            M2C_NumericChange m2CNumericChange = M2C_NumericChange.Create();
            m2CNumericChange.NumericType = a.NumericType;
            m2CNumericChange.UnitId = unit.Id;
            m2CNumericChange.Value = a.New;
            MapMessageHelper.NoticeClient(unit, m2CNumericChange, numericTypeConfig.NoticeType);

            await ETTask.CompletedTask;
        }
    }
}
