from pathlib import Path
import subprocess,json,hashlib,wave,re
import numpy as np
base=Path(__file__).parent
out=Path('/workspace/scratch/948944000d55/attack-hd2d-prototype-20261025/source/assets/Audio/hd2d_three')
# Single onset/contact clip per card. Layer delays are audio-only, never game events.
recipes={
'hd2d_swing':(.22,[('axe_whoosh_bamboo02.wav','volume=1,adelay=8')]),
'hd2d_strike':(.35,[('axe_wood_contact.wav','volume=.85'),('axe_chop_contact.wav','volume=.30,adelay=9')]),
'hd2d_ignite':(.28,[('fire_ignition.wav','atrim=0:.28,volume=.85,afade=t=out:st=.22:d=.06')]),
'hd2d_fire':(.55,[('fire_ignition.wav','atrim=.20:.55,asetpts=PTS-STARTPTS,volume=1.1,afade=t=in:st=0:d=.003,afade=t=out:st=.25:d=.10'),('fire_crackle.wav','volume=.58,adelay=65,afade=t=out:st=.38:d=.17'),('ground_soft_thud.wav','lowpass=f=260,volume=.18,afade=t=out:st=.12:d=.10')]),
'hd2d_weight':(.50,[('axe_whoosh_bamboo27.wav','asetrate=32000,aresample=48000,lowpass=f=950,volume=.50,adelay=130')]),
'hd2d_stomp':(.75,[('ground_soft_thud.wav','volume=.95'),('stone_debris_a.wav','volume=.52,adelay=45'),('stone_debris_b.wav','volume=.32,adelay=190')])}
report=[]
for name,(duration,layers) in recipes.items():
 cmd=['ffmpeg','-v','error'];filters=[]
 for i,(file,flt) in enumerate(layers):cmd+=['-i',str(base/'candidates'/file)];filters.append(f'[{i}:a]{flt}[a{i}]')
 filters.append(''.join(f'[a{i}]' for i in range(len(layers)))+f'amix=inputs={len(layers)}:normalize=0,apad,atrim=0:{duration},afade=t=out:st={duration-.025}:d=.025[out]')
 raw=subprocess.check_output(cmd+['-filter_complex',re.sub(r'(?<=[=:])\.(\d)',r'0.\1',';'.join(filters)),'-map','[out]','-ar','48000','-ac','1','-f','f32le','pipe:1'])
 samples=np.frombuffer(raw,dtype='<f4').copy();peak=float(np.max(np.abs(samples)));gain=min(1,.70/max(peak,1e-8));samples*=gain
 # TPDF dither at16bit conversion; do not normalizequiettails upward.
 rng=np.random.default_rng(20261025);pcm=np.rint(samples*32767+(rng.random(len(samples))-rng.random(len(samples)))).astype('<i2')
 path=out/(name+'.wav')
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(48000);w.writeframes(pcm.tobytes())
 report.append({'clip':name,'seconds':duration,'rate':48000,'format':'mono PCM16','layers':layers,'mix_down_gain':gain,'peak':float(np.max(np.abs(pcm.astype(float)))/32768),'fullscale_samples':int(np.sum(np.abs(pcm.astype(int))>=32767)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'auditory_review':'NOT LISTENED'})
(out/'NOTICE'/'game_mix_manifest.json').write_text(json.dumps(report,indent=2))
print(json.dumps([{k:v for k,v in x.items() if k in ['clip','peak','fullscale_samples']} for x in report],indent=2))
