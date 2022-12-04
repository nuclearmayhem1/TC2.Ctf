namespace TC2.Ctf;

public static partial class Ctf
{


	[IGamemode.Data("Siege", "")]
	public partial struct Gamemode : IGamemode
	{
		public float elapsed = 0;
		public bool paused = false;

		[IGlobal.Data(false, Net.SendType.Reliable)]
		public partial struct State : IGlobal
		{
			[Save.Ignore] public float graceEndTimestamp = 300;

			[Save.Ignore] public IFaction.Handle faction_blue = "blue";
			[Save.Ignore] public IFaction.Handle faction_red = "red";

			[Save.Ignore] public FixedArray8<KillFeedKill> killFeed = new FixedArray8<KillFeedKill>();

			public State()
			{

			}
		}

		public Gamemode()
		{

		}

		public static void Configure()
		{
			Constants.Materials.global_yield_modifier = 1f;
			Constants.Harvestable.global_yield_modifier = 1f;
			Constants.Block.global_yield_modifier = 1f;

			Constants.Organic.rotting_speed *= 10.00f;

			Constants.World.save_factions = false;
			Constants.World.save_players = true;
			Constants.World.save_characters = false;

			Constants.World.load_factions = false;
			Constants.World.load_players = true;
			Constants.World.load_characters = false;

			Constants.World.enable_autosave = false;

			Constants.Respawn.token_count_min = 10f;
			Constants.Respawn.token_count_max = 10f;
			Constants.Respawn.token_count_default = 10f;
			Constants.Respawn.token_refill_amount = 0.001f;

			Constants.Respawn.respawn_cooldown_base = 10.00f;
			Constants.Respawn.respawn_cooldown_token_modifier = 0.00f;
			Constants.Respawn.respawn_cost_base = 0.00f;

			Constants.Characters.allow_custom_characters = false;
			Constants.Characters.allow_switching = true;
			Constants.Characters.character_count_max = 1;

			Constants.Equipment.enable_equip = true;
			Constants.Equipment.enable_unequip = false;

			Constants.Factions.enable_join_faction = false;
			Constants.Factions.enable_leave_faction = false;
			Constants.Factions.enable_found_faction = false;
			Constants.Factions.enable_disband_faction = false;
			Constants.Factions.enable_kick = false;
			Constants.Factions.enable_leadership = false;

			Constants.Questing.enable_quests = false;


			IRecipe.Database.AddAssetFilter((string path, string identifier, ModInfo mod_info) =>
			{
				if (identifier.Contains("build.headquarters", StringComparison.OrdinalIgnoreCase)) return false;
				else if (identifier.Contains("build.outpost", StringComparison.OrdinalIgnoreCase)) return false;
				else if (identifier == "workbench.crossbow") return false;
				else if (identifier == "workbench.bow") return false;
				else return true;
			});


#if SERVER
			Player.OnCreate += OnPlayerCreate;
			static void OnPlayerCreate(ref Region.Data region, ref Player.Data player)
			{
				if (!player.GetControlledCharacter().IsValid())
				{
					var ent_character_soldier = Character.Create(ref region, "Soldier", prefab: "human.male", flags: Character.Flags.Human | Character.Flags.Military, origin: "soldier", gender: Organic.Gender.Male, player_id: player.id, hair_frame: 5, beard_frame: 1);
				}
			}
#endif
		}

		public static void Init()
		{
			App.WriteLine("Ctf Init!", App.Color.Magenta);
		}
	}

	[ISystem.VeryLateUpdate(ISystem.Mode.Single)]
	public static void OnUpdate(ISystem.Info info, [Source.Global] ref Ctf.Gamemode ctf)
	{
        if (!ctf.paused)
        {
			ctf.elapsed += info.DeltaTime;
		}
	}

#if SERVER
	[ISystem.AddFirst(ISystem.Mode.Single)]
	public static void OnAdd(ISystem.Info info, [Source.Owned] ref MapCycle.Global mapcycle)
	{
		ref var region = ref info.GetRegion();
		mapcycle.AddMaps(ref region, "ctf");
	}
#endif

	public struct KillFeedKill
    {
		public Player.Data victim = new Player.Data();
		public FixedString256 weapon = " ";
		public FixedString256 bodypart = " ";
		public Damage.Type damage = Damage.Type.None;
		public float timestamp = -1;
		public FixedString256 owner = " ";
		public Color32BGRA owner_color = Color32BGRA.Grey;

        public KillFeedKill()
        {
        }
    }

}