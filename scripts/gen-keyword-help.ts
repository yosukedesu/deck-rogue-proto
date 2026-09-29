// scripts/gen-keyword-help.ts — src/ui/keywordHelp.ts の KEYWORD_HELP を Unity の C# (Assets/Game/KeywordHelp.g.cs) へ生成する (2026-09-07 M2)。
// 用語解説の一次資料は TS 側 (表示網羅テストが守る)。Unity のツールチップは生成物を読むだけ。手で編集しない。
import { writeFileSync } from 'node:fs'
import { KEYWORD_HELP } from '../src/ui/keywordHelp.ts'

// ---- Unity 用の段 (2026-09-29 戦闘画面のレビュー p15) ----
// TS の KEYWORD_HELP はプロト (Web・CLI) の「伏せ」の語で書かれている (2026-09-12 ユーザー「ブラウザ版は罠とか伏せでいい」)。
// Unity (絵本の肌) と data の文面は「からくり」の語なので、キーと本文をここで写してから書く。TS の keywordHelp.ts は触らない。
// 旧: そのまま写していた＝キーが「伏せる／伏せ場／伏せ破壊」で、Unity の文 (仕込む・からくり・仕込み札) からは一度も当たらず、
// 虚弱・拘束・バランス崩し・霊気の説明にはプロトの「伏せ札」がそのまま出ていた。
// 動作 (ボタン・判・ログ) は 09-15 battle-v2 の「発動／温存」、置き場は「からくり」、札は「仕込み札」。「発動」は置換しない。

/** キーの付け替え (本文の写しより先に、元のキーで引く) */
const UNITY_KEYS: Record<string, string> = { 伏せ破壊: 'からくり壊し', 伏せ場: 'からくり', 伏せる: '仕込む' }

/** Unity にだけ足すキー (プロトには「伏せ札」の項目が無い)。直前のキーの後ろに差し込む */
const UNITY_EXTRA: { after: string; key: string; text: string }[] = [
  {
    after: '仕込む',
    key: '仕込み札',
    text: 'からくりに仕込んで使う札（カードのタイプ）。手札から直接は出せない。仕込んだ次のターンから敵の番に鳴り、行動の実値を見てから発動するか温存するかを選ぶ。鳴らないまま期限が来ると捨て札へ',
  },
]

/** 固有名 (カード・敵の名前) は写さない＝先に退避して最後に戻す */
const PROPER_NAMES = ['弾け実の罠', '罠師の茂み', '囁きの罠', '罠壊し', '罠使い']

/** 本文の写し。長い語・特定の句から順に (一括置換で既存の語を壊さないよう、写す句を名指しする) */
const UNITY_BODY: readonly [string, string][] = [
  ['伏せ場に置く＝罠を仕込む', 'からくりに置く'], // 「仕込む」の説明で「からくりに置く＝からくりを仕込む」と同語反復にならないように
  ['道化の破壊', '道化のからくり壊し'],
  ['伏せ破壊', 'からくり壊し'],
  ['伏せ場', 'からくり'],
  ['伏せ札', '仕込み札'],
  ['伏せておく', '仕込んでおく'],
  ['伏せた', '仕込んだ'],
  ['伏せる', '仕込む'],
  ['罠を仕込む', 'からくりを仕込む'],
  ['罠の当たり', 'からくりの当たり'],
  ['動かせる', '発動できる'], // 窓の名前 (被攻撃前…) の説明: 窓のボタンは「発動」
]

function toUnity(text: string): string {
  let s = text
  PROPER_NAMES.forEach((n, i) => { s = s.split(n).join(`${i}`) })
  for (const [from, to] of UNITY_BODY) s = s.split(from).join(to)
  PROPER_NAMES.forEach((n, i) => { s = s.split(`${i}`).join(n) })
  return s
}

const unityTerms: [string, string][] = []
for (const [k, v] of Object.entries(KEYWORD_HELP)) {
  const key = UNITY_KEYS[k] ?? k
  unityTerms.push([key, toUnity(v)])
  for (const x of UNITY_EXTRA) if (x.after === key) unityTerms.push([x.key, x.text])
}

// 検査: TS 側の文が変わって写し漏れが出たら生成を止める (意図どおり。表に句を足してから生成し直す)
{
  const bad: string[] = []
  const keys = new Set<string>()
  for (const [k, v] of unityTerms) {
    if (keys.has(k)) bad.push(`キーが重複: ${k}`)
    keys.add(k)
    for (const s of [k, v]) {
      let rest = s
      for (const n of PROPER_NAMES) rest = rest.split(n).join('')
      if (rest.includes('伏せ')) bad.push(`「伏せ」が残っている: ${k}`)
      if (rest.includes('罠')) bad.push(`固有名でない「罠」が残っている: ${k}`)
    }
  }
  for (const need of ['仕込む', '仕込み札', 'からくり', 'からくり壊し']) if (!keys.has(need)) bad.push(`キーが無い: ${need}`)
  if (bad.length > 0) throw new Error(`gen-keyword-help: Unity の語彙へ写せない\n${bad.join('\n')}`)
}

const esc = (s: string) => s.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n')
const lines: string[] = []
lines.push('// KeywordHelp.g.cs — scripts/gen-keyword-help.ts が src/ui/keywordHelp.ts から生成。手で編集しない。')
lines.push('// 本文とキーは Unity の「からくり」の語彙に写してある (伏せる→仕込む・伏せ場→からくり・伏せ破壊→からくり壊し。2026-09-29)。')
lines.push('using System.Collections.Generic;')
lines.push('')
lines.push('namespace DeckRogue.Game')
lines.push('{')
lines.push('    public static class KeywordHelp')
lines.push('    {')
lines.push('        public static readonly Dictionary<string, string> Terms = new Dictionary<string, string>')
lines.push('        {')
for (const [k, v] of unityTerms) lines.push(`            { "${esc(k)}", "${esc(v)}" },`)
lines.push('        };')
lines.push('')
lines.push('        /// <summary>文中に含まれる用語を長い順に探す (「呪いの烙印」が「烙印」より先に当たるように)</summary>')
lines.push('        public static List<string> FindIn(string text)')
lines.push('        {')
lines.push('            var found = new List<string>();')
lines.push('            if (string.IsNullOrEmpty(text)) return found;')
lines.push('            foreach (var k in Keys) if (text.Contains(k) && !found.Contains(k)) found.Add(k);')
lines.push('            return found;')
lines.push('        }')
lines.push('')
lines.push('        static List<string> _keys;')
lines.push('        static List<string> Keys')
lines.push('        {')
lines.push('            get')
lines.push('            {')
lines.push('                if (_keys == null) { _keys = new List<string>(Terms.Keys); _keys.Sort((a, b) => b.Length.CompareTo(a.Length)); }')
lines.push('                return _keys;')
lines.push('            }')
lines.push('        }')
lines.push('    }')
lines.push('}')
writeFileSync('unity/Assets/Game/KeywordHelp.g.cs', lines.join('\n') + '\n')
console.log(`KeywordHelp.g.cs: ${unityTerms.length} 語 (Unity の語彙)`)
