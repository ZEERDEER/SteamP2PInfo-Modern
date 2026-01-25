using Steamworks;

namespace SteamP2PInfo.Core.Services;

/// <summary>
/// 使用新版 ISteamNetworkingMessages API 的 P2P 玩家（完全复制原版逻辑）
/// </summary>
public class SteamPeerNewAPI : SteamPeerBase
{
    /// <summary>
    /// 连接信息
    /// </summary>
    private SteamNetConnectionInfo_t mConnInfo;

    /// <summary>
    /// 实时状态信息（包括延迟）
    /// </summary>
    private SteamNetConnectionRealTimeStatus_t mRealTimeStatus;

    public override bool IsOldAPI => false;

    public override string ConnectionTypeName => "SteamNetworkingSockets";

    public override double Ping => mRealTimeStatus.m_nPing;

    public override double ConnectionQuality => mRealTimeStatus.m_flConnectionQualityLocal;

    public SteamPeerNewAPI(CSteamID steamId) : base(steamId)
    {
        mConnInfo = new SteamNetConnectionInfo_t();
        mRealTimeStatus = new SteamNetConnectionRealTimeStatus_t();
    }

    public override bool UpdatePeerInfo()
    {
        SteamNetworkingIdentity networkingIdentity = new();
        networkingIdentity.SetSteamID(SteamID);

        var connState = SteamNetworkingMessages.GetSessionConnectionInfo(ref networkingIdentity, out mConnInfo, out mRealTimeStatus);
        return IsConnStateOK(connState);
    }

    public static bool IsConnStateOK(ESteamNetworkingConnectionState connState)
    {
        return connState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting ||
               connState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;
    }
}
