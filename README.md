# GuildTracker

A WPF desktop application for tracking guild members, attendance, and CP records. Includes Discord bot integration and Excel export functionality.

## Tech Stack

- .NET 8 (WPF)
- CommunityToolkit.Mvvm
- Discord.Net
- ClosedXML (Excel export)

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run
```

## Project Structure

- **Models/** – Data models (GuildMember, AttendanceRecord, CpRecord, GuildEvent)
- **ViewModels/** – MVVM view models
- **Views/** – XAML dialogs and UI components
- **Services/** – Data, export, and seed services
- **Discord/** – Discord bot integration
- **Helpers/** – Converters and utilities
