using Ionic.Zip;
using System;
using System.Data;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BananaInstaller
{
    public partial class Form1 : Form
    {
        string url = "http://play.bananaservers.com/Mods.zip";
        private static System.Threading.Timer _timer;
        public delegate void InvokeDelegate();

        public Form1()
        {
            InitializeComponent();
            this.Text = "BananaCraft Valheim Launcher " + Application.ProductVersion; 

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.AppendText("----- BananaCraft Valheim Launcher ----- " + Environment.NewLine);
            _timer = new System.Threading.Timer(Callback, null, 2500, Timeout.Infinite);
            button1.Enabled = false;
            button1.Text = "LOADING";
            label1.Text = "Status: Idle";
            textBox2.Text = Properties.Settings.Default.Server;
            toolStripTextBox1.Text = Properties.Settings.Default.Arguments;
            if(Properties.Settings.Default.Installed == false)
            {
                textBox1.AppendText("-> Please launch valheim to begin install!" + Environment.NewLine);
                return;
            }
            else
            {
                _ = update_mods();
            }

        }
        private void not_installed()
        {
            _timer = new System.Threading.Timer(Callback, null, 2500, Timeout.Infinite);
        }

        private async Task update_mods()
        {
            if (!File.Exists(Properties.Settings.Default.Path + @"\\Mods.zip"))
            {
                textBox1.AppendText("-> Mods update pending" + Environment.NewLine);
                Properties.Settings.Default.update = true;
                Properties.Settings.Default.Save();
                return;
            }
            var request = HttpWebRequest.CreateHttp(url);
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1";
            request.Method = "HEAD";
            using (var response = await request.GetResponseAsync())
            {
                label1.Text = "Status: Checking for mod updates..";
                var length_web = response.ContentLength;
                var length_system = new System.IO.FileInfo(Properties.Settings.Default.Path + @"\\Mods.zip").Length;
                
                if (length_system == length_web)
                {
                    textBox1.AppendText("-> Running latest mods" + Environment.NewLine);
                    Properties.Settings.Default.update = false;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    textBox1.AppendText("-> Mods update pending" + Environment.NewLine);
                    Properties.Settings.Default.update = true;
                    Properties.Settings.Default.Save();
                    
                }
            }
        }

        private void StartGame()
        {
            var p = new Process();
            p.StartInfo.FileName = Properties.Settings.Default.Path + "\\valheim.exe";
            p.StartInfo.Arguments = "+connect " + textBox2.Text + " " + toolStripTextBox1.Text;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

        }
        private void CheckProcess(bool kill)
        {
            if (Process.GetProcessesByName("valheim").Length > 0)
            {
                var procList = Process.GetProcesses().Where(process => process.ProcessName.Contains("valheim"));
                foreach (var process in procList)
                {
                    label1.Text = "Status: Checking for process..";
                    string valheimpath = Path.GetDirectoryName(process.MainModule.FileName);
                    Properties.Settings.Default.Path = valheimpath;
                    Properties.Settings.Default.Save();
                    label1.Text = "Killing Process";
                    if(kill == true)
                    {

                    }
                    process.Kill();
                }
            }
        }
               
        private void Callback(Object state)
        {
            Stopwatch watch = new Stopwatch();

            watch.Start();
            textBox1.BeginInvoke(new InvokeDelegate(InvokeMethod));
            _timer.Change(Math.Max(0, 3000 - watch.ElapsedMilliseconds), Timeout.Infinite);
        }

        public void InvokeMethod()
        {
            if(Properties.Settings.Default.Path == "")
            {
                CheckProcess(true);
                textBox1.AppendText("-> Valheim not found! Please launch valheim!" + Environment.NewLine);
                label1.Text = "Status: Valheim not found!";
                button1.Enabled = false;
                button1.Text = "PLAY";
            }
            else
            {
                if(Properties.Settings.Default.Installed == true)
                {
                    button1.Enabled = true;
                    button1.Text = "PLAY";
                    label1.Text = "Status: Ready to play!";
                    textBox1.AppendText("-> Ready to play!" + Environment.NewLine);
                    _timer.Dispose();
                    return;
                }
                else
                {
                    textBox1.AppendText("-> Valheim found! " + Properties.Settings.Default.Path + Environment.NewLine);
                    textBox1.AppendText("-> Ready for install!" + Environment.NewLine);
                    label1.Text = "Status: Install pending..";
                    button1.Enabled = true;
                    button1.Text = "INSTALL";
                    _timer.Dispose();
                }
            }
        }

        private void Download()
        {
            button1.Text = "DOWNLOADING";
            button1.Enabled = false;

            if(File.Exists(Properties.Settings.Default.Path + @"\\Mods.zip") == true)
            {
                textBox1.AppendText("-> Deleted old zip" + Environment.NewLine);
                label1.Text = "Status: Cleaning up..";
                try
                {
                    File.Delete(Properties.Settings.Default.Path + @"\\Mods.zip");
                    Directory.Delete(Properties.Settings.Default.Path + @"\\BepInEx\\plugins", true);
                    Directory.CreateDirectory(Properties.Settings.Default.Path + @"\\BepInEx\\plugins");
                }
                catch(Exception ex)
                {
                    
                }              
            }
           var download_task = DownloadArchiveAsync(url);         
        }

        private void Unzip()
        {
            textBox1.AppendText("-> Unzipping.. " + Environment.NewLine);
            using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(Properties.Settings.Default.Path + @"\\Mods.zip"))
            {
                zip.ExtractProgress +=
                   new EventHandler<ExtractProgressEventArgs>(zip_ExtractProgress);
                zip.ExtractAll(Properties.Settings.Default.Path, ExtractExistingFileAction.OverwriteSilently);

                string path = Properties.Settings.Default.Path + "\\BepInEx\\plugins";
                foreach (string f in Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories))
                {
                    textBox1.AppendText(f.Remove(0, path.Length) + Environment.NewLine);
                }

            }
            textBox1.AppendText("-> Unzipping completed " + Environment.NewLine);
            label1.Text = "Status: Done..";
            Properties.Settings.Default.Installed = true;
            Properties.Settings.Default.Save();
            button1.Enabled = true;
            button1.Text = "PLAY";
            label1.Text = "Status: Ready to play!";
            textBox1.AppendText("-> Ready to play!" + Environment.NewLine);

        }

        void zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.TotalBytesToTransfer > 0)
            {
                progressBar1.Value = Convert.ToInt32(100 * e.BytesTransferred / e.TotalBytesToTransfer);
                label1.Text = "Status: Files extracted " + e.CurrentEntry.ToString();
            }
        }

        async Task<bool> DownloadArchiveAsync(string fileUrl)
        {
            var downloadLink = new Uri(fileUrl);

            DownloadProgressChangedEventHandler DownloadProgressChangedEvent = (s, e) =>
            {
                progressBar1.BeginInvoke((Action)(() =>
                {
                    progressBar1.Value = e.ProgressPercentage;
                }));

                var downloadProgress = string.Format("{0} MB / {1} MB",
                        (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                        (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));

                label1.BeginInvoke((Action)(() =>
                {
                    label1.Text = ("Status: Downloading " + downloadProgress + " ..." + Environment.NewLine);
                }));

            };

            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadProgressChanged += DownloadProgressChangedEvent;
                await webClient.DownloadFileTaskAsync(downloadLink, Properties.Settings.Default.Path.ToString() + @"\\Mods.zip");
                
            }
            label1.Text = "Status: Download Complete";
            textBox1.AppendText("-> Downloaded" + Environment.NewLine);
            Unzip();
            return true;         
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if(Properties.Settings.Default.update == true)
                {
                    DialogResult dialogResult = MessageBox.Show("Do you wish to update your mods?", "BananaCraft Mods updater", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                        Properties.Settings.Default.update = false;
                        Properties.Settings.Default.Save();
                        Download();
                        return;                   
                }
                bool installed = Properties.Settings.Default.Installed;
                if(installed == true)
                {
                    button1.Enabled = true;
                    button1.Text = "PLAY";
                    label1.Text = "Status: Ready to play";
                    StartGame();
                    Properties.Settings.Default.Server = textBox2.Text;
                    Properties.Settings.Default.Arguments = toolStripTextBox1.Text;
                    Properties.Settings.Default.Save();
                    return;
                }
                else
                {
                    Download();
                }
            }
            catch (Exception ex)
            {
                textBox1.AppendText(ex.ToString());
            }
        }
        private void supportToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start("https://discord.gg/FdU4CKM");
        }

        private void modsFolderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Path == "")
            {
                textBox1.AppendText("Path not found for Valheim!");
            }
            else
            {
                Process.Start(Properties.Settings.Default.Path);
            }
        }

        private void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Properties.Settings.Default.Path;
            try
            {
                CheckProcess(false);
                label1.Text = "Status: Uninstalling";
                textBox1.AppendText("-> Uninstalling" + Environment.NewLine);
                Directory.Delete(path + "\\BepInEx", true);
                Directory.Delete(path + "\\doorstop_libs", true);
                Directory.Delete(path + "\\unstripped_corlib", true);
                File.Delete(path + "\\winhttp.dll");
                File.Delete(path + "\\doorstop_config.ini");
                label1.Text = "Status: Uninstalled!";
                textBox1.AppendText("-> Ready for installation!" + Environment.NewLine);
                button1.Text = "INSTALL";
                Properties.Settings.Default.Installed = false;
                Properties.Settings.Default.Path = "";
                Properties.Settings.Default.Save();
                not_installed();
            }
            catch (Exception ex)
            {
                textBox1.AppendText("-> Mods are not installed!" + Environment.NewLine);
                Properties.Settings.Default.Installed = false;
                Properties.Settings.Default.Path = "";
                Properties.Settings.Default.Save();
            }
        }
    }
}
