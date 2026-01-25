namespace SteamP2PInfo.Core.Services;

/// <summary>
/// P2P 玩家信息包装类（完全复制原版逻辑）
/// </summary>
internal class SteamPeerInfo
{
    internal SteamPeerBase? peer = null;
    internal bool isConnected;
    internal long lastDisconnectTimeMS = 0;

    internal SteamPeerInfo(SteamPeerBase? peer)
    {
        this.peer = peer;
        isConnected = peer != null;
    }
}
