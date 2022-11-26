namespace TC2.Ctf;
using Keg.Extensions;
using TC2.Base.Components;

public static partial class Ctf
{

	public partial struct ChangeFactionRPC : Net.IRPC<Player.Data>
	{
		public IFaction.Handle faction;

#if SERVER
		public void Invoke(ref NetConnection connection, Entity entity, ref Player.Data player)
		{
			player.ent_controlled.Delete();
			player.faction_id = faction;
			player.Sync(entity);
		}
#endif
	}


#if CLIENT
	public struct ScoreboardGUI : IGUICommand
	{
		public Player.Data player;
		public Ctf.Gamemode gamemode;
		public MapCycle.Global mapcycle;
		public MapCycle.Voting voting;
		public Region.Data region;
		public FixedArray32<Player.Data> queryPlayers;
		public int player_count;

		public static bool show;

		public void Draw()
		{
			var alive = this.player.flags.HasAny(Player.Flags.Alive);
			var lh = 32;
			//App.WriteLine(alive);

			var window_pos = (GUI.CanvasSize * new Vector2(0.50f, 0.00f)) + new Vector2(100, 48);
			using (var window = GUI.Window.Standalone("Scoreboard", position: alive ? null : window_pos, size: new Vector2(700, 400), pivot: alive ? new Vector2(0.50f, 0.00f) : new(1.00f, 0.00f)))
			{
				this.StoreCurrentWindowTypeID();
				if (window.show)
				{;
					ref var world = ref Client.GetWorld();
					ref var game_info = ref Client.GetGameInfo();

					if (alive)
					{
						GUI.DrawWindowBackground("ui_scoreboard_bg", new Vector4(8, 8, 8, 8));
					}

					using (GUI.Group.New(size: GUI.GetAvailableSize(), padding: new(14, 12)))
					{
						using (GUI.Group.New(size: new Vector2(GUI.GetRemainingWidth(), 32)))
						{
							GUI.Title($"{game_info.name}", size: 32);
							GUI.SameLine();
							GUI.TitleCentered($"Match duration: {GUI.FormatTime(MathF.Max(0.00f, this.gamemode.elapsed))}", size: 24, pivot: new Vector2(1, 1));
						}

						GUI.SeparatorThick();

						using (GUI.Group.New(padding: new Vector2(4, 4)))
						{
							using (GUI.Group.New(size: new(GUI.GetRemainingWidth() * 0.50f, 0), padding: new Vector2(8, 4)))
							{
								GUI.Label("Players:", $"{game_info.player_count}/{game_info.player_count_max}", font: GUI.Font.Superstar, size: 16);
								GUI.Label("Map:", game_info.map, font: GUI.Font.Superstar, size: 16);
								GUI.Label("Gamemode:", $"{game_info.gamemode}", font: GUI.Font.Superstar, size: 16);
							}

							GUI.SameLine();

							using (GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new Vector2(8, 4)))
							{
								var weights = new FixedArray16<float>();

								ref var votes = ref this.voting.votes;
								for (int i = 0; i < votes.Length; i++)
								{
									ref var vote = ref votes[i];
									if (vote.player_id != 0)
									{
										weights[vote.map_index] += vote.weight;
									}
								}

								using (var table = GUI.Table.New("MapCycle.Table", 3))
								{
									if (table.show)
									{
										table.SetupColumnFixed(64);
										table.SetupColumnFixed(lh);
										table.SetupColumnFlex(1);

										ref var maps = ref this.mapcycle.maps;
										for (int i = 0; i < maps.Length; i++)
										{
											ref var map = ref maps[i];
											if (!map.IsEmpty())
											{
												using (GUI.ID.Push(i))
												{
													using (var row = table.NextRow(lh))
													{
														using (row.Column(0))
														{
															if (GUI.DrawButton("Vote", new Vector2(64, lh)))
															{
																var rpc = new MapCycle.VoteRPC()
																{
																	map_index = i
																};
																rpc.Send();
															}
															if (GUI.IsItemHovered()) using (GUI.Tooltip.New()) GUI.Text("Vote for this map to be played next.");
														}

														using (row.Column(1))
														{
															GUI.TextShadedCentered($"{weights[i]:0}", pivot: new(0.50f, 0.50f), color: weights[i] > 0 ? 0xff00ff00 : default, font: GUI.Font.Superstar, size: 20, shadow_offset: new(2, 2));
														}

														using (row.Column(2, padding: new(0, 4)))
														{
															GUI.TextShadedCentered(map, pivot: new(0.00f, 0.50f), font: GUI.Font.Superstar, size: 20, shadow_offset: new(2, 2));
														}
													}
												}
											}
										}
									}
								}
							}
						}

						GUI.NewLine(4);

						GUI.SeparatorThick();

						GUI.NewLine(4);

						using (GUI.Group.New(size: GUI.GetRemainingSpace(), padding: new Vector2(4, 4)))
						{
							var factions = GetRemainingFactions(ref region);
							var factions_lenght = 0;
                            for (int i = 0; i < factions.Length; i++)
                            {
                                if (factions[i].id == 0)
                                {
									factions_lenght = i;
									break;
                                }
                            }

							using (var table = GUI.Table.New("Teams", factions_lenght, size: new Vector2(0, GUI.GetRemainingHeight())))
							{
								for (uint i = 0; i < factions_lenght; i++)
                                {
									table.SetupColumnFlex(1);
								}

								if (table.show)
								{							
									using (var row = GUI.Table.Row.New(size: new(GUI.GetRemainingWidth(), 16), header: true))
									{
                                        for (uint i = 0; i < factions_lenght; i++)
                                        {
											using (row.Column(i))
                                            {
												int currentCount = GetFactionPlayerCount(ref region, player.faction_id);
												int newCount = GetFactionPlayerCount(ref region, factions[(int)i].id);
												using (var button = GUI.Button.New("Join " + factions[(int)i].id, new Vector2(80, 30), currentCount >= newCount && player.faction_id != factions[(int)i].id))
                                                {
                                                    if (button.pressed)
                                                    {
														var rpc = new ChangeFactionRPC()
														{
															faction = factions[(int)i].id,
														};
														rpc.Send(player.ent_player);
													}
                                                }
                                            };
										}
									}

									using (var row = GUI.Table.Row.New(size: new(GUI.GetRemainingWidth(), 16)))
									{
										for (uint i = 0; i < factions_lenght; i++)
										{
											ref var faction = ref factions[(int)i].GetData();
											using (row.Column(i))
											{
												GUI.Title(faction.name, color: faction.color_a);
											};
										}
									}

									player_count = 0;
									var faction_players = new Dictionary<IFaction.Handle, List<Player.Data>>();

									for (int i = 0; i < factions_lenght; i++)
									{
										faction_players[factions[i]] = new List<Player.Data>();
									}

									queryPlayers = new FixedArray32<Player.Data>();
									region.Query<Region.GetPlayersQuery>(Func).Execute(ref this);
									static void Func(ISystem.Info info, Entity entity, in Player.Data player)
                                    {
										ref var arg = ref info.GetParameter<ScoreboardGUI>();
                                        for (int i = 0; i < arg.queryPlayers.Length; i++)
                                        {
											if (arg.queryPlayers[i].id == 0)
                                            {
												arg.queryPlayers[i] = player;
                                                if (arg.player_count < i + 1)
                                                {
													arg.player_count = i + 1;
                                                }
												break;
                                            }
                                        }
                                    }

                                    for (int i = 0; i < factions_lenght; i++)
                                    {
                                        for (uint p = 0; p < player_count; p++)
                                        {
											var player = queryPlayers[p];
											App.WriteLine(player.faction_id);
											if (player.faction_id == factions[i].id)
                                            {
												App.WriteLine(player);
                                                faction_players[factions[i]].Add(player);
                                            }
                                        }
                                    }

									int rows = 0;
                                    while (rows < 50)
                                    {
										bool playerAdded = false;
										using (var row = GUI.Table.Row.New(size: new(GUI.GetRemainingWidth(), 16)))
										{
											for (uint i = 0; i < factions_lenght; i++)
											{
                                                if (faction_players[factions[i]].Count > rows)
                                                {
													var player = faction_players[factions[i]][rows];
													using (row.Column(i))
													{
														GUI.Title(player.GetName());
													};
													playerAdded = true;
												}
                                                else
                                                {
													using (row.Column(i))
													{

													};
												}
											}
										}
										if (!playerAdded)
                                        {
											break;
                                        }
										rows++;
									}
								}
							}
						}
					}
				}
			}
		}
	}

	[ISystem.EarlyGUI(ISystem.Mode.Single)]
	public static void OnGUICtf(Entity entity, ISystem.Info info, [Source.Owned] in Player.Data player, [Source.Global] in Ctf.Gamemode g_ctf, [Source.Global] in Ctf.Gamemode.State g_ctf_state, [Source.Global] in MapCycle.Global mapcycle, [Source.Global] in MapCycle.Voting voting)
	{
		if (player.IsLocal())
		{
			ref readonly var kb = ref Control.GetKeyboard();
			if (kb.GetKeyDown(Keyboard.Key.Tab))
			{
				ScoreboardGUI.show = !ScoreboardGUI.show;
			}

			Spawn.RespawnGUI.window_offset = new Vector2(100, 90);
			Spawn.RespawnGUI.window_pivot = new Vector2(0, 0);

			if (ScoreboardGUI.show || (!player.flags.HasAny(Player.Flags.Alive) && Editor.show_respawn_menu))
			{
				var gui = new ScoreboardGUI()
				{
					player = player,
					gamemode = g_ctf,
					mapcycle = mapcycle,
					voting = voting,
					region = info.GetRegion(),
				};
				gui.Submit();
			}
		}
	}
#endif
}