using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PomodoroTray
{
    /// <summary>
    /// Simple "About" dialog showing the app icon, name, version, and a short description.
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About Pomodoro Tray";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(320, 225);
            Font = new Font("Segoe UI", 9F);

            var appIcon = LoadAppIcon();
            Icon = appIcon;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            var iconBox = new PictureBox
            {
                Image = LoadEmbeddedImage("preview_master.png") ?? appIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(48, 48),
                Location = new Point(20, 15)
            };
            Controls.Add(iconBox);

            var titleLabel = new Label
            {
                Text = "Pomodoro Tray",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(78, 15)
            };
            Controls.Add(titleLabel);

            var versionLabel = new Label
            {
                Text = $"Version {versionText}, (c) Per Hejndorf",
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(78, 45)
            };
            Controls.Add(versionLabel);

            var descLabel = new Label
            {
                Text = "A simple, configurable Pomodoro timer that lives in the "
                     + "system tray. The icon itself shows the time remaining.\n\n"
                     + "Right-click the tray icon to start, pause, skip, or reset "
                     + "a session, and to change interval lengths and startup behavior.",
                AutoSize = false,
                Size = new Size(280, 95),
                Location = new Point(20, 75)
            };
            Controls.Add(descLabel);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 190),
                Width = 80
            };
            Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = okButton;
        }

        private static Icon LoadAppIcon()
        {
            using var stream = FindEmbeddedResourceStream("pomodoro_clock_icon.ico");
            return stream != null ? new Icon(stream) : SystemIcons.Application;
        }

        private static Image? LoadEmbeddedImage(string fileName)
        {
            using var stream = FindEmbeddedResourceStream(fileName);
            if (stream == null) return null;

            // Clone into a standalone bitmap so the image stays valid after the
            // resource stream (used to construct it) is disposed.
            using var loaded = Image.FromStream(stream);
            return new Bitmap(loaded);
        }

        private static System.IO.Stream? FindEmbeddedResourceStream(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            return resourceName != null ? assembly.GetManifestResourceStream(resourceName) : null;
        }
    }
}
