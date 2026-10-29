# rcmmodern

A modern, dark Windows app for sending RCM payloads to a Nintendo Switch.

## Features

- Automatic Switch detection in RCM mode (USB VID 0955 / PID 7321)
- Detects a missing libusbK driver and helps installing it
- Downloads TegraRcmSmash and the latest Hekate payload automatically
- Favorite payloads, add via dialog or drag & drop
- Settings menu: German / English, three color themes (blue, green, purple), auto-inject
- Log view at the bottom of the window

## Download

Get the latest version from the [Releases](https://github.com/Labtoso/rcmmodern/releases) page.
The zip contains the setup and `Install.bat`, which starts it (installs per user, no admin needed).

## Build

Requirements: .NET SDK (targets .NET Framework 4.8, preinstalled on Windows 10/11).

```bat
dotnet build TegraRcmSmashGUI.csproj -c Release -o dist\portable
```

`build.bat` additionally builds the setup with Inno Setup 6.

## Credits

- [TegraRcmSmash](https://github.com/rajkosto/TegraRcmSmash) by rajkosto
- [Hekate](https://github.com/CTCaer/hekate) by CTCaer
- [libwdi](https://github.com/pbatard/libwdi) for driver installation
