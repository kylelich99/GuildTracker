using MongoDB.Bson;
using MongoDB.Driver;

namespace GuildTrackerBot;

public class BotDataService
{
    private readonly IMongoDatabase _db;

    public BotDataService(string connectionString, string databaseName)
    {
        _db = new MongoClient(connectionString).GetDatabase(databaseName);
    }

    private IMongoCollection<GuildMember> Members => _db.GetCollection<GuildMember>("GuildMembers");
    private IMongoCollection<CpRecord> CpHistory => _db.GetCollection<CpRecord>("CpRecords");
    private IMongoCollection<AttendanceRecord> Attendance => _db.GetCollection<AttendanceRecord>("AttendanceRecords");
    private IMongoCollection<ConfigDoc> Configs => _db.GetCollection<ConfigDoc>("Configs");

    public async Task<List<GuildMember>> LoadMembersAsync() =>
        await Members.Find(m => m.IsActive).ToListAsync();

    public async Task<List<GuildMember>> LoadTopMembersByCpAsync(int count = 10) =>
        await Members.Find(m => m.IsActive).SortByDescending(m => m.CombatPower).Limit(count).ToListAsync();

    public async Task InsertMemberAsync(GuildMember member) =>
        await Members.InsertOneAsync(member);

    public async Task UpdateMemberAsync(GuildMember member) =>
        await Members.ReplaceOneAsync(m => m.Id == member.Id, member, new ReplaceOptions { IsUpsert = true });

    public async Task AddCpRecordAsync(CpRecord record) =>
        await CpHistory.InsertOneAsync(record);

    public async Task<List<CpRecord>> LoadCpHistoryForMemberAsync(string memberId, int limit = 5) =>
        await CpHistory.Find(r => r.MemberId == memberId).SortByDescending(r => r.RecordedDate).Limit(limit).ToListAsync();

    public async Task<List<AttendanceRecord>> LoadAttendanceForMemberAsync(string memberId) =>
        await Attendance.Find(r => r.MemberId == memberId).ToListAsync();

    public async Task<List<string>> LoadClassesAsync()
    {
        var doc = await Configs.Find(c => c.Key == "classes").FirstOrDefaultAsync();
        return doc?.Values ?? new List<string>();
    }
}

public class ConfigDoc
{
    [MongoDB.Bson.Serialization.Attributes.BsonId]
    public string Key { get; set; } = string.Empty;
    public List<string>? Values { get; set; }
}
