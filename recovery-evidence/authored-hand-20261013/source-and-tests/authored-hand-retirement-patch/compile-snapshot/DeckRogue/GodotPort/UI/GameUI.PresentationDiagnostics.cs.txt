using System;
using Godot;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI {
 Action<bool> _setPresentationDiagnostics;Func<bool> _presentationDiagnosticsEnabled;Func<string> _presentationDiagnosticsLastFile;
 string _presentationDiagnosticStatus="描画診断: オフ";
 public void SetPresentationDiagnostics(Action<bool> set,Func<bool> enabled,Func<string> lastFile){_setPresentationDiagnostics=set;_presentationDiagnosticsEnabled=enabled;_presentationDiagnosticsLastFile=lastFile;}
 public void SetPresentationDiagnosticStatus(string value)=>_presentationDiagnosticStatus=value;
 void BuildPresentationDiagnosticsControls(Control box){if(_setPresentationDiagnostics==null)return;
  bool active=_presentationDiagnosticsEnabled?.Invoke()==true;
  var status=Text(box,_presentationDiagnosticStatus,17,Muted);
  Button(box,active?"描画診断を終了して保存":"描画診断を開始（画像取得なし）",()=>{_setPresentationDiagnostics(!(_presentationDiagnosticsEnabled?.Invoke()==true));status.Text=_presentationDiagnosticStatus;CloseOverlay();OpenSettings();});
  Text(box,"通常のフレーム間隔・描画品質・コマ送り・停止時間を記録します。画面やセーブ内容は保存しません。次の起動ではオフです。",16,Muted);
 }
}
