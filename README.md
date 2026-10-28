# 📷 LocalSendRenamer

Renames photos and videos to their capture date and time in the Persian (Jalali) calendar, so a folder of `IMG_20240512_143045.jpg` becomes `1403-02-23_14-30-45.jpg`.

It was written to clean up files received over LocalSend, whose names stop being useful in transit. Nothing in the code is specific to LocalSend — it works on any folder of media files.

## 🎯 What it does

- Scans a directory and all its subdirectories.
- Reads each file's capture date from metadata, then the filename, then the file system.
- Renames files in place within their own folder.
- Skips files that already look renamed and adds `-1`, `-2`, … when a target name is taken.
- Saves a manifest of the run so it can be undone.

## 📦 Requirements

- .NET 10 SDK (the projects target `net10.0`).

## 🛠️ Build

```powershell
dotnet build LocalSendRenamer.slnx
```

Build output goes to `artifacts/<Configuration>/`.

## 🚀 Usage

| Command | Description |
| --- | --- |
| `LocalSendRenamer` | Rename files in the current directory |
| `LocalSendRenamer rename [directory]` | Rename files in `directory` (defaults to the current one) |
| `LocalSendRenamer rename [directory] --dry-run` | List what would be renamed, change nothing |
| `LocalSendRenamer undo` | Revert the last run recorded in the current directory |
| `LocalSendRenamer install` | Add the executable's folder to the user `PATH` |
| `LocalSendRenamer uninstall` | Remove that folder from the user `PATH` |

```powershell
dotnet run --project LocalSendRenamer -- rename "D:\Photos" --dry-run
dotnet run --project LocalSendRenamer -- rename "D:\Photos"
```

Sample output:

```text
OK      IMG_20240512_143045.jpg -> 1403-02-23_14-30-45.jpg
SKIP    1403-01-05_09-12-00.png
Total  : 12
Skipped: 1
Renamed: 11
```

There is no confirmation prompt; use `--dry-run` first.

## 🗓️ File names

Files are named `<date>_<time>`, where the date is Jalali and the time is the local 24-hour clock time of the capture:

```text
1403-02-23_14-30-45.jpg
```

Files sharing the same capture second in the same folder get a counter: `1403-02-23_14-30-45-1.jpg`, `-2`, …

A name is considered already handled when it matches `YYYY-MM-DD`, optionally followed by `_HH-mm-ss`, optionally followed by `-N`.

## 🔍 Date sources

Checked in this order:

| Priority | Source |
| --- | --- |
| 1 | EXIF `DateTimeOriginal`, then EXIF IFD0 `DateTime` |
| 2 | QuickTime movie header creation time (converted from UTC to local) |
| 3 | Filename: `IMG_YYYYMMDD_HHMMSS…` / `VID_YYYYMMDD_HHMMSS…` |
| 4 | Filename: `Screenshot-…` / `Screenrecorder-…` with `yyyy-MM-dd-HH-mm-ss-fff` |
| 5 | The earlier of the file's creation time and last write time (with a warning) |

A date outside 2000–2030 is treated as implausible. If any file resolves to one, the whole run aborts and nothing is renamed.

## 🧩 Supported extensions

`.jpg` `.jpeg` `.jfif` `.png` `.bmp` `.gif` `.webp` `.heic` `.mp4`

Anything else in the scanned folders is left alone.

## ↩️ Undo

A successful run writes `.rename_manifest.json` into the directory it processed. `undo` reads that file from the current directory and moves the files back, skipping any entry whose target already exists or whose renamed file is gone. The manifest is deleted once every entry has been reverted.

The manifest holds only the latest run, so `undo` covers that run and not the ones before it.

## 🧪 Tests

```powershell
dotnet test
```

## ⚠️ Notes

- Windows-focused: `install`/`uninstall` edit the Windows user `PATH`, and the app waits for Enter before exiting.
- Files are only renamed, never moved between folders or converted.
- Existing files are never overwritten; a counter is appended instead.
