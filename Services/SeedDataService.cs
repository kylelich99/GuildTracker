using GuildTracker.Models;
using GuildTracker.Services;

namespace GuildTracker.Services;

/// <summary>
/// Generates example data for first-time use so the app isn't empty.
/// Only seeds if members.json doesn't exist yet.
/// </summary>
public static class SeedDataService
{
    public static async Task SeedIfEmptyAsync()
    {
        var dataService = new JsonDataService();
        var members = await dataService.LoadMembersAsync();
        if (members.Count > 0) return; // Already has data

        // Example Ragnarok Origin Classic members
        var sampleMembers = new List<GuildMember>
        {
            new() { IGN = "ShadowKnight", Class = "Lord Knight", CombatPower = 285000, Role = "Guild Leader" },
            new() { IGN = "HolyPriest", Class = "High Priest", CombatPower = 245000, Role = "Vice Leader" },
            new() { IGN = "WindArcher", Class = "Sniper", CombatPower = 270000, Role = "Member" },
            new() { IGN = "StormWizard", Class = "High Wizard", CombatPower = 260000, Role = "Member" },
            new() { IGN = "IronSmith", Class = "Whitesmith", CombatPower = 230000, Role = "Member" },
            new() { IGN = "NightBlade", Class = "Assassin Cross", CombatPower = 275000, Role = "Member" },
            new() { IGN = "SoulLinker", Class = "Soul Linker", CombatPower = 210000, Role = "Member" },
            new() { IGN = "BardMaster", Class = "Clown", CombatPower = 220000, Role = "Member" },
        };

        await dataService.SaveMembersAsync(sampleMembers);

        // Example CP history
        var cpHistory = new List<CpRecord>();
        var random = new Random(42);
        foreach (var member in sampleMembers)
        {
            for (int i = 7; i >= 0; i--)
            {
                cpHistory.Add(new CpRecord
                {
                    MemberId = member.Id,
                    CombatPower = member.CombatPower - (i * random.Next(2000, 5000)),
                    RecordedDate = DateTime.Now.AddDays(-i),
                    Source = "Manual"
                });
            }
        }
        await dataService.SaveCpHistoryAsync(cpHistory);

        // Example attendance
        var attendance = new List<AttendanceRecord>();
        foreach (var member in sampleMembers)
        {
            for (int i = 5; i >= 0; i--)
            {
                var status = random.Next(10) < 8 ? AttendanceStatus.Present :
                             random.Next(3) == 0 ? AttendanceStatus.Absent :
                             AttendanceStatus.Late;
                attendance.Add(new AttendanceRecord
                {
                    MemberId = member.Id,
                    EventDate = DateTime.Today.AddDays(-i),
                    EventName = i % 2 == 0 ? "WoE" : "Guild Raid",
                    Status = status
                });
            }
        }
        await dataService.SaveAttendanceAsync(attendance);
    }
}
