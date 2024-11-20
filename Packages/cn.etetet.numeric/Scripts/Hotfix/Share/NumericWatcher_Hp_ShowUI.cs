namespace ET
{
	/// <summary>
	/// 客户端监视hp数值变化，改变血条值
	/// </summary>
	[NumericWatcher(SceneType.Current, (int)NumericType.HP)]
	public class NumericWatcher_Hp_ShowUI : INumericWatcher
	{
		public void Run(Unit unit, NumbericChange args)
		{
			Log.Debug($"unit hp: {unit.Id} {unit.Config().Name} HP: {args.Old} -> {args.New}");
		}
	}
}
