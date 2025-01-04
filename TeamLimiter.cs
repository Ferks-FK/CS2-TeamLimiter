using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace TeamLimiter;

public class TeamLimiterConfig: BasePluginConfig
{
	[JsonPropertyName("max_players_per_team")]
	public int MaxPlayersPerTeam { get; set;} = 5;
}

public class TeamLimiter: BasePlugin, IPluginConfig<TeamLimiterConfig>
{
	public override string ModuleName => "TeamLimiter";
	public override string ModuleAuthor => "Ferks-FK";
	public override string ModuleVersion => "0.0.1";
	public TeamLimiterConfig Config { get; set; } = new();
	public void OnConfigParsed(TeamLimiterConfig config)
	{
		Config = config;
	}
	public override void Load(bool hotReload)
	{
		RegisterEventHandler<EventPlayerTeam>(OnPlayerTeamChange);
		AddCommandListener("jointeam", JoinTeamListener);
	}
	public HookResult OnPlayerTeamChange(EventPlayerTeam @event, GameEventInfo info)
	{
		var player = @event.Userid;

		if (player is null || !player.IsValid || player.IsBot || player.IsHLTV || (int)player.Team >= 2)
			return HookResult.Continue;

		if (IsTwoTeamsFull())
		{
			info.DontBroadcast = true;
			
			Server.NextFrame(() => {
				player.RemoveWeapons();
				player.ChangeTeam(CsTeam.Spectator);
			});
		}

		return HookResult.Continue;
	}
	public HookResult JoinTeamListener(CCSPlayerController? player, CommandInfo info)
	{
		if (player is null || !player.IsValid)
			return HookResult.Continue;

		var Team = GetTeamById(info.ArgByIndex(1));

		if (Team == CsTeam.Spectator)
		{
			return HookResult.Continue;
		}

		if (Team == CsTeam.None && IsTwoTeamsFull()) {
			player.ExecuteClientCommand("play sounds/ui/weapon_cant_buy.vsnd_c");

			return HookResult.Stop;
		}

		if ((Team == CsTeam.Terrorist && IsTeamFull(CsTeam.Terrorist)) || (Team == CsTeam.CounterTerrorist && IsTeamFull(CsTeam.CounterTerrorist)))
		{
			player.ExecuteClientCommand("play sounds/ui/weapon_cant_buy.vsnd_c");

			return HookResult.Stop;
		}

		return HookResult.Continue;
	}
	private bool IsTwoTeamsFull()
	{
		return IsTeamFull(CsTeam.Terrorist) && IsTeamFull(CsTeam.CounterTerrorist);
	}
	private bool IsTeamFull(CsTeam team)
	{
		return Utilities.GetPlayers().Count(player => player.IsValid && !player.IsBot && !player.IsHLTV && player.Team == team) >= Config.MaxPlayersPerTeam;
	}
	private static CsTeam GetTeamById(string teamId)
	{
		switch (teamId)
		{
			case "1":
				return CsTeam.Spectator;
			case "2":
				return CsTeam.Terrorist;
			case "3":
				return CsTeam.CounterTerrorist;
			default:
				return CsTeam.None;
		}
	}
}
