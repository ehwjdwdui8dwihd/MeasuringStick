using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace MeasuringStick;

public enum OverlayMode
{
    Measure,
    MeasureAndScreenshot,
    RegionScreenshot
}

public class MeasurementOverlay : Form
{
    // Measurement points
    private Point? _measureStart;
    private Point _measureEnd;
    private bool _measurementComplete;

    // Region selection points (for screenshot)
    private Point? _regionStart;
    private Point _regionEnd;
    private bool _isDragging;

    private readonly Font _distanceFont;
    private readonly Font _coordFont;
    private readonly int _elementAlpha;
    private readonly OverlayMode _mode;
    private readonly AppSettings _settings;
    private Bitmap? _backgroundImage;

    private const int WS_EX_TOOLWINDOW = 0x80;

    public MeasurementOverlay(AppSettings settings, OverlayMode mode = OverlayMode.Measure)
    {
        _settings = settings;
        _mode = mode;
        _elementAlpha = (int)(settings.Opacity * 255);
        _distanceFont = new Font("Segoe UI", 14f, FontStyle.Bold);
        _coordFont = new Font("Segoe UI", 9f, FontStyle.Regular);

        var bounds = GetVirtualScreenBounds();
        _backgroundImage = CaptureScreen(bounds);

        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = bounds.Location;
        this.Size = bounds.Size;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.Cursor = Cursors.Cross;
        this.DoubleBuffered = true;
        this.KeyPreview = true;

        this.MouseDown += OnMouseDown;
        this.MouseMove += OnMouseMove;
        this.MouseUp += OnMouseUp;
        this.KeyDown += OnKeyDown;
    }

    private static Rectangle GetVirtualScreenBounds()
    {
        int left = SystemInformation.VirtualScreen.Left;
        int top = SystemInformation.VirtualScreen.Top;
        int width = SystemInformation.VirtualScreen.Width;
        int height = SystemInformation.VirtualScreen.Height;
        return new Rectangle(left, top, width, height);
    }

