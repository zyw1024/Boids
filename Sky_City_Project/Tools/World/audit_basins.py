from pathlib import Path
import gzip,struct,json,numpy as np
repo=Path(__file__).resolve().parents[3]
out=repo/'Sky_City_Project/Captures/SkyCityWorld/WaterReview'
results=[]
with gzip.open(repo/'Sky_City_Project/Assets/SkyCity/Resources/SkyCityInfinite/Modules.bytes','rb') as f:
 magic,version,count,dc=struct.unpack('<4i',f.read(16));assert (magic,version,count,dc)==(0x534B594D,3,128,8)
 for module in range(count+dc):
  for lod in range(3):
   parts=[]
   for part in range(3):
    n,nt=struct.unpack('<2i',f.read(8));v=np.frombuffer(f.read(n*48),dtype='<f4').reshape(n,12);t=np.frombuffer(f.read(nt*4),dtype='<i4').reshape(-1,3);parts.append((v,t))
   if module>=128 or len(parts[1][0])==0:continue
   a,tri=parts[0];water=parts[1][0][:,:3];low=water.min(axis=0);high=water.max(axis=0);wy=float(water[0,1]);p=a[tri,:3]
   lo=p[:,:,1].min(axis=1);hi=p[:,:,1].max(axis=1);flat=(hi-lo<1e-5)&(hi<wy+.00001)
   p=p[flat];hi=hi[flat];clear=[]
   for u,v in [(.2,.2),(.2,.8),(.5,.5),(.8,.2),(.8,.8)]:
    x,z=low[[0,2]]+(high-low)[[0,2]]*[u,v];px=p[:,:,0];pz=p[:,:,2]
    a0=(px[:,1]-px[:,0])*(z-pz[:,0])-(pz[:,1]-pz[:,0])*(x-px[:,0])
    a1=(px[:,2]-px[:,1])*(z-pz[:,1])-(pz[:,2]-pz[:,1])*(x-px[:,1])
    a2=(px[:,0]-px[:,2])*(z-pz[:,2])-(pz[:,0]-pz[:,2])*(x-px[:,2])
    inside=((a0>=-1e-6)&(a1>=-1e-6)&(a2>=-1e-6))|((a0<=1e-6)&(a1<=1e-6)&(a2<=1e-6))
    assert inside.any(),(module,lod,'no bottom')
    gap=wy-float(hi[inside].max());assert gap>=.1-1e-5,(module,lod,gap)
    clear.append(gap-.024)
   results.append({'module':module,'lod':lod,'minimumClearanceAtWaveTrough':min(clear)})
assert len(results)==120,len(results)
report={'passed':True,'basinLods':len(results),'interiorSamples':len(results)*5,'waveAmplitudeBound':.024,'minimumClearance':min(x['minimumClearanceAtWaveTrough'] for x in results),'results':results}
out.mkdir(exist_ok=True);(out/'BasinGeometry.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in report.items() if k!='results'}))
