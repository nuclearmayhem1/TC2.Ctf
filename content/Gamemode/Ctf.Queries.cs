namespace TC2.Ctf;
using Keg.Extensions;
using TC2.Base.Components;

public static partial class Ctf
{

    [Query]
    public delegate void GetAllFlagsQuery(ISystem.Info info, Entity entity, [Source.Owned] in CtfFlag.Data flag, [Source.Owned] in Faction.Data faction);

    public struct GetAllFlagsArgs
    {
        public int count;
        public IFaction.Handle faction;
    }

    [Query]
    public delegate void GetAllSpawnsQuery(ISystem.Info info, Entity entity, [Source.Owned] ref Faction.Data faction, [Source.Owned] ref Spawn.Data spawn);

    [Query]
    public delegate void GetPlayersQuery(ISystem.Info info, Entity entity, [Source.Owned] ref Player.Data player);

    public struct GetPlayersArgs
    {
        public IFaction.Handle faction;
        public int count;
    }

    public struct GetAllSpawnsArgs
    {
        public int count;
        public IFaction.Handle lastFaction;
    }

    public struct GetFactionsWithSpawner
    {
        public FixedArray32<IFaction.Handle> factions;
    }

    public static FixedArray32<IFaction.Handle> GetRemainingFactions(ref Region.Data region)
    {
        var arg = new GetFactionsWithSpawner();

        arg.factions = new FixedArray32<IFaction.Handle>();

        region.Query<GetAllSpawnsQuery>(Func).Execute(ref arg);
        static void Func(ISystem.Info info, Entity entity, [Source.Owned] ref Faction.Data faction, [Source.Owned] ref Spawn.Data spawn)
        {
            ref var arg = ref info.GetParameter<GetFactionsWithSpawner>();
            if (!arg.IsNull())
            {

                for (int i = 0; i < arg.factions.Length; i++)
                {
                    ref var current = ref arg.factions[i];
                    if (current.id == faction.id)
                    {
                        return;
                    }
                    else if (current.id == 0)
                    {
                        current = faction.id;
                        return;
                    }
                }


            }
        }
        return arg.factions;
    }

    public static int GetFactionPlayerCount(ref Region.Data region, IFaction.Handle faction)
    {
        var arg = new GetPlayersArgs();
        arg.faction = faction;
        region.Query<GetPlayersQuery>(Func).Execute(ref arg);
        static void Func(ISystem.Info info, Entity entity, [Source.Owned] ref Player.Data player)
        {
            ref var arg = ref info.GetParameter<GetPlayersArgs>();
            if (player.faction_id == arg.faction)
            {
                arg.count++;
            }
        }
        return arg.count;
    }
}