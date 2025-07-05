namespace ET.Client
{
	/// <summary>
	/// 客户端监视speed数值变化，改变血条值
	/// </summary>
	[NumericWatcher(SceneType.Current, (int)NumericType.Speed)]
	public class NumericWatcher_Speed_ChangeMotionSpeed : INumericWatcher
	{
		public void Run(Unit unit, NumbericChange args)
		{
			unit.GetComponent<AnimatorComponent>().SetFloat(nameof(MotionType.MoveSpeed), args.New / 1000f);
		}
	}
}
