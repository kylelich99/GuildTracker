using Discord;
using Discord.Net;
using Discord.WebSocket;
using GuildTrackerBot;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new Exception("DISCORD_TOKEN environment variable not set.");
var mongoConn = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? throw new Exception("MONGODB_CONNECTION_STRING environment variable not set.");
var mongoDb = Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? "GuildTrackerDb";

var dataService = new BotDataService(mongoConn, mongoDb);

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds
});

client.Log += msg => { Console.WriteLine(msg.ToString()); return Task.CompletedTask; };
client.Ready += OnReady;
client.SlashCommandExecuted += OnSlashCommand;

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();
await Task.Delay(Timeout.Infinite);

async Task OnReady()
{
    var classes = await dataService.LoadClassesAsync();

    var commands = new List<SlashCommandBuilder>
    {
        new SlashCommandBuilder()
            .WithName("register")
            .WithDescription("Register yourself as a guild member")
            .AddOption("ign", ApplicationCommandOptionType.String, "Your in-game name", isRequired: true)
            .AddOption("combatpower", ApplicationCommandOptionType.Integer, "Your current combat power", isRequired: true)
            .AddOption(BuildClassOption("class", "Your character class", true, classes))
            .AddOption("notes", ApplicationCommandOptionType.String, "Any notes (optional)", isRequired: false),

        new SlashCommandBuilder()
            .WithName("updatecp")
            .WithDescription("Update your combat power")
            .AddOption("value", ApplicationCommandOptionType.Integer, "Your current combat power", isRequired: true),

        new SlashCommandBuilder()
            .WithName("updateign")
            .WithDescription("Update your in-game name")
            .AddOption("value", ApplicationCommandOptionType.String, "Your new IGN", isRequired: true),

        new SlashCommandBuilder()
            .WithName("updateclass")
            .WithDescription("Update your character class")
            .AddOption(BuildClassOption("value", "Your new class", true, classes)),

        new SlashCommandBuilder()
            .WithName("updatenotes")
            .WithDescription("Update your notes")
            .AddOption("value", ApplicationCommandOptionType.String, "Your new notes", isRequired: true),

        new SlashCommandBuilder()
            .WithName("myinfo")
            .WithDescription("Show your full guild member profile"),

        new SlashCommandBuilder()
            .WithName("leaderboard")
            .WithDescription("Show top 10 members by Combat Power"),

        new SlashCommandBuilder()
            .WithName("cphistory")
            .WithDescription("Show your CP progression over time"),
    };

    try
    {
        foreach (var cmd in commands)
            await client.CreateGlobalApplicationCommandAsync(cmd.Build());
        Console.WriteLine("Slash commands registered.");
    }
    catch (HttpException ex) { Console.WriteLine($"Command registration failed: {ex.Message}"); }
}

