using System.Runtime.InteropServices;

namespace SteamP2PInfo.Core.Services;

/// <summary>
/// 全局热键管理器
/// </summary>
public class GlobalHotkey : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    private const int HOTKEY_ID = 9000;
    
    private IntPtr _hwnd;
    private int _currentVk;
    private bool _isRegistered;
    
    public event Action? HotkeyPressed;
    
    public void Register(IntPtr hwnd, int virtualKey)
    {
        if (virtualKey <= 0) return;
        
        // 先注销之前的热键
        Unregister();
        
        _hwnd = hwnd;
        _currentVk = virtualKey;
        
        // 注册热键（无修饰键）
        _isRegistered = RegisterHotKey(hwnd, HOTKEY_ID, 0, (uint)virtualKey);
    }
    
    public void Unregister()
    {
        if (_isRegistered && _hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
            _isRegistered = false;
        }
    }
    
    public void ProcessMessage(IntPtr wParam, IntPtr lParam)
    {
        if (wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
        }
    }
    
    public void Dispose()
    {
        Unregister();
    }
}
