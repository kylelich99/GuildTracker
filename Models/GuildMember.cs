using MongoDB.Bson.Serialization.Attributes;

namespace GuildTracker.Models;

/// <summary>
/// Represents a guild member in Ragnarok Origin Classic.
/// </summary>
public class GuildMember
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IGN { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int CombatPower { get; set; }
    public string Role { get; set; } = "Member";
    public string Notes { get; set; } = string.Empty;
    public string DiscordId { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    public int AbsenceCount { get; set; }
    public string CpTrend { get; set; } = string.Empty; // "↑", "↓", "—"
    public bool IsPriority { get; set; }
    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public int AttendancePct { get; set; } = 100;
}
