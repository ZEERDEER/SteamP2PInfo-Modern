using System.Windows;
using WpfApplication = System.Windows.Application;

namespace SteamP2PInfo.App;

public partial class App : WpfApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 设置 DPI 感知
        SetProcessDpiAwareness();
    }
    
    private static void SetProcessDpiAwareness()
    {
        try
        {
            // Windows 10 1703+
            NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        }
        catch
        {
            try
            {
                // Windows 8.1+
                NativeMethods.SetProcessDpiAwareness(NativeMethods.PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
            }
            catch { }
        }
    }
}

internal static class NativeMethods
{
    public enum PROCESS_DPI_AWARENESS
    {
        Process_DPI_Unaware = 0,
        Process_System_DPI_Aware = 1,
        Process_Per_Monitor_DPI_Aware = 2
    }
    
    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);
    
    [System.Runtime.InteropServices.DllImport("shcore.dll")]
    public static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
}
