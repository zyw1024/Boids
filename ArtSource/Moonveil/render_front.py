import bpy
from pathlib import Path
from mathutils import Vector
s=bpy.data.scenes['Moonveil_Studio'];bpy.context.window.scene=s
s.render.resolution_x=1080;s.render.resolution_y=720;s.cycles.samples=24
s.camera.location=(8,-.4,.15);s.camera.data.ortho_scale=2.8
s.camera.rotation_euler=(Vector((-.35,0,0))-s.camera.location).to_track_quat('-Z','Y').to_euler()
s.frame_set(123);s.render.filepath=str(Path(bpy.data.filepath).parent/'Preview/Moonveil_Front.png')
bpy.ops.render.render(write_still=True)
