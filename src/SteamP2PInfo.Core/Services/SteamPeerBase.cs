using Steamworks;
using SteamP2PInfo.Core.Config;

namespace SteamP2PInfo.Core.Services;

/// <summary>
/// Steam P2P 玩家的抽象基类（完全复制原版逻辑）
/// </summary>
public abstract class SteamPeerBase : IDisposable
{
    /// <summary>
    /// 玩家的 Steam ID
    /// </summary>
    public CSteamID SteamID { get; protected set; }

    /// <summary>
    /// 玩家的 Steam 昵称
    /// </summary>
    public virtual string Name => SteamFriends.GetFriendPersonaName(SteamID);

    /// <summary>
    /// 是否使用旧版 API (ISteamNetworking)
    /// </summary>
    public abstract bool IsOldAPI { get; }

    /// <summary>
    /// 连接类型名称
    /// </summary>
    public abstract string ConnectionTypeName { get; }

    /// <summary>
    /// 延迟（毫秒）
    /// </summary>
    public abstract double Ping { get; }

    /// <summary>
    /// 连接质量 (0-1)
    /// </summary>
    public abstract double ConnectionQuality { get; }

    /// <summary>
    /// 根据延迟获取颜色（与原版一致）
    /// </summary>
    public string PingColor
    {
        get
        {
            var config = GameConfig.Current?.Overlay;
            if (config == null) return "#FFFFFFFF";
            
            PingColorRange range = new()
            {
                Threshold = double.NegativeInfinity,
                Color = config.TextColor
            };

            foreach (var r in config.PingColors)
            {
                if (r.Threshold <= Ping && r.Threshold > range.Threshold)
                    range = r;
            }

            return range.Color;
        }
    }

    protected SteamPeerBase(CSteamID steamID)
    {
        SteamID = steamID;
    }

    /// <summary>
    /// 更新玩家信息，返回 true 表示仍然连接
    /// </summary>
    public abstract bool UpdatePeerInfo();

    public virtual void Dispose() { }
}
