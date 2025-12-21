using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MeasuringStick;

public class MeasuringStickApp : IDisposable
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly AppSettings _settings;
    private MeasurementOverlay? _overlay;
    private bool _disposed;
    private const string AppName = "MeasuringStick";
    private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public MeasuringStickApp()
    {
        _settings = new AppSettings();
        _settings.Load();

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Measure", null, OnMeasureClick);
        _contextMenu.Items.Add("Measure + Screenshot", null, OnMeasureScreenshotClick);
        _contextMenu.Items.Add("Region Screenshot", null, OnRegionScreenshotClick);
        _contextMenu.Items.Add("-");

        var startupItem = new ToolStripMenuItem("Start with Windows");
        startupItem.Checked = IsStartupEnabled();
        startupItem.Click += OnStartupToggle;
        _contextMenu.Items.Add(startupItem);

        _contextMenu.Items.Add("Settings...", null, OnSettingsClick);

        _contextMenu.Items.Add("-");
        _contextMenu.Items.Add("Exit", null, OnExitClick);

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "Measuring Stick - Click to measure",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _trayIcon.Click += OnTrayIconClick;
    }

    private Icon CreateTrayIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.White, 2f);
            using var brush = new SolidBrush(Color.White);

            g.DrawLine(pen, 4, 16, 28, 16);

            var leftArrow = new Point[]
            {
                new Point(4, 16),
                new Point(10, 10),
                new Point(10, 22)
            };
            g.FillPolygon(brush, leftArrow);

            var rightArrow = new Point[]
            {
                new Point(28, 16),
                new Point(22, 10),
                new Point(22, 22)
            };
            g.FillPolygon(brush, rightArrow);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private void OnTrayIconClick(object? sender, EventArgs e)
    {
        if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
        {
            StartMeasurement(OverlayMode.Measure);
        }
    }

    private void OnMeasureClick(object? sender, EventArgs e)
    {
        StartMeasurement(OverlayMode.Measure);
    }

    private void OnMeasureScreenshotClick(object? sender, EventArgs e)
    {
        StartMeasurement(OverlayMode.MeasureAndScreenshot);
    }

    private void OnRegionScreenshotClick(object? sender, EventArgs e)
    {
        StartMeasurement(OverlayMode.RegionScreenshot);
    }

    private void StartMeasurement(OverlayMode mode)
    {
        if (_overlay != null && !_overlay.IsDisposed)
        {
            _overlay.Close();
            _overlay.Dispose();
        }

        _overlay = new MeasurementOverlay(_settings, mode);
        _overlay.Show();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        using var dialog = new SettingsDialog(_settings);
        dialog.ShowDialog();
    }

    private void OnStartupToggle(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            if (item.Checked)
            {
                DisableStartup();
                item.Checked = false;
            }
            else
            {
                EnableStartup();
                item.Checked = true;
            }
        }
    }

    private static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    private static void EnableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            var exePath = Application.ExecutablePath;
            key?.SetValue(AppName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to enable startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            key?.DeleteValue(AppName, false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to disable startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _overlay?.Dispose();
        _trayIcon.Dispose();
        _contextMenu.Dispose();
    }
}
