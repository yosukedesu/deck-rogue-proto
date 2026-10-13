// RECONSTRUCTION from preserved Unity BattleView/BattleScreen and accepted UX contracts.
// Not a recovered original Git blob. Engine, commands and RNG remain in the baseline.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private Control _uxBattle,_uxHand; private SourceAimArrow _uxAim;
    private readonly Dictionary<string,CardTile> _uxTiles=new();
    private readonly Dictionary<string,(Vector2 Pos,float Rotation,int Order)> _uxSeats=new();
    private readonly List<(int Index,EnemyDropTarget Hit,PanelContainer Intent,PanelContainer Ledger,SourceHealthBar Bar,SourceHudLabel Hp,Label Title)> _uxEnemies=new();
    private SourceHealthBar _uxHpBar;private SourceEnergyOrb _uxOrb;private RichTextLabel _uxForecast,_uxDetail;private Label _uxPhase;
    private string _uxHover,_uxDrag;private long _uxEpoch;private Vector2 _uxLastSize;private bool _uxRelayoutQueued;
    private Func<Vector2?> _uxLeaderProjection;
    private Func<GameEvent,SessionChange> _uxEventOwner;private Func<GameEvent,bool> _uxOrdinary;
    private Func<GameEvent,int?> _uxIncomingContact;private readonly HashSet<long> _uxPaid=new();
    private int _uxIncoming;private bool _uxLanded,_uxSkipFirst;private long _uxIncomingOwner=-1;
    public IReadOnlyDictionary<string,CardTile> LiveHandTiles=>_uxTiles;
    public int DisplayedIncomingRemaining=>_uxIncoming;
    public bool DisplayedIncomingLanded=>_uxLanded;
    public string DisplayedIncomingHeadline=>_uxForecast?.GetParsedText()??"";
    public bool AimActive=>_uxAim?.Active??false;
    private float BattleUnit=>Size.Y/(PhoneLayout?675f:1080f);
    private float UxCardScale=>PhoneLayout?1:.92f;
    private GameState InputCombat=>!IsInputBlocked?_session?.CurrentRun?.Combat:_view?.Combat;
    public void SetPresentationOwnership(Func<GameEvent,SessionChange> owner,Func<GameEvent,bool> ordinary){_uxEventOwner=owner;_uxOrdinary=ordinary;}
    public void SetIncomingContactProjection(Func<GameEvent,int?> provider)=>_uxIncomingContact=provider;
    public void SetLeaderProjection(Func<Vector2?> provider)=>_uxLeaderProjection=provider;
    public void SetCardRetirement(Func<string,double> provider){_uxRetirement=provider;}
    private Func<string,double> _uxRetirement;
    public IReadOnlyList<Rect2> GetEnemyLedgerBounds()=>_uxEnemies.Where(x=>GodotObject.IsInstanceValid(x.Ledger)&&x.Ledger.IsVisibleInTree()).Select(x=>x.Ledger.GetGlobalRect()).ToArray();
    private void UxResetHand(){UxCancelHandHovers(false);UxCancelHandDeals(false);_uxEpoch++;EndHandDrag();foreach(var t in _uxTiles.Values){t.Hide();t.QueueFree();}_uxTiles.Clear();_uxSeats.Clear();_uxHover=null;_uxPaid.Clear();_uxIncomingOwner=-1;}
    private Label UxText(Control parent,string text,int units,Color color,bool deco=false,bool bold=false)
    {
        var label=new Label{Text=text,MouseFilter=MouseFilterEnum.Ignore,VerticalAlignment=VerticalAlignment.Center};parent.AddChild(label);
        var font=GD.Load<Font>("res://assets/Fonts/"+(deco?"KaiseiDecol-Bold.ttf":bold?"KleeOne-SemiBold.ttf":"KleeOne-Regular.ttf"));int fs=(int)Math.Round(Math.Max(PhoneLayout?15:13,units)*BattleUnit*_textScale);
        label.AddThemeFontOverride("font",deco?SourceHudLabel.Spaced(font,fs,2):font);label.AddThemeFontSizeOverride("font_size",fs);label.AddThemeColorOverride("font_color",color);return label;
    }
    private static StyleBoxFlat UxPanelBox(Color color,Color edge,int width=2)
    {
        var box=Box(color,edge,width);
        box.ContentMarginLeft=box.ContentMarginRight=box.ContentMarginTop=box.ContentMarginBottom=0;
        return box;
    }
    private PanelContainer UxPanel(Control parent,string name,Rect2 rect,Color? edge=null)
    {
        var p=new PanelContainer{Name=name,Position=rect.Position,Size=rect.Size,MouseFilter=MouseFilterEnum.Ignore};p.AddThemeStyleboxOverride("panel",UxPanelBox(SourcePalette.Night,edge??SourcePalette.NightEdge,2));parent.AddChild(p);return p;
    }
    private Button UxButton(Control parent,string name,string text,Rect2 rect,Action action,bool enabled=true,bool gold=false)
    {
        var b=Button(parent,text,action,enabled,gold?SourcePalette.BrassLight:null);b.Name=name;b.Position=rect.Position;b.Size=rect.Size;
        if(gold){b.AddThemeStyleboxOverride("normal",Box(SourcePalette.BrassLight,SourcePalette.Brass,2));b.AddThemeStyleboxOverride("hover",Box(SourcePalette.Paper3,SourcePalette.Brass,2));b.AddThemeColorOverride("font_color",SourcePalette.BrassInk);}
        b.AddThemeFontOverride("font",GD.Load<Font>("res://assets/Fonts/KleeOne-Regular.ttf"));b.AddThemeFontSizeOverride("font_size",(int)Math.Round(15*BattleUnit*_textScale));return b;
    }
    private Control UxHeaderTag(Control parent,string name,float height,float unit,params (string Text,int Size,Color Ink,bool Deco,bool Bold)[] parts)
    {
        var panel=new Panel{Name=name,Size=new Vector2(0,height*unit),MouseFilter=MouseFilterEnum.Ignore};parent.AddChild(panel);
        panel.AddThemeStyleboxOverride("panel",SourceBattleDecor.Box(SourcePalette.Night,SourcePalette.NightEdge,unit));
        float x=10*unit;
        foreach(var part in parts)
        {
            int fs=(int)Math.Round(Math.Max(PhoneLayout?15:13,part.Size)*unit*_textScale);
            var label=SourceBattleDecor.Text(panel,name+"Text",part.Text,new Rect2(x,0,500*unit,height*unit),fs,part.Ink,part.Deco,part.Bold);
            float width=label.GetThemeFont("font").GetStringSize(part.Text,HorizontalAlignment.Left,-1,fs).X;
            label.SourceTextRect=new Rect2(x,0,width,height*unit);x+=width+6*unit;
        }
        panel.Size=new Vector2(x+4*unit,height*unit);return panel;
    }
    private void BuildBattleHeader()
    {
        if(_view?.Combat is not {} state)return;float u=BattleUnit;
        var header=new Control{Name="BattleHeader",ZIndex=30,CustomMinimumSize=new Vector2(0,56*u),MouseFilter=MouseFilterEnum.Ignore};_shell.AddChild(header);
        string encounter="";try{var node=Run.CurrentNode(_view);if(node?.EncounterId!=null)encounter=Content.EncounterName(node.EncounterId);}catch(Exception){}
        var title=UxHeaderTag(header,"EncounterTag",40,u,("幕"+_view.Act+"・行"+(_view.Row+1),15,SourcePalette.PaperDim,false,false),(encounter,19,SourcePalette.Paper,true,false));
        title.Position=new Vector2(24*u-Sizing.Edge,8*u-Sizing.Edge);title.RotationDegrees=.6f;
        var turn=UxHeaderTag(header,"TurnTag",32,u,("    ターン "+state.Turn,15,SourcePalette.Paper,false,true));
        turn.Position=new Vector2(title.Position.X+title.Size.X+12*u,12*u-Sizing.Edge);
        SourceBattleDecor.Icon(turn,"TurnIcon",SourceIntentGlyphs.Mono("turn"),new Rect2(10*u,8*u,16*u,16*u),SourcePalette.PaperDim);
        var mana=UxHeaderTag(header,"ManaTag",34,u,("    魔素",15,SourcePalette.PaperDim,false,false),(_view.Mana.GetValueOrDefault().ToString(),18,SourcePalette.ManaLight,true,false),("/50",15,SourcePalette.PaperDim,false,false));
        SourceBattleDecor.Icon(mana,"ManaIcon",SourceArtwork.Gear("mana","interfere"),new Rect2(10*u,8*u,18*u,18*u));
        var money=UxHeaderTag(header,"GoldTag",34,u,("    "+_view.Gold,18,SourcePalette.BrassLight,true,false),("G",15,SourcePalette.PaperDim,false,false));
        SourceBattleDecor.Icon(money,"GoldIcon",SourceIntentGlyphs.ColorBitmap("gold",new Color("e0b84a")),new Rect2(10*u,9*u,16*u,16*u));
        float menuWidth=(PhoneLayout?64:44)*u;var menu=UxButton(header,"BattleMenu","≡",new Rect2(Size.X-24*u-menuWidth-Sizing.Edge,2*u-Sizing.Edge,menuWidth,(PhoneLayout?52:34)*u),OpenMenu);
        menu.AddThemeFontSizeOverride("font_size",(int)Math.Round((PhoneLayout?26:18)*u*_textScale));
        menu.AddThemeStyleboxOverride("normal",SourceBattleDecor.Box(new Color("262943"),SourcePalette.Paper2,u));
        mana.Position=new Vector2(menu.Position.X-12*u-mana.Size.X,11*u-Sizing.Edge);
        money.Position=new Vector2(mana.Position.X-12*u-money.Size.X,11*u-Sizing.Edge);
        var plaque=UxPanel(header,"TurnPlaque",new Rect2(Size.X*.5f-Sizing.Edge-75*u,13*u-Sizing.Edge,150*u,30*u));plaque.AddThemeStyleboxOverride("panel",SourceBattleDecor.Box(SourcePalette.Paper,SourcePalette.BrassInk,u,1));_uxPhase=UxText(plaque,"あなたの番",15,SourcePalette.Ink,true);_uxPhase.HorizontalAlignment=HorizontalAlignment.Center;
    }
    private void BuildBattle(RunState run)
    {
        var state=run.Combat;if(state==null)return;float u=BattleUnit;_uxEnemies.Clear();_enemyReadouts.Clear();_enemyIntentReadouts.Clear();
        _uxBattle=new Control{Name="SourceBattleCanvas",ZIndex=30,Size=Size,MouseFilter=MouseFilterEnum.Ignore};AddChild(_uxBattle);
        var field=new BattlefieldDropTarget{Name="BattlefieldDrop",Size=Size,MouseFilter=MouseFilterEnum.Pass,CanPlay=CanDropHandCard,CardDropped=DropHandCard,ResolveCard=DecodeHandDrag};AddChild(field);
        if(_uxHand==null||!GodotObject.IsInstanceValid(_uxHand)){_uxHand=new Control{Name="PersistentHand",Size=Size,MouseFilter=MouseFilterEnum.Ignore};AddChild(_uxHand);}else MoveChild(_uxHand,GetChildCount()-1);
        _uxHand.Visible=true;
        // Unity BattleView.Create: FieldLayer, then HandLayer, then UiLayer.
        // HUD buttons must receive the pointer above incoming hand cards.
        MoveChild(_uxBattle,GetChildCount()-1);
        float bottom=Size.Y-(PhoneLayout?293:285)*u;
        BuildUxHero(state,bottom);
        BuildUxEnemies(field,state,bottom);
        _uxOrb=new SourceEnergyOrb{Name="EnergyOrb",Position=new Vector2(24*u,Size.Y-244*u),Size=new Vector2(128,128)*u,Energy=state.Player.Energy,Maximum=TurnEnergyCap(state),TextScale=_textScale,MouseFilter=MouseFilterEnum.Ignore};_uxBattle.AddChild(_uxOrb);
        var cap=UxPanel(_uxBattle,"NextEnergyCap",new Rect2(24*u,Size.Y-109*u,160*u,26*u));UxText(cap,"次のターン 上限 "+state.Player.EnergyMax,15,SourcePalette.BrassLight);
        var draw=UxButton(_uxBattle,"DrawPile","",new Rect2(24*u,Size.Y-76*u,150*u,52*u),()=>OpenDeck(state.Player.DrawPile.OrderBy(x=>x.Def.Name).ToArray(),"山札 (名前順・順番は非公開)"));
        SourceBattleDecor.Pile(draw,"draw","山札",state.Player.DrawPile.Count,u,_textScale,PhoneLayout);
        var discard=UxButton(_uxBattle,"DiscardPile","",new Rect2(Size.X-174*u,Size.Y-76*u,150*u,52*u),()=>OpenDeck(state.Player.DiscardPile,"捨て札"));
        SourceBattleDecor.Pile(discard,"discard","捨て札",state.Player.DiscardPile.Count,u,_textScale,PhoneLayout);
        if(state.Player.ExhaustPile.Count>0){var exhaust=UxButton(_uxBattle,"ExhaustPile","",new Rect2(Size.X-174*u,Size.Y-120*u,150*u,36*u),()=>OpenExhaust(state));SourceBattleDecor.Pile(exhaust,"exhaust","消滅",state.Player.ExhaustPile.Count,u,_textScale,PhoneLayout);}
        var end=UxButton(_uxBattle,"EndTurn","ターン終了",new Rect2(Size.X-254*u,Size.Y-(PhoneLayout?210:204)*u,230*u,64*u),BeginEndTurn,state.Phase=="player-turn"&&state.PendingScry==null,true);
        int endSize=(int)Math.Round(21*u*_textScale);end.AddThemeFontSizeOverride("font_size",endSize);end.AddThemeFontOverride("font",SourceHudLabel.Spaced(GD.Load<Font>("res://assets/Fonts/KaiseiDecol-Bold.ttf"),endSize,4));
        int sparks=state.Player.Hand.Count(x=>x.Def.SparkToken==true);if(sparks>=2)UxButton(_uxBattle,"PlayAllSparks","火種を全部撃つ ("+sparks+")",new Rect2(Size.X-254*u,Size.Y-300*u,230*u,48*u),PlayAllSparks,true,true);
        BuildUxPockets(state);
        if(_uxIncomingOwner<0||!IsInputBlocked){_uxIncoming=Effects.IncomingTotal(state);_uxLanded=false;_uxSkipFirst=state.NullifyNextAttack==true;}
        SyncUxHand();RefreshUxReadouts();
        _uxAim=new SourceAimArrow{Name="HandAim",Size=Size,Unit=u,MouseFilter=MouseFilterEnum.Ignore};AddChild(_uxAim);
        if(_play!=null&&_play.Next(InputCombat)=="target")BuildUxTargetBanner(_play);
        // Godot GUI picking follows tree order, not CanvasItem.ZIndex. Keep the
        // header's actual buttons above the transparent field receiver as siblings.
        if(_safeMargin!=null&&_safeMargin.GetParent()==this)MoveChild(_safeMargin,GetChildCount()-1);
        if(_overlay==null&&!IsInputBlocked){if(state.PendingScry!=null)PrepareScry(state);else if(state.Phase=="awaiting-reaction")PrepareReaction(state);}
    }
    private static int TurnEnergyCap(GameState s)=>Math.Max(1,s.Player.EnergyMaxAtTurnStart>0?s.Player.EnergyMaxAtTurnStart-(s.EnergyMaxRefBonus??0):s.Player.EnergyMax);
    private void BuildUxHero(GameState s,float bottom)
    {
        float u=BattleUnit;var resources=SourceBattleDecor.Resources(s);int resourceRows=Math.Min(2,(resources.Count+1)/2);float w=(PhoneLayout?208:360)*u;float h=(PhoneLayout?80+resourceRows*30:120)*u;
        if(_textScale>1){w=Math.Max(w,230*u*_textScale);h=Math.Max(h,(80+resourceRows*30)*u*_textScale);}
        var panel=UxPanel(_uxBattle,"PlayerFootLedger",new Rect2(24*u,bottom-h,w,h));
        var body=new Control{Name="HeroLedgerBody",MouseFilter=MouseFilterEnum.Ignore};panel.AddChild(body);
        _uxHpBar=new SourceHealthBar{Name="HeroHpBar",Position=new Vector2(8,8)*u,Size=new Vector2(w-16*u,22*u*_textScale),MouseFilter=MouseFilterEnum.Ignore};body.AddChild(_uxHpBar);_hpReadout=CreateHpReadout(_uxHpBar,s.Player.Hp+" / "+s.Player.MaxHp,PhoneLayout?15:17);
        _uxForecast=UxRich(body,"IncomingHeadline",PhoneLayout?18:20,true);_uxForecast.SetMeta("source_text_rect",new Rect2(8*u,32*u*_textScale,w-16*u,22*u*_textScale));
        _uxDetail=UxRich(body,"IncomingDetail",PhoneLayout?15:13,false);_uxDetail.SetMeta("source_text_rect",new Rect2(8*u,54*u*_textScale,w-16*u,22*u*_textScale));
        for(int row=0;row<resourceRows;row++)
        {
            var strip=new Control{Name="HeroStatusRow_"+row,Position=new Vector2(8*u,(80+row*30)*u*_textScale),Size=new Vector2(w-12*u,28*u*_textScale),ClipContents=true,MouseFilter=MouseFilterEnum.Ignore};body.AddChild(strip);float cx=0;
            for(int i=row*2;i<Math.Min(row*2+2,resources.Count);i++)
            {
                var r=resources[i];bool debuff=r.Text.StartsWith("弱")||r.Text.StartsWith("脆")||r.Text.StartsWith("虚")||r.Text.StartsWith("重")||r.Text.StartsWith("拘")||r.Text.StartsWith("霞");
                Color ink=debuff?new Color("cdb6ea"):r.Text.StartsWith("勢い")?SourcePalette.BrassLight:r.Text.StartsWith("成長")?new Color("a6d99a"):SourcePalette.Paper;
                var font=GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf");int fs=(int)Math.Round(15*u*_textScale);float tw=font.GetStringSize(r.Text,HorizontalAlignment.Left,-1,fs).X,tx=(r.Icon!=null?32:10)*u,cw=tx+tw+10*u;
                var chip=UxPanel(strip,"ResourceChip_"+i,new Rect2(cx,0,cw,28*u*_textScale));chip.AddThemeStyleboxOverride("panel",SourceBattleDecor.Box(debuff?new Color("33284d"):SourcePalette.NightPill,debuff?new Color("47386b"):SourcePalette.NightEdge,u,1));
                var inner=new Control{MouseFilter=MouseFilterEnum.Ignore};chip.AddChild(inner);
                if(r.Icon!=null)SourceBattleDecor.Icon(inner,"ResourceIcon",SourceIntentGlyphs.Mono(r.Icon),new Rect2(10*u,(28*u*_textScale-16*u)/2,16*u,16*u),ink);
                SourceBattleDecor.Text(inner,"ResourceText",r.Text,new Rect2(tx,0,tw,28*u*_textScale),fs,ink,false,true);cx+=cw+6*u;
            }
        }
        var hit=new Button{Name="IncomingForecast",Flat=true,MouseFilter=MouseFilterEnum.Stop,Size=new Vector2(w,80*u*_textScale),Text="",TooltipText="被ダメ予測の詳細"};foreach(var style in new[]{"normal","hover","pressed","disabled","focus","hover_pressed"})hit.AddThemeStyleboxOverride(style,new StyleBoxEmpty());body.AddChild(hit);hit.Pressed+=()=>Open("被ダメ予測",box=>{var col=ScrollColumn(box);foreach(var line in BattleForecast.Lines(_view.Combat))Text(col,line,23);});
        if(resources.Count>0){var status=new Button{Name="HeroStatusDetails",Flat=true,Text="",Position=new Vector2(0,80*u*_textScale),Size=new Vector2(w,resourceRows*30*u*_textScale),MouseFilter=MouseFilterEnum.Stop,TooltipText="状態・能力の詳細"};foreach(var style in new[]{"normal","hover","pressed","disabled","focus","hover_pressed"})status.AddThemeStyleboxOverride(style,new StyleBoxEmpty());body.AddChild(status);status.Pressed+=()=>Open("状態・能力",b=>{Text(b,PlayerStatus(_view.Combat),23);foreach(var r in SourceBattleDecor.Resources(_view.Combat))Text(b,r.Text,21);});}

    }
    private SourceHudLabel CreateHpReadout(Control parent,string text,int sourceSize)
    {
        var label=new SourceHudLabel{Name="HpReadout",Text=text,FollowParentRect=true,MouseFilter=MouseFilterEnum.Ignore,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,AutowrapMode=TextServer.AutowrapMode.Off};
        label.AddThemeFontSizeOverride("font_size",(int)Math.Round(sourceSize*BattleUnit*_textScale));label.AddThemeColorOverride("font_color",SourcePalette.Paper);
        SourceHudLabel.NightHalo(label,GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf"),BattleUnit,_textScale,deferSmallUnderlay:true);parent.AddChild(label);return label;
    }
    private RichTextLabel UxRich(Control p,string name,int size,bool deco)
    {
        var l=new RichTextLabel{Name=name,BbcodeEnabled=true,ScrollActive=false,FitContent=false,MouseFilter=MouseFilterEnum.Ignore,AutowrapMode=TextServer.AutowrapMode.Off};p.AddChild(l);l.AddThemeFontOverride("normal_font",GD.Load<Font>("res://assets/Fonts/"+(deco?"KaiseiDecol-Bold.ttf":"KleeOne-Regular.ttf")));l.AddThemeFontSizeOverride("normal_font_size",(int)Math.Round(size*BattleUnit*_textScale));l.AddThemeColorOverride("default_color",SourcePalette.Paper);return l;
    }
    private void BuildUxPockets(GameState s)
    {
        float u=BattleUnit;float y=PhoneLayout?84*u:Size.Y-400*u;var count=UxPanel(_uxBattle,"SetCountNote",new Rect2(20*u,y-30*u,145*u,28*u));UxText(count,"からくり "+s.Player.SetCards.Count+" / "+s.Player.SetSlots,15,SourcePalette.Paper);
        for(int i=0;i<s.Player.SetSlots;i++){int slot=i;var b=UxButton(_uxBattle,"SetPocket_"+i,"",new Rect2((24+i*78)*u,y,68*u,74*u),()=>{if(slot<s.Player.SetCards.Count)InspectCard(s.Player.SetCards[slot]);});foreach(var stateStyle in new[]{"normal","hover","pressed","disabled","focus","hover_pressed"})b.AddThemeStyleboxOverride(stateStyle,new StyleBoxEmpty());SourceBattleDecor.Pocket(b,68,74,u);if(i<s.Player.SetCards.Count){var pic=new TextureRect{Texture=SourceArtwork.Card(s.Player.SetCards[i].Def.Id,s.Player.SetCards[i].Def.Type),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore,TextureFilter=TextureFilterEnum.Nearest};b.AddChild(pic);pic.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);}}
    }
    private void BuildUxEnemies(Control field,GameState s,float bottom)
    {
        for(int i=0;i<s.Enemies.Count;i++)
        {
            int index=i;var e=s.Enemies[i];if(e.Hp<=0)continue;
            var hit=new EnemyDropTarget{Name="EnemyTarget_"+i,Text="",Disabled=IsInputBlocked,IsBlocked=()=>IsInputBlocked,ResolveCard=DecodeHandDrag,MouseFilter=MouseFilterEnum.Pass};foreach(var style in new[]{"normal","hover","pressed","disabled","focus","hover_pressed"})hit.AddThemeStyleboxOverride(style,new StyleBoxEmpty());field.AddChild(hit);hit.Pressed+=()=>{if(!hit.ConsumeInspection())SelectEnemy(index);};hit.Inspect=()=>InspectEnemy(index);hit.CardDropped=uid=>{var card=InputCombat?.Player.Hand.FirstOrDefault(x=>x.Uid==uid);if(card!=null){_preferredTarget=index;BeginPlay(card,index);}};
            var intent=UxPanel(hit,"EnemyIntentSurface_"+i,new Rect2(0,0,90*BattleUnit,40*BattleUnit));intent.SetMeta("enemy_index",i);intent.SetMeta("hud_role","intent");var title=new SourceHudLabel{Name="IntentNumber",MouseFilter=MouseFilterEnum.Ignore};intent.AddChild(title);_enemyIntentReadouts[i]=title;
            var ledger=UxPanel(hit,"EnemyLedgerSurface_"+i,new Rect2(0,0,180*BattleUnit,53*BattleUnit));ledger.SetMeta("enemy_index",i);ledger.SetMeta("hud_role","ledger");var body=new Control{MouseFilter=MouseFilterEnum.Ignore};ledger.AddChild(body);var name=UxText(body,(s.Enemies.Count>1?"①②③④⑤⑥⑦⑧"[i%8].ToString():"")+Content.GetEnemyDef(e.EnemyId).Name,PhoneLayout?15:19,SourcePalette.Paper,true);name.Position=new Vector2(8,0)*BattleUnit;
            var bar=new SourceHealthBar{Name="EnemyHpBar_"+i,Position=new Vector2(8,PhoneLayout?29:25)*BattleUnit,Size=new Vector2(150,20)*BattleUnit,MouseFilter=MouseFilterEnum.Ignore};body.AddChild(bar);var hp=CreateHpReadout(bar,e.Hp+" / "+e.MaxHp,PhoneLayout?15:16);_enemyReadouts[i]=hp;
            _uxEnemies.Add((i,hit,intent,ledger,bar,hp,title));
        }
        PositionUxEnemies();
    }
    // Immutable source artwork: scan alpha once per texture identity / phone geometry.
    // No GPU readback or native Image lifetime survives a miss. Unit scaling is applied later.
    private readonly Dictionary<(ulong Texture,bool Phone),float> _uxHeadHeightCache=new();
    private long _uxHeadReadbacks,_uxHeadCacheHits;
    public long EnemyHeadReadbacks=>_uxHeadReadbacks;
    public long EnemyHeadCacheHits=>_uxHeadCacheHits;
    private float UxEnemyHeadHeight(Texture2D texture)
    {
        var key=(texture.GetInstanceId(),PhoneLayout);
        if(_uxHeadHeightCache.TryGetValue(key,out float cached)){_uxHeadCacheHits++;return cached;}
        using var img=texture.GetImage();_uxHeadReadbacks++;
        int top=0;bool found=false;
        for(int y=0;y<img.GetHeight()&&!found;y++)for(int x=0;x<img.GetWidth();x++)
            if(img.GetPixel(x,y).A>=16f/255){top=y;found=true;break;}
        return _uxHeadHeightCache[key]=SourceEnemyUiAnchor.Height(img.GetWidth(),img.GetHeight(),top,PhoneLayout,img.GetWidth()==96,"normal");
    }
    private float UxEnemyGap()
    {
        float gap=float.MaxValue;
        var s=_view?.Combat;if(s==null)return gap;
        for(int i=1;i<s.Enemies.Count;i++)
        {
            float left=_enemyProjection?.Invoke(i-1)?.Feet.X??Size.X*(.60f+(i-1)*.12f);
            float right=_enemyProjection?.Invoke(i)?.Feet.X??Size.X*(.60f+i*.12f);
            gap=Math.Min(gap,Math.Abs(right-left)/BattleUnit);
        }
        return gap;
    }
    private void PositionUxEnemies()
    {
        if(_uxBattle==null||!GodotObject.IsInstanceValid(_uxBattle)||_view?.Combat is not {} state)return;float u=BattleUnit;float bottom=Size.Y-(PhoneLayout?293:285)*u;
        float gap=UxEnemyGap();bool uniformStack=_uxEnemies.Any(h=>SourceIntentLayout.ForState(state,h.Index,PhoneLayout,gap,false,_textScale)?.Stack==true);
        foreach(var h in _uxEnemies){if(!GodotObject.IsInstanceValid(h.Hit))continue;var point=_enemyProjection?.Invoke(h.Index);float x=point?.Feet.X??Size.X*(.60f+h.Index*.12f);float width=(_uxEnemies.Count==1?420:176)*u;float height=Math.Max(53*u,53*u*_textScale);float lx=Math.Clamp(x-width/2,24*u,Size.X-width-24*u);
            float head=point?.Head.Y??Size.Y*.25f;var enemy=_view.Combat.Enemies[h.Index];var tex=Art("enemies",enemy.EnemyId);if(tex!=null&&point!=null)head=point.Value.Feet.Y-UxEnemyHeadHeight(tex)*u;
            var intentSpec=SourceIntentLayout.ForState(state,h.Index,PhoneLayout,gap,uniformStack,_textScale);
            SourceIntentTag.Update(h.Intent,h.Title,intentSpec,u,PhoneLayout,_textScale);
            float iw=h.Intent.GetMeta("source_intent_width",72*u).AsSingle(),ih=h.Intent.GetMeta("source_intent_height",40*u).AsSingle();float iy=Math.Max(60*u,head-ih-8*u);h.Hit.Position=new Vector2(lx,iy);h.Hit.Size=new Vector2(width,Math.Max(MinimumHit,bottom-iy));h.Intent.GlobalPosition=new Vector2(Math.Clamp(x-iw/2,8*u,Size.X-iw-8*u),iy);h.Intent.Size=new Vector2(iw,ih);h.Ledger.GlobalPosition=new Vector2(lx,bottom-height);h.Ledger.Size=new Vector2(width,height);h.Bar.Size=new Vector2(width-16*u,Math.Max(20*u,20*u*_textScale));
            var edge=h.Index==_preferredTarget?SourcePalette.Brass:SourcePalette.NightEdge;SourceIntentTag.Highlight(h.Intent,h.Index==_preferredTarget);SourceBattleDecor.LedgerEdge(h.Ledger,h.Index==_preferredTarget,u);if(h.Bar.GetParent() is Control ledgerBody)SourceBattleDecor.Shield(ledgerBody,h.Bar,new Vector2(8,PhoneLayout?29:25)*u,width-16*u,enemy.Block,u,_textScale,PhoneLayout,true);}
    }
    private void RefreshUxReadouts()
    {
        if(_view?.Combat is not {} s)return;var p=s.Player;
        if(_uxHpBar!=null&&GodotObject.IsInstanceValid(_uxHpBar)){_uxHpBar.Hp=p.Hp;_uxHpBar.Maximum=p.MaxHp;_uxHpBar.Block=p.Block;_uxHpBar.IceBlock=p.IceBlock;_uxHpBar.PredictedLoss=s.HideIntents==true?0:Math.Max(0,_uxIncoming-p.Block-p.IceBlock);_uxHpBar.QueueRedraw();Set(_hpReadout,p.Hp+" / "+p.MaxHp);}
        if(_uxHpBar!=null&&GodotObject.IsInstanceValid(_uxHpBar)&&_uxHpBar.GetParent() is Control hpBody&&hpBody.GetParent() is Control heroPanel)SourceBattleDecor.Shield(hpBody,_uxHpBar,new Vector2(8,8)*BattleUnit,heroPanel.Size.X-16*BattleUnit,p.Block,BattleUnit,_textScale,PhoneLayout);
        if(_uxOrb!=null&&GodotObject.IsInstanceValid(_uxOrb)){_uxOrb.Energy=p.Energy;_uxOrb.Maximum=TurnEnergyCap(s);_uxOrb.QueueRedraw();}
        RefreshSourceForecast(s);
        foreach(var h in _uxEnemies){if(h.Index>=s.Enemies.Count)continue;var e=s.Enemies[h.Index];h.Bar.Hp=e.Hp;h.Bar.Maximum=e.MaxHp;h.Bar.Block=e.Block;h.Bar.QueueRedraw();h.Hp.Text=e.Hp+" / "+e.MaxHp;h.Hp.RefreshSourceAlignment();if(h.Bar.GetParent() is Control ledgerBody)SourceBattleDecor.Shield(ledgerBody,h.Bar,new Vector2(8,PhoneLayout?29:25)*BattleUnit,h.Ledger.Size.X-16*BattleUnit,e.Block,BattleUnit,_textScale,PhoneLayout,true);}
        AlignUxRichLine(_uxForecast);AlignUxRichLine(_uxDetail);
    }
    private void BuildUxTargetBanner(PlaySelection play)
    {
        var p=UxPanel(_uxBattle,"FieldTargetBanner",new Rect2(Size.X*.25f,100*BattleUnit,Size.X*.5f,MinimumHit+16));var row=Row(p);Text(row,play.Card.Def.Name+" · 対象の敵を選ぶ",20,SourcePalette.Paper).SizeFlagsHorizontal=SizeFlags.ExpandFill;Button(row,"使用を取り消す",CloseAllOverlays);
    }
    private void SyncUxHand()
    {
        var s=InputCombat??_view?.Combat;if(s==null||_uxHand==null)return;_uxHand.Size=Size;float u=BattleUnit;float cs=UxCardScale;int n=s.Player.Hand.Count;float cw=200*cs;float spacing,shift=0;
        if(PhoneLayout){float sw=Size.X/u;float tilt=Math.Max(0,(n-1)/2f*3)*Mathf.Pi/180;float over=Math.Max(0,145*Mathf.Sin(tilt)-100*(1-Mathf.Cos(tilt)))*cs;float left=24+150+24+over,right=sw-305;spacing=n>1?Math.Max(20,Math.Min(cw+12,(right-left-cw)/(n-1))):0;shift=(left+right)/2-sw/2;}
        else spacing=n==0?0:Math.Min(cw+12,1180f/n);
        var live=s.Player.Hand.Select(c=>c.Uid).ToHashSet();var removed=_uxTiles.Keys.Where(x=>!live.Contains(x)).ToArray();bool removedPlayed=UxRemovedPlayedCard(removed);int newOrdinal=0;foreach(var uid in removed){var gone=_uxTiles[uid];UxCancelHandHover(uid,false);UxCancelHandDeal(uid,false);_uxTiles.Remove(uid);_uxSeats.Remove(uid);gone.MouseFilter=MouseFilterEnum.Ignore;var tween=CreateTween();tween.TweenProperty(gone,"modulate:a",0f,_uxRetirement?.Invoke(gone.GetMeta("definition_id","").AsString())??.25);tween.TweenCallback(Callable.From(()=>{if(GodotObject.IsInstanceValid(gone))gone.QueueFree();}));}
        for(int i=0;i<n;i++)
        {
            var c=s.Player.Hand[i];string uid=c.Uid;bool added=!_uxTiles.TryGetValue(uid,out var tile);
            if(added){tile=new CardTile{Name="Hand_"+uid,CardUid=uid,IsBattleHand=true,Text="",MouseFilter=MouseFilterEnum.Stop,Size=new Vector2(200,290),CustomMinimumSize=new Vector2(200,290),PivotOffset=new Vector2(100,145)};_uxHand.AddChild(tile);_uxTiles[uid]=tile;
                tile.MouseEntered+=()=>HoverUxHand(uid,true);tile.MouseExited+=()=>HoverUxHand(uid,false);tile.Pressed+=()=>{if(!IsInputBlocked&&_overlay==null&&!tile.ConsumeInspection()&&InputCombat?.Player.Hand.FirstOrDefault(x=>x.Uid==uid) is {} card)CardActions(card);};tile.Inspect=()=>{var card=InputCombat?.Player.Hand.FirstOrDefault(x=>x.Uid==uid);if(card!=null)InspectCard(card);};
                tile.SecondaryAction=()=>{var st=InputCombat;if(st!=null&&!IsInputBlocked&&SetBase.CanSetCard(st,uid))Do(new Command_SetCard{CardUid=uid});else tile.Inspect?.Invoke();};
                tile.DragPayload=()=>new Godot.Collections.Dictionary{{"uid",uid},{"epoch",_uxEpoch}};tile.DragStarted=()=>StartHandDrag(uid);tile.DragEnded=EndHandDrag;tile.DragPreview=()=>PhoneLayout?new Control{MouseFilter=MouseFilterEnum.Ignore,CustomMinimumSize=Vector2.One}:MakeUxDragPreview(uid);
            }
            bool playable=FlowRules.PlayBlocked(s,c)==null;bool settable=s.ReactionMode!="hold-manual"&&SetBase.CanSetCard(s,uid);tile.AllowDrag=playable&&!IsInputBlocked;tile.Disabled=IsInputBlocked;tile.SetMeta("definition_id",c.Def.Id);
            SourceCardFace.Fill(tile,c,s,_textScale,PhoneLayout,_preferredTarget,Effects.EffectiveCost(s,c),s.Player.ImpulseUids.Contains(uid)?"衝動":null,playable||settable);
            float center=(n-1)/2f;float dy=Math.Abs(i-center)*10;if(!playable&&!settable&&s.Phase=="player-turn")dy+=16;
            float handY=PhoneLayout?-3:11;float cx=Size.X/2+((i-center)*spacing+shift)*u;float cy=Size.Y-(handY+145*cs)*u+dy*u;
            var pos=new Vector2(cx-100,cy-145);float rot=(i-center)*3;_uxSeats[uid]=(pos,rot,i);
            if(added)UxStartHandDeal(uid,tile,SourceHandDealMotion.Delay(newOrdinal++,removed.Length>0,removedPlayed));
            else if(!UxKeepHandDeal(uid,tile)&&!UxKeepHandHover(uid,tile)&&_uxDrag!=uid&&_uxHover!=uid){tile.Scale=Vector2.One*(cs*u);tile.Position=pos;tile.RotationDegrees=rot;}
            if(_uxDrag!=uid&&_uxHover!=uid){_uxHand.MoveChild(tile,_uxHand.GetChildCount()-1);tile.ZIndex=0;}
            // Incoming flight and its delayed cue never acquire or extend an input fence.
        }
        if(_uxHover!=null&&_uxTiles.ContainsKey(_uxHover))HoverUxHand(_uxHover,true);
    }
    private Control MakeUxDragPreview(string uid)
    {
        var c=InputCombat?.Player.Hand.FirstOrDefault(x=>x.Uid==uid);var p=new Control{MouseFilter=MouseFilterEnum.Ignore};if(c==null)return p;
        var tile=new CardTile{MouseFilter=MouseFilterEnum.Ignore,Size=new Vector2(200,290),Scale=Vector2.One*(BattleUnit*UxCardScale),Position=new Vector2(-100,-145)*BattleUnit};p.AddChild(tile);SourceCardFace.Fill(tile,c,InputCombat,_textScale,PhoneLayout,_preferredTarget,Effects.EffectiveCost(InputCombat,c));return p;
    }
    private void HoverUxHand(string uid,bool enter)
    {
        if(!_uxTiles.TryGetValue(uid,out var tile)||!_uxSeats.TryGetValue(uid,out var seat))return;
        if(enter){if(IsInputBlocked||_overlay!=null||_uxDrag!=null)return;if(_uxHover!=null&&_uxHover!=uid)HoverUxHand(_uxHover,false);_uxHover=uid;_uxHand.MoveChild(tile,_uxHand.GetChildCount()-1);tile.ZIndex=10;UxStartHandHover(uid,tile,seat.Pos-new Vector2(0,(70+(PhoneLayout?17:19))*BattleUnit),Vector2.One*(1.18f*BattleUnit),0);}
        else{if(_uxDrag==uid)return;if(_uxHover==uid)_uxHover=null;tile.ZIndex=0;UxStartHandHover(uid,tile,seat.Pos,Vector2.One*(UxCardScale*BattleUnit),seat.Rotation);RestoreUxHandOrder();}
    }
    private void RestoreUxHandOrder()
    {
        foreach(var seat in _uxSeats.OrderBy(x=>x.Value.Order))if(_uxTiles.TryGetValue(seat.Key,out var live))_uxHand.MoveChild(live,_uxHand.GetChildCount()-1);
    }
    private void StartHandDrag(string uid)
    {
        UxCancelHandHover(uid,false);UxCancelHandDeal(uid,false);_uxDrag=uid;_uxHover=null;if(_uxTiles.TryGetValue(uid,out var t)&&_uxSeats.TryGetValue(uid,out var seat)){_uxHand.MoveChild(t,_uxHand.GetChildCount()-1);t.Position=seat.Pos;t.RotationDegrees=0;t.Scale=Vector2.One*UxCardScale*BattleUnit;t.ZIndex=20;}
        UpdateHandAim();
    }
    private void EndHandDrag()
    {
        string uid=_uxDrag;_uxDrag=null;if(_uxAim!=null&&GodotObject.IsInstanceValid(_uxAim)){_uxAim.Active=false;_uxAim.QueueRedraw();}
        if(uid!=null)HoverUxHand(uid,false);
    }
    private string DecodeHandDrag(Variant data)
    {
        if(data.VariantType!=Variant.Type.Dictionary)return null;var d=data.AsGodotDictionary();if(!d.ContainsKey("uid")||!d.ContainsKey("epoch")||d["epoch"].AsInt64()!=_uxEpoch)return null;return d["uid"].AsString();
    }
    private void UpdateHandAim()
    {
        if(_uxAim==null||!GodotObject.IsInstanceValid(_uxAim))return;
        if(_uxDrag==null||IsInputBlocked||_overlay!=null||!_uxTiles.TryGetValue(_uxDrag,out var tile)){_uxAim.Active=false;_uxAim.QueueRedraw();return;}
        var pointer=GetGlobalMousePosition();_uxAim.Active=PhoneLayout&&pointer.Y<Size.Y*.64f;_uxAim.End=pointer;_uxAim.Start=tile.GetGlobalTransformWithCanvas()*new Vector2(100,-8);_uxAim.Unit=BattleUnit;_uxAim.Size=Size;
        var masks=_uxEnemies.SelectMany(x=>new[]{x.Intent.GetGlobalRect(),x.Ledger.GetGlobalRect()}).ToList();int over=_enemyResolver?.Invoke(pointer)??-1;
        foreach(var e in _uxEnemies){if(e.Index==over)continue;var p=_enemyProjection?.Invoke(e.Index);if(p!=null)masks.Add(new Rect2(p.Value.Head.X-40*BattleUnit,p.Value.Head.Y,80*BattleUnit,p.Value.Feet.Y-p.Value.Head.Y));}
        _uxAim.Blockers=masks;var hero=_uxLeaderProjection?.Invoke();_uxAim.LeaderBody=hero is Vector2 v?new Rect2(v.X-40*BattleUnit,v.Y-150*BattleUnit,80*BattleUnit,145*BattleUnit):null;
        _uxAim.Note=over<0&&InputCombat?.Enemies.Count(x=>x.Hp>0)>1?(_preferredTarget>=0?"離すと "+(_preferredTarget+1)+" の敵へ":"離すと 敵を選ぶ"):"";
        if(over>=0&&over!=_preferredTarget){foreach(var e in _uxEnemies){var edge=e.Index==over?SourcePalette.Brass:SourcePalette.NightEdge;SourceIntentTag.Highlight(e.Intent,e.Index==over);SourceBattleDecor.LedgerEdge(e.Ledger,e.Index==over,BattleUnit);}}
        _uxAim.QueueRedraw();
    }
    private void UxProcess(double delta)
    {
        if(_uxLastSize!=Size){_uxLastSize=Size;if(!_uxRelayoutQueued){_uxRelayoutQueued=true;EndHandDrag();CallDeferred(nameof(UxRelayout));}}
        UxTickHandDeals(delta);UxTickHandHovers(delta);PositionUxEnemies();UpdateHandAim();AlignUxRichLine(_uxForecast);AlignUxRichLine(_uxDetail);
    }
    private static void AlignUxRichLine(RichTextLabel label)
    {
        if(label==null||!GodotObject.IsInstanceValid(label)||!label.HasMeta("source_text_rect"))return;
        var rect=label.GetMeta("source_text_rect").AsRect2();
        float height=Math.Max(rect.Size.Y,label.GetContentHeight());
        label.Size=new Vector2(rect.Size.X,height);
        label.Position=rect.Position+new Vector2(0,(rect.Size.Y-height)/2);
    }
    private void UxRelayout(){_uxRelayoutQueued=false;if(IsInsideTree()&&CurrentRoute=="combat")Rebuild();}
    private RunState UxOrdinaryProjection(RunState current)
    {
        if(current.Combat is not {} now)return current;var display=_presentedState?.Invoke()??_view?.Combat;if(display==null)return current;
        var p=now.Player with{Hp=display.Player.Hp,MaxHp=display.Player.MaxHp,Block=display.Player.Block,IceBlock=display.Player.IceBlock,Permanents=display.Player.Permanents};return current with{Combat=now with{Player=p,Enemies=display.Enemies.Select((e,i)=>i<now.Enemies.Count&&now.Enemies[i].EnemyId==e.EnemyId?now.Enemies[i] with{Hp=e.Hp,MaxHp=e.MaxHp,Block=e.Block}:e).ToList()}};
    }
    private void UxPresentEvent(GameEvent evt)
    {
        var owner=_uxEventOwner?.Invoke(evt)??_activeChange;
        if(_uxOrdinary?.Invoke(evt)==true){if(_session?.CurrentRun is {} live)_view=UxOrdinaryProjection(live);RefreshUxReadouts();return;}
        if(owner!=null){_activeChange=owner;_costPresented=_uxPaid.Contains(owner.Revision);if(_uxIncomingOwner!=owner.Revision&&owner.Previous?.Combat is {} previous){_uxIncomingOwner=owner.Revision;_uxIncoming=Effects.IncomingTotal(previous);_uxSkipFirst=previous.NullifyNextAttack==true;_uxLanded=false;}}
        PresentEventLegacy(evt);if(_costPresented&&owner!=null)_uxPaid.Add(owner.Revision);
        if(_uxPhase!=null&&GodotObject.IsInstanceValid(_uxPhase)){if(evt is GameEvent_TurnEnded)_uxPhase.Text="敵の番";else if(evt is GameEvent_EnemyActionExecuting e)_uxPhase.Text="敵の番 "+(e.EnemyIndex+1)+"/"+(_view?.Combat?.Enemies.Count??0);else if(evt is GameEvent_TurnStarted)_uxPhase.Text="あなたの番";}
        RefreshUxReadouts();
    }
    private void UxNumericContact(GameEvent evt)
    {
        var owner=_uxEventOwner?.Invoke(evt)??_activeChange;var amount=_uxIncomingContact?.Invoke(evt);
        if(amount!=null&&owner!=null){if(_uxIncomingOwner!=owner.Revision&&owner.Previous?.Combat is {} prev){_uxIncomingOwner=owner.Revision;_uxIncoming=Effects.IncomingTotal(prev);_uxSkipFirst=prev.NullifyNextAttack==true;_uxLanded=false;}_uxLanded=true;if(_uxSkipFirst)_uxSkipFirst=false;else _uxIncoming=Math.Max(0,_uxIncoming-Math.Max(0,amount.Value));}
        PresentNumericContactLegacy(evt);RefreshUxReadouts();
    }
}
