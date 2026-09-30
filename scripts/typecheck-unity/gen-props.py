#!/usr/bin/env python3
"""Unity が書いたコンパイルの応答ファイル (Library/Bee/artifacts/<dag>/<asm>.rsp) から、
型検査用の MSBuild の props (参照・定義・言語版・抑止する警告) を作る (2026-09-30 HD-2D 見本 P00)。

usage: gen-props.py <rsp> <out.props> <win_dir> [--drop <asm名>]... [--add-ref <path>]...
  --drop Assembly-CSharp   : その名前の参照を外す (いま型検査で作る側の古い DLL を参照しないため)
  --add-ref <path>         : 参照を足す (エディタの型検査が、直前に作った実行時の DLL を見るため)
パスの書き換え: "C:/..." → /mnt/c/... 、"Library/..." (作業コピーからの相対) → <win_dir>/Library/...
"""
import os, re, sys
from xml.sax.saxutils import escape

def wsl(p, win_dir):
    p = p.strip().strip('"').replace('\\', '/')
    m = re.match(r'^([A-Za-z]):/(.*)$', p)
    if m:
        return '/mnt/%s/%s' % (m.group(1).lower(), m.group(2))
    if not p.startswith('/'):
        return os.path.join(win_dir, p)
    return p

def main():
    a = sys.argv[1:]
    if len(a) < 3:
        print(__doc__); sys.exit(2)
    rsp, out, win_dir = a[0], a[1], a[2]
    drop, add = [], []
    i = 3
    while i < len(a):
        if a[i] == '--drop': drop.append(a[i + 1]); i += 2
        elif a[i] == '--add-ref': add.append(a[i + 1]); i += 2
        else: print('unknown arg', a[i]); sys.exit(2)
    defines, refs, nowarn, lang = [], [], [], '9.0'
    for line in open(rsp, encoding='utf-8-sig'):
        s = line.strip()
        if s.startswith('-define:'):
            defines.append(s[len('-define:'):])
        elif s.startswith('-r:'):
            refs.append(wsl(s[3:], win_dir))
        elif s.startswith('-langversion:'):
            lang = s.split(':', 1)[1]
        elif s.startswith('/nowarn:') or s.startswith('-nowarn:'):
            nowarn += [x for x in s.split(':', 1)[1].split(',') if x]
    def stem(p):
        b = os.path.basename(p)
        for suf in ('.ref.dll', '.dll'):
            if b.endswith(suf): return b[:-len(suf)]
        return b
    refs = [r for r in refs if stem(r) not in drop] + add
    missing = [r for r in refs if not os.path.isfile(r)]
    if missing:
        for r in missing: print('参照が無い: ' + r, file=sys.stderr)
        sys.exit(3)
    seen, items = set(), []
    for r in refs:
        n = stem(r)
        if n in seen: continue   # 同じ名前の参照は最初の1つだけ
        seen.add(n)
        items.append('    <Reference Include="%s"><HintPath>%s</HintPath><Private>false</Private></Reference>' % (escape(n), escape(r)))
    with open(out, 'w', encoding='utf-8') as f:
        f.write('<Project>\n  <!-- 生成物: gen-props.py が %s から作った。手で直さない -->\n' % escape(rsp))
        f.write('  <PropertyGroup>\n')
        f.write('    <DefineConstants>%s</DefineConstants>\n' % escape(';'.join(defines)))
        f.write('    <LangVersion>%s</LangVersion>\n' % escape(lang))
        f.write('    <NoWarn>$(NoWarn);%s</NoWarn>\n' % escape(';'.join('CS%04d' % int(x) if x.isdigit() else x for x in nowarn)))
        f.write('  </PropertyGroup>\n  <ItemGroup>\n')
        f.write('\n'.join(items) + '\n')
        f.write('  </ItemGroup>\n</Project>\n')
    print('%s: 参照 %d・定義 %d' % (os.path.basename(out), len(items), len(defines)))

main()
