using System.Text.RegularExpressions;
using Steamworks;
using SteamP2PInfo.Core.Models;
using SteamP2PInfo.Core.Config;

namespace SteamP2PInfo.Core.Services;

public static class SteamPeerManager
{
    private const long PEER_TIMEOUT_MS = 5000;
    private const long STEAMID64_BASE = 0x0110_0001_0000_0000;
    
    // 调试模式：设为 true 可在不联机情况下测试 overlay
    public static bool DebugMode { get; set; } = false;
    
    private static readonly Regex STEAMID3_REGEX = new(@"\[U:1:(?<id>\d+)\]");
    
    // 调试日志文件
    private static readonly string DebugLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SteamP2PInfo", "debug.log");
    
    private static void Log(string message)
    {
        try
        {
            var logDir = Path.GetDirectoryName(DebugLogPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            File.AppendAllText(DebugLogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }
    
    private class PeerInfo
    {
        public CSteamID SteamId;
        public bool IsConnected;
        public long LastDisconnectTimeMs;
        public bool IsNewApi;
        public ulong NetIdentity;
        public SteamNetConnectionInfo_t? ConnInfo;
        public SteamNetConnectionRealTimeStatus_t? RealTimeStatus;
        public P2PSessionState_t? SessionState;
    }
    
    private static readonly Dictionary<ulong, PeerInfo> peers = new();
    private static FileStream? logFileStream;
    private static StreamReader? logReader;
    private static FileSystemWatcher? fsWatcher;
    private static bool mustReopenLog = true;
    private static long? lastPosInLog = null;
    private static bool isInitialized = false;
    private static readonly object lockObj = new();
    private static string? steamLogPath;
    
    public static bool IsInitialized => isInitialized;
    
    public static bool Initialize()
    {
        if (isInitialized) return true;
        try
        {
            var config = AppConfig.Load();
            steamLogPath = config.SteamLogPath;
            Log($"[Initialize] Steam log path: {steamLogPath}");
            
            if (!File.Exists(steamLogPath)) 
            {
                Log($"[Initialize] Log file does not exist!");
                return false;
            }
            
            var fileInfo = new FileInfo(steamLogPath);
            Log($"[Initialize] Log file size: {fileInfo.Length} bytes, Last modified: {fileInfo.LastWriteTime}");
            
            // 设置 FileSystemWatcher 监听日志文件变化（与原版一致）
            var logDir = Path.GetDirectoryName(steamLogPath);
            var logFileName = Path.GetFileName(steamLogPath);
            if (!string.IsNullOrEmpty(logDir) && !string.IsNullOrEmpty(logFileName))
            {
                fsWatcher = new FileSystemWatcher(logDir);
                fsWatcher.Filter = logFileName;
                fsWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                fsWatcher.Changed += (s, e) => 
                {
                    Log($"[FileWatcher] Log file changed, will reopen");
                    mustReopenLog = true;
                };
                fsWatcher.EnableRaisingEvents = true;
                Log($"[Initialize] FileSystemWatcher created for {logDir}\\{logFileName}");
            }
            
            // 初始化时设置为需要打开日志
            mustReopenLog = true;
            lastPosInLog = null;
            
            isInitialized = true;
            Log($"[Initialize] Initialized successfully, waiting for log file changes...");
            return true;
        }
        catch (Exception ex) 
        { 
            Log($"[Initialize] Error: {ex.Message}");
            return false; 
        }
    }
    
    public static void Shutdown()
    {
        lock (lockObj)
        {
            foreach (var peer in peers.Values)
                if (!peer.IsNewApi && peer.NetIdentity != 0)
                    ETWPingMonitor.Unregister(peer.NetIdentity);
            peers.Clear();
        }
        fsWatcher?.Dispose();
        fsWatcher = null;
        logReader?.Dispose();
        logFileStream?.Dispose();
        logReader = null;
        logFileStream = null;
        isInitialized = false;
        mustReopenLog = true;
        lastPosInLog = null;
        steamLogPath = null;
    }
    
    public static void UpdatePeerList()
    {
        if (!isInitialized || string.IsNullOrEmpty(steamLogPath)) 
        {
            Log("[SteamPeerManager] Not initialized or no log path");
            return;
        }
        
        try
        {
            // 强制 Steam 刷新日志（与原版一致）
            try { SteamFriends.SendClanChatMessage(new CSteamID(0), ""); } catch { }
        
        // 当日志文件发生变化时，重新打开文件（与原版一致）
        if (mustReopenLog)
        {
            Log("[SteamPeerManager] Reopening log file...");
            logReader?.Dispose();
            logFileStream?.Close();
            logFileStream?.Dispose();
            
            try
            {
                logFileStream = new FileStream(steamLogPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                logReader = new StreamReader(logFileStream);
                
                // 如果是首次打开，定位到文件末尾；否则从上次位置继续
                if (lastPosInLog == null)
                {
                    logFileStream.Seek(0, SeekOrigin.End);
                    Log($"[SteamPeerManager] First open, seeking to end (pos={logFileStream.Position})");
                }
                else
                {
                    logFileStream.Seek(lastPosInLog.Value, SeekOrigin.Begin);
                    Log($"[SteamPeerManager] Reopened, seeking to last pos={lastPosInLog.Value}");
                }
                
                // 必须清除 StreamReader 的内部缓冲区，否则 Seek 无效
                logReader.DiscardBufferedData();
                
                mustReopenLog = false;
            }
            catch (Exception ex)
            {
                Log($"[SteamPeerManager] Error reopening log: {ex.Message}");
                return;
            }
        }
        
        if (logReader == null || logFileStream == null)
        {
            Log("[SteamPeerManager] No log reader");
            return;
        }
        
        // 读取新日志行
        string? line;
        int lineCount = 0;
        while (!mustReopenLog && (line = logReader.ReadLine()) != null)
        {
            lineCount++;
            Log($"[SteamPeerManager] Log line: {line}");
            ProcessLogLine(line);
        }
        
        // 记录当前位置
        if (!mustReopenLog)
        {
            lastPosInLog = logFileStream.Position;
        }
        
        if (lineCount > 0)
            Log($"[SteamPeerManager] Processed {lineCount} log lines");
        
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var toRemove = new List<ulong>();
        lock (lockObj)
        {
            Log($"[SteamPeerManager] Current peers count: {peers.Count}");
            foreach (var (steamId, peer) in peers)
            {
                bool stillConnected = UpdatePeerConnection(peer);
                Log($"[SteamPeerManager] Peer {steamId}: IsConnected={peer.IsConnected}, stillConnected={stillConnected}, IsNewApi={peer.IsNewApi}");
                if (!stillConnected)
                {
                    if (peer.IsConnected) { peer.IsConnected = false; peer.LastDisconnectTimeMs = now; }
                    else if (now - peer.LastDisconnectTimeMs > PEER_TIMEOUT_MS)
                    {
                        toRemove.Add(steamId);
                        if (!peer.IsNewApi && peer.NetIdentity != 0) ETWPingMonitor.Unregister(peer.NetIdentity);
                    }
                }
                else { peer.IsConnected = true; if (GameConfig.Current?.SetPlayedWith == true) try { SteamFriends.SetPlayedWith(peer.SteamId); } catch { } }
            }
            foreach (var id in toRemove) peers.Remove(id);
        }
        }
        catch (Exception ex)
        {
            Log($"[UpdatePeerList] Exception: {ex.Message}");
        }
    }
    
    public static List<SteamPeer> GetPeers()
    {
        // 调试模式：返回假数据用于测试 overlay
        if (DebugMode)
        {
            return GetDebugPeers();
        }
        
        var result = new List<SteamPeer>();
        try
        {
            lock (lockObj)
            {
                Log($"[GetPeers] Total peers in dictionary: {peers.Count}");
                foreach (var (steamId, peer) in peers)
                {
                    try
                    {
                        Log($"[GetPeers] Checking peer {steamId}: IsConnected={peer.IsConnected}");
                        if (!peer.IsConnected) continue;
                        
                        double ping = 0, quality = 1.0; 
                        string connType = "Unknown";
                        
                        if (peer.IsNewApi)
                        {
                            // 尝试刷新连接信息
                            TryNewApi(peer);
                            if (peer.RealTimeStatus.HasValue)
                            {
                                ping = peer.RealTimeStatus.Value.m_nPing;
                                quality = peer.RealTimeStatus.Value.m_flConnectionQualityLocal;
                                connType = "SteamNetworkingSockets";
                            }
                            else
                            {
                                // 即使没有 RealTimeStatus，也显示玩家
                                connType = "SteamNetworkingSockets";
                            }
                        }
                        else if (peer.SessionState.HasValue)
                        {
                            ping = ETWPingMonitor.GetPing(peer.NetIdentity);
                            var jitter = ETWPingMonitor.GetJitter(peer.NetIdentity);
                            quality = 1.0 / (0.01 * jitter + 1.0);
                            connType = "SteamNetworking";
                        }
                        else
                        {
                            // 尝试刷新旧 API 信息
                            TryOldApi(peer);
                            if (peer.SessionState.HasValue)
                            {
                                ping = ETWPingMonitor.GetPing(peer.NetIdentity);
                                var jitter = ETWPingMonitor.GetJitter(peer.NetIdentity);
                                quality = 1.0 / (0.01 * jitter + 1.0);
                                connType = "SteamNetworking";
                            }
                        }
                        
                        string name = "";
                        try { name = SteamFriends.GetFriendPersonaName(peer.SteamId); } catch { name = steamId.ToString(); }
                        Log($"[GetPeers] Adding peer: Name={name}, Ping={ping}, ConnType={connType}");
                        
                        result.Add(new SteamPeer 
                        { 
                            SteamId = steamId, 
                            Name = name, 
                            Ping = ping, 
                            ConnectionQuality = quality, 
                            IsOldAPI = !peer.IsNewApi, 
                            ConnectionType = connType 
                        });
                    }
                    catch (Exception ex)
                    {
                        Log($"[GetPeers] Error processing peer {steamId}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[GetPeers] Exception: {ex.Message}");
        }
        Log($"[GetPeers] Returning {result.Count} peers");
        return result;
    }
    
    private static void ProcessLogLine(string line)
    {
        // 只处理当前游戏进程的日志行
        var currentProcessName = GameConfig.Current?.ProcessName;
        if (!string.IsNullOrEmpty(currentProcessName) && !line.Contains(currentProcessName, StringComparison.OrdinalIgnoreCase))
        {
            Log($"[ProcessLogLine] Skipped (wrong process): {line.Substring(0, Math.Min(100, line.Length))}");
            return;
        }
        
        var match = STEAMID3_REGEX.Match(line);
        if (!match.Success) 
        {
            Log($"[ProcessLogLine] No SteamID found in line");
            return;
        }
        var id = ulong.Parse(match.Groups["id"].Value);
        var steamId64 = STEAMID64_BASE + id;
        var cSteamId = new CSteamID(steamId64);
        
        if (line.Contains("BeginAuthSession")) 
        {
            Log($"[ProcessLogLine] BeginAuthSession detected for {steamId64}");
            AddPeer(cSteamId);
        }
        else if (line.Contains("EndAuthSession")) 
        {
            Log($"[ProcessLogLine] EndAuthSession detected for {steamId64}");
            RemovePeer(cSteamId);
        }
        else if (line.Contains("LeaveLobby")) 
        {
            Log($"[ProcessLogLine] LeaveLobby detected");
            ClearAllPeers();
        }
    }
    
    private static void AddPeer(CSteamID steamId)
    {
        lock (lockObj)
        {
            if (peers.ContainsKey(steamId.m_SteamID)) 
            {
                Log($"[AddPeer] Peer {steamId.m_SteamID} already exists");
                return;
            }
            var peer = new PeerInfo { SteamId = steamId, IsConnected = true };
            bool newApiSuccess = TryNewApi(peer);
            bool oldApiSuccess = false;
            
            if (newApiSuccess) 
            {
                peer.IsNewApi = true;
                Log($"[AddPeer] Peer {steamId.m_SteamID} connected via NEW API");
            }
            else 
            {
                oldApiSuccess = TryOldApi(peer);
                if (oldApiSuccess)
                {
                    peer.IsNewApi = false;
                    Log($"[AddPeer] Peer {steamId.m_SteamID} connected via OLD API");
                }
            }
            
            if (!newApiSuccess && !oldApiSuccess)
            {
                // 即使无法建立 P2P 连接，也添加 peer（与原版行为一致）
                Log($"[AddPeer] Peer {steamId.m_SteamID} detected but no P2P connection yet, adding anyway");
                peer.IsConnected = true;
                peer.LastDisconnectTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            
            peers[steamId.m_SteamID] = peer;
            Log($"[AddPeer] Total peers now: {peers.Count}");
            
            // 记录活动日志
            string peerName = "";
            try { peerName = SteamFriends.GetFriendPersonaName(steamId); } catch { peerName = steamId.m_SteamID.ToString(); }
            Logger.LogPeerConnected(steamId.m_SteamID, peerName);
        }
    }
    
    private static void RemovePeer(CSteamID steamId)
    {
        lock (lockObj)
        {
            if (peers.TryGetValue(steamId.m_SteamID, out var peer))
            { 
                peer.IsConnected = false; 
                peer.LastDisconnectTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // 记录活动日志
                string peerName = "";
                try { peerName = SteamFriends.GetFriendPersonaName(steamId); } catch { peerName = steamId.m_SteamID.ToString(); }
                Logger.LogPeerDisconnected(steamId.m_SteamID, peerName);
            }
        }
    }
    
    private static void ClearAllPeers()
    {
        lock (lockObj)
        {
            foreach (var peer in peers.Values)
                if (!peer.IsNewApi && peer.NetIdentity != 0) ETWPingMonitor.Unregister(peer.NetIdentity);
            peers.Clear();
        }
    }
    
    private static bool TryNewApi(PeerInfo peer)
    {
        try
        {
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID(peer.SteamId);
            var state = SteamNetworkingMessages.GetSessionConnectionInfo(ref identity, out var connInfo, out var realTimeStatus);
            // 检查连接状态：包括 Connecting, Connected, FindingRoute
            if (state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected || 
                state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting ||
                state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute)
            { peer.ConnInfo = connInfo; peer.RealTimeStatus = realTimeStatus; return true; }
        }
        catch { }
        return false;
    }
    
    private static bool TryOldApi(PeerInfo peer)
    {
        try
        {
            if (SteamNetworking.GetP2PSessionState(peer.SteamId, out var session) && IsSessionStateOK(session))
            {
                bool endpointChanged = !peer.SessionState.HasValue || 
                    peer.SessionState.Value.m_nRemoteIP != session.m_nRemoteIP || 
                    peer.SessionState.Value.m_nRemotePort != session.m_nRemotePort;
                
                peer.SessionState = session;
                
                if (endpointChanged)
                {
                    ETWPingMonitor.Unregister(peer.NetIdentity);
                    // 修复字节序：Steam的m_nRemoteIP是网络字节序，需要反转以匹配ETW事件中的IP格式
                    byte[] ipBytes = BitConverter.GetBytes(session.m_nRemoteIP).Reverse().ToArray();
                    peer.NetIdentity = ((ulong)session.m_nRemotePort << 32) | BitConverter.ToUInt32(ipBytes, 0);
                    ETWPingMonitor.Register(peer.NetIdentity);
                    Log($"[TryOldApi] Registered NetIdentity: {peer.NetIdentity:X16} for peer {peer.SteamId.m_SteamID}");
                }
                return true;
            }
        }
        catch (Exception ex) { Log($"[TryOldApi] Exception: {ex.Message}"); }
        return false;
    }
    
    /// <summary>
    /// 检查 P2P Session 状态是否有效（与原版一致）
    /// </summary>
    private static bool IsSessionStateOK(P2PSessionState_t session)
    {
        return session.m_eP2PSessionError == 0 && (session.m_bConnecting != 0 || session.m_bConnectionActive != 0);
    }
    
    private static bool UpdatePeerConnection(PeerInfo peer)
    {
        try
        {
            if (peer.IsNewApi) return TryNewApi(peer);
            if (SteamNetworking.GetP2PSessionState(peer.SteamId, out var session) && IsSessionStateOK(session))
            {
                bool endpointChanged = !peer.SessionState.HasValue || 
                    peer.SessionState.Value.m_nRemoteIP != session.m_nRemoteIP || 
                    peer.SessionState.Value.m_nRemotePort != session.m_nRemotePort;
                
                peer.SessionState = session;
                
                if (endpointChanged)
                {
                    ETWPingMonitor.Unregister(peer.NetIdentity);
                    // 修复字节序：Steam的m_nRemoteIP是网络字节序，需要反转以匹配ETW事件中的IP格式
                    byte[] ipBytes = BitConverter.GetBytes(session.m_nRemoteIP).Reverse().ToArray();
                    peer.NetIdentity = ((ulong)session.m_nRemotePort << 32) | BitConverter.ToUInt32(ipBytes, 0);
                    ETWPingMonitor.Register(peer.NetIdentity);
                }
                return true;
            }
        }
        catch { }
        return false;
    }
    
    /// <summary>
    /// 调试模式下返回假玩家数据，用于测试 overlay 显示效果
    /// </summary>
    private static List<SteamPeer> GetDebugPeers()
    {
        var random = new Random();
        return new List<SteamPeer>
        {
            new SteamPeer
            {
                SteamId = 76561198012345678,
                Name = "SunBro_420",
                Ping = 35 + random.Next(-5, 10),
                ConnectionQuality = 0.95,
                IsOldAPI = false,
                ConnectionType = "SteamNetworkingSockets"
            },
            new SteamPeer
            {
                SteamId = 76561198087654321,
                Name = "DarkMoon_Knight",
                Ping = 85 + random.Next(-10, 20),
                ConnectionQuality = 0.82,
                IsOldAPI = false,
                ConnectionType = "SteamNetworkingSockets"
            },
            new SteamPeer
            {
                SteamId = 76561198011111111,
                Name = "Patches_The_Hyena",
                Ping = 180 + random.Next(-20, 40),
                ConnectionQuality = 0.55,
                IsOldAPI = true,
                ConnectionType = "SteamNetworking"
            },
            new SteamPeer
            {
                SteamId = 76561198099999999,
                Name = "Solaire_of_Astora",
                Ping = 250 + random.Next(-30, 50),
                ConnectionQuality = 0.35,
                IsOldAPI = false,
                ConnectionType = "SteamNetworkingSockets"
            }
        };
    }
}
