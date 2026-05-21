using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using GuildTracker.Models;

namespace GuildTracker.Views;

public partial class MemberDetailDialog : Window
{
    private readonly ObservableCollection<AttendanceRecord> _allRecords;
    private readonly GuildMember _member;

    public MemberDetailDialog(GuildMember member, ObservableCollection<AttendanceRecord> allRecords, List<CpRecord> cpHistory)
    {
        InitializeComponent();
        _allRecords = allRecords;
        _member = member;
        DataContext = new MemberDetailViewModel(member, allRecords.ToList());
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ClearSelected_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MemberDetailViewModel;
        var selected = AbsenceList.SelectedItems.Cast<AttendanceRecord>().ToList();
        if (selected.Count == 0) return;

        var result = MessageBox.Show($"Clear {selected.Count} absence record(s)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        foreach (var record in selected)
        {
            record.IsAbsent = false;
            // Remove record entirely if it has no other data
            if (!record.IsMvp && !record.IsGodOfWar && !record.IsBestSupport)
                _allRecords.Remove(record);
        }

        // Refresh the view model
        DataContext = new MemberDetailViewModel(_member, _allRecords.ToList());
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MemberDetailViewModel;
        if (vm == null || vm.TotalAbsences == 0) return;

        var result = MessageBox.Show($"Clear ALL {vm.TotalAbsences} absences for {vm.IGN}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var absences = _allRecords.Where(r => r.MemberId == _member.Id && r.IsAbsent).ToList();
        foreach (var record in absences)
        {
            record.IsAbsent = false;
            if (!record.IsMvp && !record.IsGodOfWar && !record.IsBestSupport)
                _allRecords.Remove(record);
        }

        DataContext = new MemberDetailViewModel(_member, _allRecords.ToList());
    }
}

public class MemberDetailViewModel
{
    public MemberDetailViewModel(GuildMember member, List<AttendanceRecord> allRecords)
    {
        IGN = member.IGN;
        Class = member.Class;
        Role = member.Role;
        CombatPower = member.CombatPower;
        JoinDate = member.JoinDate;
        DiscordId = string.IsNullOrEmpty(member.DiscordId) ? "—" : member.DiscordId;
        Notes = string.IsNullOrEmpty(member.Notes) ? "No notes." : member.Notes;

        var colors = new[]
        {
            "#f38ba8", "#fab387", "#f9e2af", "#a6e3a1", "#94e2d5",
            "#89dceb", "#89b4fa", "#b4befe", "#cba6f7", "#f5c2e7",
            "#74c7ec", "#eba0ac", "#f2cdcd", "#f5e0dc", "#e78284",
            "#ef9f76", "#e5c890", "#a6d189", "#81c8be", "#99d1db",
        };
        var idx = System.Math.Abs(Class.GetHashCode()) % colors.Length;
        ClassColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[idx]));

        var memberRecords = allRecords.Where(r => r.MemberId == member.Id).ToList();

        AbsenceHistory = memberRecords.Where(r => r.IsAbsent).OrderByDescending(r => r.EventDate).ToList();
        MvpHistory = memberRecords.Where(r => r.IsMvp).OrderByDescending(r => r.EventDate).ToList();
        GodOfWarHistory = memberRecords.Where(r => r.IsGodOfWar).OrderByDescending(r => r.EventDate).ToList();
        BestSupportHistory = memberRecords.Where(r => r.IsBestSupport).OrderByDescending(r => r.EventDate).ToList();

        TotalAbsences = AbsenceHistory.Count;
        TotalMvps = MvpHistory.Count;
        TotalGodOfWar = GodOfWarHistory.Count;
        TotalBestSupport = BestSupportHistory.Count;
    }

    public string IGN { get; }
    public string Class { get; }
    public string Role { get; }
    public int CombatPower { get; }
    public DateTime JoinDate { get; }
    public string DiscordId { get; }
    public string Notes { get; }
    public SolidColorBrush ClassColor { get; }
    public int TotalAbsences { get; }
    public int TotalMvps { get; }
    public int TotalGodOfWar { get; }
    public int TotalBestSupport { get; }
    public List<AttendanceRecord> AbsenceHistory { get; }
    public List<AttendanceRecord> MvpHistory { get; }
    public List<AttendanceRecord> GodOfWarHistory { get; }
    public List<AttendanceRecord> BestSupportHistory { get; }
}
