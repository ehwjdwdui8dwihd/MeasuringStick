using System;
using System.Windows.Forms;

namespace MeasuringStick;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        using var app = new MeasuringStickApp();
        Application.Run();
    }
}
