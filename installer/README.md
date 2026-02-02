# Windowsインストーラー作成手順

1) 事前にInno Setupをインストールします。
2) ルートの publish-desktop-win-x64.bat を実行して発行します。
3) Inno Setupで installer/LPEditorApp.Desktop.iss を開き、Compileします。
4) 出力された LPEditorApp-Setup.exe を配布します。

## 注意
- WebView2 Runtime がPCに必要です（未導入だと起動時に案内メッセージが表示されます）。
