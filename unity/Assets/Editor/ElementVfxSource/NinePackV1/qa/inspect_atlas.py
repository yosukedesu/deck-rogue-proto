from PIL import Image
import json,sys
from pathlib import Path
for p in map(Path,sys.argv[1:]):
 im=Image.open(p);a=im.getchannel('A');r={'path':str(p),'dimensions':im.size,'mode':im.mode,'alpha_extrema':a.getextrema(),'aspect_ratio':im.width/im.height,'cells':[]}
 for i in range(8):
  x=round(i%4*im.width/4);y=round(i//4*im.height/2);x2=round((i%4+1)*im.width/4);y2=round((i//4+1)*im.height/2);c=a.crop((x,y,x2,y2));w,h=c.size;b=c.point(lambda v:255 if v>=16 else 0).getbbox();r['cells'].append({'frame':i,'alpha16_bbox':b,'visible_gutter_normalized_to_256':round(min(b[0]/w,b[1]/h,(w-b[2])/w,(h-b[3])/h)*256,2) if b else 128,'alpha_sum':sum(c.getdata())})
 peak=max(v['alpha_sum'] for v in r['cells'])
 for c in r['cells']:c['alpha_mass_vs_peak']=round(c['alpha_sum']/peak,3)
 (Path(__file__).parent/(p.stem+'.json')).write_text(json.dumps(r,indent=2))
 print(p.name,im.size,'alpha',a.getextrema(),'gutter16',[c['visible_gutter_normalized_to_256'] for c in r['cells']],'mass',[c['alpha_mass_vs_peak'] for c in r['cells']])
