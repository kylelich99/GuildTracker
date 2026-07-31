using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GuildTracker.Views;

public partial class PriorityDialog : Window
{
    public List<string> ResultIGNs { get; private set; } = new();

    public PriorityDialog(List<string> prefilledIGNs, List<string> allIGNs)
    {
        InitializeComponent();
        DataContext = new PriorityDialogViewModel(prefilledIGNs, allIGNs);
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var vm = (PriorityDialogViewModel)DataContext;
        if (string.IsNullOrEmpty(vm.SelectedMemberToAdd)) return;
        if (!vm.PriorityPlayers.Contains(vm.SelectedMemberToAdd))
            vm.PriorityPlayers.Add(vm.SelectedMemberToAdd);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var vm = (PriorityDialogViewModel)DataContext;
        var selected = PriorityList.SelectedItems.Cast<string>().ToList();
        foreach (var ign in selected)
            vm.PriorityPlayers.Remove(ign);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ResultIGNs = ((PriorityDialogViewModel)DataContext).PriorityPlayers.ToList();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

public class PriorityDialogViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> PriorityPlayers { get; }
    public List<string> AllMembers { get; }

    private string _selectedMemberToAdd = string.Empty;
    public string SelectedMemberToAdd
    {
        get => _selectedMemberToAdd;
        set { _selectedMemberToAdd = value; OnPropertyChanged(); }
    }

    public PriorityDialogViewModel(List<string> prefilledIGNs, List<string> allIGNs)
    {
        PriorityPlayers = new ObservableCollection<string>(prefilledIGNs.Distinct().OrderBy(x => x));
        AllMembers = allIGNs.OrderBy(x => x).ToList();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
