// Source-backed reconstruction of Unity BattleScreen.IntentMetrics/IntentTag,
// 4bdf5d4 source; dimensions independently checked against a7f86b5 layout JSON.
// Pure presentation model. No commands, game state writes, or RNG.
using System;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public static class SourceIntentLayout
{
    public sealed record Rider(string Icon,string Text,string Ink,string Fill);
    public sealed record Spec(string Kind,string Caption,bool Minor,bool Small,bool Stack,float IconSize,int FontSize,float RowHeight,float Width,float Height,IReadOnlyList<Rider> Riders);
    public static Spec ForState(GameState state,int index,bool phone,float gap,bool uniformStack,float textScale=1)
    {
        var it=Effects.EffectiveIntent(state,index);
        if(it==null)return null;
        return Build(it,state.HideIntents==true,Effects.DisplayedIntentValue(state,index,it.Kind,it.Actual),Effects.DisplayedInflict(state,it.Inflict),phone,gap,state.Enemies.Count==1,uniformStack,textScale);
    }
    public static Spec Build(EnemyIntent it,bool hidden,int displayed,StatusInflict inflict,bool phone,float gap,bool solo,bool uniformStack,float textScale=1)
    {
        bool small=gap<(phone?170:240),minor=!hidden&&(it.Kind is "defend" or "buff" or "rally" or "heal" or "rest");
        float icon=minor?(small?22:phone?26:32):small?(phone?28:36):(phone?32:44);
        float row=minor?(small?30:phone?34:44):small?(phone?38:48):(phone?42:56);
        row=Math.Max(row,phone?38:48)*textScale;
        int font=phone?25:32;
        string caption=hidden?"？":Caption(it,displayed);
        var riders=new List<Rider>();
        if(!hidden)
        {
            if(inflict!=null)riders.Add(new Rider(StatusIcon(inflict.Status),(small?"":"あなたに")+CardText.StatusName(inflict.Status)+inflict.Amount,"cdb6ea","33284d"));
            if(it.AlsoBuff.HasValue)riders.Add(new Rider("strength_up",(small?"":"同時に")+"筋力+"+it.AlsoBuff.Value,"ead08a","2a2d48"));
            if(it.AlsoDefend.HasValue)riders.Add(new Rider("shield",(small?"":"同時に")+"ブロック"+it.AlsoDefend.Value,"a8c9ea","1f344c"));
            if(it.StrengthPerMilled.HasValue)riders.Add(new Rider("strength_up","食べた分 筋力+"+it.StrengthPerMilled.Value+"/枚","ead08a","2a2d48"));
            if(it.AlsoDestroySet==true)riders.Add(new Rider("exhaust","先に壊す","f2a193","3d2028"));
        }
        float wTop=icon+(phone?6:10)+TextWidth(caption,font*textScale,true)+24;
        float wr=0;foreach(var r in riders)wr+=PillWidth(r,textScale)+6;
        bool stack=riders.Count>0&&(small||(phone&&minor)||uniformStack||wTop+wr>gap-8);
        float cap=solo?float.MaxValue:gap-10;
        if(it.Kind=="buff"&&!hidden&&(stack?Math.Max(wTop,wr+12):wTop+wr)>cap)
        {
            caption="+"+it.Actual;wTop=icon+(phone?6:10)+TextWidth(caption,font*textScale,true)+24;
        }
        float width=stack?Math.Max(72,Math.Max(wTop,wr+12)):Math.Max(72,wTop+wr);
        if(!solo)width=Math.Min(width,Math.Max(wTop,cap));
        return new Spec(it.Kind,caption,minor,small,stack,icon,font,row,width,row+(stack?(phone?30:32)*textScale:0),riders);
    }
    public static float PillWidth(Rider rider,float textScale)=>4+(rider.Icon!=null?18:0)+TextWidth(rider.Text,15*textScale)+6;
    public static float TextWidth(string text,float size,bool deco=false)
    {
        if(string.IsNullOrEmpty(text))return 0;
        float w=0;foreach(char c in text)w+=(c<0x80||c=='×'||c=='＋')?(char.IsDigit(c)?.58f:.62f):1;
        if(deco)w+=.02f*text.Length;return w*size;
    }
    public static string StatusIcon(string status)=>status switch{"weak"=>"weak","frail"=>"frail","vulnerable" or "slow"=>"exposed","mist"=>"draw",_=>null};
    public static string Caption(EnemyIntent it,int displayed)=>it.Kind switch
    {
        "attack"=>displayed+((it.Hits??1)>1?" ×"+it.Hits.Value:"")+(it.MirrorHits==true?" ×手数":""),
        "defend"=>it.Actual.ToString(),"buff"=>"筋力+"+it.Actual,"rally"=>"応援+"+it.Actual,"heal"=>"回復"+it.Actual,"hex"=>"呪い",
        "destroy-set"=>"からくり壊し","destroy-token"=>"人形狩り","steal-gold"=>"盗み"+it.Actual+"G","flee"=>"逃走","mill"=>"山札喰い"+it.Actual+"枚","seal"=>"技封じ","summon"=>"召喚","rest"=>"隙","hatch"=>"孵化",_=>it.Kind
    };
}
