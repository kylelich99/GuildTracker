using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GuildTracker.Models;

/// <summary>
/// Stores a combat power snapshot for a member at a point in time.
/// </summary>
public class CpRecord
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public int CombatPower { get; set; }
    public DateTime RecordedDate { get; set; } = DateTime.Now;
    public string Source { get; set; } = "Manual"; // "Manual" or "Discord"
}
