using System.Diagnostics;
using System.Text;
using SteamP2PInfo.Core.WinAPI;

namespace SteamP2PInfo.Core.Services;

/// <summary>
/// 窗口信息
/// </summary>
public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public uint ThreadId { get; set; }
}

/// <summary>
/// 窗口服务 - 枚举和管理窗口
/// </summary>
public static class WindowService
{
    /// <summary>
    /// 获取所有可见窗口列表
    /// </summary>
    public static List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        var shellWindow = User32.GetShellWindow();

        User32.EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == shellWindow) return true;
            if (!User32.IsWindowVisible(hWnd)) return true;

            int length = User32.GetWindowTextLength(hWnd);
            if (length == 0) return true;

            var builder = new StringBuilder(length + 1);
            User32.GetWindowText(hWnd, builder, builder.Capacity);
            var title = builder.ToString();

            if (string.IsNullOrWhiteSpace(title)) return true;

            uint threadId = User32.GetWindowThreadProcessId(hWnd, out uint processId);

            try
            {
                var process = Process.GetProcessById((int)processId);
                windows.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = process.ProcessName,
                    ProcessId = processId,
                    ThreadId = threadId
                });
            }
            catch { }

            return true;
        }, 0);

        return windows.OrderBy(w => w.Title).ToList();
    }

    /// <summary>
    /// 检查窗口是否仍然存在
    /// </summary>
    public static bool IsWindowAlive(IntPtr handle) => User32.IsWindow(handle);

    /// <summary>
    /// 检查窗口是否在前台
    /// </summary>
    public static bool IsWindowForeground(IntPtr handle) => User32.GetForegroundWindow() == handle;
}
