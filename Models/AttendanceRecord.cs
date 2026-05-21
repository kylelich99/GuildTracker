namespace GuildTracker.Models;

/// <summary>
/// Records a member's status for a specific event on a specific date.
/// If no record exists, the member is assumed present and not MVP.
/// </summary>
public class AttendanceRecord
{
    public string MemberId { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventName { get; set; } = string.Empty;
    public bool IsAbsent { get; set; } = true;
    public bool IsMvp { get; set; }
    public bool IsGodOfWar { get; set; }
    public bool IsBestSupport { get; set; }
}
