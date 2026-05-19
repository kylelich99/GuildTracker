using System.IO;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using GuildTracker.Models;
using GuildTracker.Services;

namespace GuildTracker.Discord;

/// <summary>
/// Discord bot that provides a /cp slash command.
/// Members can submit their combat power via Discord, which gets saved to local JSON.
/// 
/// Setup:
/// 1. Create a bot at https://discord.com/developers/applications
/// 2. Enable "Message Content Intent" under Bot settings
/// 3. Invite bot with applications.commands scope
/// 4. Put your bot token in Data/discord_token.txt
/// </summary>
public class DiscordBotService
{
    private DiscordSocketClient? _client;
    private readonly JsonDataService _dataService = new();
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public async Task StartAsync()
    {
        var tokenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "discord_token.txt");
        if (!File.Exists(tokenPath))
        {
            // Create a placeholder file so users know where to put the token
            await File.WriteAllTextAsync(tokenPath, "PUT_YOUR_BOT_TOKEN_HERE");
            return;
        }

        var token = (await File.ReadAllTextAsync(tokenPath)).Trim();
        if (token == "PUT_YOUR_BOT_TOKEN_HERE") return;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        });

        _client.Ready += OnReady;
        _client.SlashCommandExecuted += OnSlashCommand;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        _isRunning = true;
    }

    public async Task StopAsync()
    {
        if (_client != null)
        {
            await _client.StopAsync();
            _isRunning = false;
        }
    }

    private async Task OnReady()
    {
        // Register the /cp slash command globally
        var cpCommand = new SlashCommandBuilder()
            .WithName("cp")
            .WithDescription("Submit your combat power")
            .AddOption("value", ApplicationCommandOptionType.Integer, "Your current combat power", isRequired: true)
            .AddOption("ign", ApplicationCommandOptionType.String, "Your in-game name", isRequired: true);

        try
        {
            await _client!.CreateGlobalApplicationCommandAsync(cpCommand.Build());
        }
        catch (HttpException) { /* Command may already exist */ }
    }

    private async Task OnSlashCommand(SocketSlashCommand command)
    {
        if (command.CommandName != "cp") return;

        var cpValue = (long)command.Data.Options.First(o => o.Name == "value").Value;
        var ign = command.Data.Options.First(o => o.Name == "ign").Value.ToString()!;
        var discordId = command.User.Id.ToString();

        // Load members and try to match
        var members = await _dataService.LoadMembersAsync();
        var member = members.FirstOrDefault(m =>
            m.DiscordId == discordId || m.IGN.Equals(ign, StringComparison.OrdinalIgnoreCase));

        if (member != null)
        {
            // Update member's CP
            member.CombatPower = (int)cpValue;
            member.DiscordId = discordId;
            await _dataService.SaveMembersAsync(members);

            // Add to CP history
            var history = await _dataService.LoadCpHistoryAsync();
            history.Add(new CpRecord
            {
                MemberId = member.Id,
                CombatPower = (int)cpValue,
                RecordedDate = DateTime.Now,
                Source = "Discord"
            });
            await _dataService.SaveCpHistoryAsync(history);

            await command.RespondAsync($"✅ CP updated for **{member.IGN}**: {cpValue:N0}", ephemeral: true);
        }
        else
        {
            await command.RespondAsync($"❌ No guild member found with IGN '{ign}'. Ask a leader to add you first.", ephemeral: true);
        }
    }
}
