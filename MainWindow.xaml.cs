using System.Windows;
using System.Windows.Input;
using GuildTracker.ViewModels;
using GuildTracker.Views;

namespace GuildTracker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Keyboard shortcuts
        InputBindings.Add(new KeyBinding(new RelayCommandSimple(() =>
        {
            var vm = DataContext as MainViewModel;
            vm?.SaveCommand.Execute(null);
        }), Key.S, ModifierKeys.Control));

        InputBindings.Add(new KeyBinding(new RelayCommandSimple(() =>
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }), Key.F, ModifierKeys.Control));

        InputBindings.Add(new KeyBinding(new RelayCommandSimple(() =>
        {
            var vm = DataContext as MainViewModel;
            vm?.UndoCommand.Execute(null);
        }), Key.Z, ModifierKeys.Control));

        InputBindings.Add(new KeyBinding(new RelayCommandSimple(() =>
        {
            var vm = DataContext as MainViewModel;
            if (vm?.CurrentView == "Members")
                vm.RemoveMemberCommand.Execute(MembersGrid.SelectedItems);
        }), Key.Delete, ModifierKeys.None));
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.RemoveMemberCommand.Execute(MembersGrid.SelectedItems);
    }

    private void MarkAbsent_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.MarkAbsentCommand.Execute(AttendanceGrid.SelectedItems);
    }

    private void UnmarkAbsent_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.UnmarkAbsentCommand.Execute(AttendanceGrid.SelectedItems);
    }

    private void SetMvp_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.SetMvpCommand.Execute(AttendanceGrid.SelectedItems);
    }

    private void SetGodOfWar_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.SetGodOfWarCommand.Execute(AttendanceGrid.SelectedItems);
    }

    private void SetBestSupport_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.SetBestSupportCommand.Execute(AttendanceGrid.SelectedItems);
    }

    private void MembersGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        if (vm?.SelectedMember == null) return;

        var dialog = new MemberDetailDialog(
            vm.SelectedMember,
            vm.AttendanceRecords,
            vm.CpHistory.ToList())
        {
            Owner = this
        };
        dialog.ShowDialog();
        vm.UpdateDashboardPublic();
        vm.RefreshMemberList();
    }
}

/// <summary>
/// Simple parameterless command for keyboard bindings.
/// </summary>
public class RelayCommandSimple : ICommand
{
    private readonly Action _execute;
    public RelayCommandSimple(Action execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
