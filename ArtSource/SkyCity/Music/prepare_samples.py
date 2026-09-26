"""Fetch only the CC0 instrumental notes used by the original Sky City score."""
import concurrent.futures,json,re,urllib.request,urllib.parse,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parent
tree=json.loads((ROOT/'sample-tree.json').read_text(encoding='utf-8'))
commit=tree['sha']
files=[x['path'] for x in tree['tree'] if x['type']=='blob']
selected={
 'piano':[p for p in files if p.startswith('Keys/Upright Nr1/') and re.search(r'_(C[2-6]|G[2-5])_pp_RR1.wav$',p)],
 'harp':[p for p in files if p.startswith('Strings/Harp/') and re.search(r'_[A-G][3-5]_mf.wav$',p)],
 'violin':[p for p in files if p.startswith('Strings/Violin Section/susVib/') and re.search(r'_(A3|C4|E4|G4|B4)_v1.wav$',p)],
 'cello':[p for p in files if p.startswith('Strings/Cello Section/susvib/') and re.search(r'_(C1|E1|G1|B1|D2|F2|C3|E3|G3|B3)_v1_1.wav$',p)]
}
def fetch(item):
    instrument,path=item
    url='https://raw.githubusercontent.com/sgossner/VSCO-2-CE/'+commit+'/'+urllib.parse.quote(path)
    out=ROOT/'Samples'/instrument/Path(path).name;out.parent.mkdir(parents=True,exist_ok=True)
    if not out.exists():
        for attempt in range(3):
            try:
                with urllib.request.urlopen(url,timeout=45) as r:out.write_bytes(r.read())
                break
            except Exception:
                if attempt==2:raise
    return {'instrument':instrument,'file':str(out.relative_to(ROOT)).replace('\\','/'),'source':url,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()}
jobs=[(instrument,p) for instrument,paths in selected.items() for p in paths]
with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
    records=list(pool.map(fetch,jobs))
(ROOT/'sample-manifest.json').write_text(json.dumps({'library':'VSCO 2 Community Edition','license':'CC0-1.0','commit':commit,'samples':records},indent=2),encoding='utf-8')
(ROOT/'VSCO-CC0-LICENSE.txt').write_bytes(urllib.request.urlopen('https://raw.githubusercontent.com/sgossner/VSCO-2-CE/'+commit+'/LICENSE',timeout=30).read())
print(json.dumps({'downloaded':len(records),'counts':{k:len(v) for k,v in selected.items()},'megabytes':round(sum((ROOT/r['file']).stat().st_size for r in records)/1e6,2)}))
