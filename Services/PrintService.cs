using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhotoBoothApp.Services;

public static class PrintService
{
    public static void PrintImage(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                Verb = "print",
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("lp", filePath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Open in Preview which allows printing
            Process.Start("open", $"-a Preview \"{filePath}\"");
        }
    }
}
