using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Velopack;
using Velopack.Sources;

namespace PomodoroTray
{

    /// <summary>
    /// Runs the whole app: no main window, just a NotifyIcon whose icon is
    /// redrawn every second to show the remaining time, plus a context menu
    /// for controlling and configuring the timer.
    /// </summary>
    public class TrayApplicationContext : ApplicationContext
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        // Public GitHub repo whose releases BuildRelease.ps1 uploads to
        private const string RepoUrl = "https://github.com/kreaton/PomodoroTray";

        private readonly UpdateManager _updateManager = new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly ToolStripMenuItem _statusItem;
        private readonly ToolStripMenuItem _startPauseItem;

        private PomodoroSettings _settings;
        private PomodoroPhases _phase = PomodoroPhases.Inactive;
        private int _remainingSeconds;
        private int _completedWorkSessions;
        private bool _isRunning;

        public TrayApplicationContext()
        {
            _settings = PomodoroSettings.Load();
            _remainingSeconds = _settings.WorkMinutes * 60;

            var menu = new ContextMenuStrip();

            _statusItem = new ToolStripMenuItem("Work — 25:00") { Enabled = false };
            menu.Items.Add(_statusItem);
            menu.Items.Add(new ToolStripSeparator());

            _startPauseItem = new ToolStripMenuItem("Start", null, OnStartPause);
            menu.Items.Add(_startPauseItem);
            menu.Items.Add(new ToolStripMenuItem("Skip to next", null, OnSkip));
            menu.Items.Add(new ToolStripMenuItem("Reset", null, OnReset));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Settings...", null, OnSettings));
            menu.Items.Add(new ToolStripMenuItem("Check for updates...", null, OnCheckForUpdates));
            menu.Items.Add(new ToolStripMenuItem("About...", null, OnAbout));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Exit", null, OnExit));

            _trayIcon = new NotifyIcon
            {
                Text = "Pomodoro Timer",
                Icon = DrawIcon(_remainingSeconds, _phase),
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += OnStartPause;

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += OnTick;

            UpdateDisplay();
        }

        // ---------- Timer state machine ----------

        private void OnTick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateDisplay();
            }
            else
            {
                AdvancePhase();
            }
        }

        private void OnStartPause(object? sender, EventArgs e)
        {
            if (_isRunning) PauseTimer();
            else StartTimer();

            UpdateIcon();
        }

        private void StartTimer()
        {
            _isRunning = true;
            _timer.Start();
            _startPauseItem.Text = "Pause";
            if (_phase == PomodoroPhases.Inactive)
            {
                _phase = PomodoroPhases.Work;
            }

            //_phase = PomodoroPhases.Work;
        }

        private void PauseTimer()
        {
            _isRunning = false;
            _timer.Stop();
            _startPauseItem.Text = "Start";
        }

        private void OnReset(object? sender, EventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            _startPauseItem.Text = "Start";
            _phase = PomodoroPhases.Inactive;
            _remainingSeconds = _settings.WorkMinutes * 60;
            UpdateDisplay();
        }

        private void OnSkip(object? sender, EventArgs e)
        {
            _remainingSeconds = 0;
            AdvancePhase();
        }

        private void AdvancePhase()
        {
            _timer.Stop();
            _isRunning = false;

            string message;
            if (_phase == PomodoroPhases.Work)
            {
                _completedWorkSessions++;
                if (_completedWorkSessions % Math.Max(1, _settings.SessionsBeforeLongBreak) == 0)
                {
                    _phase = PomodoroPhases.LongBreak;
                    _remainingSeconds = _settings.LongBreakMinutes * 60;
                    message = "Nice work! Time for a long break.";
                }
                else
                {
                    _phase = PomodoroPhases.ShortBreak;
                    _remainingSeconds = _settings.ShortBreakMinutes * 60;
                    message = "Time for a short break.";
                }
            }
            else
            {
                _phase = PomodoroPhases.Work;
                _remainingSeconds = _settings.WorkMinutes * 60;
                message = "Break's over — back to work.";
            }

            if (_settings.PlaySound)
            {
                SystemSounds.Exclamation.Play();
            }

            _trayIcon.ShowBalloonTip(4000, "Pomodoro Timer", message, ToolTipIcon.Info);
            UpdateDisplay();

            if (_settings.AutoStartNext)
            {
                StartTimer();
            }
            else
            {
                _startPauseItem.Text = "Start";
            }
        }

        private int GetPhaseDuration(PomodoroPhases phase) => phase switch
        {
            PomodoroPhases.Work => _settings.WorkMinutes,
            PomodoroPhases.ShortBreak => _settings.ShortBreakMinutes,
            PomodoroPhases.LongBreak => _settings.LongBreakMinutes,
            _ => _settings.WorkMinutes
        };

        // ---------- Settings ----------

        private void OnSettings(object? sender, EventArgs e)
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _settings = form.ResultSettings;
                _settings.Save();

                // If the timer isn't running, reflect the new duration for the
                // current phase immediately; otherwise let the running countdown finish.
                if (!_isRunning)
                {
                    _remainingSeconds = GetPhaseDuration(_phase) * 60;
                    UpdateDisplay();
                }
            }
        }

        // ---------- About ----------

        private void OnAbout(object? sender, EventArgs e)
        {
            using var form = new AboutForm();
            form.ShowDialog();
        }

        private async void OnCheckForUpdates(object? sender, EventArgs e)
        {
            if (!_updateManager.IsInstalled)
            {
                MessageBox.Show("Updates are only available in the installed app.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    MessageBox.Show("No updates available.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"A new version ({newVersion.TargetFullRelease.Version}) is available. Do you want to download and install it now?",
                    "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                await _updateManager.DownloadUpdatesAsync(newVersion);

                // The restart ends the process without going through OnExit, so hide the icon here
                _trayIcon.Visible = false;
                _updateManager.ApplyUpdatesAndRestart(newVersion);
            }
            catch (Exception ex)
            {
                _trayIcon.Visible = true;
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------- Exit ----------

        private void OnExit(object? sender, EventArgs e)
        {
            _timer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }

        // ---------- Display ----------

        private void UpdateDisplay()
        {
            int m = _remainingSeconds / 60;
            int s = _remainingSeconds % 60;
            string phaseName = PhaseName(_phase);
            string timeStr = $"{m:00}:{s:00}";

            _statusItem.Text = $"{phaseName} — {timeStr}";
            //_trayIcon.Text = Truncate($"Pomodoro — {phaseName}: {timeStr}", 63);
            _trayIcon.Text = Truncate($"{phaseName}: {timeStr}", 63);
            UpdateIcon();
        }

        private static string PhaseName(PomodoroPhases phase) => phase switch
        {
            PomodoroPhases.Work => "Work",
            PomodoroPhases.ShortBreak => "Short break",
            PomodoroPhases.LongBreak => "Long break",
            _ => "Ready"
        };

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        private void UpdateIcon()
        {
            var oldIcon = _trayIcon.Icon;
            var newIcon = DrawIcon(_remainingSeconds, _phase);
            _trayIcon.Icon = newIcon;

            if (oldIcon != null)
            {
                DestroyIcon(oldIcon.Handle);
                oldIcon.Dispose();
            }
        }

        /// <summary>
        /// Renders a small colored circle with the remaining time as text and
        /// converts it into a Win32 icon for the notify icon. Minutes are shown
        /// once a minute or more remains; seconds are shown in the final minute.
        /// </summary>
        private static Icon DrawIcon(int remainingSeconds, PomodoroPhases phase)
        {
            Color bg = phase switch
            {
                PomodoroPhases.Work => Color.FromArgb(231, 76, 60),
                PomodoroPhases.ShortBreak => Color.FromArgb(46, 204, 113),
                PomodoroPhases.LongBreak => Color.FromArgb(52, 152, 219),
                _ => Color.Gray
            };

            string label = remainingSeconds >= 60
                ? ((int)Math.Ceiling(remainingSeconds / 60.0)).ToString()
                : remainingSeconds.ToString();
            if (label.Length > 2) label = label[..2];

            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                using var bgBrush = new SolidBrush(bg);
                g.FillEllipse(bgBrush, 0, 0, 31, 31);

                //float fontSize = label.Length > 1 ? 13f : 16f;
                float fontSize = label.Length > 1 ? 18f : 16f;
                using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = g.MeasureString(label, font);
                var point = new PointF((32 - textSize.Width) / 2f, (32 - textSize.Height) / 2f);

                using var textBrush = new SolidBrush(Color.AntiqueWhite);
                g.DrawString(label, font, textBrush, point);
            }

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
    }
}
