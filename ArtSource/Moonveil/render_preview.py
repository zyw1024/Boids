import bpy,sys
from pathlib import Path
from mathutils import Vector
scene=bpy.data.scenes['Moonveil_Studio']
bpy.context.window.scene=scene
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
out=Path(bpy.data.filepath).parent/'Preview'
scene.render.filepath=str(out/'Moonveil_Hero.png')
bpy.ops.render.render(write_still=True,scene=scene.name)
