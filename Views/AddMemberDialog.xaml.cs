using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GuildTracker.Views;

public partial class AddMemberDialog : Window, INotifyPropertyChanged
{
    public AddMemberDialog(List<string> classes, List<string> roles)
    {
        InitializeComponent();
        DataContext = this;
        Classes = classes;
        Roles = roles;
        SelectedClass = classes.FirstOrDefault(c => c != "All") ?? "";
        SelectedRole = roles.FirstOrDefault() ?? "Member";

        // Focus the IGN box on load
        Loaded += (_, _) => IgnBox.Focus();
    }

    // --- Properties bound to the form ---
    public List<string> Classes { get; }
    public List<string> Roles { get; }

    private string _ign = string.Empty;
    public string IGN { get => _ign; set { _ign = value; OnPropertyChanged(); } }

    private string _selectedClass = string.Empty;
    public string SelectedClass { get => _selectedClass; set { _selectedClass = value; OnPropertyChanged(); } }

    private string _selectedRole = string.Empty;
    public string SelectedRole { get => _selectedRole; set { _selectedRole = value; OnPropertyChanged(); } }

    private string _combatPower = "0";
    public string CombatPower { get => _combatPower; set { _combatPower = value; OnPropertyChanged(); } }

    private string _notes = string.Empty;
    public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

    /// <summary>
    /// Returns the parsed CP value (defaults to 0 if invalid).
    /// </summary>
    public int CombatPowerValue => int.TryParse(CombatPower, out var cp) ? cp : 0;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IGN))
        {
            MessageBox.Show("Please enter an IGN.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            IgnBox.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