    private static Bitmap CaptureScreen(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        return bitmap;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;

            if (_mode == OverlayMode.MeasureAndScreenshot && _measurementComplete)
            {
                // Phase 2: Start region selection
                _regionStart = e.Location;
                _regionEnd = e.Location;
            }
            else
            {
                // Phase 1: Start measurement (or normal measure/region modes)
                if (_mode == OverlayMode.RegionScreenshot)
                {
                    _regionStart = e.Location;
                    _regionEnd = e.Location;
                }
                else
                {
                    _measureStart = e.Location;
                    _measureEnd = e.Location;
                }
            }
            Invalidate();
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            if (_mode == OverlayMode.MeasureAndScreenshot && _measurementComplete)
            {
                _regionEnd = e.Location;
            }
            else if (_mode == OverlayMode.RegionScreenshot)
            {
                _regionEnd = e.Location;
            }
            else
            {
                _measureEnd = e.Location;
            }
            Invalidate();
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isDragging)
        {
            _isDragging = false;

            if (_mode == OverlayMode.RegionScreenshot)
            {
                // Region screenshot: save and close
                SaveRegionScreenshot();
                Close();
            }
            else if (_mode == OverlayMode.MeasureAndScreenshot)
            {
                if (_measurementComplete)
                {
                    // Phase 2 complete: save region with measurement and close
                    SaveRegionWithMeasurement();
                    Close();
                }
                else
                {
                    // Phase 1 complete: measurement done, now wait for region selection
                    _measurementComplete = true;
                    Invalidate();
                }
            }
            else
            {
                // Normal measure mode: just show measurement
                Invalidate();
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            Close();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
        else if (e.KeyCode == Keys.S && e.Control)
        {
            if (_mode == OverlayMode.RegionScreenshot && _regionStart != null)
            {
                SaveRegionScreenshot();
            }
            else if (_mode == OverlayMode.MeasureAndScreenshot && _measurementComplete && _regionStart != null)
            {
                SaveRegionWithMeasurement();
            }
            else if (_measureStart != null)
            {
                SaveFullScreenWithMeasurement();
            }
        }
        else if (e.KeyCode == Keys.C && e.Control)
        {
            if (_regionStart != null)
            {
                if (_mode == OverlayMode.MeasureAndScreenshot && _measurementComplete)
                {
                    SaveRegionWithMeasurement(clipboardOnly: true);
                }
                else
                {
                    SaveRegionScreenshot(clipboardOnly: true);
                }
            }
        }
    }

    private Rectangle GetRegionRect()
    {
        if (_regionStart == null) return Rectangle.Empty;

        int x = Math.Min(_regionStart.Value.X, _regionEnd.X);
        int y = Math.Min(_regionStart.Value.Y, _regionEnd.Y);
        int width = Math.Abs(_regionEnd.X - _regionStart.Value.X);
        int height = Math.Abs(_regionEnd.Y - _regionStart.Value.Y);

        return new Rectangle(x, y, width, height);
    }

    private void SaveRegionScreenshot(bool clipboardOnly = false)
    {
        if (_regionStart == null || _backgroundImage == null) return;

        var rect = GetRegionRect();
        if (rect.Width < 1 || rect.Height < 1) return;

        using var regionBitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(regionBitmap))
        {
            g.DrawImage(_backgroundImage, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        }

        if (_settings.CopyToClipboard || clipboardOnly)
        {
            Clipboard.SetImage(regionBitmap);
        }

        if (!clipboardOnly && _settings.AutoSaveScreenshot)
        {
            var fileName = $"MeasuringStick_Region_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(_settings.ScreenshotFolder, fileName);
            regionBitmap.Save(filePath, ImageFormat.Png);

            if (_settings.ShowNotification)
            {
                MessageBox.Show($"Screenshot saved to:\n{filePath}" +
                    (_settings.CopyToClipboard ? "\n\n(Also copied to clipboard)" : ""),
                    "Screenshot Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        else if (clipboardOnly && _settings.ShowNotification)
        {
            MessageBox.Show("Region copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SaveRegionWithMeasurement(bool clipboardOnly = false)
    {
        if (_regionStart == null || _backgroundImage == null || _measureStart == null) return;

        var rect = GetRegionRect();
        if (rect.Width < 1 || rect.Height < 1) return;

        using var regionBitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(regionBitmap))
        {
            // Draw the background region
            g.DrawImage(_backgroundImage, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);

            // Translate graphics to draw measurement relative to region
            g.TranslateTransform(-rect.X, -rect.Y);
            DrawMeasurement(g);
        }

        if (_settings.CopyToClipboard || clipboardOnly)
        {
            Clipboard.SetImage(regionBitmap);
        }

        if (!clipboardOnly && _settings.AutoSaveScreenshot)
        {
            var fileName = $"MeasuringStick_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(_settings.ScreenshotFolder, fileName);
            regionBitmap.Save(filePath, ImageFormat.Png);

            if (_settings.ShowNotification)
            {
                MessageBox.Show($"Screenshot saved to:\n{filePath}" +
                    (_settings.CopyToClipboard ? "\n\n(Also copied to clipboard)" : ""),
                    "Screenshot Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        else if (clipboardOnly && _settings.ShowNotification)
        {
            MessageBox.Show("Region copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SaveFullScreenWithMeasurement()
    {
        if (_measureStart == null || _backgroundImage == null) return;

        using var bitmap = new Bitmap(_backgroundImage.Width, _backgroundImage.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.DrawImage(_backgroundImage, 0, 0);
            DrawMeasurement(g);
        }

        if (_settings.CopyToClipboard)
        {
            Clipboard.SetImage(bitmap);
        }

        if (_settings.AutoSaveScreenshot)
        {
            var fileName = $"MeasuringStick_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var filePath = Path.Combine(_settings.ScreenshotFolder, fileName);
            bitmap.Save(filePath, ImageFormat.Png);

            if (_settings.ShowNotification)
            {
                MessageBox.Show($"Screenshot saved to:\n{filePath}" +
                    (_settings.CopyToClipboard ? "\n\n(Also copied to clipboard)" : ""),
                    "Screenshot Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void DrawMeasurement(Graphics g)
    {
        if (_measureStart == null) return;

        g.SmoothingMode = SmoothingMode.AntiAlias;

        var start = _measureStart.Value;
        var end = _measureEnd;

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        var lineColor = Color.FromArgb(_elementAlpha, Color.Red);
        var pointColor = Color.FromArgb(_elementAlpha, Color.Yellow);
        var dashColor = Color.FromArgb(_elementAlpha, Color.Cyan);
        var bgColor = Color.FromArgb(Math.Min(_elementAlpha, 220), 0, 0, 0);
        var textColor = Color.FromArgb(_elementAlpha, Color.White);

        using var linePen = new Pen(lineColor, 2f);
        linePen.CustomEndCap = new AdjustableArrowCap(5, 5);
        linePen.CustomStartCap = new AdjustableArrowCap(5, 5);
        g.DrawLine(linePen, start, end);

        using var pointBrush = new SolidBrush(pointColor);
        g.FillEllipse(pointBrush, start.X - 4, start.Y - 4, 8, 8);
        g.FillEllipse(pointBrush, end.X - 4, end.Y - 4, 8, 8);

        using var dashPen = new Pen(dashColor, 1f);
        dashPen.DashStyle = DashStyle.Dash;
        g.DrawLine(dashPen, start.X, start.Y, end.X, start.Y);
        g.DrawLine(dashPen, end.X, start.Y, end.X, end.Y);

        int midX = (start.X + end.X) / 2;
        int midY = (start.Y + end.Y) / 2;

        string distanceText = $"{distance:F1} px";
        var distanceSize = g.MeasureString(distanceText, _distanceFont);

        var distanceBgRect = new RectangleF(
            midX - distanceSize.Width / 2 - 5,
            midY - distanceSize.Height / 2 - 5,
            distanceSize.Width + 10,
            distanceSize.Height + 10
        );

        using var elementBgBrush = new SolidBrush(bgColor);
        using var elementTextBrush = new SolidBrush(textColor);
        using var borderPen = new Pen(lineColor, 1f);

        g.FillRectangle(elementBgBrush, distanceBgRect);
        g.DrawRectangle(borderPen, distanceBgRect.X, distanceBgRect.Y, distanceBgRect.Width, distanceBgRect.Height);
        g.DrawString(distanceText, _distanceFont, elementTextBrush, midX - distanceSize.Width / 2, midY - distanceSize.Height / 2);

        string hText = $"W: {Math.Abs(dx):F0} px";
        var hSize = g.MeasureString(hText, _coordFont);
        float hX = (start.X + end.X) / 2f - hSize.Width / 2;
        float hY = start.Y - hSize.Height - 5;

        using var cyanBrush = new SolidBrush(dashColor);
        g.FillRectangle(elementBgBrush, hX - 3, hY - 2, hSize.Width + 6, hSize.Height + 4);
        g.DrawString(hText, _coordFont, cyanBrush, hX, hY);

        string vText = $"H: {Math.Abs(dy):F0} px";
        var vSize = g.MeasureString(vText, _coordFont);
        float vX = end.X + 5;
        float vY = (start.Y + end.Y) / 2f - vSize.Height / 2;

        g.FillRectangle(elementBgBrush, vX - 3, vY - 2, vSize.Width + 6, vSize.Height + 4);
        g.DrawString(vText, _coordFont, cyanBrush, vX, vY);

        using var yellowBrush = new SolidBrush(pointColor);

        string startCoord = $"({start.X + this.Left}, {start.Y + this.Top})";
        var startCoordSize = g.MeasureString(startCoord, _coordFont);
        g.FillRectangle(elementBgBrush, start.X - startCoordSize.Width / 2 - 2, start.Y + 10, startCoordSize.Width + 4, startCoordSize.Height + 2);
        g.DrawString(startCoord, _coordFont, yellowBrush, start.X - startCoordSize.Width / 2, start.Y + 10);

        string endCoord = $"({end.X + this.Left}, {end.Y + this.Top})";
        var endCoordSize = g.MeasureString(endCoord, _coordFont);
        g.FillRectangle(elementBgBrush, end.X - endCoordSize.Width / 2 - 2, end.Y + 10, endCoordSize.Width + 4, endCoordSize.Height + 2);
        g.DrawString(endCoord, _coordFont, yellowBrush, end.X - endCoordSize.Width / 2, end.Y + 10);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;

        if (_backgroundImage != null)
        {
            g.DrawImage(_backgroundImage, 0, 0);
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var bgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        using var textBrush = new SolidBrush(Color.White);

        // Region screenshot mode
        if (_mode == OverlayMode.RegionScreenshot)
        {
            // Dim the background slightly
            using var dimBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
            g.FillRectangle(dimBrush, 0, 0, this.Width, this.Height);

            if (_regionStart != null)
            {
                var rect = GetRegionRect();

                // Draw the selected region from original image (undimmed)
                if (_backgroundImage != null && rect.Width > 0 && rect.Height > 0)
                {
                    g.DrawImage(_backgroundImage, rect, rect, GraphicsUnit.Pixel);
                }

                // Draw selection border
                using var selectionPen = new Pen(Color.FromArgb(_elementAlpha, Color.Lime), 2f);
                selectionPen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(selectionPen, rect);

                // Draw size label
                string sizeText = $"{rect.Width} x {rect.Height}";
                var sizeSize = g.MeasureString(sizeText, _coordFont);
                float labelX = rect.X + rect.Width / 2f - sizeSize.Width / 2;
                float labelY = rect.Y + rect.Height + 5;

                using var labelBgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
                g.FillRectangle(labelBgBrush, labelX - 5, labelY, sizeSize.Width + 10, sizeSize.Height + 4);
                g.DrawString(sizeText, _coordFont, textBrush, labelX, labelY + 2);
            }

            string instruction = _regionStart == null
                ? "Click and drag to select region | Right-click or ESC to cancel"
                : (_isDragging ? "Release to capture | Right-click or ESC to cancel" : "Ctrl+C to copy | Ctrl+S to save | Right-click or ESC to close");
            var instrSize = g.MeasureString(instruction, _coordFont);
            g.FillRectangle(bgBrush, 10, 10, instrSize.Width + 10, instrSize.Height + 6);
            g.DrawString(instruction, _coordFont, textBrush, 15, 13);
            return;
        }

        // Measure + Screenshot mode
        if (_mode == OverlayMode.MeasureAndScreenshot)
        {
            // Draw measurement if we have one
            if (_measureStart != null)
            {
                DrawMeasurement(g);
            }

            // If measurement complete, show region selection
            if (_measurementComplete)
            {
                // Dim the background slightly
                using var dimBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
                g.FillRectangle(dimBrush, 0, 0, this.Width, this.Height);

                // Redraw measurement on top of dim
                if (_measureStart != null)
                {
                    DrawMeasurement(g);
                }

                if (_regionStart != null)
                {
                    var rect = GetRegionRect();

                    // Draw the selected region from original image (undimmed) with measurement
                    if (_backgroundImage != null && rect.Width > 0 && rect.Height > 0)
                    {
                        g.DrawImage(_backgroundImage, rect, rect, GraphicsUnit.Pixel);

                        // Draw measurement within the region
                        var clipState = g.Save();
                        g.SetClip(rect);
                        DrawMeasurement(g);
                        g.Restore(clipState);
                    }

                    // Draw selection border
                    using var selectionPen = new Pen(Color.FromArgb(_elementAlpha, Color.Lime), 2f);
                    selectionPen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(selectionPen, rect);

                    // Draw size label
                    string sizeText = $"{rect.Width} x {rect.Height}";
                    var sizeSize = g.MeasureString(sizeText, _coordFont);
                    float labelX = rect.X + rect.Width / 2f - sizeSize.Width / 2;
                    float labelY = rect.Y + rect.Height + 5;

                    using var labelBgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
                    g.FillRectangle(labelBgBrush, labelX - 5, labelY, sizeSize.Width + 10, sizeSize.Height + 4);
                    g.DrawString(sizeText, _coordFont, textBrush, labelX, labelY + 2);
                }

                string instruction = _regionStart == null
                    ? "Now drag to select screenshot region | Right-click or ESC to cancel"
                    : (_isDragging ? "Release to capture | Right-click or ESC to cancel" : "Ctrl+C to copy | Ctrl+S to save | Right-click or ESC to close");
                var instrSize = g.MeasureString(instruction, _coordFont);
                g.FillRectangle(bgBrush, 10, 10, instrSize.Width + 10, instrSize.Height + 6);
                g.DrawString(instruction, _coordFont, textBrush, 15, 13);
            }
            else
            {
                // Phase 1: measuring
                string instruction = _measureStart == null
                    ? "Click and drag to measure | Right-click or ESC to cancel"
                    : (_isDragging ? "Release to set measurement | Right-click or ESC to cancel" : "");
                if (!string.IsNullOrEmpty(instruction))
                {
                    var instrSize = g.MeasureString(instruction, _coordFont);
                    g.FillRectangle(bgBrush, 10, 10, instrSize.Width + 10, instrSize.Height + 6);
                    g.DrawString(instruction, _coordFont, textBrush, 15, 13);
                }
            }
            return;
        }

        // Normal measurement mode
        if (_measureStart == null)
        {
            string startInstruction = "Click and drag to measure | Right-click or ESC to cancel";
            var startSize = g.MeasureString(startInstruction, _coordFont);
            g.FillRectangle(bgBrush, 10, 10, startSize.Width + 10, startSize.Height + 6);
            g.DrawString(startInstruction, _coordFont, textBrush, 15, 13);
            return;
        }

        DrawMeasurement(g);

        string measureInstruction = _isDragging
            ? "Release to finish | Right-click or ESC to cancel"
            : "Right-click or ESC to close";
        var instrSize2 = g.MeasureString(measureInstruction, _coordFont);
        using var elementBgBrush = new SolidBrush(Color.FromArgb(Math.Min(_elementAlpha, 220), 0, 0, 0));
        using var elementTextBrush = new SolidBrush(Color.FromArgb(_elementAlpha, Color.White));
        g.FillRectangle(elementBgBrush, 10, 10, instrSize2.Width + 10, instrSize2.Height + 6);
        g.DrawString(measureInstruction, _coordFont, elementTextBrush, 15, 13);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _distanceFont.Dispose();
            _coordFont.Dispose();
            _backgroundImage?.Dispose();
        }
        base.Dispose(disposing);
    }
}
