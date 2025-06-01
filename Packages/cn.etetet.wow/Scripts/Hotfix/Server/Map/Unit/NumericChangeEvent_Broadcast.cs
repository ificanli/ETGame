namespace ET.Server
{
    [Event(SceneType.Map)]
    public class NumericChangeEvent_Broadcast: AEvent<Scene, NumbericChange>
    {
        protected override async ETTask Run(Scene scene, NumbericChange a)
        {
            NumericTypeConfig numericTypeConfig = NumericTypeConfigCategory.Instance.Get(a.NumericType);
            M2C_NumericChange m2CNumericChange = M2C_NumericChange.Create();
            m2CNumericChange.NumericType = a.NumericType;
            m2CNumericChange.UnitId = a.Unit.Id;
            m2CNumericChange.Value = a.New;
            MapMessageHelper.NoticeClient(a.Unit, m2CNumericChange, numericTypeConfig.NoticeType);

            await ETTask.CompletedTask;
        }
    }
}