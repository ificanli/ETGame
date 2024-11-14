namespace ET
{
	public enum EffectTimeType
	{
		ServerSpellAdd = 1,
		ServerSpellHit = 2,
		ServerSpellRemove = 3,
		ServerBuffAdd = 4,
		ServerBuffRemove = 5,
		ServerBuffTick = 6,
		ServerBuffBeAttack = 13,
		ClientSpellAdd = 7,
		ClientSpellHit = 8,
		ClientSpellRemove = 9,
		ClientBuffAdd = 10,
		ClientBuffRemove = 11,
		ClientBuffTick = 12,
	}
	
	public interface IEffectTimeArgs
	{
		EffectTimeType EffectTimeType { get; }
		Scene Scene { get; }
	}

	public struct ServerSpellAdd : IEffectTimeArgs
	{
		public EffectTimeType EffectTimeType
		{
			get
			{
				return EffectTimeType.ServerSpellAdd;
			}
		}
		
		public Scene Scene { get; }
		
		public Spell Spell { get; }

		public ServerSpellAdd(Scene scene, Spell spell)
		{
			this.Scene = scene;
			this.Spell = spell;
		}
	}
	
	public struct ServerSpellHit : IEffectTimeArgs
	{
		public EffectTimeType EffectTimeType
		{
			get
			{
				return EffectTimeType.ServerSpellHit;
			}
		}

		public Scene Scene { get; }
		
		public Spell Spell { get; }

		public ServerSpellHit(Scene scene, Spell spell)
		{
			this.Scene = scene;
			this.Spell = spell;
		}
	}
	
	public struct ServerSpellRemove : IEffectTimeArgs
	{
		public EffectTimeType EffectTimeType
		{
			get
			{
				return EffectTimeType.ServerSpellRemove;
			}
		}

		public Scene Scene { get; }
		
		public Spell Spell { get; }

		public ServerSpellRemove(Scene scene, Spell spell)
		{
			this.Scene = scene;
			this.Spell = spell;
		}
	}
	
	public struct ServerBuffAdd : IEffectTimeArgs
	{
		public EffectTimeType EffectTimeType
		{
			get
			{
				return EffectTimeType.ServerBuffAdd;
			}
		}

		public Scene Scene { get; }
		
		public Spell Spell { get; }

		public ServerBuffAdd(Scene scene, Spell spell)
		{
			this.Scene = scene;
			this.Spell = spell;
		}
	}
	
	public struct ClientSpellAdd : IEffectTimeArgs
	{
		public EffectTimeType EffectTimeType
		{
			get
			{
				return EffectTimeType.ClientSpellAdd;
			}
		}

		public Scene Scene { get; }
		
		public Spell Spell { get; }

		public ClientSpellAdd(Scene scene, Spell spell)
		{
			this.Scene = scene;
			this.Spell = spell;
		}
	}
	
	public struct ClientBuffAdd : IEffectTimeArgs
	{
		public EffectTimeType EffectTimeType
		{
			get
			{
				return EffectTimeType.ClientBuffAdd;
			}
		}

		public Scene Scene { get; }
		
		public Buff Buff { get; }

		public ClientBuffAdd(Scene scene, Buff buff)
		{
			this.Scene = scene;
			this.Buff = buff;
		}
	}
}