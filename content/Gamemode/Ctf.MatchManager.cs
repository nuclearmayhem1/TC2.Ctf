namespace TC2.Ctf;
using Keg.Extensions;
using TC2.Base.Components;

public static partial class Ctf
{
	[IComponent.Data(Net.SendType.Reliable)]
	public struct CtfManager : IComponent
	{
		public float nextMapTimestamp = float.MaxValue;
		public bool restarting = false;
		public bool hasStarted = false;

		public CtfManager()
		{

		}
	}


#if SERVER
	[ISystem.Update(ISystem.Mode.Single, interval: 1f), HasTag("ctf", true, Source.Modifier.Owned)]
    public static void Update1(ISystem.Info info, Entity entity, [Source.Owned] ref Faction.Data faction, [Source.Owned] ref Spawn.Data spawn, [Source.Owned] ref Transform.Data transform)
    {
        if (GetFlags(ref info.GetRegion(), faction.id) < 1)
        {
			Server.SendChatMessage("" + faction.id + " faction has been defeated!", Chat.Channel.Global, Color32BGRA.Red);
			info.GetRegion().SpawnPrefab("explosion", transform.position);
			entity.Delete();
        }
    }

	[ISystem.Update(ISystem.Mode.Single, interval: 2f)]
	public static void Update2(ISystem.Info info, Entity entity, [Source.Owned] ref Ctf.CtfManager manager, [Source.Owned] ref Animated.Renderer.Data renderer, [Source.Global] in Ctf.Gamemode.State g_state, [Source.Global] in MapCycle.Global mapcycle, [Source.Global] ref MapCycle.Voting voting, [Source.Global] ref Ctf.Gamemode g_gamemode)
	{
		var result = GetSpawns(ref info.GetRegion());
        if (result.count < 2 && !manager.restarting)
        {
			Server.SendChatMessage("" + result.lastFaction + " Wins!", Chat.Channel.Global, Color32BGRA.Green);
			manager.nextMapTimestamp = info.WorldTime + 10;
			manager.restarting = true;
		}

        if (manager.nextMapTimestamp < info.WorldTime && manager.restarting)
        {

			ref var world = ref Server.GetWorld();

			var weights = new FixedArray16<float>();

			ref var votes = ref voting.votes;
			for (int i = 0; i < votes.Length; i++)
			{
				ref var vote = ref votes[i];
				if (vote.player_id != 0)
				{
					weights[vote.map_index] += vote.weight;
				}
			}

			var top_index = 0;
			var top_weight = 0.00f;

			for (int i = 0; i < weights.Length; i++)
			{
				var weight = weights[i];
				if (weight > top_weight)
				{
					top_weight = weight;
					top_index = i;
				}
			}

			if (top_index != -1)
			{
				var map_name = mapcycle.maps[top_index];

				voting.votes = default;
				g_gamemode.elapsed = 0.00f;

				ChangeMap(ref info.GetRegion(), map_name.ToString());
			}
        }
	}

	[ISystem.Update(ISystem.Mode.Single, interval: 1f)]
	public static void Update3(ISystem.Info info, Entity entity, [Source.Owned] ref Ctf.CtfManager manager, [Source.Owned] ref Animated.Renderer.Data renderer, [Source.Owned, Pair.Of<Body.Data>] ref Shape.Box box, [Source.Owned] ref Body.Data body, [Source.Global] in Ctf.Gamemode.State g_ctf_state, [Source.Global] in Ctf.Gamemode g_ctf)
	{
		if (g_ctf.elapsed > g_ctf_state.graceEndTimestamp && !manager.hasStarted)
		{
			manager.hasStarted = true;
			manager.Sync(entity);
			renderer.sprite = "ctf_empty";
			renderer.Sync(entity);
			box.layer = Physics.Layer.None;
			box.mask = Physics.Layer.None;
			box.size = Vector2.Zero;
			box.exclude = Physics.Layer.All;
			body.override_shape_layer = Physics.Layer.None;
			body.override_shape_mask = Physics.Layer.None;
			body.Sync(entity);
			entity.RemoveTrait<Body.Data, Shape.Box>();
			Server.SendChatMessage("Grace period over!", Chat.Channel.Global, Color32BGRA.Yellow);
		}
	}

#endif
	public static int GetFlags(ref Region.Data region, IFaction.Handle factionToFind)
	{
		var arg = new GetAllFlagsArgs();
		arg.count = 0;
		arg.faction = factionToFind;
		region.Query<GetAllFlagsQuery>(Func).Execute(ref arg);
		static void Func(ISystem.Info info, Entity entity, [Source.Owned] in CtfFlag.Data flag, [Source.Owned] in Faction.Data faction)
		{
			ref var arg = ref info.GetParameter<GetAllFlagsArgs>();
			if (!arg.IsNull())
			{
                if (faction.id == arg.faction.id)
                {
					arg.count++;
                }
			}
		}
		return arg.count;
	}

	public static GetAllSpawnsArgs GetSpawns(ref Region.Data region)
	{
		var arg = new GetAllSpawnsArgs();
		arg.count = 0;
		arg.lastFaction = "";
		region.Query<GetAllSpawnsQuery>(Func).Execute(ref arg);
		static void Func(ISystem.Info info, Entity entity, [Source.Owned] ref Faction.Data faction, [Source.Owned] ref Spawn.Data spawn)
		{
			ref var arg = ref info.GetParameter<GetAllSpawnsArgs>();
			if (!arg.IsNull())
			{
				arg.count++;
				arg.lastFaction = faction.id;
			}
		}
		return arg;
	}
#if SERVER
	public static void ChangeMap(ref Region.Data region, Map.Handle map)
	{
		//ref var region = ref world.GetAnyRegion();
		if (!region.IsNull())
		{
			var region_id_old = region.GetID();

			ref var world = ref Server.GetWorld();
			if (world.TryGetFirstAvailableRegionID(out var region_id_new))
			{
				region.Wait().ContinueWith(() =>
				{
					Net.SetActiveRegionForAllPlayers(0);

					ref var world = ref Server.GetWorld();
					world.UnloadRegion(region_id_old).ContinueWith(() =>
					{
						ref var world = ref Server.GetWorld();

						ref var region_new = ref world.ImportRegion(region_id_new, map);
						if (!region_new.IsNull())
						{
							world.SetContinueRegionID(region_id_new);

							region_new.Wait().ContinueWith(() =>
							{
								Net.SetActiveRegionForAllPlayers(region_id_new);
							});
						}
					});
				});
			}
		}
	}
#endif
}