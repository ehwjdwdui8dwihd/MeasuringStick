using System;
using System.IO;
using Microsoft.Win32;

namespace MeasuringStick;

public class AppSettings
{
    private const string SettingsKey = @"SOFTWARE\MeasuringStick";

    public double Opacity { get; set; } = 1.0;
    public string ScreenshotFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public bool CopyToClipboard { get; set; } = true;
    public bool ShowNotification { get; set; } = true;
    public bool AutoSaveScreenshot { get; set; } = true;

    public void Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKey, false);
            if (key == null) return;

            if (key.GetValue("Opacity") is int opacityPercent)
                Opacity = opacityPercent / 100.0;

            if (key.GetValue("ScreenshotFolder") is string folder && Directory.Exists(folder))
                ScreenshotFolder = folder;

            if (key.GetValue("CopyToClipboard") is int copyClip)
                CopyToClipboard = copyClip == 1;

            if (key.GetValue("ShowNotification") is int showNotif)
                ShowNotification = showNotif == 1;

            if (key.GetValue("AutoSaveScreenshot") is int autoSave)
                AutoSaveScreenshot = autoSave == 1;
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(SettingsKey);
            if (key == null) return;

            key.SetValue("Opacity", (int)(Opacity * 100));
            key.SetValue("ScreenshotFolder", ScreenshotFolder);
            key.SetValue("CopyToClipboard", CopyToClipboard ? 1 : 0);
            key.SetValue("ShowNotification", ShowNotification ? 1 : 0);
            key.SetValue("AutoSaveScreenshot", AutoSaveScreenshot ? 1 : 0);
        }
        catch { }
    }
}
