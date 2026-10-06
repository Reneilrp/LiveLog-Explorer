using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomExplorerApp
{
    public class MainForm : Form
    {
        private Panel navBarPanel;
        private Panel updateBanner;
        private Label updateBannerLabel;
        private Button updateBannerButton;
        private Button backButton;
        private Button upButton;
        private Button refreshButton;
        private TextBox pathTextBox;
        private TextBox searchBox;
        private Button dashboardButton;
        private Panel sidebarPanel;
        private ListView fileListView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusInfoLabel;
        private ToolStripStatusLabel statusCountLabel;
        private ImageList imageList;
        private ImageList sidebarImageList;
        private string currentDirectory = @"C:\Users";
        private static readonly string CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";
        private string latestDownloadUrl = "";
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isReallyClosing = false;

        public MainForm()
        {
            this.Text = "LiveLog Explorer (v" + CurrentVersion + ")";
            this.Size = new Size(980, 640);
            this.MinimumSize = new Size(780, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(249, 250, 252);

            InitializeIcons();
            InitializeSystemTray();
            BuildUi();

            string startFolder = @"C:\Users";
            if (!string.IsNullOrEmpty(AppSettings.DefaultFolder) && Directory.Exists(AppSettings.DefaultFolder))
            {
                startFolder = AppSettings.DefaultFolder;
            }
            LoadDirectory(startFolder);

            Task.Run(CheckForUpdatesAsync);
        }

        private void InitializeIcons()
        {
            imageList = new ImageList { ImageSize = new Size(18, 18), ColorDepth = ColorDepth.Depth32Bit };

            // Sleek Modern Windows 11 style folder icon
            Bitmap folderBmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(folderBmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush tabBrush = new SolidBrush(Color.FromArgb(240, 185, 55)))
                    g.FillRectangle(tabBrush, 1, 2, 7, 5);
                using (Brush folderBrush = new SolidBrush(Color.FromArgb(255, 210, 75)))
                    g.FillPath(folderBrush, GetRoundedRect(new RectangleF(0, 4, 18, 12), 2));
            }
            imageList.Images.Add("folder", folderBmp);

            // Clean Document File icon
            Bitmap fileBmp = new Bitmap(18, 18);
            using (Graphics g = Graphics.FromImage(fileBmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush bgBrush = new SolidBrush(Color.White))
                using (Pen borderPen = new Pen(Color.FromArgb(180, 190, 205), 1.2f))
                {
                    g.FillRectangle(bgBrush, 2, 1, 14, 16);
                    g.DrawRectangle(borderPen, 2, 1, 14, 16);
                }
                using (Brush lineBrush = new SolidBrush(Color.FromArgb(195, 205, 220)))
                {
                    g.FillRectangle(lineBrush, 5, 5, 8, 2);
                    g.FillRectangle(lineBrush, 5, 9, 8, 2);
                    g.FillRectangle(lineBrush, 5, 13, 5, 2);
                }
            }
            imageList.Images.Add("file", fileBmp);

            // Sidebar shortcut icons
            sidebarImageList = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            sidebarImageList.Images.Add("folder", folderBmp);
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(RectangleF r, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - 2 * radius, r.Y, 2 * radius, 2 * radius, 270, 90);
            path.AddArc(r.Right - 2 * radius, r.Bottom - 2 * radius, 2 * radius, 2 * radius, 0, 90);
            path.AddArc(r.X, r.Bottom - 2 * radius, 2 * radius, 2 * radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void BuildUi()
        {
            // 1. Update Notification Banner
            updateBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(230, 248, 238),
                Padding = new Padding(14, 4, 14, 4),
                Visible = false
            };
            updateBannerLabel = new Label
            {
                Text = "✨ A newer version is available on GitHub!",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 115, 65),
                AutoSize = true,
                Location = new Point(14, 8)
            };
            updateBanner.Controls.Add(updateBannerLabel);

            updateBannerButton = CreateStyledButton("📥 Update Now", Color.FromArgb(15, 140, 85), Color.White);
            updateBannerButton.Location = new Point(360, 4);
            updateBannerButton.Size = new Size(125, 28);
            updateBannerButton.Click += (s, e) => DownloadAndApplyUpdate();
            updateBanner.Controls.Add(updateBannerButton);

            Button dismissBtn = new Button
            {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(26, 26),
                Location = new Point(495, 4),
                ForeColor = Color.FromArgb(120, 130, 140)
            };
            dismissBtn.FlatAppearance.BorderSize = 0;
            dismissBtn.Click += (s, e) => updateBanner.Visible = false;
            updateBanner.Controls.Add(dismissBtn);

            // 2. Windows 11 Fluent Navigation Bar (TableLayoutPanel for responsive grid)
            navBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            TableLayoutPanel navTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                BackColor = Color.White
            };
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); // Back
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); // Up
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); // Refresh
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));   // Address Bar
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70)); // Browse
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));   // Search
            navTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));// Dashboard Button

            backButton = CreateIconButton("←", "Previous Directory", (s, e) => UpButton_Click(s, e));
            upButton = CreateIconButton("↑", "Up to Parent", (s, e) => UpButton_Click(s, e));
            refreshButton = CreateIconButton("🔄", "Refresh", (s, e) => LoadDirectory(currentDirectory));

            // Address Box with clean border
            pathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 252),
                ForeColor = Color.FromArgb(30, 35, 45)
            };
            pathTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && Directory.Exists(pathTextBox.Text))
                {
                    LoadDirectory(pathTextBox.Text);
                    e.SuppressKeyPress = true;
                }
            };

            Button browseBtn = CreateStyledButton("📁 Browse", Color.FromArgb(240, 242, 247), Color.FromArgb(50, 55, 65));
            browseBtn.Dock = DockStyle.Fill;
            browseBtn.Click += BrowseButton_Click;

            // Integrated Search Box with placeholder text
            searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 252),
                ForeColor = Color.FromArgb(90, 95, 105)
            };
            searchBox.Text = "🔍 Filter files...";
            searchBox.Enter += (s, e) => { if (searchBox.Text == "🔍 Filter files...") searchBox.Text = ""; };
            searchBox.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(searchBox.Text)) searchBox.Text = "🔍 Filter files..."; };
            searchBox.TextChanged += (s, e) =>
            {
                if (searchBox.Text != "🔍 Filter files...") FilterItems(searchBox.Text);
            };

            dashboardButton = CreateStyledButton("🌐 Dashboard", Color.FromArgb(235, 248, 242), Color.FromArgb(15, 135, 75));
            dashboardButton.Dock = DockStyle.Fill;
            dashboardButton.Click += (s, e) =>
            {
                if (AppSettings.EnableWebUI)
                {
                    try { Process.Start(new ProcessStartInfo { FileName = $"http://localhost:{AppSettings.WebPort}", UseShellExecute = true }); } catch { }
                }
                else
                {
                    MessageBox.Show("Web Dashboard was disabled in the startup wizard.", "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            navTable.Controls.Add(backButton, 0, 0);
            navTable.Controls.Add(upButton, 1, 0);
            navTable.Controls.Add(refreshButton, 2, 0);
            navTable.Controls.Add(pathTextBox, 3, 0);
            navTable.Controls.Add(browseBtn, 4, 0);
            navTable.Controls.Add(searchBox, 5, 0);
            navTable.Controls.Add(dashboardButton, 6, 0);
            navBarPanel.Controls.Add(navTable);

            // 3. Status Bar at Bottom
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(245, 246, 250),
                Font = new Font("Segoe UI", 9f),
                SizingGrip = true
            };
            statusInfoLabel = new ToolStripStatusLabel
            {
                Text = "🟢 Live Tracker Active - Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(60, 70, 85)
            };
            statusCountLabel = new ToolStripStatusLabel
            {
                Text = "0 folders, 0 files",
                ForeColor = Color.FromArgb(110, 120, 135)
            };
            statusStrip.Items.Add(statusInfoLabel);
            statusStrip.Items.Add(statusCountLabel);

            // 4. Quick Access Shortcuts Sidebar (Left)
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 190,
                BackColor = Color.FromArgb(248, 249, 252),
                Padding = new Padding(8, 12, 8, 12)
            };

            Label quickAccessLabel = new Label
            {
                Text = "QUICK ACCESS",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 145, 160),
                Location = new Point(12, 10),
                AutoSize = true
            };
            sidebarPanel.Controls.Add(quickAccessLabel);

            int sidebarY = 32;
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            AddSidebarItem("🏠 User Home", userHome, ref sidebarY);
            AddSidebarItem("🖥 Desktop", desktopPath, ref sidebarY);
            AddSidebarItem("📄 Documents", docsPath, ref sidebarY);
            AddSidebarItem("💾 C:\\ Drive", @"C:\", ref sidebarY);

            if (!string.IsNullOrEmpty(AppSettings.DefaultFolder) && Directory.Exists(AppSettings.DefaultFolder))
            {
                AddSidebarItem("⭐ Project Root", AppSettings.DefaultFolder, ref sidebarY);
            }

            // Divider between sidebar and file grid
            Panel divider = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = Color.FromArgb(230, 233, 240)
            };

            // 5. Main ListView with Modern Windows 11 Styling
            fileListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                SmallImageList = imageList,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 35, 45)
            };
            fileListView.Columns.Add("Name", 320);
            fileListView.Columns.Add("Date modified", 150);
            fileListView.Columns.Add("Type", 120);
            fileListView.Columns.Add("Size", 90);

            fileListView.DoubleClick += FileListView_DoubleClick;
            fileListView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && fileListView.SelectedItems.Count > 0)
                {
                    FileListView_DoubleClick(s, e);
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    UpButton_Click(s, e);
                    e.SuppressKeyPress = true;
                }
            };

            // Add Controls
            this.Controls.Add(fileListView);
            this.Controls.Add(divider);
            this.Controls.Add(sidebarPanel);
            this.Controls.Add(statusStrip);
            this.Controls.Add(navBarPanel);
            this.Controls.Add(updateBanner);
        }

        private void AddSidebarItem(string label, string path, ref int y)
        {
            Button btn = new Button
            {
                Text = "  " + label,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(50, 55, 70),
                Location = new Point(4, y),
                Size = new Size(180, 30),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 238, 246);
            btn.Click += (s, e) => { if (Directory.Exists(path)) LoadDirectory(path); };
            sidebarPanel.Controls.Add(btn);
            y += 34;
        }

        private Button CreateIconButton(string icon, string tooltip, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = icon,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(245, 246, 250),
                ForeColor = Color.FromArgb(60, 65, 80),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 234, 244);
            btn.Click += onClick;
            return btn;
        }

        private Button CreateStyledButton(string text, Color bg, Color fg)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("LiveLog-Explorer");
                    string url = "https://api.github.com/repos/Reneilrp/LiveLog-Explorer/releases/tags/latest";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return;

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
                        {
                            var asset = assets[0];
                            if (asset.TryGetProperty("browser_download_url", out var downloadUrlProp))
                            {
                                latestDownloadUrl = downloadUrlProp.GetString() ?? "";
                            }

                            string remoteVersionStr = "";
                            if (root.TryGetProperty("name", out var nameProp))
                            {
                                string title = nameProp.GetString() ?? "";
                                var match = System.Text.RegularExpressions.Regex.Match(title, @"\b(\d+\.\d+\.\d+)\b");
                                if (match.Success) remoteVersionStr = match.Groups[1].Value;
                            }

                            if (string.IsNullOrEmpty(remoteVersionStr) && root.TryGetProperty("body", out var bProp))
                            {
                                string body = bProp.GetString() ?? "";
                                var match = System.Text.RegularExpressions.Regex.Match(body, @"version:\s*(\d+\.\d+\.\d+)");
                                if (match.Success) remoteVersionStr = match.Groups[1].Value;
                            }

                            if (Version.TryParse(remoteVersionStr, out Version remoteVer) &&
                                Version.TryParse(CurrentVersion, out Version localVer))
                            {
                                if (remoteVer > localVer)
                                {
                                    this.Invoke((Action)(() =>
                                    {
                                        updateBannerLabel.Text = $"✨ New version v{remoteVer} is available on GitHub!";
                                        updateBanner.Visible = true;
                                    }));
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void DownloadAndApplyUpdate()
        {
            if (string.IsNullOrEmpty(latestDownloadUrl))
            {
                latestDownloadUrl = "https://github.com/Reneilrp/LiveLog-Explorer/releases/latest";
            }

            var result = MessageBox.Show(
                "Would you like to open the latest release download page in your browser?\n\nClick 'Yes' to download, or 'No' to cancel.",
                "Update LiveLog Explorer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = latestDownloadUrl, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open browser: " + ex.Message);
                }
            }
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo parent = Directory.GetParent(currentDirectory);
                if (parent != null)
                {
                    LoadDirectory(parent.FullName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot navigate up: " + ex.Message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = currentDirectory;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    LoadDirectory(fbd.SelectedPath);
                }
            }
        }

        private void LoadDirectory(string path)
        {
            try
            {
                currentDirectory = path;
                pathTextBox.Text = path;
                if (searchBox.Text != "🔍 Filter files...") searchBox.Text = "🔍 Filter files...";
                fileListView.Items.Clear();

                DirectoryInfo dir = new DirectoryInfo(path);
                int folderCount = 0;
                int fileCount = 0;

                // Load Folders
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    if ((subDir.Attributes & FileAttributes.Hidden) != 0 && subDir.Name.StartsWith("$")) continue;

                    var item = new ListViewItem(subDir.Name, "folder");
                    item.Tag = subDir.FullName;
                    item.SubItems.Add(subDir.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add("File folder");
                    item.SubItems.Add("");
                    fileListView.Items.Add(item);
                    folderCount++;
                }

                // Load Files
                foreach (FileInfo file in dir.GetFiles())
                {
                    if ((file.Attributes & FileAttributes.Hidden) != 0 && file.Name.StartsWith("~$")) continue;

                    var item = new ListViewItem(file.Name, "file");
                    item.Tag = file.FullName;
                    item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(file.Extension.ToUpper() + " File");
                    item.SubItems.Add(FormatFileSize(file.Length));
                    fileListView.Items.Add(item);
                    fileCount++;
                }

                statusCountLabel.Text = $"{folderCount} folders, {fileCount} files";
                statusInfoLabel.Text = $"📁 {path}";
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access Denied to this directory.", "Permission", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpButton_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterItems(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter) || filter == "🔍 Filter files...")
            {
                LoadDirectory(currentDirectory);
                return;
            }

            filter = filter.ToLower();
            for (int i = fileListView.Items.Count - 1; i >= 0; i--)
            {
                var item = fileListView.Items[i];
                if (!item.Text.ToLower().Contains(filter))
                {
                    fileListView.Items.RemoveAt(i);
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private void FileListView_DoubleClick(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0) return;

            ListViewItem item = fileListView.SelectedItems[0];
            string fullPath = item.Tag as string;
            if (string.IsNullOrEmpty(fullPath)) return;

            if (item.ImageKey == "folder")
            {
                LoadDirectory(fullPath);
            }
            else
            {
                OpenFileAndLog(fullPath);
            }
        }

        private void OpenFileAndLog(string filePath)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logMessage;

                if (AppSettings.LogFormat == "JSON")
                {
                    string jsonPath = filePath.Replace("\\", "\\\\");
                    logMessage = $"{{\"time\": \"{timestamp}\", \"action\": \"OPENED\", \"file\": \"{jsonPath}\"}}{Environment.NewLine}";
                }
                else
                {
                    logMessage = $"[{timestamp}] OPENED: {filePath}{Environment.NewLine}";
                }

                File.AppendAllText(AppSettings.LogFilePath, logMessage);

                statusInfoLabel.Text = $"📝 Logged: {Path.GetFileName(filePath)} at {timestamp}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening file: " + ex.Message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeSystemTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("📂 Open Explorer", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add("🌐 Open Dashboard", null, (s, e) => {
                if (AppSettings.EnableWebUI)
                    Process.Start(new ProcessStartInfo { FileName = $"http://localhost:{AppSettings.WebPort}", UseShellExecute = true });
            });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("❌ Exit Completely", null, (s, e) => ExitApplication());

            trayIcon = new NotifyIcon();
            trayIcon.Text = "LiveLog Explorer (Running)";
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isReallyClosing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, "LiveLog Explorer", "Minimized to tray. Live web server & tracking are still active in background.", ToolTipIcon.Info);
            }
            else
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
                base.OnFormClosing(e);
            }
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void ExitApplication()
        {
            isReallyClosing = true;
            Program.StopWebServer();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            Application.Exit();
        }
    }
}
