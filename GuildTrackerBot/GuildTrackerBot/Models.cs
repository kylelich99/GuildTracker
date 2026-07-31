using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GuildTrackerBot;

public class GuildMember
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IGN { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int CombatPower { get; set; }
    public string Role { get; set; } = "Member";
    public string Notes { get; set; } = string.Empty;
    public string DiscordId { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public int AbsenceCount { get; set; }
    public string CpTrend { get; set; } = string.Empty;
    public bool IsPriority { get; set; }
}

public class CpRecord
{
    [BsonId] public ObjectId Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public int CombatPower { get; set; }
    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "Manual";
}

public class AttendanceRecord
{
    [BsonId] public ObjectId Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventName { get; set; } = string.Empty;
    public bool IsAbsent { get; set; }
    public bool IsMvp { get; set; }
    public bool IsGodOfWar { get; set; }
    public bool IsBestSupport { get; set; }
}
