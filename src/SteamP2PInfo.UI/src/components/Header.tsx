import { Wifi, WifiOff, Settings, Gamepad2 } from 'lucide-react';

interface HeaderProps {
  isAttached: boolean;
  gameName?: string;
  peerCount: number;
  onAttachClick: () => void;
  onDetachClick: () => void;
}

export function Header({ isAttached, gameName, peerCount, onAttachClick, onDetachClick }: HeaderProps) {
  return (
    <header className="glass rounded-2xl px-6 py-4 flex items-center justify-between">
      <div className="flex items-center gap-4">
        {/* Logo */}
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-accent to-accent-dark flex items-center justify-center shadow-lg glow">
          <Wifi className="text-white" size={24} />
        </div>
        
        <div>
          <h1 className="text-xl font-bold text-white">Steam P2P Info</h1>
          <p className="text-sm text-gray-400">
            {isAttached ? (
              <span className="flex items-center gap-2">
                <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
                {gameName} · {peerCount} 名玩家
              </span>
            ) : (
              '未连接游戏'
            )}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        {isAttached ? (
          <button
            onClick={onDetachClick}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-red-500/20 text-red-400 hover:bg-red-500/30 transition-colors"
          >
            <WifiOff size={18} />
            <span className="hidden sm:inline">断开连接</span>
          </button>
        ) : (
          <button
            onClick={onAttachClick}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-accent hover:bg-accent-light text-white font-medium transition-colors shadow-lg"
          >
            <Gamepad2 size={18} />
            <span>连接游戏</span>
          </button>
        )}
      </div>
    </header>
  );
}
