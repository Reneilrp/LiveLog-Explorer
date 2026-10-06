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

        public WizardForm()
        {
            this.Text = "LiveLog Explorer - Quick Setup";
            this.Size = new Size(460, 420);
            this.MinimumSize = new Size(460, 420);
            this.MaximumSize = new Size(460, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildUi();
        }

        private void BuildUi()
        {
            // Header Banner
            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 70;
            headerPanel.BackColor = Color.FromArgb(25, 30, 45);
            headerPanel.Padding = new Padding(20, 14, 20, 10);

            Label titleLabel = new Label();
            titleLabel.Text = "LiveLog Explorer Setup";
            titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.AutoSize = true;
            headerPanel.Controls.Add(titleLabel);

            Label subtitleLabel = new Label();
            subtitleLabel.Text = "Configure tracking & dashboard preferences";
            subtitleLabel.Font = new Font("Segoe UI", 9f);
            subtitleLabel.ForeColor = Color.FromArgb(160, 175, 200);
            subtitleLabel.Location = new Point(20, 38);
            subtitleLabel.AutoSize = true;
            headerPanel.Controls.Add(subtitleLabel);

            this.Controls.Add(headerPanel);

            // Content Panel
            Panel contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(24, 15, 24, 15);

            int y = 85;

            // Group 1: Web Dashboard Toggle
            webUiCheck = new CheckBox();
            webUiCheck.Text = "Enable Live Web Dashboard";
            webUiCheck.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            webUiCheck.ForeColor = Color.FromArgb(30, 35, 45);
            webUiCheck.Location = new Point(25, y);
            webUiCheck.Size = new Size(300, 24);
            webUiCheck.Checked = true;
            this.Controls.Add(webUiCheck);
            y += 35;

            // Group 2: Port
            Label portLabel = new Label();
            portLabel.Text = "Web Dashboard Port:";
            portLabel.ForeColor = Color.FromArgb(70, 75, 85);
            portLabel.Location = new Point(25, y);
            portLabel.Size = new Size(160, 24);
            this.Controls.Add(portLabel);

            portText = new TextBox();
            portText.Text = "9999";
            portText.Location = new Point(190, y - 2);
            portText.Size = new Size(90, 26);
            portText.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(portText);
            y += 38;

            // Group 3: Log Format
            Label formatLabel = new Label();
            formatLabel.Text = "Logging Format:";
            formatLabel.ForeColor = Color.FromArgb(70, 75, 85);
            formatLabel.Location = new Point(25, y);
            formatLabel.Size = new Size(160, 24);
            this.Controls.Add(formatLabel);

            formatCombo = new ComboBox();
            formatCombo.Location = new Point(190, y - 2);
            formatCombo.Size = new Size(120, 26);
            formatCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            formatCombo.Items.AddRange(new object[] { "Text", "JSON" });
            formatCombo.SelectedIndex = 0;
            this.Controls.Add(formatCombo);
            y += 42;

            // Group 4: Default Folder
            Label folderLabel = new Label();
            folderLabel.Text = "Initial Startup Folder (Optional):";
            folderLabel.ForeColor = Color.FromArgb(70, 75, 85);
            folderLabel.Location = new Point(25, y);
            folderLabel.Size = new Size(300, 20);
            this.Controls.Add(folderLabel);
            y += 24;

            folderText = new TextBox();
            folderText.Location = new Point(25, y);
            folderText.Size = new Size(295, 26);
            folderText.BorderStyle = BorderStyle.FixedSingle;
            folderText.ReadOnly = true;
            folderText.BackColor = Color.White;
            folderText.Text = @"C:\Users";
            this.Controls.Add(folderText);

            browseButton = new Button();
            browseButton.Text = "Browse...";
            browseButton.Location = new Point(328, y - 1);
            browseButton.Size = new Size(82, 28);
            browseButton.FlatStyle = FlatStyle.Flat;
            browseButton.BackColor = Color.FromArgb(235, 238, 245);
            browseButton.ForeColor = Color.FromArgb(40, 45, 55);
            browseButton.Cursor = Cursors.Hand;
            browseButton.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog()) {
                    if (fbd.ShowDialog() == DialogResult.OK) folderText.Text = fbd.SelectedPath;
                }
            };
            this.Controls.Add(browseButton);
            y += 55;

            // Launch Button
            startButton = new Button();
            startButton.Text = "🚀  Launch Explorer";
            startButton.Location = new Point(25, y);
            startButton.Size = new Size(385, 42);
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.FlatAppearance.BorderSize = 0;
            startButton.BackColor = Color.FromArgb(40, 110, 230);
            startButton.ForeColor = Color.White;
            startButton.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            startButton.Cursor = Cursors.Hand;
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
