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
        private Panel topPanel;
        private Panel searchPanel;
        private Panel statusPanel;
        private Panel updateBanner;
        private Label updateBannerLabel;
        private Button updateBannerButton;
        private TextBox pathTextBox;
        private TextBox searchBox;
        private Button upButton;
        private Button refreshButton;
        private Button browseButton;
        private Button webUiButton;
        private ListView fileListView;
        private ImageList imageList;
        private Label statusLabel;
        private Label countLabel;
        private string currentDirectory = @"C:\Users";
        private const string CurrentVersion = "1.0.1";
        private string latestDownloadUrl = "";
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isReallyClosing = false;

        public MainForm()
        {
            this.Text = "LiveLog Explorer (v" + CurrentVersion + ")";
            this.Size = new Size(880, 620);
            this.MinimumSize = new Size(640, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 246, 250);

            InitializeImageList();
            InitializeSystemTray();
            BuildUi();

            // Set starting directory from settings or default
            string startFolder = @"C:\Users";
            if (!string.IsNullOrEmpty(AppSettings.DefaultFolder) && Directory.Exists(AppSettings.DefaultFolder))
            {
                startFolder = AppSettings.DefaultFolder;
            }
            LoadDirectory(startFolder);

            // Check for updates asynchronously in background on startup
            Task.Run(CheckForUpdatesAsync);
        }

        private void InitializeImageList()
        {
            imageList = new ImageList();
            imageList.ImageSize = new Size(20, 20);
            imageList.ColorDepth = ColorDepth.Depth32Bit;

            // Draw clean vector-like folder icon
            Bitmap folderBmp = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(folderBmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush tabBrush = new SolidBrush(Color.FromArgb(235, 175, 40)))
                    g.FillRectangle(tabBrush, 2, 3, 7, 5);
                using (Brush folderBrush = new SolidBrush(Color.FromArgb(250, 200, 60)))
                    g.FillPath(folderBrush, GetRoundedRect(new RectangleF(1, 5, 18, 12), 2));
            }
            imageList.Images.Add("folder", folderBmp);

            // Draw clean file icon
            Bitmap fileBmp = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(fileBmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush pageBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                using (Pen borderPen = new Pen(Color.FromArgb(160, 175, 200), 1.5f))
                {
                    g.FillRectangle(pageBrush, 3, 2, 13, 16);
                    g.DrawRectangle(borderPen, 3, 2, 13, 16);
                }
                using (Brush lineBrush = new SolidBrush(Color.FromArgb(180, 190, 210)))
                {
                    g.FillRectangle(lineBrush, 6, 6, 7, 2);
                    g.FillRectangle(lineBrush, 6, 10, 7, 2);
                    g.FillRectangle(lineBrush, 6, 14, 5, 2);
                }
            }
            imageList.Images.Add("file", fileBmp);
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
            // Update Notification Banner (Hidden by default)
            updateBanner = new Panel();
            updateBanner.Dock = DockStyle.Top;
            updateBanner.Height = 36;
            updateBanner.BackColor = Color.FromArgb(220, 245, 235);
            updateBanner.Padding = new Padding(14, 6, 14, 6);
            updateBanner.Visible = false;

            updateBannerLabel = new Label();
            updateBannerLabel.Text = "🎉 A new update is available on GitHub!";
            updateBannerLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            updateBannerLabel.ForeColor = Color.FromArgb(10, 100, 60);
            updateBannerLabel.AutoSize = true;
            updateBannerLabel.Location = new Point(14, 8);
            updateBanner.Controls.Add(updateBannerLabel);

            updateBannerButton = CreateStyledButton("📥 Update Now", Color.FromArgb(15, 140, 85), Color.White);
            updateBannerButton.Location = new Point(350, 4);
            updateBannerButton.Size = new Size(125, 28);
            updateBannerButton.Click += (s, e) => DownloadAndApplyUpdate();
            updateBanner.Controls.Add(updateBannerButton);

            Button dismissBtn = new Button();
            dismissBtn.Text = "✕";
            dismissBtn.FlatStyle = FlatStyle.Flat;
            dismissBtn.FlatAppearance.BorderSize = 0;
            dismissBtn.Size = new Size(26, 26);
            dismissBtn.Location = new Point(485, 4);
            dismissBtn.ForeColor = Color.FromArgb(120, 130, 140);
            dismissBtn.Click += (s, e) => updateBanner.Visible = false;
            updateBanner.Controls.Add(dismissBtn);

            // Top Navigation Bar
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 52;
            topPanel.Padding = new Padding(12, 10, 12, 6);
            topPanel.BackColor = Color.FromArgb(255, 255, 255);

            upButton = CreateStyledButton("⬆ Back", Color.FromArgb(235, 240, 248), Color.FromArgb(40, 90, 170));
            upButton.Location = new Point(12, 10);
            upButton.Size = new Size(68, 32);
            upButton.Click += UpButton_Click;
            topPanel.Controls.Add(upButton);

            refreshButton = CreateStyledButton("🔄", Color.FromArgb(240, 242, 246), Color.FromArgb(60, 65, 75));
            refreshButton.Location = new Point(86, 10);
            refreshButton.Size = new Size(36, 32);
            refreshButton.Click += (s, e) => LoadDirectory(currentDirectory);
            topPanel.Controls.Add(refreshButton);

            pathTextBox = new TextBox();
            pathTextBox.Location = new Point(128, 12);
            pathTextBox.Size = new Size(500, 26);
            pathTextBox.Font = new Font("Segoe UI", 10f);
            pathTextBox.BorderStyle = BorderStyle.FixedSingle;
            pathTextBox.BackColor = Color.FromArgb(250, 251, 254);
            pathTextBox.ForeColor = Color.FromArgb(30, 35, 45);
            pathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pathTextBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter && Directory.Exists(pathTextBox.Text))
                {
                    LoadDirectory(pathTextBox.Text);
                    e.SuppressKeyPress = true;
                }
            };
            topPanel.Controls.Add(pathTextBox);

            browseButton = CreateStyledButton("📁 Browse", Color.FromArgb(240, 242, 246), Color.FromArgb(50, 55, 65));
            browseButton.Location = new Point(636, 10);
            browseButton.Size = new Size(84, 32);
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseButton.Click += BrowseButton_Click;
            topPanel.Controls.Add(browseButton);

            webUiButton = CreateStyledButton("🌐 Dashboard", Color.FromArgb(235, 248, 242), Color.FromArgb(20, 140, 80));
            webUiButton.Location = new Point(726, 10);
            webUiButton.Size = new Size(102, 32);
            webUiButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            webUiButton.Click += (s, e) => {
                if (AppSettings.EnableWebUI)
                {
                    try {
                        Process.Start(new ProcessStartInfo { FileName = $"http://localhost:{AppSettings.WebPort}", UseShellExecute = true });
                    } catch { }
                }
                else
                {
                    MessageBox.Show("Web Dashboard was disabled in the startup wizard.", "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            topPanel.Controls.Add(webUiButton);

            // Search / Filter Bar
            searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 36;
            searchPanel.Padding = new Padding(12, 4, 12, 6);
            searchPanel.BackColor = Color.FromArgb(255, 255, 255);

            Label searchLabel = new Label();
            searchLabel.Text = "Filter:";
            searchLabel.Location = new Point(14, 8);
            searchLabel.AutoSize = true;
            searchLabel.ForeColor = Color.FromArgb(110, 115, 125);
            searchPanel.Controls.Add(searchLabel);

            searchBox = new TextBox();
            searchBox.Location = new Point(65, 5);
            searchBox.Size = new Size(240, 24);
            searchBox.Font = new Font("Segoe UI", 9f);
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.TextChanged += (s, e) => FilterItems(searchBox.Text);
            searchPanel.Controls.Add(searchBox);

            // Status Bar at Bottom
            statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Bottom;
            statusPanel.Height = 28;
            statusPanel.BackColor = Color.FromArgb(240, 242, 247);
            statusPanel.Padding = new Padding(12, 5, 12, 5);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Left;
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.FromArgb(90, 100, 115);
            statusLabel.Font = new Font("Segoe UI", 8.5f);
            statusLabel.Text = "Ready - Double click any file to log and open";
            statusPanel.Controls.Add(statusLabel);

            countLabel = new Label();
            countLabel.Dock = DockStyle.Right;
            countLabel.AutoSize = true;
            countLabel.ForeColor = Color.FromArgb(90, 100, 115);
            countLabel.Font = new Font("Segoe UI", 8.5f);
            statusPanel.Controls.Add(countLabel);

            // Main ListView with Explorer Columns
            fileListView = new ListView();
            fileListView.Dock = DockStyle.Fill;
            fileListView.View = View.Details;
            fileListView.FullRowSelect = true;
            fileListView.GridLines = false;
            fileListView.SmallImageList = imageList;
            fileListView.Font = new Font("Segoe UI", 9.5f);
            fileListView.BorderStyle = BorderStyle.None;
            fileListView.BackColor = Color.FromArgb(255, 255, 255);
            fileListView.ForeColor = Color.FromArgb(35, 40, 50);

            // Columns
            fileListView.Columns.Add("Name", 380);
            fileListView.Columns.Add("Type", 120);
            fileListView.Columns.Add("Date Modified", 160);
            fileListView.Columns.Add("Size", 100);

            fileListView.DoubleClick += FileListView_DoubleClick;

            this.Controls.Add(fileListView);
            this.Controls.Add(statusPanel);
            this.Controls.Add(searchPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(updateBanner);
        }

        private Button CreateStyledButton(string text, Color bg, Color fg)
        {
            var btn = new Button();
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
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

                            // Check remote release version against current running version
                            string remoteVersionStr = "";
                            if (root.TryGetProperty("name", out var nameProp))
                            {
                                string title = nameProp.GetString() ?? "";
                                // Look for 'vX.Y.Z' or 'X.Y.Z'
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
                searchBox.Text = "";
                fileListView.Items.Clear();

                DirectoryInfo dir = new DirectoryInfo(path);
                int folderCount = 0;
                int fileCount = 0;

                // Folders
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    if ((subDir.Attributes & FileAttributes.Hidden) != 0 && subDir.Name.StartsWith("$")) continue;

                    var item = new ListViewItem(subDir.Name, "folder");
                    item.Tag = subDir.FullName;
                    item.SubItems.Add("File folder");
                    item.SubItems.Add(subDir.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add("");
                    fileListView.Items.Add(item);
                    folderCount++;
                }

                // Files
                foreach (FileInfo file in dir.GetFiles())
                {
                    if ((file.Attributes & FileAttributes.Hidden) != 0 && file.Name.StartsWith("~$")) continue;

                    var item = new ListViewItem(file.Name, "file");
                    item.Tag = file.FullName;
                    item.SubItems.Add(file.Extension.ToUpper() + " File");
                    item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(FormatFileSize(file.Length));
                    fileListView.Items.Add(item);
                    fileCount++;
                }

                countLabel.Text = $"{folderCount} folders, {fileCount} files";
                statusLabel.Text = $"Showing: {path}";
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
            if (string.IsNullOrWhiteSpace(filter))
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

                statusLabel.Text = $"Last opened: {Path.GetFileName(filePath)} at {timestamp}";

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
