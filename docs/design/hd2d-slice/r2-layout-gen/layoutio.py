"""設計図 (act1_layout*.json) を今のファイルと同じ書式で書く: 頂は indent 2・parts は1部品1行 (区切り , :)・日本語はそのまま"""
import json


def dump_layout(L):
    parts = L['parts']
    head = {k: v for k, v in L.items() if k != 'parts'}
    s = json.dumps(head, ensure_ascii=False, indent=2)
    assert s.endswith('\n}')
    lines = [json.dumps(p, ensure_ascii=False, separators=(',', ':')) for p in parts]
    body = ',\n'.join('    ' + ln for ln in lines)
    return s[:-2] + ',\n  "parts": [\n' + body + '\n  ]\n}\n'
