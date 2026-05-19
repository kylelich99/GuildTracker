using System.IO;
using System.Text.Json;
using GuildTracker.Models;

namespace GuildTracker.Services;

/// <summary>
/// Handles all JSON file read/write operations with auto-backup.
/// Data is stored in a "Data" folder next to the executable.
/// </summary>
public class JsonDataService
{
    private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string BackupFolder = Path.Combine(DataFolder, "Backups");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonDataService()
    {
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(BackupFolder);
    }

    private string MembersPath => Path.Combine(DataFolder, "members.json");
    private string AttendancePath => Path.Combine(DataFolder, "attendance.json");
    private string CpHistoryPath => Path.Combine(DataFolder, "cp_history.json");

    // --- Members ---
    public async Task<List<GuildMember>> LoadMembersAsync()
    {
        return await LoadAsync<List<GuildMember>>(MembersPath) ?? new List<GuildMember>();
    }

    public async Task SaveMembersAsync(List<GuildMember> members)
    {
        await SaveAsync(MembersPath, members);
    }

    // --- Attendance ---
    public async Task<List<AttendanceRecord>> LoadAttendanceAsync()
    {
        return await LoadAsync<List<AttendanceRecord>>(AttendancePath) ?? new List<AttendanceRecord>();
    }

    public async Task SaveAttendanceAsync(List<AttendanceRecord> records)
    {
        await SaveAsync(AttendancePath, records);
    }

    // --- CP History ---
    public async Task<List<CpRecord>> LoadCpHistoryAsync()
    {
        return await LoadAsync<List<CpRecord>>(CpHistoryPath) ?? new List<CpRecord>();
    }

    public async Task SaveCpHistoryAsync(List<CpRecord> records)
    {
        await SaveAsync(CpHistoryPath, records);
    }

    // --- Generic Load/Save ---
    private async Task<T?> LoadAsync<T>(string path)
    {
        if (!File.Exists(path)) return default;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private async Task SaveAsync<T>(string path, T data)
    {
        // Auto-backup before saving
        if (File.Exists(path))
        {
            var backupName = $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            File.Copy(path, Path.Combine(BackupFolder, backupName), true);
            CleanOldBackups();
        }

        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    /// <summary>
    /// Keeps only the last 10 backups per file type.
    /// </summary>
    private void CleanOldBackups()
    {
        var files = Directory.GetFiles(BackupFolder, "*.json")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(30)
            .ToList();

        foreach (var file in files)
            File.Delete(file);
    }
}
