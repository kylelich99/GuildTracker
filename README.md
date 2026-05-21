# GuildTracker

A WPF desktop application for tracking guild members, attendance, and CP records. Includes Discord bot integration, Excel export, and shared MongoDB cloud database for multi-user access.

## Tech Stack

- .NET 8 (WPF)
- CommunityToolkit.Mvvm
- MongoDB Atlas (shared cloud database)
- Discord.Net
- ClosedXML (Excel export)

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run
```

## Configuration

Connection settings are in `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://<user>:<password>@<cluster>.mongodb.net/GuildTrackerDb",
    "DatabaseName": "GuildTrackerDb"
  }
}
```

## Features

- Add/edit/remove guild members
- Track attendance per event with weekly scheduling
- Mark MVP, God of War, Best Support awards
- Combat Power tracking with history and trends
- Shared database — vice leaders see the same data in real-time
- Refresh button to pull latest changes
- Export to Excel
- Discord bot integration (optional)

## Project Structure

- **Models/** – Data models (GuildMember, AttendanceRecord, CpRecord, GuildEvent)
- **ViewModels/** – MVVM view models
- **Views/** – XAML dialogs and UI components
- **Services/** – MongoDB data service, export, and seed services
- **Discord/** – Discord bot integration
- **Helpers/** – Converters and utilities

## Distribution

To share with vice leaders, publish a single `.exe`:

```bash
dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true
```

Send the output `.exe` + `appsettings.json` to your vice leaders.
