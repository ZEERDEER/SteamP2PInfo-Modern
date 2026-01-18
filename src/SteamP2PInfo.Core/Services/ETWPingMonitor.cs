using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Net;

namespace SteamP2PInfo.Core.Services;

public static class ETWPingMonitor
{
    private const int N_SAMPLES = 10;
    private const int STUN_SEND_SIZE = 56;
    private const int STUN_RECV_SIZE = 68;

    private class PingInfo
    {
        public double tFirstSend = -1;
        public double tStunSent = -1;
        public double tLastStunRecv = -1;
        public double ping = -1;
        public double avgPing = 0;
        public double jitter = 0;
        public double[] pingSamples = new double[N_SAMPLES];
        public int cnt = 0;
        public int stunSentCnt = 0;
        public int stunLateCnt = 0;
    }

    private static TraceEventSession? kernelSession;
    private static readonly Dictionary<ulong, PingInfo> pings = new();
    private static readonly object lockObj = new();
    private static bool isRunning = false;

    public static bool IsRunning => isRunning;

    public static bool Start()
    {
        if (isRunning) return true;
        if (!TraceEventSession.IsElevated() ?? false) return false;
        try
        {
            kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
            kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
            Task.Run(() =>
            {
                kernelSession.Source.Kernel.UdpIpSend += OnUdpSend;
                kernelSession.Source.Kernel.UdpIpRecv += OnUdpRecv;
                kernelSession.Source.Process();
            });
            isRunning = true;
            return true;
        }
        catch { return false; }
    }

    public static void Stop()
    {
        if (!isRunning) return;
        try { kernelSession?.Stop(); kernelSession?.Dispose(); kernelSession = null; } catch { }
        lock (lockObj) { pings.Clear(); }
        isRunning = false;
    }

    public static void Register(ulong netId)
    {
        lock (lockObj) { if (!pings.ContainsKey(netId)) pings[netId] = new PingInfo(); }
    }

    public static void Unregister(ulong netId)
    {
        lock (lockObj) { pings.Remove(netId); }
    }

    public static double GetPing(ulong netId)
    {
        lock (lockObj) { return pings.TryGetValue(netId, out var info) ? info.ping : -1; }
    }

    public static double GetAveragePing(ulong netId)
    {
        lock (lockObj) { return pings.TryGetValue(netId, out var info) ? info.avgPing : -1; }
    }

    public static double GetJitter(ulong netId)
    {
        lock (lockObj) { return pings.TryGetValue(netId, out var info) ? info.jitter : 0; }
    }

    private static uint IpAddressToUInt32(IPAddress addr)
    {
        var bytes = addr.GetAddressBytes();
        if (bytes.Length == 4)
            return BitConverter.ToUInt32(bytes, 0);
        return 0;
    }

    private static void OnUdpSend(Microsoft.Diagnostics.Tracing.Parsers.Kernel.UdpIpTraceData data)
    {
        if (data.size != STUN_SEND_SIZE) return;
        uint daddr = IpAddressToUInt32(data.daddr);
        ulong netId = ((ulong)data.dport << 32) | daddr;
        lock (lockObj)
        {
            if (pings.TryGetValue(netId, out var info))
            {
                double now = data.TimeStampRelativeMSec;
                if (info.tFirstSend < 0) info.tFirstSend = now;
                info.stunSentCnt++;
                
                // 与原版一致：前10秒内的包可能会被丢弃，假设这些包是late的
                if (info.tStunSent < 0 || now - info.tFirstSend < 10000)
                    info.tStunSent = now;
                else
                    info.stunLateCnt++;
            }
        }
    }

    private static void OnUdpRecv(Microsoft.Diagnostics.Tracing.Parsers.Kernel.UdpIpTraceData data)
    {
        if (data.size != STUN_RECV_SIZE) return;
        uint saddr = IpAddressToUInt32(data.saddr);
        ulong netId = ((ulong)data.sport << 32) | saddr;
        lock (lockObj)
        {
            if (pings.TryGetValue(netId, out var info))
            {
                // 与原版一致：只有当有pending的STUN包时才计算ping
                if (info.tStunSent >= 0)
                {
                    double now = data.TimeStampRelativeMSec;
                    info.tLastStunRecv = now;
                    info.ping = now - info.tStunSent;
                    
                    info.pingSamples[info.cnt % N_SAMPLES] = info.ping;
                    info.cnt++;
                    
                    // 与原版一致：在N_SAMPLES之后计算平均值和jitter
                    if (info.cnt >= N_SAMPLES)
                    {
                        double sum = 0;
                        for (int i = 0; i < N_SAMPLES; i++) sum += info.pingSamples[i];
                        info.avgPing = sum / N_SAMPLES;
                        
                        double jitterSum = 0;
                        for (int i = 0; i < N_SAMPLES; i++) 
                            jitterSum += Math.Pow(info.pingSamples[i] - info.avgPing, 2);
                        info.jitter = Math.Sqrt(jitterSum / N_SAMPLES);
                    }
                    
                    // 重置等待状态
                    info.tStunSent = -1;
                }
            }
        }
    }
}
