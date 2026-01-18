import { Users, Wifi, Signal, ExternalLink } from 'lucide-react';
import type { SteamPeer } from '../types';

interface PeerTableProps {
  peers: SteamPeer[];
  onOpenProfile: (steamId: string) => void;
}

export function PeerTable({ peers, onOpenProfile }: PeerTableProps) {
  if (peers.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-500">
        <Users size={48} className="mb-4 opacity-50" />
        <p className="text-lg">等待玩家加入...</p>
        <p className="text-sm mt-2">当有玩家连接时会显示在这里</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full">
        <thead>
          <tr className="border-b border-white/10">
            <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">
              <div className="flex items-center gap-2">
                <Users size={16} />
                玩家名称
              </div>
            </th>
            <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">
              <div className="flex items-center justify-center gap-2">
                <Wifi size={16} />
                延迟
              </div>
            </th>
            <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">
              <div className="flex items-center justify-center gap-2">
                <Signal size={16} />
                连接质量
              </div>
            </th>
            <th className="text-right py-3 px-4 text-gray-400 font-medium text-sm">
              操作
            </th>
          </tr>
        </thead>
        <tbody>
          {peers.map((peer, index) => (
            <tr
              key={peer.steamId}
              className="border-b border-white/5 hover:bg-white/5 transition-colors animate-fade-in"
              style={{ animationDelay: `${index * 50}ms` }}
            >
              <td className="py-4 px-4">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-accent to-accent-dark flex items-center justify-center text-white font-bold">
                    {peer.name.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <div className="font-medium text-white">{peer.name}</div>
                    <div className="text-xs text-gray-500">{peer.connectionType}</div>
                  </div>
                </div>
              </td>
              <td className="py-4 px-4 text-center">
                <span
                  className="font-mono font-bold text-lg"
                  style={{ color: peer.pingColor }}
                >
                  {peer.ping.toFixed(0)}
                </span>
                <span className="text-gray-500 text-sm ml-1">ms</span>
              </td>
              <td className="py-4 px-4 text-center">
                <div className="flex items-center justify-center gap-2">
                  <div className="w-24 h-2 bg-gray-700 rounded-full overflow-hidden">
                    <div
                      className="h-full rounded-full transition-all duration-500"
                      style={{
                        width: `${peer.connectionQuality * 100}%`,
                        backgroundColor: peer.connectionQuality > 0.7 ? '#7CFC00' : peer.connectionQuality > 0.4 ? '#FFFF00' : '#CD5C5C',
                      }}
                    />
                  </div>
                  <span className="text-gray-400 text-sm w-12">
                    {(peer.connectionQuality * 100).toFixed(0)}%
                  </span>
                </div>
              </td>
              <td className="py-4 px-4 text-right">
                <button
                  onClick={() => onOpenProfile(peer.steamId)}
                  className="p-2 rounded-lg hover:bg-accent/20 text-gray-400 hover:text-accent transition-colors"
                  title="查看 Steam 资料"
                >
                  <ExternalLink size={18} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
