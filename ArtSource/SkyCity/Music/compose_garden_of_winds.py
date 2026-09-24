"""Original 32-bar chamber miniature: Garden of Winds. Not an existing film theme.

Render CC0 acoustic multisamples, humanized MIDI, and a seamless 80-second stereo loop.
Dependencies: numpy, scipy, soundfile, mido. Run prepare_samples.py first.
"""
from pathlib import Path
from fractions import Fraction
import json,re,subprocess,shutil
import numpy as np
import soundfile as sf
import mido
from scipy.signal import resample_poly,butter,sosfilt,fftconvolve

ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[2]
OUT=REPO/'Boids_Proj/Assets/Boids/Art/SkyCity/Audio'
SR=44100; BPM=72; EIGHTH=60/BPM/2; BAR=6*EIGHTH; BARS=32; LENGTH=BAR*BARS
rng=np.random.default_rng(240915)
manifest=json.loads((ROOT/'sample-manifest.json').read_text(encoding='utf-8'))
notes={'C':0,'D':2,'E':4,'F':5,'G':7,'A':9,'B':11}
samples={name:[] for name in ['piano','harp','violin','cello']}
for item in manifest['samples']:
    file=ROOT/item['file'];instrument=item['instrument']
    match=re.search(r'_([A-G])(#?)(\d)_',file.name)
    midi=(int(match[3])+1)*12+notes[match[1]]+(1 if match[2] else 0)
    if instrument in ('violin','cello'):midi+=12 # VSCO strings use C3 for middle C.
    audio,rate=sf.read(file,dtype='float32',always_2d=True)
    if audio.shape[1]==1:audio=np.repeat(audio,2,axis=1)
    if rate!=SR:audio=resample_poly(audio,SR,rate,axis=0).astype(np.float32)
    # Remove lead-in without chopping the acoustic attack; retain the natural stereo image.
    level=np.max(np.abs(audio),axis=1);active=np.flatnonzero(level>level.max()*.012)
    if len(active):audio=audio[max(0,active[0]-int(.008*SR)):]
    audio-=audio.mean(axis=0)
    audio/=max(.001,np.max(np.abs(audio)))
    samples[instrument].append((midi,audio))

# Chords use close upper voices over a mobile bass, with ninths and suspended resolutions.
chords=[
 (50,[66,69,73,76]),(49,[64,69,71,76]),(47,[62,66,69,73]),(43,[62,66,69,71]),
 (42,[62,66,69,76]),(40,[62,66,67,71]),(45,[61,64,69,71]),(45,[62,64,69,73]),
 (50,[66,69,73,76]),(49,[64,69,71,76]),(47,[62,66,69,73]),(43,[62,66,69,71]),
 (42,[62,66,69,76]),(40,[62,66,67,71]),(45,[61,64,69,71]),(50,[62,66,69,76]),
 (47,[62,66,69,73]),(40,[62,66,67,71]),(43,[62,66,69,71]),(42,[62,66,69,76]),
 (40,[62,66,67,71]),(47,[62,66,69,73]),(43,[62,66,69,71]),(45,[61,64,69,76]),
 (50,[66,69,73,76]),(49,[64,69,71,76]),(47,[62,66,69,73]),(43,[62,66,69,71]),
 (42,[62,66,69,76]),(40,[62,66,67,71]),(45,[62,64,69,71]),(45,[61,64,69,76])]
