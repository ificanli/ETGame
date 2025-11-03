using System;

namespace ET
{
	public struct UpdateEvent
	{
	}
	
	public interface IUpdate: IEvent<UpdateEvent>
	{
	}

	[EntitySystem]
	public abstract class UpdateSystem<T> : EventSystem<T, UpdateEvent> where T: Entity, IUpdate
	{
		protected override void Event(T e, UpdateEvent t)
		{
			this.Update(e);
		}

		protected abstract void Update(T self);
	}
}
