using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using GuildTracker.Helpers;
using GuildTracker.Models;
using GuildTracker.Services;
using GuildTracker.Views;
using Microsoft.Win32;

namespace GuildTracker.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly JsonDataService _dataService = new();
    private readonly ExportService _exportService = new();

    public MainViewModel()
    {
        AddMemberCommand = new RelayCommand(_ => AddMember());
        RemoveMemberCommand = new RelayCommand(p => RemoveMembers(p));
        SaveCommand = new RelayCommand(async _ => await SaveAllAsync());
        ExportMembersCommand = new RelayCommand(_ => ExportMembers());
        ExportAttendanceCommand = new RelayCommand(_ => ExportAttendance());
        UpdateCpCommand = new RelayCommand(_ => UpdateCombatPower());
        NavigateCommand = new RelayCommand(p => CurrentView = p?.ToString() ?? "Dashboard");

        // Attendance
        MarkAbsentCommand = new RelayCommand(p => MarkAbsent(p));
        UnmarkAbsentCommand = new RelayCommand(p => UnmarkAbsent(p));
        SetMvpCommand = new RelayCommand(p => SetAward(p, "MVP"));
        SetGodOfWarCommand = new RelayCommand(p => SetAward(p, "GodOfWar"));
        SetBestSupportCommand = new RelayCommand(p => SetAward(p, "BestSupport"));
        AddAttendanceDateCommand = new RelayCommand(_ => AddAttendanceDate());
        PrevWeekCommand = new RelayCommand(_ => AttendanceDate = AttendanceDate.AddDays(-7));
        NextWeekCommand = new RelayCommand(_ => AttendanceDate = AttendanceDate.AddDays(7));
        UndoCommand = new RelayCommand(_ => Undo(), _ => _undoStack.Count > 0);
        DashboardPrevWeekCommand = new RelayCommand(_ => DashboardWeekStart = DashboardWeekStart.AddDays(-7));
        DashboardNextWeekCommand = new RelayCommand(_ => DashboardWeekStart = DashboardWeekStart.AddDays(7));

        // Settings
        AddClassCommand = new RelayCommand(_ => AddClass());
        RemoveClassCommand = new RelayCommand(_ => RemoveClass(), _ => !string.IsNullOrEmpty(SelectedClassToDelete) && SelectedClassToDelete != "All");
        AddRoleCommand = new RelayCommand(_ => AddRole());
        RemoveRoleCommand = new RelayCommand(_ => RemoveRole(), _ => !string.IsNullOrEmpty(SelectedRoleToDelete));
        AddEventCommand = new RelayCommand(_ => AddEvent());
        RemoveEventCommand = new RelayCommand(_ => RemoveEvent(), _ => SelectedEventToDelete != null);
        OpenDataFolderCommand = new RelayCommand(_ => OpenDataFolder());

        _ = LoadAllAsync();
    }

    // --- Collections ---
    public ObservableCollection<GuildMember> Members { get; } = new();
    public ObservableCollection<AttendanceRecord> AttendanceRecords { get; } = new();
    public ObservableCollection<CpRecord> CpHistory { get; } = new();
    public ObservableCollection<GuildMember> FilteredMembers { get; } = new();
    public ObservableCollection<string> AvailableClasses { get; } = new();
    public ObservableCollection<string> AvailableRoles { get; } = new();
    public ObservableCollection<GuildEvent> AvailableEvents { get; } = new();
    public ObservableCollection<string> EventTabNames { get; } = new();

    // Attendance: members shown for the selected event tab (with absent status)
    public ObservableCollection<AttendanceMemberRow> AttendanceRows { get; } = new();

    // Undo stack
    private readonly Stack<Action> _undoStack = new();

    // --- Dashboard ---
    private int _totalMembers;
    public int TotalMembers { get => _totalMembers; set => SetProperty(ref _totalMembers, value); }

    private int _averageCp;
    public int AverageCp { get => _averageCp; set => SetProperty(ref _averageCp, value); }

    private long _totalCp;
    public long TotalCp { get => _totalCp; set => SetProperty(ref _totalCp, value); }

    private int _weekAbsences;
    public int WeekAbsences { get => _weekAbsences; set => SetProperty(ref _weekAbsences, value); }

    private string _mvpOfWeek = "—";
    public string MvpOfWeek { get => _mvpOfWeek; set => SetProperty(ref _mvpOfWeek, value); }

    public ObservableCollection<EventScheduleRow> WeeklySchedule { get; } = new();

    private DateTime _dashboardWeekStart = DateTime.Today;
    public DateTime DashboardWeekStart
    {
        get => _dashboardWeekStart;
        set { SetProperty(ref _dashboardWeekStart, value); UpdateWeeklySchedule(); OnPropertyChanged(nameof(DashboardWeekLabel)); }
    }

    public string DashboardWeekLabel
    {
        get
        {
            var monday = GetMonday(DashboardWeekStart);
            var sunday = monday.AddDays(6);
            var thisMonday = GetMonday(DateTime.Today);
            if (monday == thisMonday) return "This Week";
            if (monday == thisMonday.AddDays(-7)) return "Last Week";
            if (monday == thisMonday.AddDays(7)) return "Next Week";
            return $"{monday:MMM dd} - {sunday:MMM dd}";
        }
    }

    private static DateTime GetMonday(DateTime date)
    {
        return date.AddDays(-((int)date.DayOfWeek + 6) % 7);
    }

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

    private string _selectedClassFilter = "All";
    public string SelectedClassFilter
    {
        get => _selectedClassFilter;
        set { SetProperty(ref _selectedClassFilter, value); ApplyFilter(); }
    }

    // --- Attendance ---
    private string _selectedEvent = string.Empty;
    public string SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetProperty(ref _selectedEvent, value))
            {
                // Auto-set date to this week's scheduled day
                var evt = AvailableEvents.FirstOrDefault(e => e.Name == value);
                if (evt != null)
                    _attendanceDate = evt.GetThisWeekDate();
                OnPropertyChanged(nameof(AttendanceDate));
                RefreshAttendanceRows();
            }
        }
    }

    private DateTime _attendanceDate = DateTime.Today;
    public DateTime AttendanceDate
    {
        get => _attendanceDate;
        set { SetProperty(ref _attendanceDate, value); RefreshAttendanceRows(); }
    }

    // --- CP ---
    private int _newCpValue;
    public int NewCpValue { get => _newCpValue; set => SetProperty(ref _newCpValue, value); }

    public ObservableCollection<CpRecord> SelectedMemberCpHistory { get; } = new();

    // --- Settings ---
    private string _newClassName = string.Empty;
    public string NewClassName { get => _newClassName; set => SetProperty(ref _newClassName, value); }

    private string _selectedClassToDelete = string.Empty;
    public string SelectedClassToDelete { get => _selectedClassToDelete; set => SetProperty(ref _selectedClassToDelete, value); }

    private string _newRoleName = string.Empty;
    public string NewRoleName { get => _newRoleName; set => SetProperty(ref _newRoleName, value); }

    private string _selectedRoleToDelete = string.Empty;
    public string SelectedRoleToDelete { get => _selectedRoleToDelete; set => SetProperty(ref _selectedRoleToDelete, value); }

    private string _newEventName = string.Empty;
    public string NewEventName { get => _newEventName; set => SetProperty(ref _newEventName, value); }

    private DayOfWeek _newEventDay = DayOfWeek.Monday;
    public DayOfWeek NewEventDay { get => _newEventDay; set => SetProperty(ref _newEventDay, value); }

    public Array DaysOfWeek => Enum.GetValues(typeof(DayOfWeek));

    private GuildEvent? _selectedEventToDelete;
    public GuildEvent? SelectedEventToDelete { get => _selectedEventToDelete; set => SetProperty(ref _selectedEventToDelete, value); }

    public string DataFolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    private string _statusMessage = "Ready";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public void UpdateDashboardPublic() => UpdateDashboard();

    public void RefreshMemberList() => ApplyFilter();

    private void UpdateAbsenceCounts()
    {
        foreach (var member in Members)
            member.AbsenceCount = AttendanceRecords.Count(r => r.MemberId == member.Id && r.IsAbsent);
    }

    private void UpdateCpTrends()
    {
        foreach (var member in Members)
        {
            var history = CpHistory.Where(c => c.MemberId == member.Id)
                .OrderByDescending(c => c.RecordedDate).Take(2).ToList();
            if (history.Count >= 2)
                member.CpTrend = history[0].CombatPower > history[1].CombatPower ? "↑" :
                                 history[0].CombatPower < history[1].CombatPower ? "↓" : "—";
            else
                member.CpTrend = "";
        }
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        action.Invoke();
        UpdateDashboard();
        RefreshAttendanceRows();
        ApplyFilter();
        StatusMessage = "Undo performed";
    }

    // --- Commands ---
    public ICommand AddMemberCommand { get; }
    public ICommand RemoveMemberCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ExportMembersCommand { get; }
    public ICommand ExportAttendanceCommand { get; }
    public ICommand MarkAbsentCommand { get; }
    public ICommand UnmarkAbsentCommand { get; }
    public ICommand SetMvpCommand { get; }
    public ICommand SetGodOfWarCommand { get; }
    public ICommand SetBestSupportCommand { get; }
    public ICommand AddAttendanceDateCommand { get; }
    public ICommand PrevWeekCommand { get; }
    public ICommand NextWeekCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand DashboardPrevWeekCommand { get; }
    public ICommand DashboardNextWeekCommand { get; }
    public ICommand UpdateCpCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand AddClassCommand { get; }
    public ICommand RemoveClassCommand { get; }
    public ICommand AddRoleCommand { get; }
    public ICommand RemoveRoleCommand { get; }
    public ICommand AddEventCommand { get; }
    public ICommand RemoveEventCommand { get; }
    public ICommand OpenDataFolderCommand { get; }

    // ==================== DATA ====================

    private async Task LoadAllAsync()
    {
        var members = await _dataService.LoadMembersAsync();
        var attendance = await _dataService.LoadAttendanceAsync();
        var cpHistory = await _dataService.LoadCpHistoryAsync();
        var classes = await _dataService.LoadClassesAsync();
        var roles = await _dataService.LoadRolesAsync();
        var events = await _dataService.LoadEventsAsync();

        Members.Clear();
        foreach (var m in members) Members.Add(m);

        AttendanceRecords.Clear();
        foreach (var a in attendance) AttendanceRecords.Add(a);

        CpHistory.Clear();
        foreach (var c in cpHistory) CpHistory.Add(c);

        AvailableClasses.Clear();
        AvailableClasses.Add("All");
        foreach (var c in classes) AvailableClasses.Add(c);

        AvailableRoles.Clear();
        foreach (var r in roles) AvailableRoles.Add(r);

        AvailableEvents.Clear();
        EventTabNames.Clear();
        foreach (var e in events)
        {
            AvailableEvents.Add(e);
            EventTabNames.Add(e.Name);
        }

        if (EventTabNames.Count > 0)
            SelectedEvent = EventTabNames[0];

        ApplyFilter();
        UpdateDashboard();
    }

    private async Task SaveAllAsync()
    {
        await _dataService.SaveMembersAsync(Members.ToList());
        await _dataService.SaveAttendanceAsync(AttendanceRecords.ToList());
        await _dataService.SaveCpHistoryAsync(CpHistory.ToList());
        await _dataService.SaveClassesAsync(AvailableClasses.Where(c => c != "All").ToList());
        await _dataService.SaveRolesAsync(AvailableRoles.ToList());
        await _dataService.SaveEventsAsync(AvailableEvents.ToList());
        EventTabNames.Clear();
        foreach (var e in AvailableEvents) EventTabNames.Add(e.Name);
        StatusMessage = $"Saved at {DateTime.Now:HH:mm:ss}";
        MessageBox.Show("Data saved successfully!", "Guild Tracker", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task AutoSaveAsync()
    {
        await _dataService.SaveMembersAsync(Members.ToList());
        await _dataService.SaveAttendanceAsync(AttendanceRecords.ToList());
        await _dataService.SaveCpHistoryAsync(CpHistory.ToList());
        await _dataService.SaveClassesAsync(AvailableClasses.Where(c => c != "All").ToList());
        await _dataService.SaveRolesAsync(AvailableRoles.ToList());
        await _dataService.SaveEventsAsync(AvailableEvents.ToList());
        StatusMessage = $"Auto-saved at {DateTime.Now:HH:mm:ss}";
    }

    // ==================== MEMBERS ====================

    private void AddMember()
    {
        var classes = AvailableClasses.Where(c => c != "All").ToList();
        var roles = AvailableRoles.ToList();

        var dialog = new AddMemberDialog(classes, roles)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            var member = new GuildMember
            {
                IGN = dialog.IGN.Trim(),
                Class = dialog.SelectedClass,
                Role = dialog.SelectedRole,
                CombatPower = dialog.CombatPowerValue,
                Notes = dialog.Notes
            };
            Members.Add(member);
            ApplyFilter();
            UpdateDashboard();
            SelectedMember = member;
        }
    }

    private void RemoveMembers(object? parameter)
    {
        var selectedItems = parameter as IList;
        if (selectedItems == null || selectedItems.Count == 0)
        {
            if (SelectedMember != null)
                selectedItems = new List<GuildMember> { SelectedMember };
            else
                return;
        }

        var toRemove = selectedItems.Cast<GuildMember>().ToList();
        var names = string.Join("\n• ", toRemove.Select(m => m.IGN));
        var msg = toRemove.Count == 1
            ? $"Remove \"{toRemove[0].IGN}\" from the guild?"
            : $"Remove {toRemove.Count} members?\n\n• {names}";

        var result = MessageBox.Show(msg, "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        foreach (var member in toRemove)
            Members.Remove(member);

        ApplyFilter();
        UpdateDashboard();
    }

    private void ApplyFilter()
    {
        FilteredMembers.Clear();
        var query = SearchText.ToLower();
        var filtered = Members.AsEnumerable();

        if (!string.IsNullOrEmpty(SelectedClassFilter) && SelectedClassFilter != "All")
            filtered = filtered.Where(m => m.Class == SelectedClassFilter);

        if (!string.IsNullOrWhiteSpace(query))
            filtered = filtered.Where(m =>
                m.IGN.ToLower().Contains(query)
                || m.Class.ToLower().Contains(query)
                || m.Role.ToLower().Contains(query));

        foreach (var m in filtered) FilteredMembers.Add(m);
    }

    private void UpdateDashboard()
    {
        TotalMembers = Members.Count;
        AverageCp = Members.Count > 0 ? (int)Members.Average(m => m.CombatPower) : 0;
        TotalCp = Members.Sum(m => (long)m.CombatPower);
        UpdateAbsenceCounts();
        UpdateCpTrends();

        // This week (Mon-Sun)
        var monday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek + 6) % 7);
        var sunday = monday.AddDays(6);
        WeekAbsences = AttendanceRecords.Count(r => r.IsAbsent && r.EventDate.Date >= monday && r.EventDate.Date <= sunday);

        // MVPs of the week - unique names only
        var weekMvpNames = AttendanceRecords
            .Where(r => r.IsMvp && r.EventDate.Date >= monday && r.EventDate.Date <= sunday)
            .Select(r => r.MemberId)
            .Distinct()
            .Select(id => Members.FirstOrDefault(m => m.Id == id)?.IGN)
            .Where(n => n != null)
            .ToList();
        MvpOfWeek = weekMvpNames.Count > 0 ? string.Join(", ", weekMvpNames) : "—";

        // Weekly event schedule
        UpdateWeeklySchedule();
    }

    private void UpdateWeeklySchedule()
    {
        var monday = GetMonday(DashboardWeekStart);
        var sunday = monday.AddDays(6);

        WeeklySchedule.Clear();
        foreach (var evt in AvailableEvents.OrderBy(e => ((int)e.ScheduledDay + 6) % 7))
        {
            // Calculate event date for the viewed week
            int eventOffset = ((int)evt.ScheduledDay + 6) % 7;
            var eventDate = monday.AddDays(eventOffset);
            var isDone = eventDate.Date < DateTime.Today;
            var isToday = eventDate.Date == DateTime.Today;
            var absentCount = AttendanceRecords.Count(r => r.IsAbsent && r.EventName == evt.Name && r.EventDate.Date == eventDate.Date);

            WeeklySchedule.Add(new EventScheduleRow
            {
                EventName = evt.Name,
                Day = evt.ScheduledDay.ToString(),
                Date = eventDate.ToString("MMM dd"),
                Status = isToday ? "TODAY" : isDone ? "✓ Done" : "Upcoming",
                AbsentCount = absentCount,
                IsDone = isDone,
                IsToday = isToday
            });
        }
    }

    // ==================== ATTENDANCE ====================

    /// <summary>
    /// Refreshes the attendance grid rows for the selected event + date.
    /// Shows all members with their absent/present status.
    /// </summary>
    private void RefreshAttendanceRows()
    {
        AttendanceRows.Clear();
        if (string.IsNullOrEmpty(SelectedEvent)) return;

        foreach (var member in Members)
        {
            var record = AttendanceRecords.FirstOrDefault(r =>
                r.MemberId == member.Id
                && r.EventName == SelectedEvent
                && r.EventDate.Date == AttendanceDate.Date);

            AttendanceRows.Add(new AttendanceMemberRow
            {
                MemberId = member.Id,
                IGN = member.IGN,
                Class = member.Class,
                Role = member.Role,
                IsAbsent = record?.IsAbsent ?? false,
                IsMvp = record?.IsMvp ?? false,
                IsGodOfWar = record?.IsGodOfWar ?? false,
                IsBestSupport = record?.IsBestSupport ?? false
            });
        }
    }

    private void AddAttendanceDate()
    {
        // Just refreshes the view for the new date (records are created on mark)
        RefreshAttendanceRows();
    }

    /// <summary>
    /// Marks selected members as absent for the current event + date.
    /// </summary>
    private void MarkAbsent(object? parameter)
    {
        var selectedItems = parameter as IList;
        if (selectedItems == null || selectedItems.Count == 0) return;

        foreach (AttendanceMemberRow row in selectedItems.Cast<AttendanceMemberRow>().ToList())
        {
            if (row.IsAbsent) continue;

            var existing = AttendanceRecords.FirstOrDefault(r =>
                r.MemberId == row.MemberId && r.EventName == SelectedEvent && r.EventDate.Date == AttendanceDate.Date);

            if (existing != null)
                existing.IsAbsent = true;
            else
            {
                var newRec = new AttendanceRecord
                {
                    MemberId = row.MemberId,
                    EventName = SelectedEvent,
                    EventDate = AttendanceDate,
                    IsAbsent = true
                };
                AttendanceRecords.Add(newRec);
                var capturedRec = newRec;
                _undoStack.Push(() => AttendanceRecords.Remove(capturedRec));
            }
        }
        UpdateDashboard();
        RefreshAttendanceRows();
        _ = AutoSaveAsync();
    }

    private void UnmarkAbsent(object? parameter)
    {
        var selectedItems = parameter as IList;
        if (selectedItems == null || selectedItems.Count == 0) return;

        foreach (AttendanceMemberRow row in selectedItems.Cast<AttendanceMemberRow>().ToList())
        {
            if (!row.IsAbsent) continue;

            var record = AttendanceRecords.FirstOrDefault(r =>
                r.MemberId == row.MemberId && r.EventName == SelectedEvent && r.EventDate.Date == AttendanceDate.Date);

            if (record != null)
            {
                if (record.IsMvp)
                    record.IsAbsent = false; // keep record for MVP
                else
                    AttendanceRecords.Remove(record); // no reason to keep
            }
        }
        UpdateDashboard();
        RefreshAttendanceRows();
        _ = AutoSaveAsync();
    }

    private void SetAward(object? parameter, string awardType)
    {
        var selectedItems = parameter as IList;
        if (selectedItems == null || selectedItems.Count == 0) return;

        var selected = selectedItems.Cast<AttendanceMemberRow>().First();
        var record = AttendanceRecords.FirstOrDefault(r =>
            r.MemberId == selected.MemberId && r.EventName == SelectedEvent && r.EventDate.Date == AttendanceDate.Date);

        // If same player already has the award, toggle it off
        if (record != null)
        {
            bool alreadyHas = awardType == "MVP" ? record.IsMvp :
                              awardType == "GodOfWar" ? record.IsGodOfWar : record.IsBestSupport;
            if (alreadyHas)
            {
                if (awardType == "MVP") record.IsMvp = false;
                else if (awardType == "GodOfWar") record.IsGodOfWar = false;
                else record.IsBestSupport = false;

                // Remove record if nothing left on it
                if (!record.IsAbsent && !record.IsMvp && !record.IsGodOfWar && !record.IsBestSupport)
                    AttendanceRecords.Remove(record);

                RefreshAttendanceRows();
                return;
            }
        }

        // Clear existing winner for this award
        var existing = AttendanceRecords.FirstOrDefault(r =>
            r.EventName == SelectedEvent && r.EventDate.Date == AttendanceDate.Date &&
            (awardType == "MVP" ? r.IsMvp : awardType == "GodOfWar" ? r.IsGodOfWar : r.IsBestSupport));
        if (existing != null)
        {
            if (awardType == "MVP") existing.IsMvp = false;
            else if (awardType == "GodOfWar") existing.IsGodOfWar = false;
            else existing.IsBestSupport = false;
        }

        // Assign to selected player
        if (record != null)
        {
            if (awardType == "MVP") record.IsMvp = true;
            else if (awardType == "GodOfWar") record.IsGodOfWar = true;
            else record.IsBestSupport = true;
        }
        else
        {
            var newRecord = new AttendanceRecord
            {
                MemberId = selected.MemberId,
                EventName = SelectedEvent,
                EventDate = AttendanceDate,
                IsAbsent = false
            };
            if (awardType == "MVP") newRecord.IsMvp = true;
            else if (awardType == "GodOfWar") newRecord.IsGodOfWar = true;
            else newRecord.IsBestSupport = true;
            AttendanceRecords.Add(newRecord);
        }

        RefreshAttendanceRows();
        UpdateDashboard();
        _ = AutoSaveAsync();
    }

    // ==================== COMBAT POWER ====================

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
        NewCpValue = 0;
    }

    private void LoadMemberCpHistory()
    {
        SelectedMemberCpHistory.Clear();
        if (SelectedMember == null) return;
        var history = CpHistory.Where(c => c.MemberId == SelectedMember.Id)
            .OrderByDescending(c => c.RecordedDate);
        foreach (var r in history) SelectedMemberCpHistory.Add(r);
    }

    // ==================== SETTINGS ====================

    private void AddClass()
    {
        if (string.IsNullOrWhiteSpace(NewClassName)) return;
        var trimmed = NewClassName.Trim();
        if (!AvailableClasses.Contains(trimmed))
        {
            AvailableClasses.Add(trimmed);
            NewClassName = string.Empty;
        }
    }

    private void RemoveClass()
    {
        if (string.IsNullOrEmpty(SelectedClassToDelete) || SelectedClassToDelete == "All") return;
        var result = MessageBox.Show($"Remove class \"{SelectedClassToDelete}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            AvailableClasses.Remove(SelectedClassToDelete);
            SelectedClassToDelete = string.Empty;
        }
    }

    private void AddRole()
    {
        if (string.IsNullOrWhiteSpace(NewRoleName)) return;
        var trimmed = NewRoleName.Trim();
        if (!AvailableRoles.Contains(trimmed))
        {
            AvailableRoles.Add(trimmed);
            NewRoleName = string.Empty;
        }
    }

    private void RemoveRole()
    {
        if (string.IsNullOrEmpty(SelectedRoleToDelete)) return;
        var result = MessageBox.Show($"Remove role \"{SelectedRoleToDelete}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            AvailableRoles.Remove(SelectedRoleToDelete);
            SelectedRoleToDelete = string.Empty;
        }
    }

    private void AddEvent()
    {
        if (string.IsNullOrWhiteSpace(NewEventName)) return;
        var trimmed = NewEventName.Trim();
        if (AvailableEvents.Any(e => e.Name == trimmed)) return;

        var newEvent = new GuildEvent { Name = trimmed, ScheduledDay = NewEventDay };
        AvailableEvents.Add(newEvent);
        EventTabNames.Add(newEvent.Name);
        NewEventName = string.Empty;
    }

    private void RemoveEvent()
    {
        if (SelectedEventToDelete == null) return;
        var result = MessageBox.Show($"Remove event \"{SelectedEventToDelete.Name}\"?\nThis won't delete existing records.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            EventTabNames.Remove(SelectedEventToDelete.Name);
            AvailableEvents.Remove(SelectedEventToDelete);
            SelectedEventToDelete = null;
        }
    }

    private void OpenDataFolder()
    {
        Process.Start("explorer.exe", DataFolderPath);
    }

    // ==================== EXPORT ====================

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

public class EventScheduleRow
{
    public string EventName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AbsentCount { get; set; }
    public bool IsDone { get; set; }
    public bool IsToday { get; set; }
}

public class AttendanceMemberRow : ViewModelBase
{
    public string MemberId { get; set; } = string.Empty;
    public string IGN { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    private bool _isAbsent;
    public bool IsAbsent
    {
        get => _isAbsent;
        set => SetProperty(ref _isAbsent, value);
    }

    private bool _isMvp;
    public bool IsMvp
    {
        get => _isMvp;
        set => SetProperty(ref _isMvp, value);
    }

    private bool _isGodOfWar;
    public bool IsGodOfWar
    {
        get => _isGodOfWar;
        set => SetProperty(ref _isGodOfWar, value);
    }

    private bool _isBestSupport;
    public bool IsBestSupport
    {
        get => _isBestSupport;
        set => SetProperty(ref _isBestSupport, value);
    }
}
