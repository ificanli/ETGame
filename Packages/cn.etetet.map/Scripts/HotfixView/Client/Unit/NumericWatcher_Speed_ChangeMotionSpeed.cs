namespace ET.Client
{
	/// <summary>
	/// 客户端监视角色能力移速变化。
	/// 动画 MoveSpeed 不能直接使用该数值，否则静止时也会保持跑动速度。
	/// </summary>
	[NumericWatcher(SceneType.Current, NumericType.Speed)]
	public class NumericWatcher_Speed_ChangeMotionSpeed : INumericWatcher
	{
        public void Run(Unit unit, NumbericChange args)
        {
            // no-op: 动画速度由 MoveStart/MoveStop 和可视位移刷新驱动
        }
    }
}
