"""Audit the serialized vocabulary, not only its authoring recipe."""
from pathlib import Path
import gzip, json, struct
import numpy as np

repo = Path(__file__).resolve().parents[3]
library = repo / 'Sky_City_Project/Assets/Boids/Resources/SkyCityInfinite/Modules.bytes'
records = []
with gzip.open(library, 'rb') as stream:
    assert struct.unpack('<4i', stream.read(16)) == (0x534B594D, 3, 128, 8)
    for item in range(136):
        for lod in range(3):
            for part in range(3):
                nv, ni = struct.unpack('<2i', stream.read(8))
                v = np.frombuffer(stream.read(nv * 48), dtype='<f4').reshape(nv, 12)
                t = np.frombuffer(stream.read(ni * 4), dtype='<i4').reshape(-1, 3)
                assert np.isfinite(v).all(), (item, lod, part, 'non-finite attribute')
                assert not len(t) or (t.min() >= 0 and t.max() < nv), (item, lod, part, 'bad index')
                if not len(t):
                    continue
                assert nv <= 800000 and ni <= 2400000, (item, lod, part, 'reader limit')
                leaf_triangles = t[np.all(np.abs(v[t, 9] - .4) < .002, axis=1)] if part == 0 else np.empty((0, 3), dtype=int)
                largest_leaf_edge = 0.
                if len(leaf_triangles):
                    p = v[leaf_triangles, :3]
                    largest_leaf_edge = float(np.linalg.norm(p - np.roll(p, 1, axis=1), axis=2).max())
                records.append(dict(item=item, lod=lod, part=part, vertices=nv, triangles=len(t), leafTriangles=len(leaf_triangles), largestLeafEdge=largest_leaf_edge))
    assert not stream.read(1), 'Unexpected trailing stream data'
assert library.stat().st_size < 100 * 1024 * 1024, 'GitHub single-file limit'
assert len({(r['item'], r['lod']) for r in records if r['part'] == 0}) == 408
report = dict(passed=True, modules=128, districts=8, geometryLods=408, compressedBytes=library.stat().st_size,
              foliageLods=sum(r['leafTriangles'] > 0 for r in records),
              largestLeafEdge=max(r['largestLeafEdge'] for r in records),
              limitation='Attribute and geometry checks; appearance is reviewed separately in runtime captures.', records=records)
out = repo / 'Sky_City_Project/Captures/SkyCityWorld/StyleReview/Geometry.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, indent=2), encoding='utf8')
print(json.dumps({k:v for k,v in report.items() if k != 'records'}))
