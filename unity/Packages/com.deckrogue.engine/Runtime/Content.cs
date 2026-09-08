// Content.cs — data/*.json の読み込みと参照 (src/engine/content.ts の移植)。
// JSON は本実装へそのまま持っていく共通資産。スキーマは types.ts (= Generated/Types.g.cs) が一次資料。
//
// TS は import assertion でモジュール読込時に JSON を取り込むが、C# は明示的に Content.Load(dataDir) を
// 1回呼ぶ (テストは "../../src/data")。読み込み後の参照関数は TS と同じ順序・同じ例外メッセージ。

using System;
using System.Collections.Generic;
using System.IO;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckRogue.Engine
{
    public static class Content
    {
        // ---- 読み込んだデータ (Load 前は空。TS の module-level const に相当) ----
        public static IReadOnlyList<CardDef> AllCards { get; private set; } = new List<CardDef>();
        public static IReadOnlyList<EnemyDef> AllEnemies { get; private set; } = new List<EnemyDef>();
        public static IReadOnlyList<EncounterDef> AllEncounters { get; private set; } = new List<EncounterDef>();
        public static IReadOnlyList<DeckDef> AllDecks { get; private set; } = new List<DeckDef>();
        public static IReadOnlyList<LeaderDef> AllLeaders { get; private set; } = new List<LeaderDef>();
        public static IReadOnlyList<RelicDef> AllRelics { get; private set; } = new List<RelicDef>();
        public static IReadOnlyList<EventDef> AllEvents { get; private set; } = new List<EventDef>();

        /// <summary>手書きレシピの生の配列 (要素は { a, b, result: CardDef })。</summary>
        public static JArray AllFusionsRaw { get; private set; } = new JArray();

        /// <summary>
        /// 手書きレシピ (data/fusions.json)。TS 側の FusionRecipe は fusion.ts のローカル型で
        /// Generated に無いため、record 定義は Fusion.cs が持つ (ここは読んで公開するだけ)。
        /// </summary>
        public static IReadOnlyList<FusionRecipe> AllFusions { get; private set; } = new List<FusionRecipe>();

        private static readonly Dictionary<string, CardDef> CardIndex = new Dictionary<string, CardDef>();
        private static readonly Dictionary<string, EnemyDef> EnemyIndex = new Dictionary<string, EnemyDef>();
        private static readonly Dictionary<string, EncounterDef> EncounterIndex = new Dictionary<string, EncounterDef>();
        private static readonly Dictionary<string, DeckDef> DeckIndex = new Dictionary<string, DeckDef>();
        private static readonly Dictionary<string, LeaderDef> LeaderIndex = new Dictionary<string, LeaderDef>();
        private static readonly Dictionary<string, RelicDef> RelicIndex = new Dictionary<string, RelicDef>();
        private static readonly Dictionary<string, EventDef> EventIndex = new Dictionary<string, EventDef>();

        private static bool _loaded;

        /// <summary>読み込み済みか (Load を忘れたまま参照した時の診断用)</summary>
        public static bool IsLoaded => _loaded;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            // JSON にあって型に無いキーは黙って無視する (TS の as キャストと同じ許容度)
            MissingMemberHandling = MissingMemberHandling.Ignore,
            // 日付らしき文字列を DateTime に化けさせない (カード名・説明文を素の string で読む)
            DateParseHandling = DateParseHandling.None,
        };

        /// <summary>data/*.json を読み込む。テストは Content.Load("../../src/data")。</summary>
        public static void Load(string dataDir)
        {
            LoadFrom(file =>
            {
                var path = Path.Combine(dataDir, file);
                if (!File.Exists(path)) throw new InvalidOperationException($"データファイルが見つからない: {path}");
                return File.ReadAllText(path);
            });
        }

        /// <summary>ファイル名→JSON文字列 の関数から読み込む (Android の APK 内など File が使えない環境用。Unity は Resources の TextAsset を渡す)。</summary>
        public static void LoadFrom(Func<string, string> read)
        {
            // 色は JSON に書かず、ファイル単位でここで付与する (JSONを本実装へ持ち込む際の共通規約)
            var cards = new List<CardDef>();
            cards.AddRange(WithColor(ParseList<CardDef>(read("cards.green.json"), "cards.green.json"), CardColors.Green));
            cards.AddRange(WithColor(ParseList<CardDef>(read("cards.blue.json"), "cards.blue.json"), CardColors.Blue));
            cards.AddRange(WithColor(ParseList<CardDef>(read("cards.red.json"), "cards.red.json"), CardColors.Red));
            cards.AddRange(WithColor(ParseList<CardDef>(read("cards.white.json"), "cards.white.json"), CardColors.White));
            cards.AddRange(WithColor(ParseList<CardDef>(read("cards.black.json"), "cards.black.json"), CardColors.Black));
            AllCards = cards;

            AllEnemies = ParseList<EnemyDef>(read("enemies.json"), "enemies.json");
            AllEncounters = ParseList<EncounterDef>(read("encounters.json"), "encounters.json");
            AllDecks = ParseList<DeckDef>(read("decks.json"), "decks.json");
            AllLeaders = ParseList<LeaderDef>(read("leaders.json"), "leaders.json");
            AllRelics = ParseList<RelicDef>(read("relics.json"), "relics.json");
            AllEvents = ParseList<EventDef>(read("events.json"), "events.json");
            AllFusionsRaw = JArray.Parse(read("fusions.json"));
            AllFusions = DeserializeFusions<FusionRecipe>();

            // 索引 (TS の find と同じ「先頭一致」を保つため、重複 id は最初の1件だけ登録する)
            Reindex(CardIndex, AllCards, c => c.Id);
            Reindex(EnemyIndex, AllEnemies, e => e.Id);
            Reindex(EncounterIndex, AllEncounters, e => e.Id);
            Reindex(DeckIndex, AllDecks, d => d.Id);
            Reindex(LeaderIndex, AllLeaders, l => l.Id);
            Reindex(RelicIndex, AllRelics, r => r.Id);
            Reindex(EventIndex, AllEvents, e => e.Id);
            _loaded = true;
        }

        /// <summary>fusions.json を任意の型へ流し込む (レシピ型を差し替えたい時の口)。</summary>
        public static IReadOnlyList<T> DeserializeFusions<T>()
        {
            return AllFusionsRaw.ToObject<List<T>>(JsonSerializer.Create(Settings)) ?? new List<T>();
        }

        private static List<T> ParseList<T>(string json, string name)
        {
            if (json == null) throw new InvalidOperationException($"データファイルが見つからない: {name}");
            var list = JsonConvert.DeserializeObject<List<T>>(json, Settings);
            if (list == null) throw new InvalidOperationException($"データファイルを読めない: {name}");
            return list;
        }

        private static IEnumerable<CardDef> WithColor(IReadOnlyList<CardDef> cards, string color)
        {
            for (int i = 0; i < cards.Count; i++) yield return cards[i] with { Color = color };
        }

        private static void Reindex<T>(Dictionary<string, T> index, IReadOnlyList<T> items, Func<T, string> idOf)
        {
            index.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var id = idOf(items[i]);
                if (!index.ContainsKey(id)) index[id] = items[i];
            }
        }

        /// <summary>
        /// 敵ID or 編成ID を編成メンバー列に解決する (確定済みルール表「戦闘形式」)。
        /// 編成IDが優先。どちらでもなければエラー。敵ID直指定はソロ編成 (後方互換)
        /// </summary>
        public static IReadOnlyList<EncounterMember> ResolveEncounter(string id)
        {
            if (EncounterIndex.TryGetValue(id, out var enc)) return enc.Members;
            if (EnemyIndex.ContainsKey(id)) return new List<EncounterMember> { new EncounterMember { EnemyId = id } };
            throw new InvalidOperationException($"未定義の敵/編成: {id}");
        }

        /// <summary>表示名 (編成名 or 敵名)</summary>
        public static string EncounterName(string id)
        {
            if (EncounterIndex.TryGetValue(id, out var enc)) return enc.Name;
            return GetEnemyDef(id).Name;
        }

        public static EventDef GetEventDef(string id)
        {
            if (!EventIndex.TryGetValue(id, out var def)) throw new InvalidOperationException($"未定義イベント: {id}");
            return def;
        }

        public static RelicDef GetRelicDef(string id)
        {
            if (!RelicIndex.TryGetValue(id, out var def)) throw new InvalidOperationException($"未定義レリック: {id}");
            return def;
        }

        /// <summary>A型レリックを「戦闘開始時から場にある不可視の置物」として実体化する (リーダーパッシブと同型)</summary>
        public static CardInstance BuildRelicPermanent(RelicDef relic)
        {
            return new CardInstance
            {
                Uid = $"relic_{relic.Id}",
                Innate = true, // 戦闘開始時から場にある = 置物数参照で数えない (2026-08-26)
                Def = new CardDef
                {
                    Id = $"{relic.Id}_passive",
                    Name = relic.Name,
                    Cost = 0,
                    Type = CardTypes.Permanent,
                    Color = CardColors.Green,
                    Effects = relic.Effects ?? new List<DeclarativeEffect>(),
                },
            };
        }

        public static LeaderDef GetLeaderDef(string id)
        {
            if (!LeaderIndex.TryGetValue(id, out var def)) throw new InvalidOperationException($"未定義リーダー: {id}");
            return def;
        }

        /// <summary>リーダーの色アイデンティティで使えるデッキか (統率者方式)</summary>
        public static bool DeckAllowedForLeader(LeaderDef leader, DeckDef deck)
        {
            for (int i = 0; i < leader.Colors.Count; i++)
                if (leader.Colors[i] == deck.Color) return true;
            return false;
        }

        /// <summary>リーダーのパッシブを「戦闘開始時から場にある置物」として実体化する</summary>
        public static CardInstance BuildLeaderPassive(LeaderDef leader)
        {
            return new CardInstance
            {
                Uid = $"leader_{leader.Id}",
                Innate = true, // 戦闘開始時から場にある = 置物数参照で数えない (2026-08-26)
                Def = new CardDef
                {
                    Id = $"{leader.Id}_passive",
                    Name = $"{leader.Name}の能力",
                    Cost = 0,
                    Type = CardTypes.Permanent,
                    Color = leader.Colors[0],
                    Effects = leader.Passive,
                },
            };
        }

        /// <summary>
        /// 負傷 (状態異常カード): 敵が捨て札に混入させる使用不可の死に札。
        /// onPlay 効果を持たないため isPlayableFromHand が自然に false になる。
        /// 報酬プール (AllCards) には含めない。色は便宜上 red (無色概念は未導入)
        /// </summary>
        public static readonly CardDef WoundDef = new CardDef
        {
            Id = "status_wound",
            Name = "負傷",
            Cost = 0,
            Type = CardTypes.Spell,
            Color = CardColors.Red,
            Effects = new List<DeclarativeEffect>(),
        };

        /// <summary>
        /// 火傷 (状態異常カード 2026-09-02 敵ギミック第1波)。本家StSのBurn相当:
        /// 使用不可の死に札で、**自ターン終了時に手札にあると自傷2**。戦闘終了で消える (デッキに残らない)。
        /// </summary>
        public static readonly CardDef ScaldDef = new CardDef
        {
            Id = "status_scald",
            Name = "火傷",
            Cost = 0,
            Type = CardTypes.Spell,
            Color = CardColors.Red,
            Effects = new List<DeclarativeEffect>(),
        };

        /// <summary>
        /// 呪いの烙印 (状態異常カード 2026-09-02 呪いイベント用)。火傷の恒久版・弱化形:
        /// 使用不可で、自ターン終了時に手札にあると自傷1。**ランのデッキに残る**
        /// </summary>
        public static readonly CardDef BrandDef = new CardDef
        {
            Id = "status_brand",
            Name = "呪いの烙印",
            Cost = 0,
            Type = CardTypes.Spell,
            Color = CardColors.Black,
            Effects = new List<DeclarativeEffect>(),
        };

        /// <summary>
        /// 仮初の烙印 (2026-09-02 StS2 Guilty式の時限呪い): 烙印と同じ滞留HP-1だが、
        /// 5戦すると自然に消える (CardInstance.ExpiresAfterBattles)
        /// </summary>
        public static readonly CardDef GuiltDef = new CardDef
        {
            Id = "status_guilt",
            Name = "仮初の烙印",
            Cost = 0,
            Type = CardTypes.Spell,
            Color = CardColors.Black,
            Effects = new List<DeclarativeEffect>(),
        };

        /// <summary>
        /// がらくた (状態異常カード): 罠壊しが山札に混ぜ込む使用不可の死に札。
        /// 負傷 (捨て札に混入) と違い山札へ直接混ざるため、すぐ引かされる = 手札事故を即座に作る。
        /// </summary>
        public static readonly CardDef JunkDef = new CardDef
        {
            Id = "status_junk",
            Name = "がらくた",
            Cost = 0,
            Type = CardTypes.Physical,
            Color = CardColors.Red,
            Effects = new List<DeclarativeEffect>(),
        };

        // TS 名そのままの別名 (並行翻訳で TS の定数名を書いても通るように)
        public static CardDef WOUND_DEF => WoundDef;
        public static CardDef SCALD_DEF => ScaldDef;
        public static CardDef BRAND_DEF => BrandDef;
        public static CardDef GUILT_DEF => GuiltDef;
        public static CardDef JUNK_DEF => JunkDef;

        public static CardDef GetCardDef(string id)
        {
            if (id == WoundDef.Id) return WoundDef;
            if (id == ScaldDef.Id) return ScaldDef;
            if (id == BrandDef.Id) return BrandDef;
            if (id == GuiltDef.Id) return GuiltDef;
            if (id == JunkDef.Id) return JunkDef;
            if (!CardIndex.TryGetValue(id, out var def)) throw new InvalidOperationException($"未定義カード: {id}");
            return def;
        }

        public static EnemyDef GetEnemyDef(string id)
        {
            if (!EnemyIndex.TryGetValue(id, out var def)) throw new InvalidOperationException($"未定義の敵: {id}");
            return def;
        }

        public static DeckDef GetDeckDef(string id)
        {
            if (!DeckIndex.TryGetValue(id, out var def)) throw new InvalidOperationException($"未定義デッキ: {id}");
            return def;
        }

        /// <summary>デッキ定義から実カードリストを構築 (同一カード複数枚は uid で区別)</summary>
        public static IReadOnlyList<CardInstance> BuildDeck(string deckId)
        {
            var deck = GetDeckDef(deckId);
            var cards = new List<CardInstance>();
            for (int e = 0; e < deck.Cards.Count; e++)
            {
                var entry = deck.Cards[e];
                var def = GetCardDef(entry.CardId);
                for (int i = 0; i < entry.Count; i++)
                {
                    cards.Add(new CardInstance { Uid = $"{entry.CardId}#{i}", Def = def });
                }
            }
            return cards;
        }

        /// <summary>デッキの総枚数 (UI 表示用)</summary>
        public static int DeckSize(DeckDef deck)
        {
            int sum = 0;
            for (int i = 0; i < deck.Cards.Count; i++) sum += deck.Cards[i].Count;
            return sum;
        }

        // ---- デバッグ・オーバーレイ (図鑑の調整モード = UI 専用。移植対象外のスタブ) ----
        /// <summary>常に false。調整案のライブ適用はブラウザUIの機能なので移植しない</summary>
        public static bool DebugOverridesActive() => false;
    }
}
