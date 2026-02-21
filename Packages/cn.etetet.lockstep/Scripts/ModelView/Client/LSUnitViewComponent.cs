namespace ET.Client
{
	[ComponentOf(typeof(Room))]
	public class LSUnitViewComponent: Entity, IAwake, IDestroy
	{
		public EntityRef<LSUnitView> myUnitView;
	}
}