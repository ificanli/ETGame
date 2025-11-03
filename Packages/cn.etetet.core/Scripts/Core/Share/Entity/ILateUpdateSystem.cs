using System;

namespace ET
{
	public struct LateUpdateEvent
	{
	}
	
	public interface ILateUpdate: IEvent<LateUpdateEvent>
	{
	}

	[EntitySystem]
	public abstract class LateUpdateSystem<T> : EventSystem<T, LateUpdateEvent> where T: Entity, ILateUpdate
	{
		protected override void Event(T e, LateUpdateEvent t)
		{
			this.LateUpdate(e);
		}

		protected abstract void LateUpdate(T self);
	}
}
