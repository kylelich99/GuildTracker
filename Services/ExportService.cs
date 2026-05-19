using ClosedXML.Excel;
using GuildTracker.Models;

namespace GuildTracker.Services;

/// <summary>
/// Exports guild data to Excel using ClosedXML.
/// </summary>
public class ExportService
{
    public void ExportMembers(List<GuildMember> members, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Members");

        // Headers
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
        for (int i = 0; i < dates.Count; i++)
            ws.Cell(1, i + 2).Value = dates[i].ToShortDateString();
        ws.Cell(1, dates.Count + 2).Value = "Attendance %";

        // Data rows
        for (int row = 0; row < members.Count; row++)
        {
            var member = members[row];
            ws.Cell(row + 2, 1).Value = member.IGN;

            int present = 0;
            for (int col = 0; col < dates.Count; col++)
            {
                var record = records.FirstOrDefault(r => r.MemberId == member.Id && r.EventDate.Date == dates[col]);
                var status = record?.Status ?? AttendanceStatus.Present;
                ws.Cell(row + 2, col + 2).Value = status.ToString();
                if (status == AttendanceStatus.Present) present++;
            }

            var pct = dates.Count > 0 ? (double)present / dates.Count * 100 : 0;
            ws.Cell(row + 2, dates.Count + 2).Value = $"{pct:F1}%";
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
