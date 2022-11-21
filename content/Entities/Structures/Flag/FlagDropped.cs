using Keg.Extensions;
using TC2.Base.Components;

namespace TC2.Base.Components;

public static partial class FlagDropped
{

	[IComponent.Data(Net.SendType.Reliable)]
	public struct Data : IComponent
	{
		public Faction.Data faction = new Faction.Data();
		public Entity ent_owner = Entity.None;

		public Data()
        {

        }
    }

	[IComponent.Data(Net.SendType.Unreliable)]
	public struct State : IComponent
	{
		public float returnTimestamp = float.MaxValue;

		public State()
		{

		}

	}

	[ISystem.AddFirst(ISystem.Mode.Single)]
	public static void OnAdd(ISystem.Info info, Entity entity, [Source.Owned] ref FlagDropped.Data data, [Source.Owned] ref FlagDropped.State state)
    {
		state.returnTimestamp = info.WorldTime + 10;
    }

#if SERVER
	[ISystem.Update(ISystem.Mode.Single)]
	public static void Update(ISystem.Info info, Entity entity, [Source.Owned] ref FlagDropped.Data data, [Source.Owned] ref FlagDropped.State state,
        [Source.Parent] in Player.Data player)
	{

        if (player.faction_id == data.faction.id)
        {
            if (state.returnTimestamp < info.WorldTime)
            {
				state.returnTimestamp = float.MaxValue;
				ref var flag = ref data.ent_owner.GetComponent<CtfFlag.State>();
				flag.hasFlag = true;
				flag.Sync(data.ent_owner);
				Server.SendChatMessage("" + data.faction.id + " flag returned by " + player.GetName() + "!", Chat.Channel.Global, Color32BGRA.Green);
				entity.Delete();
			}
		}
		else
        {
			state.returnTimestamp = info.WorldTime + 10;
        }
	}

	[ISystem.Update(ISystem.Mode.Single)]
	public static void ReturnFlag(ISystem.Info info, Entity entity, [Source.Owned] ref FlagDropped.Data data, [Source.Owned] ref FlagDropped.State state)
	{

        if (state.returnTimestamp + 5 < info.WorldTime)
        {
			state.returnTimestamp = float.MaxValue;
			ref var flag = ref data.ent_owner.GetComponent<CtfFlag.State>();
			flag.hasFlag = true;
			flag.Sync(data.ent_owner);
			Server.SendChatMessage("" + data.faction.id + " flag returned by " + "Timeout" + "!", Chat.Channel.Global, Color32BGRA.Green);
			entity.Delete();
		}
	}
#endif
}