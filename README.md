# VRCLearn Launcher

[![Build](https://github.com/VRLearn-Official/VRCLearn_Launcher/actions/workflows/build.yml/badge.svg)](https://github.com/VRLearn-Official/VRCLearn_Launcher/actions/workflows/build.yml)

**English** | [简体中文](README.zh-CN.md)

A VRChat package and project manager for Windows, built with .NET 8 and WPF.

VRCLearn Launcher manages VPM repositories, packages and Unity projects in one place. It is an independent implementation, not affiliated with VRChat Inc. or Unity Technologies.

## Features

- **Repositories** — add VPM repositories, search packages, install and upgrade with dependency resolution
- **Projects** — list, create, favorite and archive projects; launch Unity with a selected editor version; back up and restore
- **Resources** — manage local unitypackages with covers, tags, search and manuals
- **Launcher backup** — back up configuration only or everything, restore into a chosen directory
- **Templates** — import and create `.vrclearn-template` projects
- **Tools** — environment check, Unity Hub / Editor downloads, optional Unity Hub auto-update opt-out
- **Desktop integration** — `vcc://` deep links and file associations
- **Self-update** — checks `version.txt` at startup and links to the download page
- **Localization** — Simplified Chinese and English

## Project layout

| Path | Contents |
|---|---|
| `src/VRCLearn.App` | WPF application: views, view models, localization, themes |
| `src/VRCLearn.Core` | Platform-independent logic, BCL only |
| `src/VRCLearn.Infrastructure` | Windows APIs, file system, HTTP, storage |

Dependency direction: `App` → `Infrastructure` → `Core`.

## Build

Requirements:

- Windows 10 or later, x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Restore and build the application project:

```
dotnet restore src/VRCLearn.App/VRCLearn.App.csproj
dotnet build src/VRCLearn.App/VRCLearn.App.csproj -c Release
```

Run from source:

```
dotnet run --project src/VRCLearn.App/VRCLearn.App.csproj
```

The build targets `net8.0-windows` and `win-x64`. Published builds are self-contained: the .NET 8 runtime is bundled, so the installed application does not need a separate runtime.

GitHub Actions builds the installer on every push to `main` and on `v*` tags. The Setup executable is attached to each workflow run as an artifact. Pushing a `v*` tag also creates a draft GitHub Release with the Setup executable and a SHA256 checksum.

## Data directory

By default the launcher shares `%LocalAppData%\VRChatCreatorCompanion` with VRChat Creator Companion. Launcher-specific data lives in the `vrclearn\` folder under it.

## License

[MIT](LICENSE)
