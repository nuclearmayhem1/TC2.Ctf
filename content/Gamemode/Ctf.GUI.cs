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

	[IComponent.Data(Net.SendType.Reliable), IComponent.AddTo<Character.Data>]
	public struct HitTracker : IComponent
	{
		public Damage.Type lastDamageType = Damage.Type.None;
		public Entity attacker = Entity.None;
		public Entity bodypart = Entity.None;
		public Entity owner = Entity.None;
		public Color32BGRA owner_color = Color32BGRA.Grey;

		public HitTracker()
		{

		}
	}


	[ISystem.Event<Health.DamageEvent>(ISystem.Mode.Single)]
	public static void OnDie(ISystem.Info info, Entity entity, Entity ent_organic_state, [Source.Shared] ref Character.Data character, [Source.Owned] in Organic.State organic_state,
		[Source.Shared] ref Ctf.HitTracker hit_tracker, ref Health.DamageEvent data)
	{
		if (entity == character.ent_controlled)
		{
			hit_tracker.lastDamageType = data.damage_type;
			hit_tracker.attacker = data.ent_attacker;
			hit_tracker.bodypart = ent_organic_state;
			hit_tracker.owner = data.ent_owner;
            if (data.ent_owner != Entity.None)
            {
				var faction = data.ent_owner.GetFaction();
				ref var faction_data = ref faction.GetData();
                if (faction_data.IsNotNull())
                {
					hit_tracker.owner_color = data.ent_owner.GetFaction().GetData().color_a;
				}
			}
		}
	}



	[ISystem.RemoveLast(ISystem.Mode.Single)]
	public static void OnDie(ISystem.Info info, Entity entity, Entity ent_organic_state, [Source.Shared] ref Character.Data character, [Source.Owned] in Organic.State organic_state, [Source.Global] ref Ctf.Gamemode.State g_ctf_state,
	[Source.Shared] ref Ctf.HitTracker hit_tracker, [Source.Shared] ref Player.Data player)
	{
		if (entity == character.ent_controlled)
		{
			var owner_text = new FixedString256("Nobody");
			if (hit_tracker.owner != Entity.None)
            {
				ref var owner = ref hit_tracker.owner.GetComponent<Character.Data>();
				if (owner.IsNotNull())
				{
					ref var owner_player = ref owner.ent_character.GetComponent<Player.Data>();
					if (owner_player.IsNotNull())
					{
						owner_text = new FixedString256(owner_player.GetName());
					}
					else
					{
						owner_text = new FixedString256(owner.name);
					}
				}
			}

			bool foundSlot = false;
            for (int i = 0; i < g_ctf_state.killFeed.Length; i++)
            {
                if (g_ctf_state.killFeed[i].timestamp <= info.WorldTime - 9.5f)
                {
					g_ctf_state.killFeed[i] = new KillFeedKill()
					{
						victim = player,
						bodypart = new FixedString256(hit_tracker.bodypart.GetName()),
						damage = hit_tracker.lastDamageType,
						weapon = new FixedString256(hit_tracker.attacker.GetName()),
						timestamp = info.WorldTime + 10,
						owner = new FixedString256(owner_text),
						owner_color = hit_tracker.owner_color,
					};
					foundSlot = true;
					break;
                }
            }
            if (!foundSlot)
            {
				g_ctf_state.killFeed[0] = new KillFeedKill()
				{
					victim = player,
					bodypart = new FixedString256(hit_tracker.bodypart.GetName()),
					damage = hit_tracker.lastDamageType,
					weapon = new FixedString256(hit_tracker.attacker.GetName()),
					timestamp = info.WorldTime + 10,
					owner = new FixedString256(owner_text),
					owner_color = hit_tracker.owner_color,
				};
			}
			hit_tracker.owner = Entity.None;
			hit_tracker.bodypart = Entity.None;
			hit_tracker.attacker = Entity.None;
			hit_tracker.lastDamageType = Damage.Type.None;
			hit_tracker.owner_color = Color32BGRA.Grey;
		}
	}


