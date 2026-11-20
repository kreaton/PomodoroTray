using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PomodoroTray
{
    /// <summary>
    /// Small modal dialog for editing the configurable interval lengths and behavior.
    /// Built entirely in code so no designer file is needed.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly NumericUpDown _workUpDown;
        private readonly NumericUpDown _shortBreakUpDown;
        private readonly NumericUpDown _longBreakUpDown;
        private readonly NumericUpDown _sessionsUpDown;
        private readonly CheckBox _autoStartCheck;
        private readonly CheckBox _soundCheck;
        private readonly CheckBox _notificationCheck;
        private readonly CheckBox _launchAtStartupCheck;
        private readonly GroupBox _soundsGroup;
        private readonly SoundPicker _shortBreakSound;
        private readonly SoundPicker _longBreakSound;
        private readonly SoundPicker _workSound;
        private readonly ToolTip _toolTip = new();

        public PomodoroSettings ResultSettings { get; private set; }

        public SettingsForm(PomodoroSettings current)
        {
            Text = "Pomodoro Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(444, 447);
            Font = new Font("Segoe UI", 9F);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                Location = new Point(12, 12),
                Size = new Size(316, 130),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            _workUpDown = AddRow(layout, "Work (minutes):", current.WorkMinutes, 1, 180);
            _shortBreakUpDown = AddRow(layout, "Short break (minutes):", current.ShortBreakMinutes, 1, 60);
            _longBreakUpDown = AddRow(layout, "Long break (minutes):", current.LongBreakMinutes, 1, 90);
            _sessionsUpDown = AddRow(layout, "Sessions before long break:", current.SessionsBeforeLongBreak, 1, 12);

            Controls.Add(layout);

            _autoStartCheck = new CheckBox
            {
                Text = "Automatically start the next interval",
                Checked = current.AutoStartNext,
                AutoSize = true,
                Location = new Point(12, 165)
            };
            Controls.Add(_autoStartCheck);

            _notificationCheck = new CheckBox
            {
                Text = "Show a notification when an interval ends",
                Checked = current.ShowNotification,
                AutoSize = true,
                Location = new Point(12, 192)
            };
            Controls.Add(_notificationCheck);

            _launchAtStartupCheck = new CheckBox
            {
                Text = "Start automatically when Windows starts",
                Checked = AutoStartManager.IsEnabled(),
                AutoSize = true,
                Location = new Point(12, 219)
            };
            Controls.Add(_launchAtStartupCheck);

            // Directly above the sounds group it switches on and off.
            _soundCheck = new CheckBox
            {
                Text = "Play a sound when an interval ends",
                Checked = current.PlaySound,
                AutoSize = true,
                Location = new Point(12, 246)
            };
            Controls.Add(_soundCheck);

            _soundsGroup = new GroupBox
            {
                Text = "Transition sounds (.wav)",
                Location = new Point(12, 273),
                Size = new Size(420, 124)
            };
            _shortBreakSound = new SoundPicker(_soundsGroup, _toolTip, "Work → short break:", current.ShortBreakStartSound, 24);
            _longBreakSound = new SoundPicker(_soundsGroup, _toolTip, "Work → long break:", current.LongBreakStartSound, 56);
            _workSound = new SoundPicker(_soundsGroup, _toolTip, "Break → work:", current.WorkStartSound, 88);
            Controls.Add(_soundsGroup);

            _soundsGroup.Enabled = _soundCheck.Checked;
            _soundCheck.CheckedChanged += (_, _) => _soundsGroup.Enabled = _soundCheck.Checked;

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(264, 412),
                Width = 80
            };
            okButton.Click += OnOk;
            Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(352, 412),
                Width = 80
            };
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            ResultSettings = current.Clone();
        }

        private static NumericUpDown AddRow(TableLayoutPanel layout, string label, int value, int min, int max)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 10, 3, 3)
            };
            var upDown = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Min(Math.Max(value, min), max),
                Width = 70,
                Margin = new Padding(3, 6, 3, 3)
            };

            layout.Controls.Add(lbl);
            layout.Controls.Add(upDown);
            return upDown;
        }

        private void OnOk(object? sender, EventArgs e)
        {
            ResultSettings = new PomodoroSettings
            {
                WorkMinutes = (int)_workUpDown.Value,
                ShortBreakMinutes = (int)_shortBreakUpDown.Value,
                LongBreakMinutes = (int)_longBreakUpDown.Value,
                SessionsBeforeLongBreak = (int)_sessionsUpDown.Value,
                AutoStartNext = _autoStartCheck.Checked,
                PlaySound = _soundCheck.Checked,
                ShowNotification = _notificationCheck.Checked,
                ShortBreakStartSound = _shortBreakSound.WavPath,
                LongBreakStartSound = _longBreakSound.WavPath,
                WorkStartSound = _workSound.WavPath
            };

            AutoStartManager.SetEnabled(_launchAtStartupCheck.Checked);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// One row in the sounds group: a label, a dropdown with the Windows default sound,
        /// the sounds that ship with the app and the user's own file, plus buttons to
        /// choose a file of their own and to play the selected sound.
        /// </summary>
        /// <remarks>
        /// The bundled sounds are listed by name because their folder sits under the
        /// hidden AppData folder, where a casual user would never find it in a file picker.
        /// </remarks>
        private sealed class SoundPicker
        {
            private sealed class Choice(string text, string? path, string toolTip)
            {
                public string? Path { get; } = path;
                public string ToolTip { get; } = toolTip;
                public override string ToString() => text;
            }

            private readonly ComboBox _combo;
            private readonly ToolTip _toolTip;
            private Choice? _ownFile;

            public string? WavPath => (_combo.SelectedItem as Choice)?.Path;

            public SoundPicker(Control parent, ToolTip toolTip, string label, string? wavPath, int top)
            {
                _toolTip = toolTip;

                parent.Controls.Add(new Label
                {
                    Text = label,
                    AutoSize = true,
                    Location = new Point(10, top + 4)
                });

                _combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(140, top),
                    Width = 200
                };
                _combo.Items.Add(new Choice("Windows default sound", null, "The Windows default sound"));
                foreach (string file in BundledSounds())
                {
                    _combo.Items.Add(new Choice(Path.GetFileNameWithoutExtension(file), file, "Comes with Pomodoro Tray"));
                }
                _combo.SelectedIndexChanged += (_, _) =>
                    _toolTip.SetToolTip(_combo, (_combo.SelectedItem as Choice)?.ToolTip);
                parent.Controls.Add(_combo);

                var browseButton = new Button { Text = "...", Location = new Point(346, top - 1), Size = new Size(30, 25) };
                browseButton.Click += OnBrowse;
                toolTip.SetToolTip(browseButton, "Choose a .wav file of your own");
                parent.Controls.Add(browseButton);

                var playButton = new Button { Text = "▶", Location = new Point(378, top - 1), Size = new Size(30, 25) };
                playButton.Click += OnPlay;
                toolTip.SetToolTip(playButton, "Play this sound");
                parent.Controls.Add(playButton);

                Select(string.IsNullOrWhiteSpace(wavPath) ? null : wavPath);
            }

            private static string[] BundledSounds()
            {
                try
                {
                    return Directory.GetFiles(TransitionSound.BundledSoundsFolder, "*.wav")
                        .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                catch (Exception)
                {
                    // Folder missing (e.g. a trimmed-down copy of the app): only the default and own files.
                    return [];
                }
            }

            /// <summary>Selects the matching entry, adding it as the user's own file if it isn't bundled.</summary>
            private void Select(string? wavPath)
            {
                if (wavPath == null)
                {
                    _combo.SelectedIndex = 0;
                    return;
                }

                foreach (Choice choice in _combo.Items)
                {
                    if (choice.Path != null && SamePath(choice.Path, wavPath))
                    {
                        _combo.SelectedItem = choice;
                        return;
                    }
                }

                // Only one own file per row: a newly chosen one replaces the previous one.
                if (_ownFile != null)
                {
                    _combo.Items.Remove(_ownFile);
                }

                string name = Path.GetFileNameWithoutExtension(wavPath);
                _ownFile = new Choice(
                    File.Exists(wavPath) ? $"{name} (your file)" : $"{name} (missing)",
                    wavPath,
                    wavPath);
                _combo.Items.Add(_ownFile);
                _combo.SelectedItem = _ownFile;
            }

            private static bool SamePath(string a, string b)
            {
                try
                {
                    return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
                }
            }

            private void OnBrowse(object? sender, EventArgs e)
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Choose a sound",
                    Filter = "WAV files (*.wav)|*.wav"
                };

                // Reopen the folder of the user's own file. Otherwise leave the folder unset so
                // Windows opens wherever the user last picked a file: their sounds live in their
                // own folders, and the bundled ones are already in the dropdown.
                string? ownFolder = _ownFile?.Path != null ? Path.GetDirectoryName(_ownFile.Path) : null;
                if (ownFolder != null && Directory.Exists(ownFolder))
                {
                    dialog.InitialDirectory = ownFolder;
                }

                if (dialog.ShowDialog(_combo.FindForm()) == DialogResult.OK)
                {
                    Select(dialog.FileName);
                }
            }

            private void OnPlay(object? sender, EventArgs e)
            {
                string? wavPath = WavPath;
                if (wavPath == null)
                {
                    TransitionSound.PlayDefault();
                }
                else if (!TransitionSound.TryPlayFile(wavPath))
                {
                    MessageBox.Show(
                        _combo.FindForm(),
                        $"This file can't be played:\n{wavPath}\n\nThe default sound will be used instead.",
                        "Pomodoro Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
