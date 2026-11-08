#!/usr/bin/env python3
"""A6: draw original 32×32 pixel icons directly. No input bitmap or resampling.

Only integer coordinates are used. Pillow primitives rasterize on the native
grid. Preview enlargements use NEAREST and never feed back into the artwork.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib

ROOT = Path(__file__).resolve().parents[1]
P = {
    'edge': '#24344d', 'deep': '#17283f', 'steel0': '#344a66',
    'steel1': '#55718c', 'steel2': '#819eaf', 'steel3': '#bad2d9',
    'brass0': '#675236', 'brass1': '#a88147', 'brass2': '#cfaa63',
    'brass3': '#dfc77f', 'teal0': '#2d5a67', 'teal1': '#528c94',
    'teal2': '#9fcaca', 'red0': '#663746', 'red1': '#a64f5f',
    'red2': '#dc8290', 'violet0': '#4c416e', 'violet1': '#8574a5',
    'violet2': '#b7a8ce', 'ember0': '#7e4142', 'ember1': '#c36843',
    'ember2': '#e9a85b', 'leaf0': '#325a58', 'leaf1': '#609081',
    'leaf2': '#a4c9ad',
}

def col(k): return P.get(k, k)
def start():
    global im,d
    im=Image.new('RGBA',(32,32),(0,0,0,0)); d=ImageDraw.Draw(im)
def poly(points,fill,outline=None): d.polygon(points,fill=col(fill),outline=col(outline) if outline else None)
def line(points,fill,width=1): d.line(points,fill=col(fill),width=width)
def rect(box,fill): d.rectangle(box,fill=col(fill))
def ellipse(box,fill=None,outline=None,width=1): d.ellipse(box,fill=col(fill) if fill else None,outline=col(outline) if outline else None,width=width)
def pixel(x,y,fill): d.point((x,y),fill=col(fill))

def chevron(y,color='brass2'):
    poly([(5,y),(8,y),(15,y+6),(22,y),(25,y),(25,y+3),(15,y+11),(5,y+3)],'brass0')
    line([(6,y+1),(15,y+9),(24,y+1)],color,2)
    line([(6,y),(15,y+7),(23,y)],'brass3')

def weak():
    chevron(5); chevron(16)

def vulnerable():
    # Exposed split heart, unlike target/exposed and cracked shield/frail.
    poly([(5,7),(10,5),(15,9),(20,5),(25,7),(28,12),(26,18),(16,28),(5,18),(3,12)],'red0')
    poly([(6,8),(10,7),(14,11),(12,15),(15,18),(12,23),(6,17),(5,12)],'red1')
    poly([(21,7),(25,9),(26,13),(24,18),(17,25),(18,19),(15,16),(18,12)],'red1')
    line([(7,9),(10,8),(12,10)],'red2'); line([(22,8),(24,10),(25,13)],'red2')
    line([(15,9),(13,14),(17,17),(14,22),(16,27)],'deep',2)
    # Small brass prongs suggest an opening rather than a solid heart token.
    line([(1,14),(5,14)],'brass2'); line([(26,14),(30,14)],'brass2')

def frail():
    poly([(5,5),(15,3),(26,5),(25,18),(21,24),(16,29),(10,25),(6,18)],'brass1')
    poly([(7,7),(15,5),(24,7),(23,18),(19,24),(16,26),(11,22),(8,17)],'steel0')
    poly([(8,8),(15,6),(15,14),(12,17),(12,22),(9,17)],'steel1')
    line([(6,6),(15,4),(25,6)],'brass3')
    line([(17,5),(14,12),(18,15),(13,20),(16,27)],'deep',2)
    line([(18,6),(16,11),(20,14)],'steel2')
    line([(10,17),(13,17)],'deep')

def restrain():
    # Two open-centered iron shackles joined by brass chain.
    poly([(3,11),(5,6),(11,4),(15,7),(15,13),(12,18),(6,19),(3,16)],'steel1')
    poly([(5,11),(7,8),(11,7),(12,9),(12,13),(10,15),(6,16),(5,14)],'deep')
    line([(4,10),(6,6),(11,5),(14,7)],'steel3')
    poly([(17,18),(19,13),(25,12),(29,15),(29,21),(26,26),(20,27),(17,24)],'steel1')
    poly([(19,18),(21,16),(25,15),(26,17),(26,21),(24,23),(20,24),(19,22)],'deep')
    line([(18,18),(20,14),(25,13),(28,15)],'steel3')
    ellipse((11,12,18,20),None,'brass2',2)
    ellipse((14,15,21,23),None,'brass1',2)
    line([(12,13),(15,13)],'brass3')

def mist():
    # Slate-blue card obscured by horizontal fog ribbons (not paper).
    poly([(7,3),(24,5),(23,28),(6,26)],'brass1')
    poly([(9,5),(22,7),(21,25),(8,24)],'steel0')
    line([(10,7),(20,8)],'steel2'); line([(11,11),(18,11)],'steel1')
    poly([(2,13),(5,11),(10,12),(14,14),(22,14),(26,12),(29,14),(27,16),(20,17),(11,16),(7,14)],'teal1')
    line([(3,12),(7,12),(12,14),(22,15),(27,13)],'teal2')
    poly([(3,21),(8,18),(14,19),(19,21),(25,20),(29,22),(26,24),(20,24),(13,21),(8,21)],'steel1')
    line([(5,20),(9,19),(14,20),(19,22),(25,21)],'steel3')

def slow():
    # Weight and heavy handle; no target-shaped ornament.
    ellipse((11,3,21,12),'brass1'); ellipse((13,5,19,10),'deep')
    poly([(10,10),(22,10),(28,27),(26,29),(6,29),(4,27)],'steel0')
    poly([(11,12),(21,12),(24,24),(21,26),(8,26)],'steel1')
    poly([(11,12),(16,12),(14,26),(8,26)],'steel2')
    line([(10,11),(22,11)],'brass2'); line([(7,28),(26,28)],'brass1')
    rect((14,16,17,22),'deep'); pixel(15,17,'steel3')

def wound():
    # Three torn cuts, with staggered contours and one small blood drop.
    for x,y,L in [(5,5,15),(12,4,19),(20,6,14)]:
        poly([(x,y),(x+4,y+2),(x+2,y+6),(x+4,y+9),(x+1,y+L),(x-1,y+L-2),(x,y+9),(x-2,y+6)],'red0')
        line([(x+1,y+2),(x,y+6),(x+2,y+10),(x,y+L-1)],'red2')
        line([(x+3,y+4),(x+2,y+7),(x+3,y+9)],'red1')
    poly([(25,23),(28,27),(27,29),(24,29),(23,27)],'red1'); pixel(25,26,'red2')

def scald():
    # Charred palm and central hot blister; steam, not a flame icon.
    poly([(9,18),(7,13),(8,11),(10,12),(12,15),(12,8),(14,7),(15,15),(16,6),(18,6),(18,15),(20,8),(22,9),(21,17),(24,13),(26,14),(24,21),(21,26),(14,28),(10,25)],'ember0')
    poly([(12,17),(15,16),(19,17),(21,21),(19,25),(14,25),(11,22)],'ember1')
    ellipse((14,18,20,24),'red0'); ellipse((15,19,18,22),'ember2')
    line([(5,7),(4,5),(5,3)],'steel2'); line([(26,8),(27,6),(26,3)],'steel2')
    line([(10,25),(14,27),(20,25)],'brass1')

def confusion():
    # A continuous counterturning spiral, with displaced angular motes.
    p=[(26,11),(25,7),(21,4),(14,3),(8,6),(5,11),(5,18),(9,24),(16,27),(22,24),(25,20),(25,15),(21,11),(16,10),(12,12),(11,16),(14,19),(18,19),(20,16),(18,14),(16,15)]
    line(p,'violet0',4); line(p,'violet1',2)
    line([(24,8),(21,5),(14,4),(8,7),(6,12)],'violet2')
    line([(10,24),(16,26),(21,23)],'steel2')
    rect((2,24,3,25),'brass2'); rect((27,4,28,5),'brass2')

def exposed():
    # Thin concentric brass target and four sparse sight marks.
    ellipse((6,6,25,25),None,'brass1',2)
    d.arc((6,6,25,25),190,280,fill=col('brass3'),width=1)
    ellipse((11,11,20,20),None,'steel2',1)
    poly([(16,12),(19,16),(16,19),(12,16)],'red1'); pixel(15,15,'red2')
    line([(15,2),(15,8)],'brass2'); line([(15,23),(15,29)],'brass2')
    line([(2,15),(8,15)],'brass2'); line([(23,15),(29,15)],'brass2')

def enemy_weak():
    # Horned foe head plus two small descending chevrons = intimidation.
    poly([(6,12),(3,7),(4,3),(9,8),(13,7),(19,7),(23,8),(28,3),(28,8),(24,13),(23,20),(18,24),(13,24),(8,20)],'steel0')
    poly([(9,10),(14,9),(20,9),(23,12),(21,19),(17,22),(12,20)],'steel1')
    line([(4,4),(6,8),(9,10)],'brass2'); line([(27,4),(25,8),(22,10)],'brass2')
    line([(9,13),(13,15)],'deep',2); line([(18,15),(22,13)],'deep',2)
    pixel(12,15,'red2'); pixel(19,15,'red2')
    line([(11,24),(16,27),(21,24)],'brass2',2)
    line([(13,28),(16,30),(19,28)],'brass1')

def burn_resist():
    # Flame behind a closed steel shield, not a burn status flame alone.
    poly([(7,20),(4,16),(5,10),(8,12),(8,5),(12,10),(15,4),(16,12),(20,9),(22,15),(19,22),(12,24)],'ember0')
    poly([(7,16),(9,12),(10,15),(13,8),(14,15),(18,13),(18,19),(13,22),(9,20)],'ember1')
    poly([(11,19),(13,13),(15,20)],'ember2')
    poly([(13,12),(20,9),(28,12),(27,22),(24,27),(20,30),(15,26)],'brass1')
    poly([(15,14),(20,12),(26,14),(25,22),(22,26),(20,27),(17,24)],'steel1')
    poly([(20,12),(26,14),(25,22),(22,26),(20,27)],'steel0')
    line([(14,13),(20,10),(27,13)],'brass3'); line([(20,13),(20,25)],'steel3')

def armor():
    # Breastplate: negative neck opening and two shoulder plates.
    poly([(7,5),(11,4),(13,8),(19,8),(21,4),(25,5),(29,12),(26,15),(24,14),(25,26),(21,29),(11,29),(7,26),(8,14),(5,15),(2,12)],'steel0')
    poly([(9,8),(12,10),(16,11),(20,10),(23,8),(22,16),(24,25),(20,27),(11,27),(9,25),(10,17)],'steel1')
    poly([(10,11),(15,13),(15,25),(11,25),(11,19)],'steel2')
    line([(3,11),(7,6),(10,6)],'steel3'); line([(22,6),(25,7),(27,11)],'steel2')
    line([(11,9),(16,11),(21,9)],'brass2'); line([(9,25),(16,27),(23,25)],'brass1')
    line([(16,12),(16,25)],'brass2'); pixel(15,12,'brass3')

def thorns():
    # Thorned vine coil, differentiated from circular target by silhouette.
    poly([(8,8),(14,5),(22,7),(27,14),(24,23),(17,28),(8,25),(4,17)],'leaf0')
    poly([(10,11),(15,9),(20,11),(23,15),(21,21),(17,24),(11,21),(8,16)],'deep')
    line([(7,10),(13,6),(21,8),(26,14)],'leaf2')
    line([(24,21),(18,27),(9,24),(5,17)],'leaf1',2)
    for pts in [[(10,7),(10,1),(15,5)],[(20,7),(26,3),(24,12)],[(26,13),(30,12),(27,19)],[(23,23),(27,29),(19,26)],[(12,26),(8,30),(7,23)],[(5,18),(1,15),(6,12)]]:
        poly(pts,'leaf1'); line(pts[:2],'leaf2')
    line([(11,23),(17,20),(19,13),(15,9)],'brass1')

FUNCS = [weak,vulnerable,frail,restrain,mist,slow,wound,scald,confusion,exposed,enemy_weak,burn_resist,armor,thorns]

def create_sheets():
    targets=json.loads((ROOT/'source/targets.json').read_text())['targets']
    font=ImageFont.load_default()
    for scale in (1,4):
        cw,ch=128,32*scale+40
        sheet=Image.new('RGB',(cw*7,ch*2+45),'#101a2b'); sd=ImageDraw.Draw(sheet)
        sd.text((8,8),'A6 / NIGHT GLASS + BRASS / 14 STATUS ICONS',font=font,fill='#c4a467')
        for i,t in enumerate(targets):
            art=Image.open(ROOT/'png'/('status_'+t['asset_key']+'.png'))
            if scale!=1: art=art.resize((32*scale,32*scale),Image.Resampling.NEAREST)
            x=(i%7)*cw+(cw-art.width)//2; y=36+(i//7)*ch
            sheet.paste(art,(x,y),art)
            sd.text(((i%7)*cw+4,y+art.height+7),t['asset_key'],font=font,fill='#afc4d0')
        sheet.save(ROOT/'qa'/f'contactsheet_{scale}x.png')
    # Native silhouettes on dark, muted-blue and pale backgrounds; no label overlays.
    backgrounds=['#101a2b','#293d52','#d2dce0']
    sheet=Image.new('RGB',(14*52,3*56+24),'#101a2b');sd=ImageDraw.Draw(sheet)
    for r,bg in enumerate(backgrounds):
        sd.rectangle((0,24+r*56,sheet.width,24+(r+1)*56),fill=bg)
        for c,t in enumerate(targets):
            art=Image.open(ROOT/'png'/('status_'+t['asset_key']+'.png'))
            sheet.paste(art,(c*52+10,36+r*56),art)
    sd.text((8,6),'32px native | dark / blue / pale',font=font,fill='#c4a467')
    sheet.save(ROOT/'qa'/'native_backgrounds.png')

if __name__=='__main__':
    (ROOT/'png').mkdir(exist_ok=True); (ROOT/'qa').mkdir(exist_ok=True)
    for f in FUNCS:
        start(); f(); im.save(ROOT/'png'/('status_'+f.__name__+'.png'))
    atlas=Image.new('RGBA',(224,64),(0,0,0,0))
    for i,f in enumerate(FUNCS):
        atlas.paste(Image.open(ROOT/'png'/('status_'+f.__name__+'.png')),((i%7)*32,(i//7)*32))
    atlas.save(ROOT/'status_atlas.png')
    (ROOT/'palette.json').write_text(json.dumps(P,indent=2)+'\n')
    create_sheets()
