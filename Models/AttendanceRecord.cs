namespace GuildTracker.Models;

/// <summary>
/// Tracks attendance for a specific member on a specific event date.
/// </summary>
public class AttendanceRecord
{
    public string MemberId { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}

public enum AttendanceStatus
{
    Present,
    Absent,
    Late,
    Partial
}
