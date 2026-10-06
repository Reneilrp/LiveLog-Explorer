# 📁 LiveLog-Explorer

A modern, transparent Windows File Explorer with an integrated real-time Web Dashboard for developers testing code on external laptops.

[![Build and Release](https://github.com/Reneilrp/LiveLog-Explorer/actions/workflows/build.yml/badge.svg)](https://github.com/Reneilrp/LiveLog-Explorer/actions/workflows/build.yml)
[![Latest Release](https://img.shields.io/github/v/release/Reneilrp/LiveLog-Explorer?label=Latest%20Version)](https://github.com/Reneilrp/LiveLog-Explorer/releases/latest)

---

## ⚡ Quick Download & Install (Zero Setup Needed)

No cloning, compiling, or SDK installation is required! You can download the pre-compiled, standalone portable executable directly from GitHub:

👉 **[Download Latest LiveLog-Explorer.exe](https://github.com/Reneilrp/LiveLog-Explorer/releases/download/latest/LiveLog-Explorer.exe)**

1. Download `LiveLog-Explorer.exe`.
2. Double-click to run (no installer needed).
3. The Setup Wizard will guide you through your initial preferences.

---

## 🌟 Key Features

* **Windows 11 Fluent Interface:** Responsive navigation bar, dynamic address bar, search filtering, and Quick Access shortcuts.
* **Live Web Dashboard:** Built-in ultra-fast web server serving an auto-refreshing dark-mode dashboard on `http://localhost:9999`.
* **In-Place 1-Click Auto-Updates:** Detects newer releases on GitHub and updates itself in-place without generating duplicate files.
* **On-the-Fly Settings (`⚙️`):** Change logging format (`JSON` vs `Text`), web port, or default root folder at any time without restarting.
* **System Tray Persistence:** Minimizes to the Windows system tray when you close the window (`X`), keeping your live server active in the background. Cleanly releases all ports on exit.
* **100% Transparent & Safe:** Logs only the files opened intentionally through the explorer. No system-level hooks, no antivirus alarms.

---

## 🚀 How to Use

1. **Launch `LiveLog-Explorer.exe`** on the machine.
2. Choose your preferred port (default `9999`) and logging format (`JSON` or `Text`).
3. Click **🚀 Launch Explorer**.
4. Click **`🌐 Dashboard`** in the top navigation bar to view your live tracking dashboard in Google Chrome / Edge.
5. Double-click any file to log your activity and open it in its default application.

---

## 🛠 Automated CI/CD Pipeline
Every push to the `main` branch automatically triggers GitHub Actions to compile, compress, and publish the latest build to [GitHub Releases](https://github.com/Reneilrp/LiveLog-Explorer/releases).
