namespace ET.Server
{
	public struct UnitEnterSightRange
	{
		public EntityRef<AOIEntity> A;
		public EntityRef<AOIEntity> B;
	}

	public struct UnitLeaveSightRange
	{
		public EntityRef<AOIEntity> A;
		public EntityRef<AOIEntity> B;
	}
}