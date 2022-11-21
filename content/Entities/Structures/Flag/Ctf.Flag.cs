using Keg.Extensions;
using TC2.Base.Components;

namespace TC2.Base.Components;

public static partial class CtfFlag
{

	[IComponent.Data(Net.SendType.Reliable)]
	public struct Data : IComponent
	{

		public Data()
		{

		}
	}

	[IComponent.Data(Net.SendType.Unreliable)]
	public struct State : IComponent
	{
		public bool hasFlag = true;
		public bool spawnFlag = false;

		public State()
        {

        }

	}

#if SERVER
	[ISystem.Update(ISystem.Mode.Single)]
	public static void Update(ISystem.Info info, Entity entity, [Source.Owned] ref CtfFlag.Data data, [Source.Owned] ref Animated.Renderer.Data renderer, 
		[Source.Owned] ref CtfFlag.State state, [Source.Owned] ref Transform.Data transform, [Source.Owned] ref Faction.Data faction)
    {
        if (state.spawnFlag)
        {
			var region = info.GetRegion();
			state.spawnFlag = false;
			state.Sync(entity);

			var faction_tmp = faction;

			region.SpawnPrefab("ctf_flag_dropped", transform.position, faction_id: faction.id).ContinueWith((ent) =>
            {
				ref var fac = ref faction_tmp;
				ref var owner = ref entity;
				ref FlagDropped.Data flag = ref ent.GetOrAddComponent<FlagDropped.Data>();
				flag.faction = fac;
				flag.ent_owner = owner;
			});
		}

        if (state.hasFlag && renderer.sprite.frame != new Silk.NET.Maths.Vector2D<uint>(0, 0))
        {
			renderer.sprite.frame = new Silk.NET.Maths.Vector2D<uint>(0, 0);
			renderer.Sync(entity);
		}
        else if (!state.hasFlag && renderer.sprite.frame != new Silk.NET.Maths.Vector2D<uint>(1, 0))
        {
			renderer.sprite.frame = new Silk.NET.Maths.Vector2D<uint>(1, 0);
			renderer.Sync(entity);
		}
	}
#endif


	public partial struct TakeFlagRPC : Net.IRPC<CtfFlag.State>
	{
		public Player.Data player;
		public Faction.Data faction;

#if SERVER
		public void Invoke(ref NetConnection connection, Entity entity, ref CtfFlag.State state)
		{
			state.hasFlag = false;
			state.spawnFlag = true;
			Server.SendChatMessage("" + faction.id + " flag stolen by " + player.GetName() + "!", Chat.Channel.Global, Color32BGRA.Red);
			state.Sync(entity);
		}
#endif
	}

	public partial struct CaptureFlagRPC : Net.IRPC<CtfFlag.State>
	{
		public Player.Data player;
		public Entity flag;
		public Faction.Data flag_faction;
		public FlagDropped.Data flagData;

#if SERVER
		public void Invoke(ref NetConnection connection, Entity entity, ref CtfFlag.State state)
		{
			Server.SendChatMessage("" + flag_faction.id + " flag captured by " + player.GetName() + "!", Chat.Channel.Global, Color32BGRA.Red);
			flagData.ent_owner.Delete();
			flag.Delete();
		}
#endif
	}



#if CLIENT
	public struct FlagGui : IGUICommand
    {
		public Entity ent_flag;
		public Faction.Data faction;
		public Player.Data player;

        public void Draw()
        {
			using (var window = GUI.Window.Interaction("ctf_flag", this.ent_flag))
            {
                if (player.faction_id != faction.id)
                {
					using (var button = GUI.Button.New("Steal the flag!", GUI.GetAvailableSize()))
					{

						if (button.pressed)
						{
							var rpc = new TakeFlagRPC
							{
								player = this.player,
								faction = this.faction,
							};
							rpc.Send(ent_flag);
						}
					}
				}
                else
                {
					using (var button = GUI.Button.New("Capture the flag!", GUI.GetAvailableSize()))
					{

						if (button.pressed)
						{
							ref var held = ref player.ent_pickup_target.GetComponent<FlagDropped.Data>();

                            if (held.IsNotNull())
                            {
								var rpc = new CaptureFlagRPC
								{
									flag = player.ent_pickup_target,
									flag_faction = held.faction,
									player = player,
									flagData = held,
								};
								rpc.Send(ent_flag);
                            }
						}
					}
				}
            }
        }
    }
#endif

#if CLIENT
	[ISystem.EarlyGUI(ISystem.Mode.Single)]
	public static void OnGUI(Entity entity, Entity ent_data, [Source.Owned] ref CtfFlag.Data data, [Source.Owned] ref CtfFlag.State state, [Source.Owned] in Faction.Data faction, [Source.Owned] in Interactable.Data interactable, [Source.Owned] ref Transform.Data transform)
	{
		if (interactable.show && state.hasFlag)
		{
			var gui = new FlagGui()
			{
				ent_flag = ent_data,
				faction = faction,
				player = Client.GetPlayer(),
			};
			gui.Submit();
		}
	}
#endif
}