using GuildTracker.Models;

namespace GuildTracker.Services;

public static class SeedDataService
{
    public static async Task SeedIfEmptyAsync()
    {
        var dataService = new JsonDataService();
        var members = await dataService.LoadMembersAsync();
        if (members.Count > 0) return;

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

        // CP history
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

        // Attendance - mark a few members as absent on random days
        var attendance = new List<AttendanceRecord>();
        var events = new[] { "Guild League 1", "Guild League 2", "Emperium Overrun" };
        foreach (var evt in events)
        {
            for (int i = 5; i >= 0; i--)
            {
                // Randomly mark 1-2 members absent per event per day
                var absentMembers = sampleMembers.OrderBy(_ => random.Next()).Take(random.Next(1, 3));
                foreach (var member in absentMembers)
                {
                    attendance.Add(new AttendanceRecord
                    {
                        MemberId = member.Id,
                        EventDate = DateTime.Today.AddDays(-i),
                        EventName = evt
                    });
                }
            }
        }
        await dataService.SaveAttendanceAsync(attendance);
    }
}
