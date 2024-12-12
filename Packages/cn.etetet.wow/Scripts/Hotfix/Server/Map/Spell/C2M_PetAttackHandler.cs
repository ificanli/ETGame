
namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class C2M_PetAttackHandler : MessageLocationHandler<Unit, C2M_PetAttack>
	{
		protected override async ETTask Run(Unit unit, C2M_PetAttack message)
		{
			Unit target = unit.GetParent<UnitComponent>().Get(message.UnitId);
			if (target == null)
			{
				return;
			}

			Unit pet = PetHelper.GetPet(unit);
			if (pet == null)
			{
				return;
			}
			pet.GetComponent<TargetComponent>().Unit = target;

			await ETTask.CompletedTask;
		}
	}
}