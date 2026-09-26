# Authored limestone cliffs

`AuthoredCliffs.blend` contains nine editable cliff compositions created and
reviewed in Blender through MCP. `sculpt_cliff_family.py` reproduces them in a
new, dedicated scene without replacing the existing architectural scene.

The large masses, their placement, lean, depth and terminal widths are authored
separately for the main island, water terrace, pavilion and six additional
landforms. Small erosion is applied only after these primary forms are defined.
Vertical fracture edges retain distinct normals. There is no ring of duplicated
scanned faces in the new island silhouettes.

Each design exports four distance meshes and surface-derived planting anchors.
`build_hero.py` selects the design by its stable island seed. The eight streamed
district compositions use the same selection and export through `Modules.bytes`.
Vines start on the rock surface, below the architectural planters. The hero and
district assembly scripts incise the cliff behind the existing spillways, so
wider shoulders do not hide the falling water. Vines leave these chutes clear.

Unity uses metre-scale triplanar colour from the existing continuous
`WeatheredLimestone.png` texture and shallow surface-gradient relief. The scan
atlas remains an archived reference; its stretched UV padding is not projected
over new geometry. The three-island cliff renderer uses the global ambient probe:
one interpolated local probe at this large renderer's centre samples inside
the rock volume and previously produced excessively black cliffs.

The existing `rock_face_01` files remain the CC0 surface/reference source by
Dario Barresi from <https://polyhaven.com/a/rock_face_01>. `LimestoneCliffs.blend`
and `Cliff_LOD*.npz` preserve the earlier scan-assembly experiment; new island
generation reads only `Cliff_<design>_LOD*.npz`. Existing small garden boulders
continue to use `Boulder_LOD*.npz`.

Run `audit_cliff_family.py` with Python and NumPy to check finite attributes,
indices, coordinate-conversion winding, normals and distinct geometry at all
four detail levels. Unity captures and validation are in
`Sky_City_Project/Captures/RockDiscussion`.
