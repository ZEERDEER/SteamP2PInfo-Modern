using Steamworks;

namespace SteamP2PInfo.Core.Services;

/// <summary>
/// 使用旧版 ISteamNetworking API 的 P2P 玩家（完全复制原版逻辑）
/// </summary>
public class SteamPeerOldAPI : SteamPeerBase
{
    /// <summary>
    /// IP/端口组合，用于 ETW ping 监控
    /// </summary>
    private ulong mNetIdentity;

    /// <summary>
    /// P2P 会话状态
    /// </summary>
    private P2PSessionState_t mSessionState;

    public override bool IsOldAPI => true;

    public override string ConnectionTypeName => "SteamNetworking";

    public override double Ping => ETWPingMonitor.GetPing(mNetIdentity);

    public override double ConnectionQuality => 1d / (0.01d * ETWPingMonitor.GetJitter(mNetIdentity) + 1d);

    public SteamPeerOldAPI(CSteamID steamId) : base(steamId)
    {
        mSessionState = new P2PSessionState_t();
    }

    public override void Dispose()
    {
        ETWPingMonitor.Unregister(mNetIdentity);
    }

    public override bool UpdatePeerInfo()
    {
        if (!SteamNetworking.GetP2PSessionState(SteamID, out P2PSessionState_t session) || !IsSessionStateOK(session))
            return false;

        bool endpointChanged = mSessionState.m_nRemoteIP != session.m_nRemoteIP || 
                               mSessionState.m_nRemotePort != session.m_nRemotePort;
        mSessionState = session;

        if (endpointChanged)
        {
            ETWPingMonitor.Unregister(mNetIdentity);

            // 与原版一致：修复字节序
            byte[] ipBytes = BitConverter.GetBytes(mSessionState.m_nRemoteIP).Reverse().ToArray();
            mNetIdentity = (ulong)mSessionState.m_nRemotePort << 32 | BitConverter.ToUInt32(ipBytes, 0);
            ETWPingMonitor.Register(mNetIdentity);
        }
        return true;
    }

    public static bool IsSessionStateOK(P2PSessionState_t session)
    {
        return session.m_eP2PSessionError == 0 && (session.m_bConnecting != 0 || session.m_bConnectionActive != 0);
    }
}
