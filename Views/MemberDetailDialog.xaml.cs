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
    private MemberDetailViewModel _vm;

    public bool WasSaved { get; private set; }

    public MemberDetailDialog(GuildMember member, ObservableCollection<AttendanceRecord> allRecords, List<CpRecord> cpHistory, List<string> classes, List<string> roles)
    {
        InitializeComponent();
        _allRecords = allRecords;
        _member = member;
        _vm = new MemberDetailViewModel(member, allRecords.ToList(), classes, roles);
        DataContext = _vm;
    }

    // Keep old constructor signature working
    public MemberDetailDialog(GuildMember member, ObservableCollection<AttendanceRecord> allRecords, List<CpRecord> cpHistory)
        : this(member, allRecords, cpHistory, new List<string>(), new List<string>())
    {
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _member.Class = _vm.Class;
        _member.Role = _vm.Role;
        _member.CombatPower = _vm.CombatPower;
        _member.Notes = _vm.Notes;
        _member.DiscordId = _vm.DiscordId;
        WasSaved = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ClearSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = AbsenceList.SelectedItems.Cast<AttendanceRecord>().ToList();
        if (selected.Count == 0) return;

        var result = MessageBox.Show($"Clear {selected.Count} absence record(s)?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        foreach (var record in selected)
        {
            record.IsAbsent = false;
            if (!record.IsMvp && !record.IsGodOfWar && !record.IsBestSupport)
                _allRecords.Remove(record);
        }

        _vm = new MemberDetailViewModel(_member, _allRecords.ToList(), _vm.Classes, _vm.Roles);
        DataContext = _vm;
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.TotalAbsences == 0) return;

        var result = MessageBox.Show($"Clear ALL {_vm.TotalAbsences} absences for {_vm.IGN}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var absences = _allRecords.Where(r => r.MemberId == _member.Id && r.IsAbsent).ToList();
        foreach (var record in absences)
        {
            record.IsAbsent = false;
            if (!record.IsMvp && !record.IsGodOfWar && !record.IsBestSupport)
                _allRecords.Remove(record);
        }

        _vm = new MemberDetailViewModel(_member, _allRecords.ToList(), _vm.Classes, _vm.Roles);
        DataContext = _vm;
    }
}

public class MemberDetailViewModel : INotifyPropertyChanged
{
    public MemberDetailViewModel(GuildMember member, List<AttendanceRecord> allRecords, List<string> classes, List<string> roles)
    {
        IGN = member.IGN;
        Class = member.Class;
        Role = member.Role;
        CombatPower = member.CombatPower;
        JoinDate = member.JoinDate;
        DiscordId = member.DiscordId ?? "";
        Notes = member.Notes ?? "";
        Classes = classes;
        Roles = roles;

        var memberRecords = allRecords.Where(r => r.MemberId == member.Id).ToList();
        AbsenceHistory = memberRecords.Where(r => r.IsAbsent).OrderByDescending(r => r.EventDate).ToList();
        MvpHistory = memberRecords.Where(r => r.IsMvp).OrderByDescending(r => r.EventDate).ToList();

        TotalAbsences = AbsenceHistory.Count;
        TotalMvps = MvpHistory.Count;
        TotalGodOfWar = memberRecords.Count(r => r.IsGodOfWar);
        TotalBestSupport = memberRecords.Count(r => r.IsBestSupport);
    }

    public string IGN { get; }
    public DateTime JoinDate { get; }
    public List<string> Classes { get; }
    public List<string> Roles { get; }

    private string _class = "";
    public string Class { get => _class; set { _class = value; OnPropertyChanged(); } }

    private string _role = "";
    public string Role { get => _role; set { _role = value; OnPropertyChanged(); } }

    private int _combatPower;
    public int CombatPower { get => _combatPower; set { _combatPower = value; OnPropertyChanged(); } }

    private string _discordId = "";
    public string DiscordId { get => _discordId; set { _discordId = value; OnPropertyChanged(); } }

    private string _notes = "";
    public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

    public int TotalAbsences { get; }
    public int TotalMvps { get; }
    public int TotalGodOfWar { get; }
    public int TotalBestSupport { get; }
    public List<AttendanceRecord> AbsenceHistory { get; }
    public List<AttendanceRecord> MvpHistory { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
