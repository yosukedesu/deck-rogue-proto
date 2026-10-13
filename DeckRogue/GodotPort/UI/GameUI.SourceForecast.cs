// Source IncomingTexts/ApplyIncomingText/LethalEdge from Stage41 BattleScreen.
#nullable enable
using System;
using Godot;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private void RefreshSourceForecast(GameState s)
    {
        if(_uxForecast==null||!GodotObject.IsInstanceValid(_uxForecast)||_uxDetail==null||!GodotObject.IsInstanceValid(_uxDetail))return;
        var p=s.Player;int defense=p.Block+p.IceBlock,left=Math.Max(0,_uxIncoming-defense);
        int small=(int)Math.Round((PhoneLayout?18:20)*(PhoneLayout?.85:.75)*BattleUnit*_textScale);
        bool hidden=s.HideIntents==true,lethal=!hidden&&_uxIncoming>0&&p.Hp-left<=0;
        string little(string text)=>$"[font_size={small}]{text}[/font_size]";
        string colored(string hex,string text)=>$"[color=#{hex}]{text}[/color]";
        if(hidden){_uxForecast.Text=little("受けるダメージ ？");_uxDetail.Text="ルーンの円蓋で見えない";}
        else if(_uxIncoming<=0){_uxForecast.Text=little(_uxLanded?"この番の攻撃は終わった":"この番は攻撃されない");_uxDetail.Text="";}
        else
        {
            _uxForecast.Text=lethal?$"HP {Math.Max(0,p.Hp)} → 0 致死":$"HP {p.Hp} → {p.Hp-left}"+little(colored(left>0?"f2a193":"a6d99a",$"（−{left}）"));
            _uxDetail.Text=(PhoneLayout?"受ける ":"受けるダメージ ")+colored("f2a193",$"[b]{_uxIncoming}[/b]")+(defense>0?" − ブロック "+colored("a8c9ea",$"[b]{defense}[/b]"):"");
        }
        _uxForecast.AddThemeColorOverride("default_color",lethal?new Color("f2a193"):SourcePalette.Paper);
        _uxDetail.AddThemeColorOverride("default_color",SourcePalette.PaperDim);
        _uxDetail.AddThemeFontOverride("bold_font",GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf"));
        if(_uxForecast.GetParent() is Control body&&body.GetParent() is Control ledger)
        {
            var edge=body.GetNodeOrNull<Panel>("SourceLethalEdge");
            if(edge==null){edge=new Panel{Name="SourceLethalEdge",ZIndex=-1,MouseFilter=MouseFilterEnum.Ignore};edge.AddThemeStyleboxOverride("panel",SourceBattleDecor.Box(Colors.Transparent,new Color("c9635a"),BattleUnit,3));body.AddChild(edge);}
            edge.Position=Vector2.One*-3*BattleUnit;edge.Size=ledger.Size+Vector2.One*6*BattleUnit;edge.Visible=lethal;
        }
    }
}
