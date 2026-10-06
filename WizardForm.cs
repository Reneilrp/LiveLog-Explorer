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
            this.Text = "Tracker Setup Wizard";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 20;

            // Web UI Checkbox
            webUiCheck = new CheckBox { Text = "Enable Web Dashboard", Location = new Point(20, y), Checked = true, Width = 200 };
            this.Controls.Add(webUiCheck);
            y += 40;

            // Port
            this.Controls.Add(new Label { Text = "Web Port:", Location = new Point(20, y), Width = 100 });
            portText = new TextBox { Text = "9999", Location = new Point(130, y), Width = 100 };
            this.Controls.Add(portText);
            y += 40;

            // Format
            this.Controls.Add(new Label { Text = "Log Format:", Location = new Point(20, y), Width = 100 });
            formatCombo = new ComboBox { Location = new Point(130, y), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            formatCombo.Items.AddRange(new object[] { "Text", "JSON" });
            formatCombo.SelectedIndex = 0;
            this.Controls.Add(formatCombo);
            y += 40;

            // Folder
            this.Controls.Add(new Label { Text = "Default Project Folder:", Location = new Point(20, y), Width = 200 });
            y += 25;
            folderText = new TextBox { Location = new Point(20, y), Width = 250, ReadOnly = true };
            this.Controls.Add(folderText);
            
            browseButton = new Button { Text = "Browse...", Location = new Point(280, y), Width = 80 };
            browseButton.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog()) {
                    if (fbd.ShowDialog() == DialogResult.OK) folderText.Text = fbd.SelectedPath;
                }
            };
            this.Controls.Add(browseButton);
            y += 60;

            // Start Button
            startButton = new Button { Text = "Launch Tracker", Location = new Point(120, y), Width = 150, Height = 40, BackColor = Color.LightGreen };
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // Save choices to AppSettings
            AppSettings.EnableWebUI = webUiCheck.Checked;
            if (int.TryParse(portText.Text, out int port)) AppSettings.WebPort = port;
            AppSettings.LogFormat = formatCombo.SelectedItem.ToString();
            AppSettings.DefaultFolder = folderText.Text;
            
            // Close wizard and tell Program.cs to continue
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
