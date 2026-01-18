using SteamP2PInfo.Core.Config;

namespace SteamP2PInfo.Core.Services;

/// <summary>
/// 活动日志记录器，用于记录玩家连接/断开事件
/// </summary>
public static class Logger
{
    private static StreamWriter? _writer;
    private static DateTime _lastLogCreated;
    private static string _lastLoggedGame = "";
    
    private static void CreateOrOpenLogFile()
    {
        var dateTime = DateTime.Now;
        var processName = GameConfig.Current?.ProcessName ?? "unknown";
        
        if (_writer == null || _lastLogCreated.Day != dateTime.Day || _lastLoggedGame != processName)
        {
            _writer?.Close();
            _writer?.Dispose();
            
            var gameLogDir = AppPaths.GetGameLogDir(processName);
            
            var logPath = Path.Combine(gameLogDir, $"{processName}-{dateTime:yyyy-MM-dd}.log");
            _writer = File.AppendText(logPath);
            _writer.AutoFlush = true;
            _lastLogCreated = dateTime;
            _lastLoggedGame = processName;
        }
    }
    
    public static void Write(string message)
    {
        if (GameConfig.Current == null || !GameConfig.Current.LogActivity) return;
        
        try
        {
            CreateOrOpenLogFile();
            _writer?.Write($"[{DateTime.Now:HH:mm:ss.ff}] {message}");
        }
        catch { }
    }
    
    public static void WriteLine(string message)
    {
        if (GameConfig.Current == null || !GameConfig.Current.LogActivity) return;
        
        try
        {
            CreateOrOpenLogFile();
            _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.ff}] {message}");
        }
        catch { }
    }
    
    public static void LogPeerConnected(ulong steamId, string name)
    {
        WriteLine($"CONNECTED: {name} (SteamID: {steamId})");
    }
    
    public static void LogPeerDisconnected(ulong steamId, string name)
    {
        WriteLine($"DISCONNECTED: {name} (SteamID: {steamId})");
    }
    
    public static void Shutdown()
    {
        _writer?.Close();
        _writer?.Dispose();
        _writer = null;
    }
}
