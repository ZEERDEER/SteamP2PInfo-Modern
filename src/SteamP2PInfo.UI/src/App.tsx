import { useState } from 'react';
import { Users, Settings } from 'lucide-react';
import { useBridge } from './hooks/useBridge';
import { Header } from './components/Header';
import { PeerTable } from './components/PeerTable';
import { ConfigPanel } from './components/ConfigPanel';
import { WindowSelector } from './components/WindowSelector';
import { SteamConsoleDialog } from './components/SteamConsoleDialog';
import type { WindowInfo } from './types';

type Tab = 'session' | 'config';

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>('session');
  const [showWindowSelector, setShowWindowSelector] = useState(false);
  const [showSteamConsoleDialog, setShowSteamConsoleDialog] = useState(false);
  
  const {
    session,
    config,
    appConfig,
    isLoading,
    getWindows,
    attachGame,
    detachGame,
    updateConfig,
    updateAppConfig,
    openProfile,
    openSteamConsole,
    copyToClipboard,
    browseFile,
    showFontDialog,
  } = useBridge();

  const handleSelectWindow = async (window: WindowInfo, steamAppId?: number, debugMode?: boolean) => {
    setShowWindowSelector(false);
    const success = await attachGame(window.handle, steamAppId, debugMode);
    if (success && !debugMode) {
      // 连接成功后显示 Steam 控制台提示（调试模式不显示）
      setShowSteamConsoleDialog(true);
    }
  };

  const tabs = [
    { id: 'session' as Tab, label: '会话信息', icon: <Users size={18} /> },
    { id: 'config' as Tab, label: '设置', icon: <Settings size={18} /> },
  ];

  return (
    <div className="h-screen p-6 flex flex-col gap-6 overflow-hidden">
      {/* Header */}
      <Header
        isAttached={session?.isAttached ?? false}
        gameName={session?.gameName}
        peerCount={session?.peers.length ?? 0}
        onAttachClick={() => setShowWindowSelector(true)}
        onDetachClick={detachGame}
      />

      {/* Main Content */}
      <main className="glass rounded-2xl flex-1 overflow-hidden flex flex-col">
        {/* Tabs */}
        <div className="flex border-b border-white/10">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-6 py-4 font-medium transition-colors relative ${
                activeTab === tab.id
                  ? 'text-accent'
                  : 'text-gray-400 hover:text-white'
              }`}
            >
              {tab.icon}
              {tab.label}
              {activeTab === tab.id && (
                <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-accent" />
              )}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        <div className="flex-1 min-h-0 overflow-y-auto p-6">
          {activeTab === 'session' && (
            <PeerTable
              peers={session?.peers ?? []}
              onOpenProfile={openProfile}
            />
          )}
          
          {activeTab === 'config' && (
            <ConfigPanel
              config={config}
              appConfig={appConfig}
              onUpdate={updateConfig}
              onUpdateAppConfig={updateAppConfig}
              onBrowseFile={browseFile}
              onShowFontDialog={showFontDialog}
            />
          )}
        </div>
      </main>

      {/* Footer */}
      <footer className="text-center text-gray-600 text-sm">
        Steam P2P Info · Original by tremwil · Redesigned by zeer
      </footer>

      {/* Window Selector Modal */}
      <WindowSelector
        isOpen={showWindowSelector}
        onClose={() => setShowWindowSelector(false)}
        onSelect={handleSelectWindow}
        getWindows={getWindows}
      />

      {/* Loading Overlay */}
      {isLoading && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="w-12 h-12 border-4 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {/* Steam Console Dialog */}
      <SteamConsoleDialog
        isOpen={showSteamConsoleDialog}
        onClose={() => setShowSteamConsoleDialog(false)}
        onOpenConsole={openSteamConsole}
        onCopyCommand={copyToClipboard}
      />
    </div>
  );
}
