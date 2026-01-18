export interface SteamPeer {
  steamId: string;
  name: string;
  ping: number;
  connectionQuality: number;
  isOldAPI: boolean;
  connectionType: string;
  pingColor: string;
}

export interface GameSession {
  isAttached: boolean;
  gameName: string;
  processName: string;
  steamAppId: number;
  peers: SteamPeer[];
  lastUpdate: string;
}

export interface WindowInfo {
  handle: string;
  title: string;
  processName: string;
  processId: number;
  threadId: number;
}

export interface AppConfig {
  steamLogPath: string;
  steamBootstrapLogPath: string;
  theme: string;
  accentColor: string;
}

export interface GameConfig {
  processName: string;
  steamAppId: number;
  setPlayedWith: boolean;
  openProfileInOverlay: boolean;
  logActivity: boolean;
  hotkeysEnabled: boolean;
  playSoundOnNewSession: boolean;
  overlay: OverlayConfig;
}

export interface OverlayConfig {
  enabled: boolean;
  showSteamId: boolean;
  showConnectionQuality: boolean;
  hotkey: number;
  bannerFormat: string;
  font: string;
  xOffset: number;
  yOffset: number;
  anchor: string;
  textColor: string;
  strokeColor: string;
  strokeWidth: number;
}

// WebView2 桥接接口
declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: string) => void;
        addEventListener: (event: string, handler: (e: { data: string }) => void) => void;
      };
    };
  }
}
