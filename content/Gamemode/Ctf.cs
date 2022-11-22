namespace TC2.Ctf;

public static partial class Ctf
{


	[IGamemode.Data("Siege", "")]
	public partial struct Gamemode : IGamemode
	{
		[IGlobal.Data(false, Net.SendType.Reliable)]
		public partial struct State : IGlobal
		{
			[Save.Ignore] public float graceEndTimestamp = 300;

			[Save.Ignore] public IFaction.Handle faction_blue = "blue";
			[Save.Ignore] public IFaction.Handle faction_red = "red";

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

			Constants.Respawn.respawn_cooldown_base = 5.00f;
			Constants.Respawn.respawn_cooldown_token_modifier = 0.00f;
			Constants.Respawn.respawn_cost_base = 0.00f;

			Constants.Characters.allow_custom_characters = false;
			Constants.Characters.allow_switching = true;

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
				else return true;
			});


#if SERVER
			Player.OnCreate += OnPlayerCreate;
			static void OnPlayerCreate(ref Region.Data region, ref Player.Data player)
			{
				var playerCount = region.GetConnectedPlayerCount();
				int blueCount = 0;
				int redCount = 0;

                for (uint i = 0; i < playerCount; i++)
                {
                    if (region.GetConnectedPlayerByIndex(i).faction_id == "blue")
                    {
						blueCount++;
                    }
                    else if (region.GetConnectedPlayerByIndex(i).faction_id == "red")
                    {
						redCount++;
                    }
                }

                if (blueCount > redCount)
                {
					player.SetFaction("red");
				}
                else
                {
					player.SetFaction("blue");
				}

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
}