async Task OnSlashCommand(SocketSlashCommand command)
{
    var discordId = command.User.Id.ToString();
    var members = await dataService.LoadMembersAsync();
    var member = members.FirstOrDefault(m => m.DiscordId == discordId);

    switch (command.CommandName)
    {
        case "register":
        {
            if (member != null)
            {
                await command.RespondAsync($"❌ You are already registered as **{member.IGN}**. Use `/updatecp`, `/updateign`, etc. to make changes.", ephemeral: true);
                return;
            }

            var ign = command.Data.Options.First(o => o.Name == "ign").Value.ToString()!.Trim();
            var cp = (int)(long)command.Data.Options.First(o => o.Name == "combatpower").Value;
            var cls = command.Data.Options.First(o => o.Name == "class").Value.ToString()!;
            var notes = command.Data.Options.FirstOrDefault(o => o.Name == "notes")?.Value?.ToString() ?? string.Empty;

            if (members.Any(m => m.IGN.Equals(ign, StringComparison.OrdinalIgnoreCase)))
            {
                await command.RespondAsync($"❌ IGN **{ign}** is already registered. Contact a leader if this is your character.", ephemeral: true);
                return;
            }

            var newMember = new GuildMember
            {
                IGN = ign,
                CombatPower = cp,
                Class = cls,
                Notes = notes,
                DiscordId = discordId,
                JoinDate = DateTime.UtcNow,
                Role = "Member",
                IsActive = true
            };

            await dataService.InsertMemberAsync(newMember);
            await dataService.AddCpRecordAsync(new CpRecord
            {
                MemberId = newMember.Id,
                CombatPower = cp,
                RecordedDate = DateTime.UtcNow,
                Source = "Discord"
            });

            await command.RespondAsync(
                $"✅ Registered! Welcome **{ign}**!\n" +
                $"⚔️ CP: **{cp:N0}** | 🧙 Class: **{cls}**\n" +
                $"You can now use `/updatecp`, `/myinfo`, and other commands.",
                ephemeral: true);
            break;
        }

        case "updatecp":
        {
            if (member == null) { await NotRegistered(command); return; }

            var cp = (int)(long)command.Data.Options.First(o => o.Name == "value").Value;
            var oldCp = member.CombatPower;
            member.CombatPower = cp;
            await dataService.UpdateMemberAsync(member);
            await dataService.AddCpRecordAsync(new CpRecord
            {
                MemberId = member.Id,
                CombatPower = cp,
                RecordedDate = DateTime.UtcNow,
                Source = "Discord"
            });

            var trend = cp > oldCp ? "↑" : cp < oldCp ? "↓" : "—";
            await command.RespondAsync($"✅ CP updated: {oldCp:N0} → **{cp:N0}** {trend}", ephemeral: true);
            break;
        }

        case "updateign":
        {
            if (member == null) { await NotRegistered(command); return; }

            var newIgn = command.Data.Options.First(o => o.Name == "value").Value.ToString()!.Trim();
            if (members.Any(m => m.Id != member.Id && m.IGN.Equals(newIgn, StringComparison.OrdinalIgnoreCase)))
            {
                await command.RespondAsync($"❌ IGN **{newIgn}** is already taken.", ephemeral: true);
                return;
            }

            var oldIgn = member.IGN;
            member.IGN = newIgn;
            await dataService.UpdateMemberAsync(member);
            await command.RespondAsync($"✅ IGN updated: **{oldIgn}** → **{newIgn}**", ephemeral: true);
            break;
        }

        case "updateclass":
        {
            if (member == null) { await NotRegistered(command); return; }

            var newClass = command.Data.Options.First(o => o.Name == "value").Value.ToString()!;
            member.Class = newClass;
            await dataService.UpdateMemberAsync(member);
            await command.RespondAsync($"✅ Class updated to **{newClass}**", ephemeral: true);
            break;
        }

        case "updatenotes":
        {
            if (member == null) { await NotRegistered(command); return; }

            member.Notes = command.Data.Options.First(o => o.Name == "value").Value.ToString()!;
            await dataService.UpdateMemberAsync(member);
            await command.RespondAsync($"✅ Notes updated.", ephemeral: true);
            break;
        }

        case "myinfo":
        {
            if (member == null) { await NotRegistered(command); return; }

            var attendance = await dataService.LoadAttendanceForMemberAsync(member.Id);
            var cpHistory = await dataService.LoadCpHistoryForMemberAsync(member.Id);

            var mvpCount = attendance.Count(r => r.IsMvp);
            var godOfWarCount = attendance.Count(r => r.IsGodOfWar);
            var bestSupportCount = attendance.Count(r => r.IsBestSupport);

            var recentCp = cpHistory.Count > 0
                ? string.Join("\n", cpHistory.Select(r => $"  • {r.CombatPower:N0} — {r.RecordedDate:MMM dd, yyyy} ({r.Source})"))
                : "  No history yet.";

            var awards = new List<string>();
            if (mvpCount > 0) awards.Add($"🏆 MVP x{mvpCount}");
            if (godOfWarCount > 0) awards.Add($"⚔️ God of War x{godOfWarCount}");
            if (bestSupportCount > 0) awards.Add($"🛡️ Best Support x{bestSupportCount}");

            var embed = new EmbedBuilder()
                .WithTitle($"📋 {member.IGN}")
                .WithColor(Color.Gold)
                .AddField("Class", string.IsNullOrEmpty(member.Class) ? "—" : member.Class, inline: true)
                .AddField("Role", member.Role, inline: true)
                .AddField("Combat Power", $"{member.CombatPower:N0} {member.CpTrend}", inline: true)
                .AddField("Absences", member.AbsenceCount.ToString(), inline: true)
                .AddField("Priority", member.IsPriority ? "✅ Yes" : "No", inline: true)
                .AddField("Joined", member.JoinDate.ToString("MMM dd, yyyy"), inline: true)
                .AddField("Awards", awards.Count > 0 ? string.Join("  ", awards) : "None")
                .AddField("CP History (last 5)", recentCp)
                .WithFooter(string.IsNullOrEmpty(member.Notes) ? "" : $"📝 {member.Notes}")
                .Build();

            await command.RespondAsync(embed: embed, ephemeral: true);
            break;
        }

        case "leaderboard":
        {
            var top = await dataService.LoadTopMembersByCpAsync(10);
            if (top.Count == 0)
            {
                await command.RespondAsync("No members registered yet.");
                return;
            }

            var medals = new[] { "🥇", "🥈", "🥉" };
            var lines = top.Select((m, i) =>
            {
                var prefix = i < 3 ? medals[i] : $"`{i + 1}.`";
                return $"{prefix} **{m.IGN}** — {m.CombatPower:N0} CP  _{m.Class}_";
            });

            var embed = new EmbedBuilder()
                .WithTitle("⚔️ Guild CP Leaderboard")
                .WithColor(Color.Gold)
                .WithDescription(string.Join("\n", lines))
                .WithFooter($"Top {top.Count} members by Combat Power")
                .WithCurrentTimestamp()
                .Build();

            await command.RespondAsync(embed: embed); // public — not ephemeral
            break;
        }

        case "cphistory":
        {
            if (member == null) { await NotRegistered(command); return; }

            var history = await dataService.LoadCpHistoryForMemberAsync(member.Id, 20);
            if (history.Count == 0)
            {
                await command.RespondAsync("❌ No CP history found.", ephemeral: true);
                return;
            }

            var sorted = history.OrderBy(r => r.RecordedDate).ToList();
            var lines = sorted.Select(r =>
            {
                var src = r.Source == "Discord" ? "🤖" : "💻";
                return $"`{r.RecordedDate:MMM dd, yyyy}` {src} **{r.CombatPower:N0}**";
            });

            var first = sorted.First().CombatPower;
            var last = sorted.Last().CombatPower;
            var gain = last - first;
            var gainStr = gain >= 0 ? $"+{gain:N0}" : $"{gain:N0}";

            var embed = new EmbedBuilder()
                .WithTitle($"📈 CP History — {member.IGN}")
                .WithColor(Color.Blue)
                .WithDescription(string.Join("\n", lines))
                .WithFooter($"Total gain: {gainStr} CP over {sorted.Count} records")
                .Build();

            await command.RespondAsync(embed: embed, ephemeral: true);
            break;
        }
    }
}

Task NotRegistered(SocketSlashCommand command) =>
    command.RespondAsync("❌ You are not registered. Use `/register` to join the guild.", ephemeral: true);

SlashCommandOptionBuilder BuildClassOption(string name, string description, bool required, List<string> classes)
{
    var builder = new SlashCommandOptionBuilder()
        .WithName(name)
        .WithDescription(description)
        .WithType(ApplicationCommandOptionType.String)
        .WithRequired(required);
    foreach (var c in classes)
        builder.AddChoice(c, c);
    return builder;
}
