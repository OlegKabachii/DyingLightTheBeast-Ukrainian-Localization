using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UkrainianLocalizationInstaller
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }

    internal sealed class PayloadItem
    {
        public string Name;
        public string RelativeTarget;
        public string Sha256;
        public long Size;
    }

    internal sealed class BackupEntry
    {
        public string RelativeTarget;
        public bool Existed;
        public string BackupName;
    }

    internal sealed class InstallerForm : Form
    {
        private const string ProductTitle = "Dying Light: The Beast — Ukrainian Localization v2.0";
        private const string BackupFolderName = "UkrainianLocalizationBackup_v2";
        private const string StateFileName = "install_state.txt";
        private readonly TextBox pathBox = new TextBox();
        private readonly TextBox logBox = new TextBox();
        private readonly Button browseButton = new Button();
        private readonly Button installButton = new Button();
        private readonly Button verifyButton = new Button();
        private readonly Button uninstallButton = new Button();

        private static readonly PayloadItem[] Payload =
        {
            new PayloadItem { Name="data0.pak", RelativeTarget=@"ph_ft\work\data0.pak", Sha256="a85972c3592e2aff6d3a7f441beb3c0db0143744fdac4df19f5e26e8b05ba3b8", Size=1220062 },
            new PayloadItem { Name="dataen.pak", RelativeTarget=@"ph_ft\work\data_lang\dataen.pak", Sha256="666dbcbdcd256c70c80354f2416143f02dc3cb8adfe29fe92a64b2858ad6df1f", Size=1445895 },
            new PayloadItem { Name="gui_common_pc.rpack", RelativeTarget=@"ph_ft\work\data_platform\pc\assets\gui_common_pc.rpack", Sha256="feea2554812d1ee1fa281a8b6340862e1620316fe738515521fc48334964640b", Size=1606514752 }
        };

        public InstallerForm()
        {
            Text = ProductTitle;
            ClientSize = new Size(760, 455);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10F);

            AddLabel(ProductTitle, 24, 20, 18F, FontStyle.Bold);
            AddLabel("Автор: Oleg Kabachii   |   Версія: 2.0", 26, 58, 10F, FontStyle.Regular);
            AddLabel("Мова гри у Steam має залишатися English. Перед операцією закрийте гру.", 26, 84, 10F, FontStyle.Regular);
            AddLabel("Папка гри:", 26, 119, 10F, FontStyle.Regular);

            pathBox.SetBounds(26, 145, 610, 28);
            Controls.Add(pathBox);
            browseButton.Text = "Огляд…";
            browseButton.SetBounds(646, 143, 88, 32);
            browseButton.Click += Browse;
            Controls.Add(browseButton);

            installButton.Text = "Встановити / Оновити";
            installButton.SetBounds(26, 194, 220, 42);
            installButton.Click += delegate { RunAction(InstallOrUpdate); };
            Controls.Add(installButton);
            verifyButton.Text = "Перевірити файли";
            verifyButton.SetBounds(257, 194, 220, 42);
            verifyButton.Click += delegate { RunAction(VerifyInstalled); };
            Controls.Add(verifyButton);
            uninstallButton.Text = "Видалити";
            uninstallButton.SetBounds(488, 194, 220, 42);
            uninstallButton.Click += delegate { RunAction(Uninstall); };
            Controls.Add(uninstallButton);

            logBox.SetBounds(26, 255, 708, 170);
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            Controls.Add(logBox);

            pathBox.Text = DetectGameFolder() ?? "";
        }

        private void AddLabel(string text, int x, int y, float size, FontStyle style)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            label.Font = new Font("Segoe UI", size, style);
            Controls.Add(label);
        }

        private void Browse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Виберіть кореневу папку Dying Light The Beast";
                dialog.SelectedPath = Directory.Exists(pathBox.Text) ? pathBox.Text : "";
                if (dialog.ShowDialog(this) == DialogResult.OK) pathBox.Text = dialog.SelectedPath;
            }
        }

        private void RunAction(Action action)
        {
            SetButtons(false);
            try { action(); }
            catch (Exception ex)
            {
                Log("ПОМИЛКА: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetButtons(true); }
        }

        private void SetButtons(bool enabled)
        {
            browseButton.Enabled = enabled;
            installButton.Enabled = enabled;
            verifyButton.Enabled = enabled;
            uninstallButton.Enabled = enabled;
        }

        private void Log(string text)
        {
            logBox.AppendText(text + Environment.NewLine);
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
            Application.DoEvents();
        }

        private string GetGameRoot()
        {
            string root = Path.GetFullPath(pathBox.Text.Trim());
            if (!Directory.Exists(Path.Combine(root, "ph_ft")))
                throw new InvalidOperationException("У вибраній папці не знайдено ph_ft. Виберіть кореневу папку гри.");
            return root.TrimEnd(Path.DirectorySeparatorChar);
        }

        private static string DataRoot { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"); } }

        private static void EnsureGameClosed()
        {
            foreach (Process process in Process.GetProcesses())
            {
                string name = process.ProcessName;
                if (name.IndexOf("DyingLight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("TheBeast", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Гра запущена. Повністю закрийте її та повторіть операцію.");
            }
        }

        private void ValidatePayload()
        {
            foreach (PayloadItem item in Payload)
            {
                string source = Path.Combine(DataRoot, item.Name);
                if (!File.Exists(source)) throw new FileNotFoundException("У папці data відсутній файл " + item.Name, source);
                FileInfo info = new FileInfo(source);
                if (info.Length != item.Size) throw new InvalidDataException("Неправильний розмір payload: " + item.Name);
                Log("Перевірка пакета: " + item.Name);
                if (!HashFile(source).Equals(item.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Неправильна SHA-256 сума payload: " + item.Name);
            }
        }

        private void InstallOrUpdate()
        {
            EnsureGameClosed();
            string root = GetGameRoot();
            ValidatePayload();
            string backupRoot = Path.Combine(root, BackupFolderName);
            string statePath = Path.Combine(backupRoot, StateFileName);
            List<BackupEntry> state;

            if (File.Exists(statePath))
            {
                state = ReadState(statePath);
                Log("Наявну резервну копію збережено без змін.");
            }
            else
            {
                Directory.CreateDirectory(backupRoot);
                state = new List<BackupEntry>();
                foreach (PayloadItem item in Payload)
                {
                    string target = SafeTarget(root, item.RelativeTarget);
                    bool existed = File.Exists(target);
                    string backupName = existed ? item.Name + ".previous" : "";
                    if (existed)
                    {
                        Log("Резервна копія: " + item.RelativeTarget);
                        CopyFile(target, Path.Combine(backupRoot, backupName), true);
                    }
                    state.Add(new BackupEntry { RelativeTarget=item.RelativeTarget, Existed=existed, BackupName=backupName });
                }
                WriteState(statePath, state);
            }

            foreach (PayloadItem item in Payload)
            {
                string source = Path.Combine(DataRoot, item.Name);
                string target = SafeTarget(root, item.RelativeTarget);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                string temp = target + ".ua_v2_tmp";
                Log("Встановлення: " + item.Name);
                try
                {
                    CopyFile(source, temp, true);
                    if (!HashFile(temp).Equals(item.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Помилка перевірки скопійованого файла: " + item.Name);
                    File.Copy(temp, target, true);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            Log("ГОТОВО: українську локалізацію встановлено.");
            MessageBox.Show(this, "Українську локалізацію встановлено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void VerifyInstalled()
        {
            string root = GetGameRoot();
            bool ok = true;
            foreach (PayloadItem item in Payload)
            {
                string target = SafeTarget(root, item.RelativeTarget);
                if (!File.Exists(target)) { Log("ВІДСУТНІЙ: " + item.RelativeTarget); ok = false; continue; }
                Log("Перевірка: " + item.Name);
                if (HashFile(target).Equals(item.Sha256, StringComparison.OrdinalIgnoreCase)) Log("OK: " + item.Name);
                else { Log("НЕПРАВИЛЬНА SHA-256: " + item.Name); ok = false; }
            }
            if (!ok) throw new InvalidDataException("Знайдено відсутні або змінені файли локалізації.");
            MessageBox.Show(this, "Усі встановлені файли локалізації справні.", "Перевірка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Uninstall()
        {
            EnsureGameClosed();
            string root = GetGameRoot();
            string backupRoot = Path.Combine(root, BackupFolderName);
            string statePath = Path.Combine(backupRoot, StateFileName);
            if (!File.Exists(statePath)) throw new FileNotFoundException("Не знайдено стан встановлення v2.", statePath);
            foreach (BackupEntry entry in ReadState(statePath))
            {
                string target = SafeTarget(root, entry.RelativeTarget);
                if (entry.Existed)
                {
                    string backup = Path.Combine(backupRoot, entry.BackupName);
                    if (!File.Exists(backup)) throw new FileNotFoundException("Відсутня резервна копія.", backup);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    Log("Відновлення: " + entry.RelativeTarget);
                    CopyFile(backup, target, true);
                }
                else if (File.Exists(target))
                {
                    Log("Видалення: " + entry.RelativeTarget);
                    File.Delete(target);
                }
            }
            Directory.Delete(backupRoot, true);
            Log("ГОТОВО: локалізацію видалено, попередні файли відновлено.");
            MessageBox.Show(this, "Локалізацію видалено. Попередні файли відновлено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string SafeTarget(string root, string relative)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(Path.Combine(fullRoot, relative));
            if (!target.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Небезпечний шлях призначення.");
            return target;
        }

        private static void CopyFile(string source, string destination, bool overwrite)
        {
            const int bufferSize = 1024 * 1024;
            FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;
            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
            using (FileStream output = new FileStream(destination, mode, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
                input.CopyTo(output, bufferSize);
        }

        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan))
            {
                byte[] hash = sha.ComputeHash(stream);
                StringBuilder result = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) result.Append(b.ToString("x2"));
                return result.ToString();
            }
        }

        private static void WriteState(string path, IEnumerable<BackupEntry> entries)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("DyingLightTheBeast_Ukrainian_v2");
            foreach (BackupEntry entry in entries)
                text.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.RelativeTarget))).Append('\t')
                    .Append(entry.Existed ? "1" : "0").Append('\t')
                    .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.BackupName ?? ""))).AppendLine();
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static List<BackupEntry> ReadState(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0 || lines[0] != "DyingLightTheBeast_Ukrainian_v2")
                throw new InvalidDataException("Невідомий формат стану встановлення.");
            List<BackupEntry> result = new List<BackupEntry>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(lines[i])) continue;
                string[] parts = lines[i].Split('\t');
                if (parts.Length != 3) throw new InvalidDataException("Пошкоджений стан встановлення.");
                result.Add(new BackupEntry {
                    RelativeTarget=Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])),
                    Existed=parts[1] == "1",
                    BackupName=Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]))
                });
            }
            return result;
        }

        private static string DetectGameFolder()
        {
            List<string> candidates = new List<string>();
            AddCandidate(candidates, AppDomain.CurrentDomain.BaseDirectory);
            string steam = ReadSteamPath(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
            if (!String.IsNullOrEmpty(steam)) AddSteamCandidates(candidates, steam);
            steam = ReadSteamPath(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            if (!String.IsNullOrEmpty(steam)) AddSteamCandidates(candidates, steam);
            foreach (DriveInfo drive in DriveInfo.GetDrives())
                if (drive.IsReady) AddCandidate(candidates, Path.Combine(drive.RootDirectory.FullName, @"SteamLibrary\steamapps\common\Dying Light The Beast"));
            foreach (string candidate in candidates)
                if (Directory.Exists(Path.Combine(candidate, "ph_ft"))) return candidate;
            return null;
        }

        private static string ReadSteamPath(RegistryKey root, string subKey, string valueName)
        {
            try { using (RegistryKey key = root.OpenSubKey(subKey, false)) { return key == null ? null : key.GetValue(valueName) as string; } }
            catch { return null; }
        }

        private static void AddSteamCandidates(List<string> candidates, string steamRoot)
        {
            AddCandidate(candidates, Path.Combine(steamRoot, @"steamapps\common\Dying Light The Beast"));
            string vdf = Path.Combine(steamRoot, @"steamapps\libraryfolders.vdf");
            if (!File.Exists(vdf)) return;
            try
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\\\"path\\\"\\s*\\\"([^\\\"]+)\\\""))
                    AddCandidate(candidates, Path.Combine(match.Groups[1].Value.Replace(@"\\", @"\"), @"steamapps\common\Dying Light The Beast"));
            }
            catch { }
        }

        private static void AddCandidate(List<string> candidates, string candidate)
        {
            if (String.IsNullOrWhiteSpace(candidate)) return;
            try
            {
                string full = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);
                if (!candidates.Contains(full)) candidates.Add(full);
            }
            catch { }
        }
    }
}
