namespace ET.Server
{
	public struct UnitEnterSightRange
	{
		public EntityRef<Unit> A;
		public EntityRef<Unit> B;
	}

	public struct UnitLeaveSightRange
	{
		public EntityRef<Unit> A;
		public EntityRef<Unit> B;
	}
}