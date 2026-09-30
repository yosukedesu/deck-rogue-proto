#!/usr/bin/env python3
"""本家 (オクトパストラベラー) の公式スクショを手元のキャッシュに置く (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §6)。

置き場は ~/.cache/deck-rogue/hd2d-ref/ (リポジトリには入れない = 本家の画は著作物。比べる時にユーザーへ送るだけ)。
名前は ot_<appid>_<番号>.jpg (番号は Steam の appdetails の screenshots の並び。2026-09-30 に調べた時の並び)。
門を決める3枚 (ot_921570_16・_7・_11) は並びが変わっても同じ画を指すよう、画像の名前 (ss_…) と sha256 で固定してある。

使い方
  scripts/hd2d-fetch-ref.py                     # 門の3枚だけ (オクトラ1)
  scripts/hd2d-fetch-ref.py --all               # オクトラ1 の全部 (appdetails から)。--app 1971650 でオクトラ2 (等身が違うので門には混ぜない)
  scripts/hd2d-fetch-ref.py --from <フォルダ>    # ネットに出られない時: 手元の ot_*.jpg を写す (例 scratchpad/hd2d/steam)
  scripts/hd2d-fetch-ref.py --out <置き場>       # 置き場を変える
キャッシュに ref-patches.json (hd2d-measure.py の REF_PATCHES の写し。本家の画の UI・キャラ・地面の矩形) と sources.json (URL と sha256) も書く。
"""
import argparse
import hashlib
import importlib.util
import json
import os
import shutil
import sys
import urllib.request

sys.dont_write_bytecode = True   # 読み込む hd2d-measure.py などの .pyc を scripts/ に残さない

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_OUT = os.path.expanduser('~/.cache/deck-rogue/hd2d-ref')
CDN = 'https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/%s/%s.1920x1080.jpg'
APPDETAILS = 'https://store.steampowered.com/api/appdetails?appids=%s&l=japanese'
# 門の3枚 (2026-09-30 に調べた時の画。hd2d-result の調査と patches.png の矩形はこの画で取った)
PINNED = {
    'ot_921570_16': ('921570', 'ss_c99181547954783d2b28050e1424e27257bf1764', '88cf73c2b123912ba25c67cad83bafa4f7ddf23e950185b4368ec05c42893221'),
    'ot_921570_7': ('921570', 'ss_fe9f05cf35e6b4d561c5be76a8b93726ab6be517', '3258b53a360153c32f8ef820cfb389300b3fbc19a77c906c69a950c12f887a68'),
    'ot_921570_11': ('921570', 'ss_31f2a23ca3b46d1e00183664c1b6cd151816a26c', '4f8b42a9e112447f203c29e1a5946067d95015e8642a5a0ba35169e6b16edf05'),
}
UA = {'User-Agent': 'Mozilla/5.0 (deck-rogue hd2d-fetch-ref)'}


def sha256(p):
    h = hashlib.sha256()
    with open(p, 'rb') as f:
        for b in iter(lambda: f.read(1 << 16), b''):
            h.update(b)
    return h.hexdigest()


def get(url, timeout=30):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read()


def ref_patches():
    spec = importlib.util.spec_from_file_location('hd2d_measure', os.path.join(HERE, 'hd2d-measure.py'))
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m.REF_PATCHES


def main():
    ap = argparse.ArgumentParser(description='本家の公式スクショをキャッシュへ')
    ap.add_argument('--out', default=DEFAULT_OUT)
    ap.add_argument('--all', action='store_true', help='appdetails の全部 (門の3枚に加えて)')
    ap.add_argument('--app', default='921570', help='--all の時の appid (オクトラ1=921570・オクトラ2=1971650)')
    ap.add_argument('--from', dest='src', help='ネットの代わりに写す手元のフォルダ (ot_<appid>_<番号>.jpg)')
    ap.add_argument('--force', action='store_true', help='あっても取り直す')
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    sources = {}
    sp = os.path.join(a.out, 'sources.json')
    if os.path.exists(sp):
        try:
            sources = json.load(open(sp, encoding='utf-8'))
        except Exception:
            sources = {}
    bad = 0

    def put(name, url, want_sha=None, src_file=None):
        nonlocal bad
        dst = os.path.join(a.out, name + '.jpg')
        if os.path.exists(dst) and not a.force:
            got = sha256(dst)
            if want_sha and got != want_sha:
                print('⚠ %s は門の画と違う (sha256 %s…)。--force で取り直す' % (name, got[:12]))
                bad += 1
            else:
                print('ある: %s' % dst)
            sources[name] = {'url': url, 'sha256': got}
            return
        try:
            if src_file:
                shutil.copyfile(src_file, dst)
            else:
                data = get(url)
                with open(dst, 'wb') as f:
                    f.write(data)
        except Exception as ex:
            print('取れない: %s (%s)' % (name, ex))
            bad += 1
            return
        got = sha256(dst)
        if want_sha and got != want_sha:
            print('⚠ %s の sha256 が門の画と違う (%s…)。Steam の画が差し替わったかもしれない。門は古い画で決めてある' % (name, got[:12]))
            bad += 1
        else:
            print('置いた: %s' % dst)
        sources[name] = {'url': url, 'sha256': got}

    for name, (app, ss, want) in PINNED.items():
        url = CDN % (app, ss)
        src = os.path.join(a.src, name + '.jpg') if a.src else None
        if src and not os.path.exists(src):
            print('手元に無い: %s' % src)
            bad += 1
            continue
        put(name, url, want, src)
    if a.all:
        if a.src:
            import glob
            for p in sorted(glob.glob(os.path.join(a.src, 'ot_%s_*.jpg' % a.app))):
                name = os.path.basename(p)[:-4]
                if name not in PINNED:
                    put(name, 'file:' + p, None, p)
        else:
            try:
                d = json.loads(get(APPDETAILS % a.app).decode('utf-8'))
                shots = d[str(a.app)]['data'].get('screenshots', [])
            except Exception as ex:
                print('appdetails を読めない (%s)。--from で手元から写す' % ex)
                shots = []
                bad += 1
            for s in shots:
                name = 'ot_%s_%s' % (a.app, s.get('id'))
                if name in PINNED:
                    continue
                url = (s.get('path_full') or '').split('?')[0]
                if url:
                    put(name, url)
    with open(os.path.join(a.out, 'ref-patches.json'), 'w', encoding='utf-8') as f:
        json.dump(ref_patches(), f, ensure_ascii=False, indent=1)
    with open(sp, 'w', encoding='utf-8') as f:
        json.dump(sources, f, ensure_ascii=False, indent=1)
    print('置き場: %s (%d 枚・ref-patches.json・sources.json)' % (a.out, len(sources)))
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
