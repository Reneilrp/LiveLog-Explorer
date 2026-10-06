# LiveLog-Explorer

A custom, lightweight Windows File Explorer built with C# (.NET 8) that provides real-time activity tracking and transparency for developers testing code on external machines.

## Features
* **Custom Desktop UI:** A simple, clean file explorer interface for navigating directories and opening files.
* **Live Web Dashboard:** An embedded ASP.NET Core web server that serves a beautifully styled, auto-refreshing dashboard to view activity logs in real-time.
* **Configuration Wizard:** A startup UI to easily configure Web UI ports, JSON/Text log formatting, and default project directories.
* **100% Transparent:** Only tracks files manually opened through the Custom Explorer UI, ensuring privacy and avoiding system-level spyware behavior.

## How to Build
This application requires the .NET 8 SDK and must be compiled on a Windows machine.

1. Clone the repository.
2. Open a terminal in the project folder.
3. Run `dotnet run` to test it locally.
4. Run the following command to compile it into a single `.exe` file:
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Usage
1. Launch `LiveLog-Explorer.exe`.
2. Configure your desired port and format in the Wizard.
3. Browse to your project folder and double-click files to open them.
4. Navigate to `http://localhost:[YOUR_PORT]` in your web browser to view the live dashboard!