#if CLIENT

	public struct GracePeriodCountdownGUI : IGUICommand
	{
		public Ctf.Gamemode.State g_ctf_state;
		public float time;

		public void Draw()
		{
			using (var window = GUI.Window.Standalone("Grace timer", position: new Vector2(GUI.CanvasSize.X / 2, GUI.CanvasSize.Y / 16), pivot: new Vector2(0,0), size: new Vector2(200, 16)))
            {
                if (window.show)
                {
					float timeLeft = g_ctf_state.graceEndTimestamp - time;

					if (timeLeft > 0)
                    {
						Color32BGRA timerColor = Color32BGRA.Lerp(Color32BGRA.Red, Color32BGRA.Green, timeLeft / g_ctf_state.graceEndTimestamp);

						GUI.Title($"Grace ends in {timeLeft:0}s", color: timerColor);
					}
                    else
                    {
						GUI.Title($"Grace ended {Math.Abs(timeLeft):0}s ago", color: Color32BGRA.Yellow);
					}
                }
            }
		}
	}

	public struct KillfeedGUI : IGUICommand
    {
		public Ctf.Gamemode.State g_ctf_state;
		public float time;
		//public static readonly Texture.Handle defaultKillIcon = "ui_deathicon_generic";

		public void Draw()
        {

			using (var window = GUI.Window.Standalone("Killfeed", position: new Vector2(GUI.CanvasSize.X -460, 10), pivot: new Vector2(0,0), size: new Vector2(450, 400)))
			{
                if (window.show)
                {
					using (var table = GUI.Table.New("Killfeed.table", 1, size: new Vector2(450, 600)))
					{
                        if (table.show)
                        {
							table.SetupColumnFixed(400);

							for (int i = 0; i < g_ctf_state.killFeed.Length; i++)
							{
								var kill = g_ctf_state.killFeed[i];

								if (kill.timestamp > time)
								{
									using (var row = GUI.Table.Row.New(size: new(GUI.GetAvailableWidth(), 32)))
									{
										using (row.Column(0))
										{
											/*
											ref var renderer = ref kill.bodypart.GetComponent<Animated.Renderer.Data>();
											if (renderer.IsNotNull())
											{
												GUI.DrawSprite(renderer.sprite.texture, 1);
												App.WriteLine("drawing bodypart");
											}
											else
											{
												GUI.DrawSprite(defaultKillIcon, 0.5f);
												App.WriteLine("drawing default bodypart");
											}

											GUI.SameLine(10);
											
											ref var renderer = ref kill.weapon.GetComponent<Animated.Renderer.Data>();
											if (renderer.IsNotNull())
											{
												GUI.DrawSprite(renderer.sprite.texture, 1);
												App.WriteLine("drawing weapon");
											}
											else
											{
												GUI.DrawSprite(defaultKillIcon, 0.5f);
												App.WriteLine("drawing default weapon");
											}*/

											GUI.Title(kill.owner, color: kill.owner_color);

											GUI.SameLine(5);

											string weaponText = kill.weapon;

                                            if (weaponText == "0")
                                            {
												weaponText = "Nothing";
                                            }

											weaponText = weaponText.Replace("projectile", "", StringComparison.InvariantCultureIgnoreCase);
											weaponText = weaponText.Replace(".", " ");
											weaponText = weaponText.Replace("_", " ");

											GUI.Title(weaponText, color: Color32BGRA.White);

											GUI.SameLine(5);

											ref var faction_data = ref kill.victim.faction_id.GetData();
                                            if (faction_data.IsNotNull())
                                            {
												GUI.Title(kill.victim.GetName(), color: kill.victim.faction_id.GetData().color_a);
											}
                                            else
                                            {
												GUI.Title(kill.victim.GetName(), color: Color32BGRA.Grey);
											}
										};
									}
								}
							}
						}
					}
				}
			}
        }
    }




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
												using (GUI.ID.Push(factions[(int)i].id))
                                                {
													using (var button = GUI.Button.New("Join", new Vector2(80, 30), currentCount >= newCount && player.faction_id != factions[(int)i].id))
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
											if (player.faction_id == factions[i].id)
                                            {
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
            else
            {
				if (g_ctf.elapsed - 30f < g_ctf_state.graceEndTimestamp)
				{
					var graceCountdown = new GracePeriodCountdownGUI()
					{
						g_ctf_state = g_ctf_state,
						time = g_ctf.elapsed,
					};
					graceCountdown.Submit();
				}
			}

			var killfeed = new KillfeedGUI()
			{
				g_ctf_state = g_ctf_state,
				time = info.WorldTime,
			};
			killfeed.Submit();
		}
	}
#endif
}