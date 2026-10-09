# City Dragons

Fly a dragon through a living, photoreal city: hunt the shard, land on rooftops, ram traffic, and scope your prey with
Hunter Vision. A hobby game by Andy Williams and Archon, built in Unreal Engine 5.8 on Epic's City Sample.

**Download:** see [Releases](../../releases/latest). Windows 64-bit only.

## Requirements
- Windows 10 or 11, 64-bit, DirectX 12
- GPU: NVIDIA RTX 3060 Ti class or better recommended (DLSS is used when available)
- RAM: 32 GB recommended (the city streams about 17 GB)
- Disk: about 21 GB free with the installer (40 GB for the manual route)

## Install (one click)
1. Download **`CityDragons-Setup.exe`** from the [latest release](../../releases/latest) and run it.
2. Press Enter to install to `C:\Games` (or type another folder). It downloads the game (18.4 GB), checks every piece
   against `SHA256SUMS.txt`, installs it, and puts a **City Dragons** shortcut on your desktop.

Windows SmartScreen may warn because the installer is unsigned: More info, then Run anyway. Its full source is in
[`installer/Setup.cs`](installer/Setup.cs) (built with the C# compiler that ships with Windows).
First launch compiles some shaders, so give it a minute.

### Manual install (no installer)
Download every `CityDragons-win64.tar.part*` file and `Join-and-Extract.bat` into one folder, double-click the .bat, then
run `CityDragons\Play City Dragons.bat`.

## Controls
| Key | Action |
|---|---|
| W / S | Throttle |
| A / D | Turn and bank |
| SPACE / C | Climb / descend |
| SHIFT | Burst speed |
| B | Brake and land |
| E | Dismount once landed |
| Q | Hunter Vision |
| RMB / LMB | Scope / fire a dart |
| ESC | Pause menu |

## Credits and licenses
- Built with Unreal Engine. Unreal and its logo are Epic Games' trademarks or registered trademarks in the US and
  elsewhere. City Sample content © Epic Games, used under the Unreal Engine EULA, and shipped only in this packaged
  build. Raw Epic content and engine source are not distributed.
- NVIDIA DLSS. See `NOTICES.txt` in the game folder for third-party notices.
- Free hobby release with no warranty. The game's source code is not published here.
