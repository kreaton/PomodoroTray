using System;
using System.Drawing;
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
        private readonly CheckBox _launchAtStartupCheck;

        public PomodoroSettings ResultSettings { get; private set; }

        public SettingsForm(PomodoroSettings current)
        {
            Text = "Pomodoro Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(340, 300);
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

            _soundCheck = new CheckBox
            {
                Text = "Play a sound when an interval ends",
                Checked = current.PlaySound,
                AutoSize = true,
                Location = new Point(12, 192)
            };
            Controls.Add(_soundCheck);

            _launchAtStartupCheck = new CheckBox
            {
                Text = "Start automatically when Windows starts",
                Checked = AutoStartManager.IsEnabled(),
                AutoSize = true,
                Location = new Point(12, 219)
            };
            Controls.Add(_launchAtStartupCheck);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(158, 260),
                Width = 80
            };
            okButton.Click += OnOk;
            Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(246, 260),
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
                PlaySound = _soundCheck.Checked
            };

            AutoStartManager.SetEnabled(_launchAtStartupCheck.Checked);
        }
    }
}
