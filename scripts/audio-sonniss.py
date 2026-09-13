#!/usr/bin/env python3
"""Sonniss GDC バンドル (zip 群) から、音の設計書 (docs/audio-design.md §8) の当てのライブラリだけを抜き出す。

  python3 scripts/audio-sonniss.py index  <zipdir>            # zip ごとの中身 (ライブラリ=先頭のフォルダ名) を一覧して docs/design/audio/sonniss-index.json へ
  python3 scripts/audio-sonniss.py extract <zipdir> <outdir>  # 当てのライブラリ (LIBS の部分一致) の wav だけを outdir へ展開
  python3 scripts/audio-sonniss.py sheet <outdir>             # 鍵ごとに候補を並べた試聴シート docs/design/audio/se-sheet-sonniss.html を生成 (mp3 に変換して同梱)

ダウンロードは Cloudflare のため人が行う (D:\\assets\\sonniss など)。zip は読むだけで消さない。
"""
import sys, os, json, zipfile, re, subprocess, html

LIBS = ['steampunk', 'clock', 'mechanic', 'nuts and bolts', 'chess', 'board game', 'turning the page', 'paper',
        'melee', 'epic impact', 'magic', 'druid', 'whoosh', 'dreamcatcher', 'cymbals from hell vol. 3', 'coins',
        'monster', 'rpg orchestral', 'pure nature', 'organic user interface', 'retro game', 'videogame foley',
        'medieval', 'fantasy', 'sword', 'wood', 'impact', 'ui', 'user interface', 'foley', 'bells', 'doors']

# 鍵 → (ファイル名に含めたい語 (小文字・部分一致), 最大秒数)
KEYS = {
    'CardSet':            (['wind', 'ratchet', 'latch', 'lock', 'gear', 'mechanism', 'crank', 'cog'], 3),
    'ReactionTriggered':  (['spring', 'snap', 'release', 'trigger', 'click', 'twang'], 2.5),
    'SetCardExpired':     (['unwind', 'stop', 'release', 'down', 'drop', 'slow'], 3),
    'SetCardDestroyed':   (['wood', 'crack', 'break', 'crate', 'splinter'], 3),
    'ActionNegated':      (['dispel', 'fizzle', 'reverse', 'vanish', 'dissolve', 'negate'], 3),
    'CardPlayed':         (['card', 'flip', 'place', 'slide', 'tap'], 2),
    'CardsDrawn':         (['card', 'deal', 'draw', 'shuffle', 'riffle', 'page'], 2.5),
    'DamageDealt.player': (['axe', 'blade', 'impact', 'hit', 'slash', 'chop'], 2.5),
    'DamageDealt.enemy':  (['punch', 'body', 'thud', 'hit', 'flesh'], 2.5),
    'BlockGained':        (['shield', 'block', 'parry', 'wood'], 2.5),
    'HpHealed':           (['heal', 'bell', 'chime', 'sparkle', 'enchant'], 3),
    'CardExhausted':      (['burn', 'fire', 'flame', 'crumple', 'tear'], 3),
    'PermanentPlayed':    (['stone', 'thud', 'place', 'boom', 'drop', 'heavy'], 3),
    'EnemyDied':          (['death', 'die', 'monster', 'creature'], 3),
    'StatusInflicted':    (['curse', 'dark', 'poison', 'debuff', 'hex'], 3),
    'GrowthAdded':        (['power', 'buff', 'charge', 'rise', 'up'], 3),
    'TurnStarted':        (['swoosh', 'transition', 'drum', 'hit', 'stinger'], 2.5),
    'GoldStolen':         (['coin', 'gold', 'money', 'pouch'], 3),
    'ui.pick_relic':      (['discover', 'treasure', 'pickup', 'item', 'mystery', 'reveal'], 5),
    'ui.upgrade':         (['anvil', 'hammer', 'forge', 'blacksmith', 'metal'], 3),
    'ui.rest':            (['campfire', 'fire', 'crackle'], 40),
    'ui.node':            (['footstep', 'step', 'gravel', 'dirt', 'leaves'], 2),
    'ui.click':           (['click', 'button', 'tap', 'ui'], 1.5),
    'ui.win':             (['success', 'victory', 'discovery', 'win', 'fanfare'], 10),
    'ui.lose':            (['failure', 'fail', 'lose', 'defeat', 'sad'], 10),
}

