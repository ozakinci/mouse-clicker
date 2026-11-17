# AGENTS.md

Guidance for agents working in this repository.

## Project

A small C# auto-clicker for Windows (.NET 8, console app). F8 toggles clicking on and off, the interval is configurable, and it clicks at the current cursor position. One source file, no dependencies.

## Files

- `Program.cs` - the whole program: hotkey loop, timer, click call.
- `MouseClicker.csproj` - the .NET 8 project file, product metadata and version.
- `releases/`, `bin/`, `obj/` - build output. All three are gitignored and never committed.

## Commands

```powershell
dotnet build -c Release
dotnet run -- --interval-ms 250
```

## Rules

- F8 stays the only toggle, and the default interval stays 100 ms unless the user asks otherwise.
- Keep it one file. A single `Program.cs` is the point of the project; split only when the user asks for it.
- Change the version in `MouseClicker.csproj` when a user-visible behaviour changes.
- Never commit `bin/`, `obj/` or `releases/`.
- Verify a change by building and running it: press F8, confirm clicking starts and stops, and confirm the interval flag takes effect.

## Session startup

1. **Project memory** - this file is auto-loaded; use it directly.
2. **README freshness** - check that `README.md` matches the current features, flags and commands; update it when they drift.
3. **Clock in / clock out** - this project defines no extra start or end steps; the global blocks apply as-is.
