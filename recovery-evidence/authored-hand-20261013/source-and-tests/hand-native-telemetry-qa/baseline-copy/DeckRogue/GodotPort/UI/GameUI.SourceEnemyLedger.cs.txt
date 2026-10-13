// Source-backed ledger construction. Semantic content belongs to the captured board;
// receipt-driven numeric HP/block may change without rebuilding these labels.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private SourceEnemyHudSnapshot _uxEnemyHudSnapshot;
    private long _uxEnemyHudBoardRevision;
    private float _uxLedgerHeightWidth=-1,_uxLedgerHeightScale=-1,_uxLedgerHeight;
    private bool _uxLedgerHeightPhone;
    private readonly Dictionary<int,SourceLedgerSeat> _uxLedgerSeats=new();
    private sealed class SourceLedgerSeat
    {
        public SourceEnemyHudSnapshot Snapshot; public string Id; public Control Body,Mask,Forecast;
        public SourceHudLabel Name; public IReadOnlyList<SourceEnemyLedgerModel.Pill> Pills;
        public float Mark; public string LayoutKey; public ColorRect Marker,MarkerCore;
    }
    private void CaptureSourceEnemyLedger(GameState state)
    {
        _uxEnemyHudSnapshot=SourceEnemyHudSnapshot.SelectForBuild(_uxEnemyHudSnapshot,state,_session?.CurrentRun?.Combat,_uxEpoch,_activeChange?.Revision??-1,IsInputBlocked);
        _uxEnemyHudBoardRevision=_uxEnemyHudSnapshot.BoardRevision;
        _uxLedgerSeats.Clear();_uxLedgerHeightWidth=-1;
        // The existing canvas owns lifetime. Deferred teardown of an older canvas
        // must not clear a newer same-revision capture created by a rebuild.
        var capture=_uxEnemyHudSnapshot;
        _uxBattle.TreeExiting+=()=>
        {
            if(!ReferenceEquals(_uxEnemyHudSnapshot,capture))return;
            _uxEnemyHudSnapshot=null;_uxLedgerSeats.Clear();_uxLedgerHeightWidth=-1;
        };
    }
    private void BuildSourceEnemyLedger(Control body,int index,string id,SourceHealthBar bar,SourceHudLabel hp)
    {
        if(!_uxEnemyHudSnapshot.TryRead(_uxEnemyHudSnapshot,_uxEpoch,_uxEnemyHudBoardRevision,index,id,out var state,out var enemy))return;
        var def=Content.GetEnemyDef(id);
        var name=new SourceHudLabel{Name="EnemyName_"+index,MouseFilter=MouseFilterEnum.Ignore,ClipText=true,TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis,AutowrapMode=TextServer.AutowrapMode.Off};body.AddChild(name);
        var mask=new Control{Name="EnemyStatusMask_"+index,ClipContents=true,MouseFilter=MouseFilterEnum.Ignore};body.AddChild(mask);
        var forecast=new Control{Name="EnemyForecast_"+index,ClipContents=true,MouseFilter=MouseFilterEnum.Ignore};body.AddChild(forecast);
        var marker=new ColorRect{Name="EnemyInterruptMark_"+index,Color=SourcePalette.Ink,MouseFilter=MouseFilterEnum.Ignore,Visible=false};bar.AddChild(marker);bar.MoveChild(marker,0);
        var core=new ColorRect{Name="MarkCore",Color=SourcePalette.Brass,MouseFilter=MouseFilterEnum.Ignore};marker.AddChild(core);
        hp.ZIndex=1;
        _uxLedgerSeats[index]=new SourceLedgerSeat{Snapshot=_uxEnemyHudSnapshot,Id=id,Body=body,Name=name,Mask=mask,Forecast=forecast,Pills=SourceEnemyLedgerModel.Pills(state,index,def),Mark=SourceEnemyLedgerModel.Mark(def,enemy),Marker=marker,MarkerCore=core};
    }
    private bool ReadSourceEnemyLedger(int index,out SourceLedgerSeat seat,out GameState state,out EnemyState enemy)
    {
        state=null;enemy=null;
        return _uxLedgerSeats.TryGetValue(index,out seat)&&seat.Snapshot.TryRead(_uxEnemyHudSnapshot,_uxEpoch,_uxEnemyHudBoardRevision,index,seat.Id,out state,out enemy);
    }
    private float SourceEnemyLedgerWidth(float gap)=>SourceEnemyLedgerModel.Width(gap,_uxEnemyHudSnapshot?.State.Enemies.Count??0,PhoneLayout);
    private float SourceEnemyLedgerHeight(float width)
    {
        if(_uxLedgerHeightWidth==width&&_uxLedgerHeightScale==_textScale&&_uxLedgerHeightPhone==PhoneLayout)return _uxLedgerHeight;
        var s=_uxEnemyHudSnapshot?.ForecastEligibilityState;if(s==null)return SourceEnemyLedgerModel.Height(false,PhoneLayout);
        bool forecast=false;
        for(int i=0;i<s.Enemies.Count;i++)if(s.Enemies[i].Hp>0&&SourceEnemyLedgerModel.Forecast(s,i,Content.GetEnemyDef(s.Enemies[i].EnemyId),width,PhoneLayout,_textScale).Count>0){forecast=true;break;}
        // User text accessibility grows the existing row; the source normal-scale
        // constants remain exact. There is no a7 130% rich-ledger reference.
        _uxLedgerHeightWidth=width;_uxLedgerHeightScale=_textScale;_uxLedgerHeightPhone=PhoneLayout;
        return _uxLedgerHeight=SourceEnemyLedgerModel.Height(forecast,PhoneLayout)*Math.Max(1,_textScale);
    }
    private void LayoutSourceEnemyLedger(int index,float width,SourceHealthBar bar,SourceHudLabel hp,int displayedHp,int displayedMax)
    {
        if(!ReadSourceEnemyLedger(index,out var seat,out var state,out var enemy))return;
        float u=BattleUnit,ts=Math.Max(1,_textScale),top=(PhoneLayout?2:4)*u*ts,nameH=(PhoneLayout?25:30)*u*ts;
        string key=$"{width:R}|{u:R}|{_textScale:R}|{PhoneLayout}";
        if(seat.LayoutKey!=key)
        {
            seat.LayoutKey=key;var def=Content.GetEnemyDef(seat.Id);
            int nf=(int)Math.Round((PhoneLayout?15:19)*u*_textScale),pf=(int)Math.Round((PhoneLayout?15:13)*u*_textScale);
            var nameFont=GD.Load<Font>("res://assets/Fonts/KaiseiDecol-Bold.ttf");var pillFont=GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf");
            float measure(string t)=>pillFont.GetStringSize(t,HorizontalAlignment.Left,-1,pf).X/u;
            string num=state.Enemies.Count>1&&index<SourceEnemyLedgerModel.Circled.Length?SourceEnemyLedgerModel.Circled[index].ToString():"";
            string name=num+def.Name,min=num+(def.Name.Length>2?def.Name.Substring(0,2):def.Name)+"…";
            var pack=SourceEnemyLedgerModel.Pack(seat.Pills,width,state.Enemies.Count,nameFont.GetStringSize(name,HorizontalAlignment.Left,-1,nf).X/u,nameFont.GetStringSize(min,HorizontalAlignment.Left,-1,nf).X/u,measure);
            seat.Name.Text=name;seat.Name.SourceTextRect=new Rect2(8*u,top,pack.NameWidth*u,nameH);seat.Name.AddThemeFontOverride("font",nameFont);seat.Name.AddThemeFontSizeOverride("font_size",nf);seat.Name.AddThemeColorOverride("font_color",SourcePalette.Paper);seat.Name.RefreshSourceAlignment();
            seat.Mask.Position=new Vector2((width-6-pack.Reserve)*u,top);seat.Mask.Size=new Vector2(Math.Max(0,pack.Reserve*u),nameH);
            ClearSourceLedgerChildren(seat.Mask);
            float total=pack.Shown.Select(i=>SourceEnemyLedgerModel.PillWidth(seat.Pills[i],pack.Form,measure)).Sum()+Math.Max(0,pack.Shown.Count-1)*4;
            float x=pack.Reserve-total;
            foreach(int p in pack.Shown)
            {
                var pill=seat.Pills[p];float w=SourceEnemyLedgerModel.PillWidth(pill,pack.Form,measure),h=(PhoneLayout?22:24)*ts;
                var panel=new Panel{Name="EnemyPill_"+index+"_"+p,Position=new Vector2(x*u,(nameH-h*u)/2),Size=new Vector2(w*u,h*u),TooltipText=pill.Tip,MouseFilter=MouseFilterEnum.Pass};
                panel.AddThemeStyleboxOverride("panel",new StyleBoxEmpty());seat.Mask.AddChild(panel);
                SourceEnemyLedgerGlyphs.PillPaper(panel,new Color(pill.Paper),u);
                string text=SourceEnemyLedgerModel.DisplayText(pill,pack.Form);bool tight=pack.Form!=0;float left=tight?2:4,right=tight&&text.Length>0?6:left;
                float tw=text.Length>0?Math.Max(10,measure(text)):0,content=(pill.Icon!=null?16:0)+(pill.Icon!=null&&text.Length>0?(tight?1:2):0)+tw;
                float px=left+Math.Max(0,(w-left-right-content)/2);
                if(pill.Icon!=null){SourceBattleDecor.Icon(panel,"PillIcon",SourceEnemyLedgerGlyphs.Mono(pill.Icon),new Rect2(px*u,(h-16)*u/2,16*u,16*u),new Color(pill.Ink));px+=16+(text.Length>0?(tight?1:2):0);}
                if(text.Length>0){var label=SourceBattleDecor.Text(panel,"PillText",text,new Rect2(px*u,0,tw*u,h*u),pf,new Color(pill.Ink),false,true);label.HorizontalAlignment=HorizontalAlignment.Center;}
                x+=w+4;
            }
            seat.Forecast.Position=new Vector2(8*u,(PhoneLayout?51:58)*u*ts);seat.Forecast.Size=new Vector2(Math.Max(0,(width-16)*u),(PhoneLayout?26:28)*u*ts);
            var parts=SourceEnemyLedgerModel.Forecast(state,index,def,width,PhoneLayout,_textScale);
            FillSourceEnemyForecast(seat.Forecast,parts,pf);
        }
        bar.SourceInsetUnit=u;
        FitSourceEnemyHp(seat,bar,hp,displayedHp,displayedMax);
    }
    private static void ClearSourceLedgerChildren(Control node){foreach(var child in node.GetChildren()){node.RemoveChild(child);child.QueueFree();}}
    private void FillSourceEnemyForecast(Control host,IReadOnlyList<SourceEnemyLedgerModel.ForecastPart> parts,int size)
    {
        ClearSourceLedgerChildren(host);host.TooltipText=string.Join("　",parts.Select(p=>p.Text));
        var font=GD.Load<Font>("res://assets/Fonts/KleeOne-Regular.ttf");float measure(string t)=>font.GetStringSize(t,HorizontalAlignment.Left,-1,size).X;
        string full=string.Join("　",parts.Select(p=>p.Text));bool trim=measure(full)>host.Size.X;float room=Math.Max(0,host.Size.X-(trim?measure("…"):0)),x=0;bool stopped=false;
        for(int i=0;i<parts.Count&&!stopped;i++)
        {
            string text=(i>0?"　":"")+parts[i].Text;
            if(trim&&x+measure(text)>room){int n=0;while(n<text.Length&&x+measure(text.Substring(0,n+1))<=room)n++;text=text.Substring(0,n)+"…";stopped=true;}
            float w=measure(text);SourceBattleDecor.Text(host,"ForecastPart_"+i,text,new Rect2(x,0,w,host.Size.Y),size,new Color(parts[i].Ink));x+=w;
        }
    }
    private void FitSourceEnemyHp(SourceLedgerSeat seat,SourceHealthBar bar,SourceHudLabel hp,int value,int max)
    {
        float u=BattleUnit;var font=hp.GetThemeFont("font");int fs=hp.GetThemeFontSize("font_size");
        var p=SourceEnemyLedgerModel.PlaceHp(value,max,bar.Size.X/u,seat.Mark,t=>font.GetStringSize(t,HorizontalAlignment.Left,-1,fs).X/u);
        p=SourceEnemyHpFit.ReserveShield(p,value,bar.Size.X/u,seat.Mark,bar.Block>0,_textScale,font.GetStringSize(max+" / "+max,HorizontalAlignment.Left,-1,fs).X/u);
        hp.FollowParentRect=false;hp.Text=p.Text;hp.HorizontalAlignment=p.AlignRight?HorizontalAlignment.Right:HorizontalAlignment.Center;
        float span=Math.Max(0,p.Right-p.Left)*u;
        float natural=Math.Max(hp.GetCombinedMinimumSize().X,font.GetStringSize(p.Text,HorizontalAlignment.Left,-1,fs).X);
        float fit=SourceEnemyHpFit.HorizontalScale(span,natural,_textScale,p.HpOnly);
        hp.Scale=new Vector2(fit,1);hp.SourceTextRect=new Rect2(p.Left*u,0,span/fit,bar.Size.Y);hp.RefreshSourceAlignment();
        hp.SetMeta("source_hp_horizontal_fit",fit);hp.SetMeta("source_hp_only",p.HpOnly);
        var painted=hp.PaintedGlyphBoundsInParent;
        hp.SetMeta("source_hp_fitted_paint_rect",new Rect2(hp.Position+(painted.Position-hp.Position)*hp.Scale,painted.Size*hp.Scale));
        seat.Marker.Visible=seat.Mark>0&&seat.Mark<1;
        if(seat.Marker.Visible){float x=3*u+seat.Mark*(bar.Size.X-6*u);seat.Marker.Position=new Vector2(x-3*u,-3*u);seat.Marker.Size=new Vector2(6*u,bar.Size.Y+6*u);seat.MarkerCore.Position=new Vector2(1.5f*u,u);seat.MarkerCore.Size=seat.Marker.Size-new Vector2(3,2)*u;}
    }
    private void RefreshSourceEnemyHp(int index,SourceHealthBar bar,SourceHudLabel hp,int value,int max){if(ReadSourceEnemyLedger(index,out var seat,out _,out _))FitSourceEnemyHp(seat,bar,hp,value,max);}
}
