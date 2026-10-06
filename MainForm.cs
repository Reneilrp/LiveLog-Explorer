using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CustomExplorerApp
{
    public class MainForm : Form
    {
        private ListBox fileListBox;
        private TextBox pathTextBox;
        private Button browseButton;
        private Button upButton;
        private string currentDirectory;

        public MainForm()
        {
            this.Text = "Custom Tracker & Explorer";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Top Bar: Up Button
            upButton = new Button();
            upButton.Text = "⬆ Up";
            upButton.Location = new Point(10, 10);
            upButton.Size = new Size(50, 25);
            upButton.Click += UpButton_Click;
            this.Controls.Add(upButton);

            // Top Bar: Path Text Box
            pathTextBox = new TextBox();
            pathTextBox.Location = new Point(65, 12);
            pathTextBox.Size = new Size(425, 20);
            pathTextBox.ReadOnly = true;
            this.Controls.Add(pathTextBox);

            // Top Bar: Browse Button
            browseButton = new Button();
            browseButton.Text = "Select Folder";
            browseButton.Location = new Point(500, 10);
            browseButton.Size = new Size(80, 25);
            browseButton.Click += BrowseButton_Click;
            this.Controls.Add(browseButton);

            // Main Area: File List
            fileListBox = new ListBox();
            fileListBox.Location = new Point(10, 45);
            fileListBox.Size = new Size(570, 400);
            fileListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fileListBox.DoubleClick += FileListBox_DoubleClick;
            this.Controls.Add(fileListBox);

            // Determine Root/Starting Folder
            string startingFolder = @"C:\Users"; // Default to C:\Users
            
            // If they picked a specific folder in the Wizard, use that instead
            if (!string.IsNullOrEmpty(AppSettings.DefaultFolder) && Directory.Exists(AppSettings.DefaultFolder))
            {
                startingFolder = AppSettings.DefaultFolder;
            }
            // Alternatively, to start exactly in their specific user folder automatically:
            // startingFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            LoadDirectory(startingFolder);
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the parent directory
                DirectoryInfo parentInfo = Directory.GetParent(currentDirectory);
                if (parentInfo != null)
                {
                    LoadDirectory(parentInfo.FullName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot go up further: " + ex.Message);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = currentDirectory; // Start the dialog where we currently are
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
                fileListBox.Items.Clear();
                currentDirectory = path;
                pathTextBox.Text = path;

                // 1. Load Sub-Folders first
                string[] folders = Directory.GetDirectories(path);
                foreach (string folder in folders)
                {
                    fileListBox.Items.Add($"📁 {Path.GetFileName(folder)}");
                }

                // 2. Load Files below the folders
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    fileListBox.Items.Add(Path.GetFileName(file));
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("You do not have permission to view this folder.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpButton_Click(null, null); // Bounce them back up
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading folder: " + ex.Message);
            }
        }

        private void FileListBox_DoubleClick(object sender, EventArgs e)
        {
            if (fileListBox.SelectedItem != null)
            {
                string selectedItem = fileListBox.SelectedItem.ToString();
                
                // Check if they clicked a folder or a file
                if (selectedItem.StartsWith("📁 "))
                {
                    // It's a folder! Navigate into it.
                    string folderName = selectedItem.Substring(3); // Remove the emoji and space
                    string fullFolderPath = Path.Combine(currentDirectory, folderName);
                    LoadDirectory(fullFolderPath);
                }
                else
                {
                    // It's a file! Open and log it.
                    string fullFilePath = Path.Combine(currentDirectory, selectedItem);
                    OpenFileAndLog(fullFilePath);
                }
            }
        }

        private void OpenFileAndLog(string filePath)
        {
            try
            {
                string logMessage;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

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

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening file: " + ex.Message);
            }
        }
    }
}
