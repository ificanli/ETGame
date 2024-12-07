namespace ET
{
	[NumericWatcher(SceneType.WOW, (int)NumericType.HP)]
	public class NumericWatcher_Hp : INumericWatcher
	{
		public void Run(Unit unit, NumbericChange args)
		{
			//临时处理全监听
			unit.DynamicEvent(args).NoContext();
		}
	}
}
