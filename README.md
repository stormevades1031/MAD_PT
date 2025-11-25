# MAD_PT

A .NET MAUI 9.0 mobile app implementing a Van Gogh artist detail screen:
- Rounded profile photo with gold border
- “Read More” expander with tail-truncated intro
- Equal-width statistics grid using star sizing
- Gallery using CollectionView with 2-column grid

## Prerequisites
- .NET SDK 9
- .NET MAUI workload installed
- Visual Studio 2022 (latest) with .NET MAUI workload

## Setup
```powershell
dotnet workload install maui
dotnet restore
dotnet build MAD_PT/MAD_PT.csproj -f net9.0-android -c Debug
```

## Run
Use Visual Studio to deploy to an Android emulator/device, or:
```powershell
dotnet build MAD_PT/MAD_PT.csproj -f net9.0-android -c Debug
```

## Tech
- .NET MAUI 9.0
- CommunityToolkit.Maui (Expander)