melody=[
 [(0,78,2),(2,76,1),(3,69,1),(4,74,1.6)],
 [(0,73,3),(3.5,71,1),(5,69,.8)],
 [(0,78,1.5),(1.5,81,.75),(2.5,78,1),(4,76,1.6)],
 [(.5,74,2),(3,71,1),(4.5,69,1.2)],
 [(0,69,.8),(1,74,1),(2,78,1.5),(4,76,1.5)],
 [(0,79,2),(2,78,1),(3,76,2.5)],
 [(0,73,1.5),(1.5,76,1.5),(3,71,1.5),(4.5,69,1)],
 [(0,74,3),(4,73,1.2)],
 [(0,78,1.5),(1.5,81,1.5),(3,83,1),(4,81,1.6)],
 [(0,80,2),(2.5,76,1),(4,73,1.5)],
 [(0,78,2),(2,76,1),(3,74,1.5),(5,73,.8)],
 [(0,71,2.5),(3,74,1),(4.5,78,1.2)],
 [(0,81,2.5),(3,78,1.5),(5,76,.8)],
 [(0,79,1.5),(1.5,78,1),(3,76,2.5)],
 [(0,73,1),(1,71,1),(2,69,2),(4.5,73,1)],
 [(0,74,4.5)],
 [(.5,78,1.5),(2,83,2),(4.5,81,1)],
 [(0,79,2),(2,78,1),(3.5,76,2)],
 [(0,78,1.5),(2,81,1.5),(4,83,1.6)],
 [(0,81,3),(3.5,78,2)],
 [(0,79,1),(1,83,1.5),(3,86,2.5)],
 [(0,85,1.5),(2,81,1),(3.5,78,2)],
 [(0,83,2),(2.5,81,1),(4,78,1.5)],
 [(0,76,2),(2,73,1),(3.5,71,1.8)],
 [(0,78,2),(2,76,1),(3,69,1),(4,74,1.6)],
 [(0,73,3),(3.5,71,1),(5,69,.8)],
 [(0,78,1.5),(2,76,1),(3.5,74,2)],
 [(0,71,3),(4,69,1.3)],
 [(0,74,2),(2.5,78,1),(4,76,1.5)],
 [(0,71,2),(2,74,1),(3.5,76,2)],
 [(0,74,2),(2,73,1),(3,71,2)],
 [(0,69,2.5),(4,73,1.3)]]
events=[]
def event(inst,bar,eighth,pitch,length,volume,pan=0):
    time=bar*BAR+eighth*EIGHTH+float(rng.uniform(-.010,.010))
    events.append(dict(instrument=inst,time=max(0,time),note=pitch,duration=length*EIGHTH,volume=volume*float(rng.uniform(.92,1.07)),pan=pan))
for bar,(bass,upper) in enumerate(chords):
    section=1.0 if bar<8 else 1.07 if bar<16 else 1.22 if bar<24 else .94
    for at,pitch,length in melody[bar]:event('piano',bar,at,pitch,length,.16*section,-.10)
    # Piano left hand breathes on the compound-meter pulses; harp supplies the sparkle.
    event('piano',bar,0,bass,2.8,.080,-.20)
    event('piano',bar,3,bass+12,2.5,.054,-.12)
    pattern=[upper[0],upper[1],upper[2],upper[3],upper[2],upper[1]]
    for i,pitch in enumerate(pattern):
        event('harp',bar,i+.10,pitch,2.9,(.061 if i in (0,3) else .042)*section,.24)
    strings=.016 if bar<4 else .026 if bar<16 else .036 if bar<24 else .022
    for k,pitch in enumerate([upper[0]+12,upper[1]+12,upper[2]]):
        event('violin',bar,.13+k*.07,pitch,5.75,strings,(-.32,.32,.12)[k])
    event('cello',bar,.12,bass,5.85,.040 if bar<16 else .048,-.20)

cache={}
def voice(inst,pitch):
    key=(inst,pitch)
    if key in cache:return cache[key]
    root,a=min(samples[inst],key=lambda x:abs(x[0]-pitch))
    ratio=Fraction(2**((root-pitch)/12)).limit_denominator(700)
    b=resample_poly(a,ratio.numerator,ratio.denominator,axis=0).astype(np.float32)
    cutoff={'piano':5500,'harp':6600,'violin':4900,'cello':3300}[inst]
    b=sosfilt(butter(2,cutoff,fs=SR,output='sos'),b,axis=0).astype(np.float32)
    cache[key]=b;return b

n=int(round(LENGTH*SR));dry=np.zeros((n,2),dtype=np.float32)
for e in events:
    inst=e['instrument'];a=voice(inst,e['note'])
    release=2.4 if inst in ('piano','harp') else 1.05
    count=min(len(a),int((e['duration']+release)*SR));sound=a[:count].copy();t=np.arange(count)/SR
    if inst in ('violin','cello'):
        attack=.65 if inst=='violin' else .50
        env=np.sin(np.minimum(1,t/attack)*np.pi*.5)**2
        env*=.87+.13*np.sin(np.pi*np.clip(t/(e['duration']+release),0,1))
    else:env=np.minimum(1,t/.004)
    env*=np.cos(np.clip((t-e['duration'])/release,0,1)*np.pi*.5)**2
    # Equal-power pan, with preserved acoustic stereo width.
    pan=e['pan'];g=np.array([np.cos((pan+1)*np.pi/4),np.sin((pan+1)*np.pi/4)])*1.4142
    sound*=env[:,None]*e['volume']*g
    start=int(e['time']*SR);end=start+count
    if end<=n:dry[start:end]+=sound
    else:dry[start:]+=sound[:n-start];dry[:end-n]+=sound[n-start:]

