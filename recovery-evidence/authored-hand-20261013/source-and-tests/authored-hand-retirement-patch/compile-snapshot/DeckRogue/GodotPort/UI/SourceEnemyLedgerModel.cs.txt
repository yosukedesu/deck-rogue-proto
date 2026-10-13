// Pure source-policy model. Stage41 BattleScreen LedgerStrip/ForecastLine/InterruptMarkRatio.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public static class SourceEnemyLedgerModel
{
    public sealed record Pill(string? Icon,string Text,string Short,string Ink,string Paper,string Tip);
    public sealed record ForecastPart(string Text,string Ink,int Priority);
    public sealed record Spec(string Name,IReadOnlyList<Pill> Pills,IReadOnlyList<ForecastPart> Forecast,float Mark,bool Alive);
    public sealed record Packing(int Form,float Reserve,float NameWidth,IReadOnlyList<int> Shown);
    public sealed record HpPlacement(string Text,float Left,float Right,bool AlignRight,bool HpOnly);
    public const string Circled="①②③④⑤⑥⑦⑧";
    public static float Width(float gap,int seats,bool phone)=>seats==1?(phone?420:440):Math.Clamp(gap-(phone?4:12),phone?96:116,phone?176:220);
    public static float Height(bool forecast,bool phone)=>phone?(forecast?79:53):(forecast?92:64);
    public static string Short(string? icon,string text)
    {
        if(icon==null)return text;int k=text.Length;while(k>0&&char.IsDigit(text[k-1]))k--;
        if(k<text.Length&&k>0&&(text[k-1]=='+'||text[k-1]=='-'))k--;
        return k<text.Length?text.Substring(k):text.Length<=2?text:"";
    }
    public static IReadOnlyList<Pill> Pills(GameState state,int index,EnemyDef? def)
    {
        var e=state.Enemies[index];var ps=new List<Pill>();if(e.Hp<=0)return ps;
        void add(string? icon,string text,string ink="f4ecd6",string paper="2a2d48",string? tip=null)
        {
            tip??=text+string.Concat(KeywordHelp.FindIn(text).Select(t=>"\n"+t+": "+KeywordHelp.Terms[t]));
            ps.Add(new Pill(icon,text,Short(icon,text),ink,paper,tip));
        }
        if(e.Strength!=0)add(e.Strength>0?"strength_up":"strength_down","筋力"+(e.Strength>0?"+":"")+e.Strength,e.Strength>0?"ead08a":"a8c9ea");
        if(e.Burn>0)add("burn","延焼"+e.Burn);
        if(e.Exposed>0)add("exposed","急所"+e.Exposed,"ead08a");
        if(e.Confusion>0)add("confuse","混乱"+e.Confusion,"cdb6ea","33284d");
        if(e.Weak>0)add("weak","威圧"+e.Weak,"a8c9ea");
        if(e.Artifact>0)add(null,"アーティファクト"+e.Artifact);
        if(e.BurrowActive==true)add(null,"潜伏");
        if(e.Slippery>0)add(null,"朧 残り"+e.Slippery+"回");
        if(e.Sealed?.Count>0)add(null,"封じ: "+string.Join("・",e.Sealed.Select(c=>c.Def.Name)));
        if(state.Enemies.Count==1&&def!=null)foreach(var b in CardText.EnemyTraitBadges(def).Take(3))
            add(b.Icon,b.Label,"c4beb2",tip:b.Full+(b.Term!=null&&KeywordHelp.Terms.TryGetValue(b.Term,out var help)?"\n"+help:""));
        return ps;
    }
    // Full traits already expose sealed cards and definition-backed live Slippery.
    // Keep unusual fixture/non-definition live Slippery accessible as a status.
    public static string InspectionStatus(GameState state,int index,EnemyDef? definition)=>string.Join("\n",
        Pills(state,index,null).Where(p=>!p.Text.StartsWith("封じ: ")&&!(definition?.Slippery!=null&&p.Text.StartsWith("朧 残り"))).Select(p=>p.Tip));
    public static Packing Pack(IReadOnlyList<Pill> pills,float width,int seats,float nameFull,float nameMin,Func<string,float> measure)
    {
        float full=0,shortW=0;var iconOnly=new float[pills.Count];
        for(int i=0;i<pills.Count;i++)
        {
            var p=pills[i];bool icon=p.Icon!=null;
            float wf=Math.Max(24,(icon?26:8)+measure(p.Text))+1;
            float ws=p.Short.Length==0?24:Math.Max(24,(icon?25:8)+measure(p.Short))+1;
            iconOnly[i]=icon?24:ws;full+=(i>0?4:0)+wf;shortW+=(i>0?4:0)+ws;
        }
        float inner=width-16,cap=inner-4-nameMin,reserve=0;int form=0;var shown=new List<int>();
        if(pills.Count>0)
        {
            bool allowFull=width>=150&&seats==1;
            if(allowFull&&nameFull+4+full<=inner){form=0;reserve=full;}
            else if(nameFull+4+shortW<=inner){form=1;reserve=shortW;}
            else if(shortW<=cap){form=1;reserve=shortW;}
            else if(allowFull&&full<=cap){form=0;reserve=full;}
            else
            {
                form=2;for(int i=0;i<pills.Count;i++){float add=(shown.Count>0?4:0)+iconOnly[i];if(reserve+add>cap)continue;reserve+=add;shown.Add(i);}
                if(shown.Count==0){int first=-1;for(int i=0;i<pills.Count;i++)if(pills[i].Icon!=null){first=i;break;}if(first<0)first=0;shown.Add(first);reserve=iconOnly[first];}
            }
            if(form!=2)shown.AddRange(Enumerable.Range(0,pills.Count));
        }
        return new Packing(form,reserve,Math.Max(0,inner-(reserve>0?reserve+4:0)),shown);
    }
    public static string DisplayText(Pill p,int form)=>form==0?p.Text:form==1||p.Icon==null?p.Short:"";
    public static float PillWidth(Pill p,int form,Func<string,float> measure)
    {
        string text=DisplayText(p,form);bool tight=form!=0;float pads=tight?(text.Length>0?8:4):8;
        float content=(p.Icon!=null?16:0)+(p.Icon!=null&&text.Length>0?(tight?1:2):0)+(text.Length>0?Math.Max(10,measure(text)):0);
        return Math.Max(24,pads+content);
    }
    public static float Estimate(string text,bool phone,float textScale=1)
    {
        float n=0;foreach(char c in text)n+=(c<0x80||c=='×'||c=='＋')?(char.IsDigit(c)?.58f:.62f):1;
        return n*(phone?15:13)*textScale;
    }
    public static IReadOnlyList<ForecastPart> Forecast(GameState state,int index,EnemyDef? def,float width,bool phone,float textScale=1)
    {
        var e=state.Enemies[index];if(e.Hp<=0)return Array.Empty<ForecastPart>();
        var forms=new List<string[]>();var inks=new List<string>();var priorities=new List<int>();
        void add(string[] f,string ink,int priority){forms.Add(f);inks.Add(ink);priorities.Add(priority);}
        bool others=state.Enemies.Where((x,i)=>i!=index).Any(x=>x.Hp>0);
        int gi=FlowRules.GuardRedirect(state,index);if(gi>=0&&gi!=index){string c=gi<Circled.Length?Circled[gi].ToString():"";add(new[]{c+"が庇う（単体は"+c+"へ）",c+"が庇う"},"c4beb2",1);}
        if(state.Enemies.Count>1&&def!=null){var labels=CardText.EnemyTraitBadges(def).Where(b=>b.Term!="庇う"||!others).Select(b=>b.Label).ToList();if(labels.Count>=2)add(new[]{labels[0]+"・"+labels[1],labels[0]},"c4beb2",0);else if(labels.Count==1)add(new[]{labels[0]},"c4beb2",0);}
        var br=CardText.BranchForms(state,index);if(br!=null)add(br,"b5ddd6",2);
        if(def?.Interrupts!=null&&e.InterruptBlocked!=true)for(int i=0;i<def.Interrupts.Count;i++)
        {
            if(e.FiredInterrupts?.Contains(i)==true)continue;var it=def.Interrupts[i];var first=EnemyGraph.FirstMoveOf(def,it.Goto);string? move=first!=null?CardText.MoveShort(first,e.Strength):null;
            string when,shortWhen;
            if(it.On==EnemyInterruptTriggers.HpBelowHalf){when="HP"+(e.MaxHp/2)+"以下で";shortWhen="HP半分で";}
            else if(it.On==EnemyInterruptTriggers.DamageTaken){when="あと"+Math.Max(0,(it.Amount??0)-(e.DamageTakenTotal??0))+"ダメージで";shortWhen=when;}
            else if(it.On==EnemyInterruptTriggers.Alone){if(!others)continue;when="仲間が全滅すると";shortWhen="ひとりになると";}
            else if(it.On==EnemyInterruptTriggers.AllyDied){if(!others)continue;when=shortWhen="仲間が倒れると";}
            else continue;
            var f=new List<string>();if(move!=null){f.Add(when+" "+move);if(shortWhen!=when)f.Add(shortWhen+" "+move);}f.Add(shortWhen+"行動が変わる");f.Add(shortWhen+"変わる");add(f.ToArray(),"ead08a",3);break;
        }
        var levels=new int[forms.Count];float total()=>forms.Count==0?0:(forms.Count-1)*Estimate("　",phone,textScale)+forms.Select((f,i)=>Estimate(f[levels[i]],phone,textScale)).Sum();
        for(int guard=0;guard<16&&total()>width-16;guard++){int pick=-1;for(int pr=0;pr<4&&pick<0;pr++)for(int i=0;i<forms.Count;i++)if(priorities[i]==pr&&levels[i]+1<forms[i].Length){pick=i;break;}if(pick<0)break;levels[pick]++;}
        return forms.Select((f,i)=>new ForecastPart(f[levels[i]],inks[i],priorities[i])).ToArray();
    }
    public static float Mark(EnemyDef? def,EnemyState enemy)
    {
        if(def?.Interrupts==null||enemy.MaxHp<=0||enemy.InterruptBlocked==true)return -1;
        for(int i=0;i<def.Interrupts.Count;i++)
        {
            if(enemy.FiredInterrupts?.Contains(i)==true)continue;var it=def.Interrupts[i];if(it.On==EnemyInterruptTriggers.HpBelowHalf)return .5f;
            if(it.On==EnemyInterruptTriggers.DamageTaken){int remain=Math.Max(0,(it.Amount??0)-(enemy.DamageTakenTotal??0));float at=(float)(enemy.Hp-remain)/enemy.MaxHp;if(at>0&&at<1)return at;}
        }
        return -1;
    }
    public static HpPlacement PlaceHp(int hp,int max,float width,float mark,Func<string,float> measure)
    {
        string full=Math.Max(0,hp)+" / "+max;if(mark<=0||mark>=1||width<=0)return new(full,0,width,false,false);
        float mx=3+mark*(width-6),tw=measure(max+" / "+max),gap=11;
        if(Math.Abs(mx-width/2)>=tw/2+gap)return new(full,0,width,false,false);
        float la=10,lb=mx-gap,ra=mx+gap,rb=width-8;
        if(rb-ra>=tw)return new(full,ra,rb,false,false);if(lb-la>=tw)return new(full,la,lb,false,false);
        string one=Math.Max(0,hp).ToString();float hw=measure(max.ToString());return rb-ra>=hw||rb-ra>=lb-la?new(one,ra,rb,true,true):new(one,la,lb,true,true);
    }
}
