namespace GuildTracker.Models;

/// <summary>
/// Stores a combat power snapshot for a member at a point in time.
/// </summary>
public class CpRecord
{
    public string MemberId { get; set; } = string.Empty;
    public int CombatPower { get; set; }
    public DateTime RecordedDate { get; set; } = DateTime.Now;
    public string Source { get; set; } = "Manual"; // "Manual" or "Discord"
}