# Deterministic stereo chamber response. Periodic convolution folds reverberant tails
# across the loop boundary, so there is no silent seam or chopped-off last chord.
wet=np.zeros_like(dry)
for ch in range(2):
    count=int(SR*4.4);t=np.arange(count)/SR
    ir=rng.normal(0,1,count)*np.exp(-t/1.1)
    ir[:int(.026*SR)]=0
    ir=sosfilt(butter(2,4300,fs=SR,output='sos'),ir)
    ir*=.22/np.sqrt(np.sum(ir*ir))
    for delay,gain in [(0.037+ch*.009,.18),(.081-ch*.013,.12),(.127+ch*.017,.085),(.203,.05)]:ir[int(delay*SR)]+=gain
    conv=fftconvolve(dry[:,ch]*.8+dry[:,1-ch]*.2,ir).astype(np.float32)
    wet[:,ch]=conv[:n];wet[:len(conv)-n,ch]+=conv[n:]
mix=dry+wet*.70
mix=sosfilt(butter(2,42,fs=SR,btype='highpass',output='sos'),mix,axis=0).astype(np.float32)
mix*=.78/max(.001,float(np.max(np.abs(mix))))
# Remove the tiny discontinuity left by the final high-pass filter without a volume dip.
edge=int(.008*SR);seam=(mix[0]-mix[-1]).copy()
mix[-edge:]+=np.linspace(0,1,edge)[:,None]*seam
OUT.mkdir(parents=True,exist_ok=True)
sf.write(ROOT/'GardenOfWinds_master.wav',mix,SR,subtype='PCM_24')
subprocess.run([shutil.which('ffmpeg') or r'C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe','-y','-hide_banner','-loglevel','error','-i',str(ROOT/'GardenOfWinds_master.wav'),'-c:a','libvorbis','-q:a','6',str(OUT/'GardenOfWinds.ogg')],check=True)

mid=mido.MidiFile(ticks_per_beat=480)
meta=mido.MidiTrack();mid.tracks.append(meta)
meta.append(mido.MetaMessage('track_name',name='Garden of Winds - original score'))
meta.append(mido.MetaMessage('set_tempo',tempo=mido.bpm2tempo(BPM)))
meta.append(mido.MetaMessage('time_signature',numerator=6,denominator=8))
meta.append(mido.MetaMessage('key_signature',key='D'))
for channel,(inst,program) in enumerate([('piano',0),('harp',46),('violin',48),('cello',42)]):
    track=mido.MidiTrack();mid.tracks.append(track);track.append(mido.MetaMessage('track_name',name=inst))
    track.append(mido.Message('program_change',channel=channel,program=program))
    sequence=[]
    for e in events:
        if e['instrument']!=inst:continue
        start=round(e['time']*BPM/60*480);end=round((e['time']+e['duration'])*BPM/60*480)
        velocity=int(np.clip(e['volume']*300+28,32,92))
        sequence.extend([(start,mido.Message('note_on',channel=channel,note=e['note'],velocity=velocity)),(end,mido.Message('note_off',channel=channel,note=e['note'],velocity=0))])
    cursor=0
    for tick,msg in sorted(sequence,key=lambda x:x[0]):msg.time=tick-cursor;track.append(msg);cursor=tick
mid.save(ROOT/'GardenOfWinds.mid')
(ROOT/'score-events.json').write_text(json.dumps({'title':'Garden of Winds','bpm':BPM,'meter':'6/8','key':'D major','bars':BARS,'seconds':LENGTH,'events':events},indent=2),encoding='utf-8')
report={'title':'Garden of Winds','durationSeconds':LENGTH,'sampleRate':SR,'channels':2,'peakDbFS':round(float(20*np.log10(np.abs(mix).max())),2),'rmsDbFS':round(float(20*np.log10(np.sqrt(np.mean(mix**2)))),2),'loopBoundaryJump':float(np.max(np.abs(mix[0]-mix[-1]))),'noteEvents':len(events),'originalComposition':True,'sampleLibrary':'VSCO 2 CE (CC0-1.0)'}
(ROOT/'audio-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
