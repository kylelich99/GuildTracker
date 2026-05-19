using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GuildTracker.Helpers;
using GuildTracker.Models;
using GuildTracker.Services;
using Microsoft.Win32;

namespace GuildTracker.ViewModels;

/// <summary>
/// Main ViewModel - manages members, attendance, CP history, and navigation.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly JsonDataService _dataService = new();
    private readonly ExportService _exportService = new();

    public MainViewModel()
    {
        AddMemberCommand = new RelayCommand(_ => AddMember());
        RemoveMemberCommand = new RelayCommand(_ => RemoveMember(), _ => SelectedMember != null);
        SaveCommand = new RelayCommand(async _ => await SaveAllAsync());
        ExportMembersCommand = new RelayCommand(_ => ExportMembers());
        ExportAttendanceCommand = new RelayCommand(_ => ExportAttendance());
        AddAttendanceEventCommand = new RelayCommand(_ => AddAttendanceEvent());
        MarkAbsentCommand = new RelayCommand(_ => MarkAttendance(AttendanceStatus.Absent));
        MarkLateCommand = new RelayCommand(_ => MarkAttendance(AttendanceStatus.Late));
        MarkPartialCommand = new RelayCommand(_ => MarkAttendance(AttendanceStatus.Partial));
        MarkPresentCommand = new RelayCommand(_ => MarkAttendance(AttendanceStatus.Present));
        UpdateCpCommand = new RelayCommand(_ => UpdateCombatPower());
        NavigateCommand = new RelayCommand(p => CurrentView = p?.ToString() ?? "Dashboard");

        _ = LoadAllAsync();
    }

    // --- Collections ---
    public ObservableCollection<GuildMember> Members { get; } = new();
    public ObservableCollection<AttendanceRecord> AttendanceRecords { get; } = new();
    public ObservableCollection<CpRecord> CpHistory { get; } = new();
    public ObservableCollection<GuildMember> FilteredMembers { get; } = new();

    // --- Dashboard Properties ---
    private int _totalMembers;
    public int TotalMembers { get => _totalMembers; set => SetProperty(ref _totalMembers, value); }

    private int _averageCp;
    public int AverageCp { get => _averageCp; set => SetProperty(ref _averageCp, value); }

    private double _attendancePercent;
    public double AttendancePercent { get => _attendancePercent; set => SetProperty(ref _attendancePercent, value); }

    private int _absentCount;
    public int AbsentCount { get => _absentCount; set => SetProperty(ref _absentCount, value); }

    // --- Selection & Navigation ---
    private GuildMember? _selectedMember;
    public GuildMember? SelectedMember
    {
        get => _selectedMember;
        set { SetProperty(ref _selectedMember, value); LoadMemberCpHistory(); }
    }

    private string _currentView = "Dashboard";
    public string CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ApplyFilter(); }
    }

    private DateTime _selectedEventDate = DateTime.Today;
    public DateTime SelectedEventDate { get => _selectedEventDate; set => SetProperty(ref _selectedEventDate, value); }

    private string _eventName = string.Empty;
    public string EventName { get => _eventName; set => SetProperty(ref _eventName, value); }

    private int _newCpValue;
    public int NewCpValue { get => _newCpValue; set => SetProperty(ref _newCpValue, value); }

    // --- Member CP History for display ---
    public ObservableCollection<CpRecord> SelectedMemberCpHistory { get; } = new();

    // --- Commands ---
    public ICommand AddMemberCommand { get; }
    public ICommand RemoveMemberCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ExportMembersCommand { get; }
    public ICommand ExportAttendanceCommand { get; }
    public ICommand AddAttendanceEventCommand { get; }
    public ICommand MarkAbsentCommand { get; }
    public ICommand MarkLateCommand { get; }
    public ICommand MarkPartialCommand { get; }
    public ICommand MarkPresentCommand { get; }
    public ICommand UpdateCpCommand { get; }
    public ICommand NavigateCommand { get; }

    // --- Data Operations ---
    private async Task LoadAllAsync()
    {
        var members = await _dataService.LoadMembersAsync();
        var attendance = await _dataService.LoadAttendanceAsync();
        var cpHistory = await _dataService.LoadCpHistoryAsync();

        Members.Clear();
        foreach (var m in members) Members.Add(m);

        AttendanceRecords.Clear();
        foreach (var a in attendance) AttendanceRecords.Add(a);

        CpHistory.Clear();
        foreach (var c in cpHistory) CpHistory.Add(c);

        ApplyFilter();
        UpdateDashboard();
    }

    private async Task SaveAllAsync()
    {
        await _dataService.SaveMembersAsync(Members.ToList());
        await _dataService.SaveAttendanceAsync(AttendanceRecords.ToList());
        await _dataService.SaveCpHistoryAsync(CpHistory.ToList());
        MessageBox.Show("Data saved successfully!", "Guild Tracker", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddMember()
    {
        var member = new GuildMember { IGN = "New Member", Class = "Novice" };
        Members.Add(member);
        ApplyFilter();
        UpdateDashboard();
        SelectedMember = member;
    }

    private void RemoveMember()
    {
        if (SelectedMember == null) return;
        Members.Remove(SelectedMember);
        ApplyFilter();
        UpdateDashboard();
    }

    private void ApplyFilter()
    {
        FilteredMembers.Clear();
        var query = SearchText.ToLower();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? Members
            : new ObservableCollection<GuildMember>(
                Members.Where(m => m.IGN.ToLower().Contains(query)
                    || m.Class.ToLower().Contains(query)
                    || m.Role.ToLower().Contains(query)));

        foreach (var m in filtered) FilteredMembers.Add(m);
    }

    private void UpdateDashboard()
    {
        TotalMembers = Members.Count;
        AverageCp = Members.Count > 0 ? (int)Members.Average(m => m.CombatPower) : 0;

        // Calculate attendance % from last 7 days
        var recentDate = DateTime.Today.AddDays(-7);
        var recentRecords = AttendanceRecords.Where(r => r.EventDate >= recentDate).ToList();
        var presentCount = recentRecords.Count(r => r.Status == AttendanceStatus.Present);
        AttendancePercent = recentRecords.Count > 0 ? (double)presentCount / recentRecords.Count * 100 : 100;

        // Today's absent count
        var todayRecords = AttendanceRecords.Where(r => r.EventDate.Date == DateTime.Today).ToList();
        AbsentCount = todayRecords.Count(r => r.Status == AttendanceStatus.Absent);
    }

    private void AddAttendanceEvent()
    {
        // Creates a "Present" record for all members on the selected date
        foreach (var member in Members)
        {
            var existing = AttendanceRecords.FirstOrDefault(r =>
                r.MemberId == member.Id && r.EventDate.Date == SelectedEventDate.Date && r.EventName == EventName);
            if (existing == null)
            {
                AttendanceRecords.Add(new AttendanceRecord
                {
                    MemberId = member.Id,
                    EventDate = SelectedEventDate,
                    EventName = EventName,
                    Status = AttendanceStatus.Present
                });
            }
        }
        UpdateDashboard();
    }

    private void MarkAttendance(AttendanceStatus status)
    {
        if (SelectedMember == null) return;
        var record = AttendanceRecords.FirstOrDefault(r =>
            r.MemberId == SelectedMember.Id && r.EventDate.Date == SelectedEventDate.Date && r.EventName == EventName);
        if (record != null)
            record.Status = status;
        else
            AttendanceRecords.Add(new AttendanceRecord
            {
                MemberId = SelectedMember.Id,
                EventDate = SelectedEventDate,
                EventName = EventName,
                Status = status
            });
        UpdateDashboard();
    }

    private void UpdateCombatPower()
    {
        if (SelectedMember == null || NewCpValue <= 0) return;
        SelectedMember.CombatPower = NewCpValue;
        CpHistory.Add(new CpRecord
        {
            MemberId = SelectedMember.Id,
            CombatPower = NewCpValue,
            RecordedDate = DateTime.Now,
            Source = "Manual"
        });
        LoadMemberCpHistory();
        UpdateDashboard();
    }

    private void LoadMemberCpHistory()
    {
        SelectedMemberCpHistory.Clear();
        if (SelectedMember == null) return;
        var history = CpHistory.Where(c => c.MemberId == SelectedMember.Id)
            .OrderByDescending(c => c.RecordedDate);
        foreach (var r in history) SelectedMemberCpHistory.Add(r);
    }

    private void ExportMembers()
    {
        var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "GuildMembers.xlsx" };
        if (dlg.ShowDialog() == true)
        {
            _exportService.ExportMembers(Members.ToList(), dlg.FileName);
            MessageBox.Show("Members exported!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ExportAttendance()
    {
        var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "Attendance.xlsx" };
        if (dlg.ShowDialog() == true)
        {
            _exportService.ExportAttendance(Members.ToList(), AttendanceRecords.ToList(), dlg.FileName);
            MessageBox.Show("Attendance exported!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