def index(zipdir):
    out = {}
    for f in sorted(os.listdir(zipdir)):
        if not f.lower().endswith('.zip'): continue
        p = os.path.join(zipdir, f)
        try:
            z = zipfile.ZipFile(p)
        except Exception as e:
            print('skip', f, e); continue
        libs = {}
        for n in z.namelist():
            if not n.lower().endswith(('.wav', '.flac', '.ogg', '.mp3')): continue
            top = n.split('/')[0]
            libs.setdefault(top, 0); libs[top] += 1
        out[f] = libs
        print(f, len(libs), 'libs', sum(libs.values()), 'files')
    json.dump(out, open('docs/design/audio/sonniss-index.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

def wanted(top):
    t = top.lower()
    return any(k in t for k in LIBS)

def extract(zipdir, outdir):
    os.makedirs(outdir, exist_ok=True)
    n = 0
    for f in sorted(os.listdir(zipdir)):
        if not f.lower().endswith('.zip'): continue
        z = zipfile.ZipFile(os.path.join(zipdir, f))
        for info in z.infolist():
            if info.is_dir() or not info.filename.lower().endswith(('.wav', '.flac', '.ogg', '.mp3')): continue
            if not wanted(info.filename.split('/')[0]): continue
            dst = os.path.join(outdir, info.filename)
            if os.path.exists(dst): continue
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with z.open(info) as src, open(dst, 'wb') as d: d.write(src.read())
            n += 1
        print(f, 'done', n)
    print('extracted', n)

def duration(path):
    try:
        r = subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', path], capture_output=True, text=True)
        return float(r.stdout.strip())
    except Exception:
        return 999

def sheet(outdir, per_key=5):
    files = []
    for root, _, fs in os.walk(outdir):
        for f in fs:
            if f.lower().endswith(('.wav', '.flac', '.ogg', '.mp3')):
                files.append(os.path.join(root, f))
    print(len(files), 'files')
    mp3dir = 'docs/design/audio/se-sonniss'
    os.makedirs(mp3dir, exist_ok=True)
    picks = {}
    for key, (words, maxdur) in KEYS.items():
        scored = []
        for p in files:
            rel = os.path.relpath(p, outdir).lower()
            score = sum(1 for w in words if w in rel)
            if score == 0: continue
            scored.append((score, p))
        scored.sort(key=lambda x: (-x[0], x[1]))
        chosen = []
        for _, p in scored:
            if len(chosen) >= per_key: break
            d = duration(p)
            if d > maxdur or d <= 0.02: continue
            chosen.append((p, d))
        picks[key] = chosen
        print(key, len(scored), 'matches ->', [os.path.basename(p)[:40] for p, _ in chosen])
    # mp3 化して同梱
    secs = []
    for key, chosen in picks.items():
        cards = ''
        for i, (p, d) in enumerate(chosen):
            slug = re.sub(r'[^a-z0-9]+', '_', os.path.basename(p).lower())[:60]
            mp3 = f'{mp3dir}/{key}__{slug}.mp3'
            if not os.path.exists(mp3):
                subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', p, '-ac', '1', '-b:a', '128k', mp3])
            rel = os.path.relpath(p, outdir)
            lib = rel.split(os.sep)[0]
            cards += f'''<div class="cand" data-key="{html.escape(key)}" data-slug="{html.escape(rel)}"><div class="meta"><b>{html.escape(os.path.basename(p))}</b><div class="tags">{html.escape(lib)} ／ {d:.1f}s</div></div><audio controls preload="none" src="se-sonniss/{os.path.basename(mp3)}"></audio><label class="pick"><input type="radio" name="pick-{html.escape(key)}" value="{html.escape(rel)}"> 採用</label></div>'''
        secs.append(f'''<section class="scene" id="scene-{html.escape(key)}"><h2><code>{html.escape(key)}</code></h2><div class="cands">{cards or "<p class=note>候補なし（語を足す）</p>"}</div><label class="pick none"><input type="radio" name="pick-{html.escape(key)}" value=""> どれも違う</label><textarea placeholder="ひとこと（任意）" data-memo="{html.escape(key)}"></textarea></section>''')
    tpl = open('docs/design/audio/se-sheet.html', encoding='utf-8').read()
    head = tpl[:tpl.index('<main>') + len('<main>')].replace('SE 試聴シート', 'SE 試聴シート（Sonniss）')
    head = re.sub(r'<p>候補は .*?</p>', '<p>候補は Sonniss GDC バンドル（商用可・クレジット不要・改変可）。鍵ごとにファイル名の語で機械的に絞った候補。採用を選んで「選んだ結果をコピー」→ AI に貼ってください。</p>', head, flags=re.S)
    tail = tpl[tpl.index('<div class="bar">'):].replace("deckrogue.audio.se.v1", "deckrogue.audio.se.sonniss")
    open('docs/design/audio/se-sheet-sonniss.html', 'w', encoding='utf-8').write(head + ''.join(secs) + '</main>' + tail)
    print('wrote docs/design/audio/se-sheet-sonniss.html')

if __name__ == '__main__':
    cmd = sys.argv[1]
    if cmd == 'index': index(sys.argv[2])
    elif cmd == 'extract': extract(sys.argv[2], sys.argv[3])
    elif cmd == 'sheet': sheet(sys.argv[2])
    else: print(__doc__)
