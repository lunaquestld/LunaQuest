// Installateur de LunaQuest : installe l'appli pour l'utilisateur courant (sans droits administrateur),
// crée les raccourcis et l'entrée « Applications installées » de Windows, et sait se désinstaller.
// Les fichiers à installer sont embarqués dans l'exe (ressource payload.zip).
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Installation de LunaQuest")]
[assembly: AssemblyProduct("LunaQuest")]

namespace LunaQuestSetup
{
    static class Program
    {
        public const string AppName = "LunaQuest";
        public const string UninstallerName = "Desinstaller-LunaQuest.exe";
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\LunaQuest";

        static bool Silent;

        static void Notify(string text, MessageBoxIcon icon)
        {
            if (!Silent) MessageBox.Show(text, AppName, MessageBoxButtons.OK, icon);
        }

        public static string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(3); }
        }

        public static string DefaultDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName); }
        }

        static string StartMenuShortcut
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk"); }
        }

        static string DesktopShortcut
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"); }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Silent = Array.IndexOf(args, "/silent") >= 0;
            args = args.Where(a => a != "/silent").ToArray();
            if (args.Length >= 1 && args[0] == "/uninstall")
            {
                Uninstall(args.Length >= 2 ? args[1] : null);
                return;
            }
            // Installation sans fenêtre : LunaQuest-Setup.exe /silent ["dossier"]
            if (Silent)
            {
                try { Install(args.Length >= 1 ? Path.GetFullPath(args[0]) : DefaultDir, false, delegate { }); }
                catch { Environment.Exit(1); }
                return;
            }
            Application.Run(new SetupForm());
        }

        public static string InstalledDir()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(UninstallKey))
                return key == null ? null : key.GetValue("InstallLocation") as string;
        }

        public static bool LunaQuestRunning()
        {
            return Process.GetProcessesByName(AppName).Length > 0;
        }

        // Copie les fichiers, puis les raccourcis et l'entrée de désinstallation.
        public static void Install(string dir, bool desktop, Action<string> progress)
        {
            Directory.CreateDirectory(dir);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string target = Path.GetFullPath(Path.Combine(dir, entry.FullName));
                    // Jamais en dehors du dossier d'installation.
                    if (!target.StartsWith(Path.GetFullPath(dir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                    progress("Copie de " + entry.Name + "…");
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    entry.ExtractToFile(target, true);
                }
            }

            progress("Création des raccourcis…");
            string exe = Path.Combine(dir, AppName + ".exe");
            File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(dir, UninstallerName), true);
            CreateShortcut(StartMenuShortcut, exe, dir);
            if (desktop) CreateShortcut(DesktopShortcut, exe, dir);

            long size = new DirectoryInfo(dir).GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length) / 1024;
            using (var key = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                key.SetValue("DisplayName", AppName);
                key.SetValue("DisplayVersion", Version);
                key.SetValue("Publisher", AppName);
                key.SetValue("DisplayIcon", exe);
                key.SetValue("InstallLocation", dir);
                key.SetValue("UninstallString", "\"" + Path.Combine(dir, UninstallerName) + "\" /uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", (int)size, RegistryValueKind.DWord);
            }
        }

        static void CreateShortcut(string path, string target, string workingDir)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
            var linkType = link.GetType();
            linkType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { target });
            linkType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { workingDir });
            linkType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { target + ",0" });
            linkType.InvokeMember("Description", BindingFlags.SetProperty, null, link, new object[] { "Streaming PC VR filaire pour Meta Quest" });
            linkType.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
        }

        static string ShortcutTarget(string path)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, Activator.CreateInstance(shellType), new object[] { path });
                return (link.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, link, null) as string) ?? "";
            }
            catch { return ""; }
        }

        // Le désinstallateur est dans le dossier à supprimer : il se relance depuis le dossier temporaire.
        static void Uninstall(string dir)
        {
            string self = Assembly.GetExecutingAssembly().Location;
            if (dir == null)
            {
                dir = Path.GetDirectoryName(self);
                if (MessageBox.Show("Désinstaller LunaQuest ?\n\nTes réglages et préréglages LunaQuest seront aussi supprimés. "
                        + "ALVR, SteamVR et l'appli du casque ne sont pas touchés.",
                        "LunaQuest", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                while (LunaQuestRunning())
                {
                    if (MessageBox.Show("Ferme LunaQuest avant de continuer.", "LunaQuest",
                            MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) != DialogResult.Retry)
                        return;
                }
                string temp = Path.Combine(Path.GetTempPath(), "LunaQuest-Desinstallation.exe");
                File.Copy(self, temp, true);
                Process.Start(new ProcessStartInfo(temp, "/uninstall \"" + dir + "\"") { UseShellExecute = false });
                return;
            }

            // Sécurité : on ne supprime que le dossier enregistré à l'installation, et seulement s'il contient LunaQuest.
            string registered = InstalledDir();
            bool ok = registered != null
                && string.Equals(Path.GetFullPath(registered).TrimEnd('\\'), Path.GetFullPath(dir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
                && File.Exists(Path.Combine(dir, AppName + ".exe"));
            try
            {
                System.Threading.Thread.Sleep(800); // laisse le premier désinstallateur se fermer
                if (ok) Directory.Delete(dir, true);
                // Seulement nos raccourcis : ceux qui pointent dans le dossier d'installation.
                foreach (var link in new[] { StartMenuShortcut, DesktopShortcut })
                    if (File.Exists(link) && ShortcutTarget(link).StartsWith(Path.GetFullPath(dir).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                        File.Delete(link);
                Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);
                Notify(ok ? "LunaQuest a été désinstallé." : "Raccourcis supprimés. Le dossier n'a pas été reconnu, supprime-le à la main si besoin :\n" + dir,
                    MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                Notify("La désinstallation n'a pas pu se terminer :\n" + e.Message, MessageBoxIcon.Error);
            }
            // Supprime cette copie temporaire après sa fermeture.
            Process.Start(new ProcessStartInfo("cmd.exe", "/c timeout /t 2 /nobreak >nul & del \"" + self + "\"")
            { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false });
        }
    }

    // ================================================================ Fenêtre d'installation

    class SetupForm : Form
    {
        static readonly Color NightTop = Color.FromArgb(8, 13, 36);
        static readonly Color NightBottom = Color.FromArgb(24, 40, 94);
        static readonly Color Field = Color.FromArgb(11, 19, 50);
        static readonly Color TextColor = Color.FromArgb(242, 245, 255);
        static readonly Color Soft = Color.FromArgb(162, 184, 236);
        static readonly Color Accent = Color.FromArgb(76, 141, 255);
        static readonly Color Green = Color.FromArgb(94, 227, 176);
        static readonly Color Red = Color.FromArgb(255, 122, 133);

        readonly TextBox txtDir;
        readonly CheckBox chkDesktop, chkLaunch;
        readonly Button btnInstall, btnBrowse, btnCancel;
        readonly Label lblStatus;
        bool done;

        public SetupForm()
        {
            Text = "Installation de LunaQuest";
            ClientSize = new Size(560, 340);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = NightTop;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Controls.Add(MakeLabel("LunaQuest", new Font("Segoe UI Semibold", 24f), TextColor, 28, 20));
            Controls.Add(MakeLabel("Version " + Program.Version + " · streaming PC VR filaire pour Meta Quest", new Font("Segoe UI", 9.5f), Soft, 32, 68));
            Controls.Add(MakeLabel("Dossier d'installation", new Font("Segoe UI Semibold", 9.5f), Soft, 32, 112));

            txtDir = new TextBox
            {
                Location = new Point(34, 138), Size = new Size(386, 26),
                BackColor = Field, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle,
                Text = Program.InstalledDir() ?? Program.DefaultDir
            };
            Controls.Add(txtDir);
            btnBrowse = MakeButton("Modifier…", false, 430, 136, 98, 30);
            btnBrowse.Click += delegate
            {
                using (var dlg = new FolderBrowserDialog { Description = "Où installer LunaQuest ?", SelectedPath = txtDir.Text })
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtDir.Text = dlg.SelectedPath.TrimEnd('\\').EndsWith(Program.AppName, StringComparison.OrdinalIgnoreCase)
                            ? dlg.SelectedPath : Path.Combine(dlg.SelectedPath, Program.AppName);
            };
            Controls.Add(btnBrowse);

            chkDesktop = MakeCheck("Créer un raccourci sur le bureau", 34, 182);
            chkLaunch = MakeCheck("Lancer LunaQuest à la fin", 34, 210);
            Controls.Add(chkDesktop);
            Controls.Add(chkLaunch);

            Controls.Add(MakeLabel("L'installation n'envoie rien sur internet. Installé pour toi seul, sans droits administrateur.",
                new Font("Segoe UI", 8.5f), Soft, 32, 246));

            lblStatus = MakeLabel("", new Font("Segoe UI", 9f), Soft, 32, 296);
            lblStatus.AutoSize = false;
            lblStatus.Size = new Size(280, 36);
            Controls.Add(lblStatus);

            btnCancel = MakeButton("Annuler", false, 318, 288, 96, 36);
            btnCancel.Click += delegate { Close(); };
            btnInstall = MakeButton("Installer", true, 424, 288, 106, 36);
            btnInstall.Click += delegate { OnInstall(); };
            Controls.Add(btnCancel);
            Controls.Add(btnInstall);
            AcceptButton = btnInstall;
        }

        void OnInstall()
        {
            if (done) { Close(); return; }
            string dir;
            try { dir = Path.GetFullPath(txtDir.Text.Trim()); }
            catch { SetStatus("Ce dossier n'est pas valide.", Red); return; }
            if (Program.LunaQuestRunning()) { SetStatus("Ferme LunaQuest, puis clique à nouveau sur Installer.", Red); return; }

            foreach (Control c in Controls) if (c is Button || c is TextBox || c is CheckBox) c.Enabled = false;
            bool desktop = chkDesktop.Checked, launch = chkLaunch.Checked;
            var worker = new System.Threading.Thread(() =>
            {
                string error = null;
                try { Program.Install(dir, desktop, msg => BeginInvoke((Action)(() => SetStatus(msg, Soft)))); }
                catch (Exception e) { error = e.Message; }
                BeginInvoke((Action)(() => Finish(dir, launch, error)));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        void Finish(string dir, bool launch, string error)
        {
            foreach (Control c in Controls) c.Enabled = true;
            if (error != null) { SetStatus("Échec : " + error, Red); return; }
            done = true;
            SetStatus("LunaQuest est installé. Tu le trouveras dans le menu Démarrer.", Green);
            btnInstall.Text = "Terminer";
            btnCancel.Visible = btnBrowse.Enabled = txtDir.Enabled = chkDesktop.Enabled = chkLaunch.Enabled = false;
            if (launch)
            {
                try { Process.Start(new ProcessStartInfo(Path.Combine(dir, Program.AppName + ".exe")) { WorkingDirectory = dir }); Close(); }
                catch (Exception e) { SetStatus("Installé, mais impossible de lancer LunaQuest : " + e.Message, Red); }
            }
        }

        void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(ClientRectangle, NightTop, NightBottom, 75f))
                e.Graphics.FillRectangle(brush, ClientRectangle);
            var rnd = new Random(7);
            for (int i = 0; i < 40; i++)
            {
                float s = 1 + (float)rnd.NextDouble() * 1.6f;
                using (var star = new SolidBrush(Color.FromArgb(50 + rnd.Next(120), 255, 255, 255)))
                    e.Graphics.FillEllipse(star, rnd.Next(ClientSize.Width), rnd.Next(ClientSize.Height), s, s);
            }
        }

        static Label MakeLabel(string text, Font font, Color color, int x, int y)
        {
            return new Label { Text = text, Font = font, ForeColor = color, BackColor = Color.Transparent, AutoSize = true, Location = new Point(x, y) };
        }

        static CheckBox MakeCheck(string text, int x, int y)
        {
            return new CheckBox { Text = text, Checked = true, AutoSize = true, Location = new Point(x, y), BackColor = Color.Transparent, ForeColor = TextColor };
        }

        static Button MakeButton(string text, bool primary, int x, int y, int w, int h)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat,
                BackColor = primary ? Accent : Field, ForeColor = TextColor, Cursor = Cursors.Hand,
                Font = new Font("Segoe UI Semibold", 9.5f)
            };
            b.FlatAppearance.BorderColor = primary ? Accent : Color.FromArgb(70, 100, 170);
            return b;
        }
    }
}
