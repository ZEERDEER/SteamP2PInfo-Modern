namespace SteamP2PInfo.Core.Models;

/// <summary>
/// 游戏会话状态
/// </summary>
public class GameSession
{
    public bool IsAttached { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int SteamAppId { get; set; }
    public IntPtr WindowHandle { get; set; }
    public List<SteamPeer> Peers { get; set; } = new();
    public DateTime LastUpdate { get; set; }
}
