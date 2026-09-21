"""Switch the editable Blender room to the game's night ambience without rebuilding meshes."""
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parent.parent
path=ROOT/'SourceArt/Blender/Room.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
texture=bpy.data.images.load(str(ROOT/'Assets/Game/Resources/Models/RoomTextures/Window_Night.png'),check_existing=True)
texture.pack()
sky=bpy.data.materials['Room_Sky']
for node in sky.node_tree.nodes:
    if node.type=='TEX_IMAGE': node.image=texture
sky.node_tree.nodes.get('Principled BSDF').inputs['Emission Strength'].default_value=.18
scene=bpy.context.scene; scene.world.color=(.025,.035,.065)
for obj in list(scene.objects):
    if obj.type=='LIGHT': bpy.data.objects.remove(obj,do_unlink=True)
def point(p): return Vector((-p[0],-p[2],p[1]))
for name,position,power,color,radius in [
    ('Warm ceiling lamp',(-.5,5.5,.3),500,(1,.77,.48),.4),
    ('Desk lamp',(-2.988,2.516,2.657),45,(1,.70,.33),.09)]:
    bpy.ops.object.light_add(type='POINT',location=point(position)); light=bpy.context.object
    light.name=name; light.data.energy=power; light.data.color=color; light.data.shadow_soft_size=radius
bpy.ops.wm.save_as_mainfile(filepath=str(path))
print('BLENDER_NIGHT_SOURCE_READY')
