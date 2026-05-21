using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using GuildTracker.Models;

namespace GuildTracker.Services;

public class MongoDataService
{
    private readonly IMongoDatabase _db;

    public MongoDataService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config["MongoDB:ConnectionString"];
        var databaseName = config["MongoDB:DatabaseName"];
        var client = new MongoClient(connectionString);
        _db = client.GetDatabase(databaseName);
    }

    private IMongoCollection<GuildMember> Members => _db.GetCollection<GuildMember>("GuildMembers");
    private IMongoCollection<AttendanceRecord> Attendance => _db.GetCollection<AttendanceRecord>("AttendanceRecords");
    private IMongoCollection<CpRecord> CpHistory => _db.GetCollection<CpRecord>("CpRecords");
    private IMongoCollection<ConfigDoc> Configs => _db.GetCollection<ConfigDoc>("Configs");

    // --- Members ---
    public async Task<List<GuildMember>> LoadMembersAsync() =>
        await Members.Find(_ => true).ToListAsync();

    public async Task SaveMembersAsync(List<GuildMember> members)
    {
        var existingIds = await Members.Find(_ => true).Project(m => m.Id).ToListAsync();
        var newIds = members.Select(m => m.Id).ToHashSet();

        // Remove members that no longer exist
        var toDelete = existingIds.Where(id => !newIds.Contains(id)).ToList();
        if (toDelete.Count > 0)
            await Members.DeleteManyAsync(m => toDelete.Contains(m.Id));

        // Upsert each member
        foreach (var member in members)
            await Members.ReplaceOneAsync(m => m.Id == member.Id, member, new ReplaceOptions { IsUpsert = true });
    }

    // --- Attendance ---
    public async Task<List<AttendanceRecord>> LoadAttendanceAsync() =>
        await Attendance.Find(_ => true).ToListAsync();

    public async Task SaveAttendanceAsync(List<AttendanceRecord> records)
    {
        await Attendance.DeleteManyAsync(_ => true);
        if (records.Count > 0)
            await Attendance.InsertManyAsync(records);
    }

    // --- CP History ---
    public async Task<List<CpRecord>> LoadCpHistoryAsync() =>
        await CpHistory.Find(_ => true).ToListAsync();

    public async Task SaveCpHistoryAsync(List<CpRecord> records)
    {
        await CpHistory.DeleteManyAsync(_ => true);
        if (records.Count > 0)
            await CpHistory.InsertManyAsync(records);
    }

    // --- Classes ---
    public async Task<List<string>> LoadClassesAsync()
    {
        var doc = await Configs.Find(c => c.Key == "classes").FirstOrDefaultAsync();
        if (doc?.Values == null || doc.Values.Count == 0)
        {
            var defaults = new List<string>
            {
                "Lord Knight", "Paladin", "High Wizard", "Professor",
                "Assassin Cross", "Stalker", "Sniper", "Minstrel",
                "Gypsy", "High Priest", "Champion", "Mastersmith",
                "Biochemist", "Summoner"
            };
            await SaveClassesAsync(defaults);
            return defaults;
        }
        return doc.Values;
    }

    public async Task SaveClassesAsync(List<string> classes)
    {
        await Configs.ReplaceOneAsync(
            c => c.Key == "classes",
            new ConfigDoc { Key = "classes", Values = classes },
            new ReplaceOptions { IsUpsert = true });
    }

    // --- Roles ---
    public async Task<List<string>> LoadRolesAsync()
    {
        var doc = await Configs.Find(c => c.Key == "roles").FirstOrDefaultAsync();
        if (doc?.Values == null || doc.Values.Count == 0)
        {
            var defaults = new List<string> { "Guild Leader", "Vice Leader", "Officer", "Member" };
            await SaveRolesAsync(defaults);
            return defaults;
        }
        return doc.Values;
    }

    public async Task SaveRolesAsync(List<string> roles)
    {
        await Configs.ReplaceOneAsync(
            c => c.Key == "roles",
            new ConfigDoc { Key = "roles", Values = roles },
            new ReplaceOptions { IsUpsert = true });
    }

    // --- Events ---
    public async Task<List<GuildEvent>> LoadEventsAsync()
    {
        var doc = await Configs.Find(c => c.Key == "events").FirstOrDefaultAsync();
        if (doc?.Events == null || doc.Events.Count == 0)
        {
            var defaults = new List<GuildEvent>
            {
                new() { Name = "Guild League 1", ScheduledDay = DayOfWeek.Tuesday },
                new() { Name = "Guild League 2", ScheduledDay = DayOfWeek.Thursday },
                new() { Name = "Emperium Overrun", ScheduledDay = DayOfWeek.Sunday }
            };
            await SaveEventsAsync(defaults);
            return defaults;
        }
        return doc.Events;
    }

    public async Task SaveEventsAsync(List<GuildEvent> events)
    {
        await Configs.ReplaceOneAsync(
            c => c.Key == "events",
            new ConfigDoc { Key = "events", Events = events },
            new ReplaceOptions { IsUpsert = true });
    }
}

/// <summary>
/// Helper document for storing config lists in a single collection.
/// </summary>
public class ConfigDoc
{
    [MongoDB.Bson.Serialization.Attributes.BsonId]
    public string Key { get; set; } = string.Empty;
    public List<string>? Values { get; set; }
    public List<GuildEvent>? Events { get; set; }
}
