using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomExplorerApp
{
    static class Program
    {
        private static HttpListener? httpListener;
        private static CancellationTokenSource? cts;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Hook application exit to immediately terminate and release port
            Application.ApplicationExit += (s, e) => StopWebServer();

            // 1. Show the Wizard first
            using (var wizard = new WizardForm())
            {
                if (wizard.ShowDialog() != DialogResult.OK)
                {
                    return; // User closed the wizard, exit app.
                }
            }

            // 2. Start Web Server if enabled
            if (AppSettings.EnableWebUI)
            {
                Task.Run(() => StartWebServer());
            }

            // 3. Start Main File Explorer
            Application.Run(new MainForm());

            // Ensure web server is stopped after MainForm finishes
            StopWebServer();
        }

        public static void StopWebServer()
        {
            try
            {
                cts?.Cancel();
                if (httpListener != null)
                {
                    httpListener.Stop();
                    httpListener.Close();
                    httpListener = null;
                }
            }
            catch { }
        }

        static void StartWebServer()
        {
            try
            {
                cts = new CancellationTokenSource();
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://localhost:{AppSettings.WebPort}/");
                httpListener.Prefixes.Add($"http://127.0.0.1:{AppSettings.WebPort}/");
                httpListener.Start();

                Task.Run(async () =>
                {
                    while (httpListener != null && httpListener.IsListening && !cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var context = await httpListener.GetContextAsync();
                            _ = ProcessRequestAsync(context);
                        }
                        catch
                        {
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start Web UI. The port might be in use.\n\n" + ex.Message, "Web Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url?.AbsolutePath ?? "/";

                if (path == "/api/logs")
                {
                    string logs = "";
                    if (File.Exists(AppSettings.LogFilePath))
                    {
                        logs = await File.ReadAllTextAsync(AppSettings.LogFilePath);
                    }
                    byte[] data = Encoding.UTF8.GetBytes(logs);
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.ContentLength64 = data.Length;
                    await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
                }
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes(DashboardHtml);
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = data.Length;
                    await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
                }
            }
            catch { }
            finally
            {
                try { context.Response.OutputStream.Close(); } catch { }
            }
        }

        private static readonly string DashboardHtml = """
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <title>Tracker Dashboard</title>
            <style>
                body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #1e1e2f; color: #c8c8d0; margin: 0; padding: 40px; }
                h1 { color: #ffffff; border-bottom: 2px solid #32354a; padding-bottom: 15px; margin-bottom: 30px; }
                .container { max-width: 1200px; margin: 0 auto; }
                table { width: 100%; border-collapse: collapse; background: #27293d; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.2); }
                th, td { padding: 18px 20px; text-align: left; border-bottom: 1px solid #32354a; }
                th { background: #1f2235; color: #ffffff; font-weight: 600; text-transform: uppercase; font-size: 13px; letter-spacing: 1px; }
                tr:hover { background: #32354a; transition: background 0.2s; }
                .badge { background: #00f2c3; color: #1d1e2c; padding: 5px 10px; border-radius: 4px; font-size: 12px; font-weight: bold; text-transform: uppercase; }
                .status { float: right; font-size: 14px; color: #00f2c3; margin-top: 15px; font-weight: bold; display: flex; align-items: center; gap: 8px; }
                .pulse { width: 10px; height: 10px; background: #00f2c3; border-radius: 50%; box-shadow: 0 0 10px #00f2c3; animation: pulse-anim 1.5s infinite; }
                @keyframes pulse-anim { 0% { opacity: 1; } 50% { opacity: 0.4; } 100% { opacity: 1; } }
                .file-path { font-family: 'Consolas', 'Courier New', monospace; color: #a9a9b5; font-size: 14px; word-break: break-all; }
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='status'><div class='pulse'></div> Live Polling</div>
                <h1>Activity Dashboard</h1>
                <table>
                    <thead>
                        <tr>
                            <th style="width: 220px;">Timestamp</th>
                            <th style="width: 120px;">Action</th>
                            <th>File Path</th>
                        </tr>
                    </thead>
                    <tbody id='logTableBody'>
                        <tr><td colspan='3' style='text-align:center;'>Waiting for data...</td></tr>
                    </tbody>
                </table>
            </div>

            <script>
                async function fetchLogs() {
                    try {
                        const response = await fetch('/api/logs');
                        if (!response.ok) return;
                        
                        const text = await response.text();
                        const lines = text.trim().split('\n');
                        const tbody = document.getElementById('logTableBody');
                        
                        if (!text) {
                            tbody.innerHTML = '<tr><td colspan=\'3\' style=\'text-align:center; padding: 40px; color: #666;\'>No activity logged yet. Open a file to begin.</td></tr>';
                            return;
                        }

                        tbody.innerHTML = '';
                        
                        // Reverse so the newest events show at the top of the table
                        lines.reverse().forEach(line => {
                            if (!line.trim()) return;
                            
                            let time = '-', action = '-', file = line;
                            
                            // Auto-detect JSON vs Plain Text
                            if (line.startsWith('{')) {
                                try {
                                    const data = JSON.parse(line);
                                    time = data.time || '-';
                                    action = data.action || '-';
                                    file = data.file || '-';
                                } catch (e) {}
                            } else {
                                const match = line.match(/^\[(.*?)\] (.*?): (.*)$/);
                                if (match) {
                                    time = match[1];
                                    action = match[2];
                                    file = match[3];
                                }
                            }

                            const tr = document.createElement('tr');
                            tr.innerHTML = `
                                <td style='color: #fff;'>${time}</td>
                                <td><span class='badge'>${action}</span></td>
                                <td class='file-path'>${file}</td>
                            `;
                            tbody.appendChild(tr);
                        });
                    } catch (error) {
                        console.error('Error fetching logs:', error);
                    }
                }

                // Fetch immediately on load, then poll every 2 seconds
                fetchLogs();
                setInterval(fetchLogs, 2000);
            </script>
        </body>
        </html>
        """;
    }
}
