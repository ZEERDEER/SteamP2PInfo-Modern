using Steamworks;

namespace SteamP2PInfo.Core.Models;

/// <summary>
/// Steam 玩家信息模型（用于 UI 展示）
/// </summary>
public class SteamPeer
{
    public ulong SteamId64 { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Ping { get; set; }
    public double ConnectionQuality { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    public string PingColor { get; set; } = "#FFFFFFFF";
}
