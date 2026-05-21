using ClosedXML.Excel;
using GuildTracker.Models;

namespace GuildTracker.Services;

public class ExportService
{
    public void ExportMembers(List<GuildMember> members, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Members");

        ws.Cell(1, 1).Value = "IGN";
        ws.Cell(1, 2).Value = "Class";
        ws.Cell(1, 3).Value = "Combat Power";
        ws.Cell(1, 4).Value = "Role";
        ws.Cell(1, 5).Value = "Notes";
        ws.Cell(1, 6).Value = "Join Date";
        ws.Cell(1, 7).Value = "Active";

        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            ws.Cell(i + 2, 1).Value = m.IGN;
            ws.Cell(i + 2, 2).Value = m.Class;
            ws.Cell(i + 2, 3).Value = m.CombatPower;
            ws.Cell(i + 2, 4).Value = m.Role;
            ws.Cell(i + 2, 5).Value = m.Notes;
            ws.Cell(i + 2, 6).Value = m.JoinDate.ToShortDateString();
            ws.Cell(i + 2, 7).Value = m.IsActive ? "Yes" : "No";
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportAttendance(List<GuildMember> members, List<AttendanceRecord> records, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Attendance");

        // Get unique dates
        var dates = records.Select(r => r.EventDate.Date).Distinct().OrderBy(d => d).ToList();

        // Headers
        ws.Cell(1, 1).Value = "IGN";
        ws.Cell(1, 2).Value = "Event";
        for (int i = 0; i < dates.Count; i++)
            ws.Cell(1, i + 3).Value = dates[i].ToShortDateString();
        ws.Cell(1, dates.Count + 3).Value = "Absent Count";

        // Group by event
        var events = records.Select(r => r.EventName).Distinct().ToList();
        int row = 2;

        foreach (var member in members)
        {
            foreach (var evt in events)
            {
                ws.Cell(row, 1).Value = member.IGN;
                ws.Cell(row, 2).Value = evt;

                int absentCount = 0;
                for (int col = 0; col < dates.Count; col++)
                {
                    var isAbsent = records.Any(r =>
                        r.MemberId == member.Id && r.EventName == evt && r.EventDate.Date == dates[col]);
                    ws.Cell(row, col + 3).Value = isAbsent ? "Absent" : "Present";
                    if (isAbsent) absentCount++;
                }
                ws.Cell(row, dates.Count + 3).Value = absentCount;
                row++;
            }
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
