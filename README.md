# GuildTracker

A WPF desktop application for tracking guild members, attendance, CP records, and auction distribution. Shared MongoDB cloud database for multi-user access.

## Tech Stack

- .NET 8 (WPF)
- CommunityToolkit.Mvvm
- MongoDB Atlas (shared cloud database)
- Discord.Net
- ClosedXML (Excel export)
- LiveChartsCore.SkiaSharpView.WPF (CP charts)

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

- Add/edit/remove guild members (soft-delete — history is preserved)
- Track attendance per event with weekly scheduling
- Attendance % column — quick health check per member
- Mark MVP, God of War, Best Support awards
- Combat Power tracking with line chart history
- Color-coded class labels throughout the UI
- Auction distribution with randomizer (configurable item types, max per player)
- Cycle system — reset auction without losing history, old data preserved by cycle ID
- Priority system — mark players as priority for auction (sorted by CP)
- Missed players view — see who didn't receive items this cycle
- Shared database — vice leaders see the same data in real-time
- Refresh button to pull latest changes
- Export to Excel (members, attendance, auction)
- Discord bot integration (see below)

## Project Structure

- **Models/** – Data models (GuildMember, AttendanceRecord, CpRecord, GuildEvent, AuctionModels)
- **ViewModels/** – MVVM view models
- **Views/** – XAML dialogs and UI components
- **Services/** – MongoDB data service and export service
- **Helpers/** – Converters and utilities
- **GuildTrackerBot/** – Standalone Discord bot (separate .NET 8 console project)

## Distribution

To share with vice leaders, publish a single `.exe`:

```bash
dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true -c Release
```

Send the output `.exe` + `appsettings.json` (with real connection string) to your vice leaders.

---

## Discord Bot

Located in `GuildTrackerBot/GuildTrackerBot/`. Standalone .NET 8 console app deployed on Railway.

### Bot Commands

| Command | Description | Visible |
|---|---|---|
| `/register` | Register with IGN, CP, Class, Notes | Private |
| `/updatecp` | Update your combat power | Private |
| `/updateign` | Update your in-game name | Private |
| `/updateclass` | Update your class | Private |
| `/updatenotes` | Update your notes | Private |
| `/myinfo` | Full profile — CP, awards, attendance, history | Private |
| `/leaderboard` | Top 10 members by CP | **Public** |
| `/cphistory` | Your CP progression over time | Private |

### Bot Environment Variables (Railway)

| Variable | Value |
|---|---|
| `DISCORD_TOKEN` | Your bot token from Discord Developer Portal |
| `MONGODB_CONNECTION_STRING` | Your MongoDB connection string |
| `MONGODB_DATABASE` | `GuildTrackerDb` |

### Deploy to Railway

1. Push this repo to GitHub
2. Go to [railway.app](https://railway.app) → New Project → Deploy from GitHub
3. Select the repo, set **Root Directory** to `GuildTrackerBot/GuildTrackerBot`
4. Add the three environment variables above
5. Railway auto-detects the Dockerfile and deploys

### Local Bot Run

```bash
cd GuildTrackerBot/GuildTrackerBot
set DISCORD_TOKEN=your_token
set MONGODB_CONNECTION_STRING=your_conn_string
set MONGODB_DATABASE=GuildTrackerDb
dotnet run
```
