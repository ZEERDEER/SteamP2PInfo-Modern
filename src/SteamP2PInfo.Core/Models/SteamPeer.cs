using Steamworks;

namespace SteamP2PInfo.Core.Models;

/// <summary>
/// Steam 玩家信息模型
/// </summary>
public class SteamPeer
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Ping { get; set; }
    public double ConnectionQuality { get; set; }
    public bool IsOldAPI { get; set; }
    public string ConnectionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 根据 Ping 值获取颜色（用于前端显示）
    /// 绿色(最佳) -> 蓝色 -> 橙色 -> 红色(最差)
    /// </summary>
    public string PingColor => Ping switch
    {
        < 50 => "#7CFC00",   // 绿色 - 极佳
        < 100 => "#00BFFF",  // 蓝色 - 良好
        < 200 => "#FFA500",  // 橙色 - 一般
        _ => "#CD5C5C"       // 红色 - 较差
    };
}
