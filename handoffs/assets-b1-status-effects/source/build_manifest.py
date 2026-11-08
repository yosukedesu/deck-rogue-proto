#!/usr/bin/env python3
from pathlib import Path
from PIL import Image
import json,hashlib
R=Path(__file__).resolve().parents[1]
SHA='114ee430310c5a2df02ea048f23d1f8375c22c66'
D=[65,65,70,80,85,85,100,70]
S=[
 ('enemy_weak','威圧','EnemyState.weak','weak','enemy',202,'藍紫の圧迫輪＋下降する二重山形'),
 ('vulnerable','脆弱','PlayerState.vulnerable / PlayerStatus.vulnerable','exposed','player',110,'枝分かれした赤いひび'),
 ('frail','虚弱','PlayerState.frail / PlayerStatus.frail','frail','player',128,'割れて開く青灰の盾'),
 ('restrain','拘束','PlayerState.restrain / PlayerStatus.restrain',None,'player',133,'真鍮色の縄と結び目'),
 ('mist','霞み','PlayerState.mist / PlayerStatus.mist','draw','player',116,'左右へ流れる霧の帯'),
 ('slow','重り','PlayerState.slow / PlayerStatus.slow','exposed','player',121,'落下する錘と低い衝撃輪'),
 ('wound','負傷','PlayerStatus.wound',None,'card',1446,'札表面の赤い裂け'),
 ('scald','火傷','PlayerStatus.scald',None,'card',1447,'札の下辺の焦げと小さい炎。burnとは別'),
 ('confusion','混乱','EnemyState.confusion','confuse','enemy',178,'紫の渦とずれる軌道'),
]
def sha(p):return hashlib.sha256((R/p).read_bytes()).hexdigest()
m={
 'schema_version':1,'bundle':'B1-status-application','date':'2026-11-08','branch':'codex/assets-b1-20261108',
 'source_main_commit':SHA,'source_backlog':f'https://github.com/yosukedesu/deck-rogue-proto/blob/{SHA}/docs/dot-asset-backlog-2026-11-02.md#L76',
 'scope':'original art-only handoff; no runtime code integration',
 'creation_method':'direct-native-64x64-pixel-drawing; no bitmap input, no resampling of game assets',
 'runtime_integration_tested':False,'theme':'night glass and brass, latest user instruction takes precedence over old paper theme',
 'render':{'format':'PNG','mode':'RGBA','alpha':'straight, binary 0/255','filter':'nearest','mipmaps':False,'blend':'normal alpha','trim_transparent_pixels':False,'scale':'same native pixel scale as battle units; prefer integer screen scale','canvas_origin':'top_left','x_direction':'right','y_direction':'down'},
 'playback':{'frame_count':8,'layout':'horizontal, left to right','cell_size':[64,64],'sheet_size':[512,64],'frame_durations_ms':D,'total_duration_ms':sum(D),'loop':False,'clear_frame_index':7,'clear_on_finish':True,'mark_start_ms':550,'mark_start_frame_index':7,'mark_layer':'below application VFX; card text above both','mark_lifetime':'until the actual status or corresponding card is removed','mark_loop':False,'duplicate_policy':'refresh application at new gain; keep one mark per target/status; engine decides stacking'},
 'assets':[],
 'previews':{'contactsheet_native.png':'native 1x; mock card and anchor guides are preview-only','readability_2x.png':'peak 2x nearest and mark 1x on three backgrounds','status_application_preview.png':{'format':'APNG','frames':10,'duration_ms':D+[900,220],'total_duration_ms':1740,'scale':3,'loop':'preview only','mark_composited_from_frame':7}},
 'implementation_notes':['No game source is modified. Event hooks, target IDs, stacking and removal must be connected by the implementer.','enemy_weak is an asset alias for EnemyState.weak, not a new engine status.','wound/scald are card overlays, never enemy foot counters. Clip to the actual card face; keep text above VFX.','Published main predates the active Godot integration and the 2026-11-05 color addendum. Current runtime/token parity is unverified.','marks for multiple statuses require deliberate layout; do not pile every mark at one anchor.'],
 'palette':'palette.json','source':'source/draw_status_fx.py','verification':'qa/verification.json','visual_review':'qa/visual_review.md'
}
for key,jp,engine,alias,target,L,motif in S:
 card=target=='card';apply=f'Art/fx/status_{key}_8f.png';mark=f'Art/fx/status_{key}_mark.png'
 a={'key':key,'label_ja':jp,'engine_mapping_at_source_commit':engine,'legacy_unity_display_alias':alias,'target_kind':target,'source':f'https://github.com/yosukedesu/deck-rogue-proto/blob/{SHA}/src/engine/types.ts#L{L}',
 'motif':motif,'anchor_px':[32,32] if card else [32,52],'anchor_meaning':'card center' if card else 'target foot / state indicator baseline','destination_clip':'actual card face, below text' if card else None,
 'application':{'path':apply,'size':[512,64],'sha256':sha(apply),'frame_durations_ms':D,'frames':[{'index':i,'rect':[i*64,0,64,64],'duration_ms':d,'bbox_local':Image.open(R/apply).crop((i*64,0,(i+1)*64,64)).getbbox()} for i,d in enumerate(D)]},
 'mark':{'path':mark,'size':[64,64],'sha256':sha(mark),'bbox_local':Image.open(R/mark).getbbox(),'placement':'on target card' if card else 'at target foot or dedicated status indicator','status_lifetime_driven':True}}
 m['assets'].append(a)
(R/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
print('Manifest: 9 application sheets and 9 marks, duration 620ms, 2 card overlay targets.')
