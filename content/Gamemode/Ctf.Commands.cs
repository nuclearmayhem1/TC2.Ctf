using Keg.Extensions;
using TC2.Base.Components;

namespace TC2.Ctf
{
	public static partial class Ctf
	{
#if SERVER
		[ChatCommand.Region("nextmap", "", admin: true)]
		public static void NextMapCommand(ref ChatCommand.Context context, string map)
		{
			ref var region = ref context.GetRegion();
			if (!region.IsNull())
			{
				var map_handle = new Map.Handle(map);
				//App.WriteLine($"nextmapping {map}; {map_handle}; {map_handle.id}");

				//if (map_handle.id != 0)
				{
					Ctf.ChangeMap(ref region, map_handle);
				}
			}
		}

		[ChatCommand.Region("pause", "", admin: true)]
		public static void PauseCommand(ref ChatCommand.Context context, bool? value = null)
		{
			ref var region = ref context.GetRegion();
			if (!region.IsNull())
			{
				ref var g_ctf = ref region.GetSingletonComponent<Ctf.Gamemode>();
				if (!g_ctf.IsNull())
				{
					g_ctf.paused = !g_ctf.paused;
					region.SyncGlobal<Ctf.Gamemode>(ref g_ctf);
					Server.SendChatMessage(g_ctf.paused? "paused" : "unpaused", Chat.Channel.Global, color: Color32BGRA.Yellow);
				}
			}
		}
#endif
	}
}
