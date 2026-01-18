import { useState } from 'react';
import { Terminal, Copy, Check, X } from 'lucide-react';

const STEAM_COMMAND = 'log_ipc "BeginAuthSession,EndAuthSession,LeaveLobby,SendClanChatMessage"';

interface SteamConsoleDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onOpenConsole: () => Promise<void>;
  onCopyCommand: (text: string) => Promise<void>;
}

export function SteamConsoleDialog({ isOpen, onClose, onOpenConsole, onCopyCommand }: SteamConsoleDialogProps) {
  const [copied, setCopied] = useState(false);

  if (!isOpen) return null;

  const handleCopyCommand = async () => {
    await onCopyCommand(STEAM_COMMAND);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleOpenConsoleAndCopy = async () => {
    await onOpenConsole();
    await handleCopyCommand();
  };

  return (
    <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 animate-fade-in">
      <div className="bg-[#1a1a2e] border border-white/10 rounded-2xl w-[500px] max-w-[90vw] overflow-hidden shadow-2xl">
        {/* Header */}
        <div className="bg-accent/20 px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-accent/30 flex items-center justify-center">
              <Terminal className="text-accent" size={24} />
            </div>
            <h2 className="text-xl font-bold text-white">必要步骤</h2>
          </div>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-lg bg-white/10 flex items-center justify-center hover:bg-white/20 transition-colors"
          >
            <X size={18} className="text-gray-400" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-4">
          <p className="text-gray-300 leading-relaxed">
            Steam 控制台已打开。请在控制台中输入以下命令以启用匹配日志记录：
          </p>

          {/* Command Box */}
          <div className="bg-black/40 border border-white/10 rounded-xl p-4">
            <code className="text-accent font-mono text-sm break-all">
              {STEAM_COMMAND}
            </code>
          </div>

          <p className="text-gray-500 text-sm">
            此命令告诉 Steam 记录 P2P 连接事件，这是程序检测其他玩家的必要条件。
          </p>
        </div>

        {/* Actions */}
        <div className="px-6 pb-6 flex gap-3">
          <button
            onClick={handleOpenConsoleAndCopy}
            className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-accent text-white rounded-xl font-medium hover:bg-accent/90 transition-colors"
          >
            <Terminal size={18} />
            打开控制台并复制
          </button>
          <button
            onClick={handleCopyCommand}
            className={`flex items-center justify-center gap-2 px-4 py-3 rounded-xl font-medium transition-colors ${
              copied
                ? 'bg-green-600 text-white'
                : 'bg-white/10 text-gray-300 hover:bg-white/20'
            }`}
          >
            {copied ? <Check size={18} /> : <Copy size={18} />}
            {copied ? '已复制' : '复制'}
          </button>
        </div>

        {/* Footer */}
        <div className="px-6 pb-4">
          <button
            onClick={onClose}
            className="w-full py-2 text-gray-500 hover:text-gray-300 transition-colors text-sm"
          >
            稍后手动操作
          </button>
        </div>
      </div>
    </div>
  );
}
