using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MeasuringStick;

public class SettingsDialog : Form
{
    private readonly AppSettings _settings;
    private TextBox _folderTextBox = null!;
    private CheckBox _copyClipboardCheck = null!;
    private CheckBox _showNotificationCheck = null!;
    private CheckBox _autoSaveCheck = null!;
    private TrackBar _opacityTrack = null!;
    private Label _opacityLabel = null!;

    public SettingsDialog(AppSettings settings)
    {
        _settings = settings;
        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        this.Text = "Measuring Stick Settings";
        this.Size = new Size(450, 320);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.White;

        int y = 20;

        // Screenshot folder
        var folderLabel = new Label
        {
            Text = "Screenshot Save Location:",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(folderLabel);

        y += 25;

        _folderTextBox = new TextBox
        {
            Location = new Point(20, y),
            Size = new Size(300, 23),
            ReadOnly = true
        };
        this.Controls.Add(_folderTextBox);

        var browseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(330, y - 1),
            Size = new Size(80, 25)
        };
        browseButton.Click += OnBrowseClick;
        this.Controls.Add(browseButton);

        y += 40;

        // Opacity
        var opacityTitleLabel = new Label
        {
            Text = "Element Opacity:",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(opacityTitleLabel);

        _opacityLabel = new Label
        {
            Text = "100%",
            Location = new Point(130, y),
            AutoSize = true
        };
        this.Controls.Add(_opacityLabel);

        y += 25;

        _opacityTrack = new TrackBar
        {
            Location = new Point(20, y),
            Size = new Size(390, 45),
            Minimum = 10,
            Maximum = 100,
            TickFrequency = 10,
            LargeChange = 10,
            SmallChange = 10
        };
        _opacityTrack.ValueChanged += (s, e) => _opacityLabel.Text = $"{_opacityTrack.Value}%";
        this.Controls.Add(_opacityTrack);

        y += 50;

        // Checkboxes
        _copyClipboardCheck = new CheckBox
        {
            Text = "Copy screenshots to clipboard",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(_copyClipboardCheck);

        y += 30;

        _showNotificationCheck = new CheckBox
        {
            Text = "Show notification after saving screenshot",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(_showNotificationCheck);

        y += 30;

        _autoSaveCheck = new CheckBox
        {
            Text = "Auto-save screenshot on close (Measure + Screenshot mode)",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(_autoSaveCheck);

        y += 40;

        // Buttons
        var saveButton = new Button
        {
            Text = "Save",
            Location = new Point(230, y),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };
        saveButton.Click += OnSaveClick;
        this.Controls.Add(saveButton);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(320, y),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(cancelButton);

        this.AcceptButton = saveButton;
        this.CancelButton = cancelButton;
    }

    private void LoadSettings()
    {
        _folderTextBox.Text = _settings.ScreenshotFolder;
        _opacityTrack.Value = Math.Max(10, Math.Min(100, (int)(_settings.Opacity * 100)));
        _opacityLabel.Text = $"{_opacityTrack.Value}%";
        _copyClipboardCheck.Checked = _settings.CopyToClipboard;
        _showNotificationCheck.Checked = _settings.ShowNotification;
        _autoSaveCheck.Checked = _settings.AutoSaveScreenshot;
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select screenshot save location",
            SelectedPath = _settings.ScreenshotFolder,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _folderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        _settings.ScreenshotFolder = _folderTextBox.Text;
        _settings.Opacity = _opacityTrack.Value / 100.0;
        _settings.CopyToClipboard = _copyClipboardCheck.Checked;
        _settings.ShowNotification = _showNotificationCheck.Checked;
        _settings.AutoSaveScreenshot = _autoSaveCheck.Checked;
        _settings.Save();
    }
}
