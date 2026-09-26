"""Validate exported cliff data independently of Blender viewport state."""
from pathlib import Path
import hashlib, json
import numpy as np

root=Path(__file__).resolve().parent
report=[]
for path in sorted(root.glob('Cliff_*_LOD*.npz')):
    with np.load(path) as data:
        p,n,t=data['p'],data['n'],data['t']
        assert np.isfinite(p).all() and np.isfinite(n).all(), path
        assert t.min()>=0 and t.max()<len(p), path
        tri=p[t];cross=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0])
        area=np.linalg.norm(cross,axis=1)
        valid=area>1e-10
        alignment=np.einsum('ij,ij->i',cross[valid]/area[valid,None],n[t[valid]].mean(axis=1))
        # Winding must agree with the normals after the coordinate reflection.
        assert (alignment>0).mean()>.98, (path,float((alignment>0).mean()))
        assert (np.linalg.norm(n,axis=1)>.9).mean()>.999, path
        assert (data['anchors'][:,1]<.02).all(), path
        report.append(dict(file=path.name,vertices=len(p),triangles=len(t),
                           min=p.min(axis=0).tolist(),max=p.max(axis=0).tolist(),
                           normalAgreement=float((alignment>0).mean()),
                           geometryHash=hashlib.sha256(p.tobytes()).hexdigest()))
assert len(report)==36, len(report)
for lod in range(4):
    selected=[r for r in report if r['file'].endswith('LOD%d.npz'%lod)]
    assert len({r['geometryHash'] for r in selected})==9
out=root.parents[2]/'Sky_City_Project/Captures/RockDiscussion/CliffValidation.json'
out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(dict(passed=True,distinctDesigns=9,lods=4,assets=report),indent=2))
print(json.dumps(dict(passed=True,distinctDesigns=9,lods=4,assets=len(report))))
