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

    public struct GetAllSpawnsArgs
    {
        public int count;
        public IFaction.Handle lastFaction;
    }
}