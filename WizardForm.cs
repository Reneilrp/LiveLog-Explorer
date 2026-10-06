using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomExplorerApp
{
    public class WizardForm : Form
    {
        private CheckBox webUiCheck;
        private TextBox portText;
        private ComboBox formatCombo;
        private TextBox folderText;
        private Button browseButton;
        private Button startButton;
        private bool isSettingsMode;

        public WizardForm(bool isSettingsMode = false)
        {
            this.isSettingsMode = isSettingsMode;
            this.Text = isSettingsMode ? "LiveLog Explorer - Settings" : "LiveLog Explorer - Quick Setup";
            this.Size = new Size(500, 460);
            this.MinimumSize = new Size(500, 460);
            this.MaximumSize = new Size(500, 460);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildUi();
        }

        private void BuildUi()
        {
            // Modern Clean Header
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.FromArgb(246, 248, 252),
                Padding = new Padding(24, 16, 24, 12)
            };

            Label titleLabel = new Label
            {
                Text = isSettingsMode ? "Explorer Settings" : "LiveLog Explorer Setup",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 30, 45),
                AutoSize = true
            };
            headerPanel.Controls.Add(titleLabel);

            Label subtitleLabel = new Label
            {
                Text = isSettingsMode ? "Adjust your logging format, ports, or root folder" : "Configure tracking & live dashboard preferences",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 110, 125),
                Location = new Point(24, 42),
                AutoSize = true
            };
            headerPanel.Controls.Add(subtitleLabel);

            Panel headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(225, 230, 240)
            };
            headerPanel.Controls.Add(headerBorder);
            this.Controls.Add(headerPanel);

            int y = 98;

            // Option 1: Web Dashboard Toggle
            webUiCheck = new CheckBox
            {
                Text = "Enable Live Web Dashboard",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 35, 45),
                Location = new Point(28, y),
                Size = new Size(320, 24),
                Checked = AppSettings.EnableWebUI,
                Cursor = Cursors.Hand
            };
            this.Controls.Add(webUiCheck);
            y += 40;

            // Option 2: Port
            Label portLabel = new Label
            {
                Text = "Dashboard Port:",
                ForeColor = Color.FromArgb(70, 75, 85),
                Location = new Point(28, y + 2),
                Size = new Size(130, 22)
            };
            this.Controls.Add(portLabel);

            portText = new TextBox
            {
                Text = AppSettings.WebPort.ToString(),
                Location = new Point(170, y),
                Size = new Size(100, 26),
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 252)
            };
            this.Controls.Add(portText);
            y += 42;

            // Option 3: Logging Format
            Label formatLabel = new Label
            {
                Text = "Logging Format:",
                ForeColor = Color.FromArgb(70, 75, 85),
                Location = new Point(28, y + 2),
                Size = new Size(130, 22)
            };
            this.Controls.Add(formatLabel);

            formatCombo = new ComboBox
            {
                Location = new Point(170, y),
                Size = new Size(120, 26),
                Font = new Font("Segoe UI", 9.5f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(248, 249, 252)
            };
            formatCombo.Items.AddRange(new object[] { "Text", "JSON" });
            formatCombo.SelectedItem = AppSettings.LogFormat;
            if (formatCombo.SelectedIndex == -1) formatCombo.SelectedIndex = 0;
            this.Controls.Add(formatCombo);
            y += 46;

            // Option 4: Startup Folder
            Label folderLabel = new Label
            {
                Text = "Default Project Root (Optional):",
                ForeColor = Color.FromArgb(70, 75, 85),
                Location = new Point(28, y),
                Size = new Size(300, 20)
            };
            this.Controls.Add(folderLabel);
            y += 24;

            folderText = new TextBox
            {
                Location = new Point(28, y),
                Size = new Size(330, 26),
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 249, 252),
                Text = string.IsNullOrEmpty(AppSettings.DefaultFolder) ? @"C:\Users" : AppSettings.DefaultFolder
            };
            this.Controls.Add(folderText);

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(366, y - 1),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 242, 248),
                ForeColor = Color.FromArgb(40, 45, 60),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            browseButton.FlatAppearance.BorderSize = 0;
            browseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 235, 245);
            browseButton.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = folderText.Text;
                    if (fbd.ShowDialog() == DialogResult.OK) folderText.Text = fbd.SelectedPath;
                }
            };
            this.Controls.Add(browseButton);
            y += 58;

            // Action Button
            startButton = new Button
            {
                Text = isSettingsMode ? "💾  Save Changes" : "🚀  Launch Explorer",
                Location = new Point(28, y),
                Size = new Size(428, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 103, 192),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            startButton.FlatAppearance.BorderSize = 0;
            startButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 90, 170);
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            AppSettings.EnableWebUI = webUiCheck.Checked;
            if (int.TryParse(portText.Text, out int port)) AppSettings.WebPort = port;
            AppSettings.LogFormat = formatCombo.SelectedItem?.ToString() ?? "Text";
            AppSettings.DefaultFolder = folderText.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
