using System.Windows;
using GuildTracker.Discord;
using GuildTracker.Services;

namespace GuildTracker;

public partial class App : Application
{
    private DiscordBotService? _discordBot;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Seed example data on first run
        await SeedDataService.SeedIfEmptyAsync();

        // Start Discord bot (non-blocking, fails silently if no token)
        _discordBot = new DiscordBotService();
        try { await _discordBot.StartAsync(); }
        catch { /* Bot is optional - runs fine without it */ }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_discordBot?.IsRunning == true)
            await _discordBot.StopAsync();
        base.OnExit(e);
    }
}
