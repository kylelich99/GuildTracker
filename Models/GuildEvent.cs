namespace GuildTracker.Models;

/// <summary>
/// A guild event with a weekly schedule.
/// Week starts on Monday, ends on Sunday.
/// </summary>
public class GuildEvent
{
    public string Name { get; set; } = string.Empty;
    public DayOfWeek ScheduledDay { get; set; } = DayOfWeek.Sunday;

    /// <summary>
    /// Gets this week's date for the event using Monday as start of week.
    /// Monday=0 ... Sunday=6, so Sunday is always at the end.
    /// </summary>
    public DateTime GetThisWeekDate()
    {
        var today = DateTime.Today;
        int todayOffset = MondayOffset(today.DayOfWeek);
        int eventOffset = MondayOffset(ScheduledDay);
        int diff = eventOffset - todayOffset;
        return today.AddDays(diff);
    }

    /// <summary>
    /// Converts DayOfWeek to Monday-based offset (Mon=0, Tue=1, ... Sun=6).
    /// </summary>
    private static int MondayOffset(DayOfWeek day)
    {
        return ((int)day + 6) % 7;
    }
}
