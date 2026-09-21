"""Reference-sheet gameplay assets, authored in Blender and exported to Unity.
blender --background --threads 6 --python Tools/create_reference_assets.py
"""
import sys, math, json
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import room_modeling as m
from room_modeling import sphere,box,rod,ring,cylinder,point
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Assets/Game/Resources/Models'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
skin=m.material('Ref_Skin',(.63,.34,.22),rough=.56)
nail=m.material('Ref_Nail',(.74,.49,.37),rough=.35)
black=m.material('Ref_Black',(.028,.032,.034),rough=.38)
blue=m.material('Ref_Blue',(.025,.18,.61),rough=.29)
steel=m.material('Ref_Steel',(.41,.47,.49),metal=.8,rough=.30)
green=m.material('Ref_CoilGreen',(.055,.14,.06),rough=.9)
ember=m.material('Ref_Ember',(1,.22,.025),rough=.4)
white=m.material('Ref_Porcelain',(.85,.83,.76),rough=.23)
red=m.material('Ref_Red',(.55,.035,.02),rough=.35)
gold=m.material('Ref_Gold',(.62,.41,.08),metal=.6,rough=.3)
brown=m.material('Ref_MosquitoBody',(.07,.055,.034),rough=.35)
stripe=m.material('Ref_MosquitoStripe',(.60,.49,.29),rough=.55)
wing=m.material('Ref_Wing',(.52,.62,.64),rough=.32)
vein=m.material('Ref_Vein',(.16,.19,.18),rough=.45)
assets={}

def begin(name):
    m.category=name
    return len(m.assets)

def end(name,start): assets[name]=m.assets[start:]

start=begin('Hand')
sphere('Palm',(0,0,0),(.55,.16,.62),skin)
sphere('Thumb mound',(-.19,-.018,-.06),(.23,.18,.34),skin)
sphere('Wrist',(0,0,-.39),(.32,.14,.38),skin)
for i,(x,length) in enumerate([(-.20,.46),(-.065,.55),(.075,.51),(.20,.39)]):
    sphere('Finger root '+str(i),(x,0,.27+length*.24),(.13,.135,length*.77),skin)
    sphere('Finger tip '+str(i),(x,-.012,.27+length*.65),(.115,.12,length*.45),skin)
sphere('Thumb base',(-.30,-.005,.03),(.26,.145,.19),skin)
thumb=sphere('Thumb tip',(-.41,-.005,.14),(.13,.13,.29),skin); thumb.rotation_euler.z=.65
# Fuse the skin volumes into a continuous editable surface, with rounded finger webs.
bpy.ops.object.select_all(action='DESELECT')
hand_parts=m.assets[start:].copy()
for obj in hand_parts: obj.select_set(True)
bpy.context.view_layer.objects.active=hand_parts[0]; bpy.ops.object.join()
hand=bpy.context.object; hand.name='Hand continuous skin'
hand.data.remesh_voxel_size=.012; bpy.ops.object.voxel_remesh()
smooth=hand.modifiers.new('Soft skin','SMOOTH'); smooth.factor=.8; smooth.iterations=5
bpy.ops.object.modifier_apply(modifier=smooth.name)
decimate=hand.modifiers.new('Game mesh','DECIMATE'); decimate.ratio=.30
bpy.ops.object.modifier_apply(modifier=decimate.name)
for face in hand.data.polygons: face.use_smooth=True
m.assets[start:]=[hand]
for i,(x,length) in enumerate([(-.20,.46),(-.065,.55),(.075,.51),(.20,.39)]):
    sphere('Fingernail '+str(i),(x,.046,.27+length*.71),(.075,.014,.105),nail)
end('Hand',start)

start=begin('Zapper')
frame=ring('Blue oval outer frame',(0,0,0),.49,.048,blue); frame.scale.x=1.05; frame.scale.y=1.25
rim=ring('Inner metal frame',(0,.004,0),.447,.014,steel); rim.scale.x=1.05; rim.scale.y=1.25
for i in range(-8,9):
    x=i*.050; zlen=.555*math.sqrt(max(0,1-(x/.454)**2))
    rod('Mesh longitudinal',(x,0,-zlen),(x,0,zlen),.0035,steel)
for i in range(-10,11):
    z=i*.050; xlen=.454*math.sqrt(max(0,1-(z/.555)**2))
    rod('Mesh transverse',(-xlen,.006,z),(xlen,.006,z),.0035,steel)
box('Racket neck',(0,0,-.68),(.19,.09,.28),black,.032)
box('Ergonomic grip',(0,0,-1.06),(.17,.13,.59),black,.058)
box('Power button',(0,.072,-.88),(.065,.023,.13),blue,.02)
for z in [-1.04,-1.13,-1.22]: box('Grip rib',(0,.063,z),(.13,.012,.024),black,.006)
end('Zapper',start)

start=begin('Incense')
box('Metal ash tray',(0,-.01,0),(.76,.035,.76),steel,.045)
for x in [-.25,.25]: rod('Coil stand',(x,.025,-.24),(0,.18,0),.013,steel)
previous=None
for i in range(185):
    a=i/184*math.pi*9; r=.048+i/184*.29
    p=(math.cos(a)*r,.20,math.sin(a)*r)
    if previous: rod('Green spiral',previous,p,.020,green)
    previous=p
sphere('Glowing coil tip',previous,(.045,.036,.042),ember)
end('Incense',start)

start=begin('LuckyCat')
sphere('Cat body',(0,.16,0),(.24,.31,.20),white)
sphere('Cat head',(0,.35,-.015),(.255,.22,.22),white)
for x in [-.085,.085]:
    ear=cylinder('Pointed ear',(x,.477,-.008),.06,.12,white,.004)
    sphere('Pink ear',(x,.479,-.036),(.064,.069,.017),red)
    sphere('Cat foot',(x,.038,-.052),(.092,.065,.11),white)
    sphere('Smiling eye',(x*.62,.371,-.119),(.026,.014,.007),black)
sphere('Nose',(0,.337,-.130),(.029,.021,.015),red)
collar=ring('Red collar',(0,.268,0),.093,.017,red)
sphere('Bell',(0,.263,-.117),(.047,.047,.040),gold)
sphere('Raised paw',(-.145,.344,-.003),(.079,.19,.079),white)
sphere('Lower paw',(.129,.171,-.077),(.065,.13,.072),white)
coin=sphere('Lucky gold coin',(.032,.15,-.107),(.11,.18,.034),gold)
for x in [-.020,.020]: rod('Coin engraved mark',(x,.12,-.13),(x,.18,-.13),.006,black)
end('LuckyCat',start)

start=begin('Tower')
box('Tower pedestal',(0,.016,0),(.38,.032,.38),brown,.008)
levels=[(.03,.16),(.25,.105),(.47,.065),(.75,.015)]
for side in [-1,1]:
    for depth in [-1,1]:
        for (ya,ra),(yb,rb) in zip(levels,levels[1:]):
            rod('Tower angled leg',(side*ra,ya,depth*ra),(side*rb,yb,depth*rb),.015,gold)
for (ya,ra),(yb,rb) in zip(levels,levels[1:]):
    for side in [-1,1]:
        rod('Tower X brace',(-ra,ya,side*ra),(rb,yb,side*rb),.006,gold)
        rod('Tower X brace',(ra,ya,side*ra),(-rb,yb,side*rb),.006,gold)
        rod('Tower X brace',(side*ra,ya,-ra),(side*rb,yb,rb),.006,gold)
        rod('Tower X brace',(side*ra,ya,ra),(side*rb,yb,-rb),.006,gold)
for y,r in levels[1:-1]: box('Observation deck',(0,y,0),(r*2.45,.025,r*2.45),brown,.004)
rod('Tower spire',(0,.75,0),(0,.92,0),.008,gold)
end('Tower',start)

start=begin('Mosquito')
sphere('Thorax',(0,0,0),(.14,.15,.23),brown)
for i in range(7):
    z=-.09-i*.035; radius=.062*(1-i*.075)
    sphere('Abdominal segment',(0,-.008,z),(radius*2,.095-i*.006,.07),brown)
    if i<6:
        stripe_ring=ring('Abdominal band',(0,-.008,z-.017),radius*.87,.006,stripe)
        stripe_ring.rotation_euler.x=math.pi/2
sphere('Head',(0,.008,.14),(.112,.102,.10),brown)
for side in [-1,1]:
    sphere('Compound eye',(side*.045,.018,.16),(.041,.059,.054),black)
    for index in range(3):
        z=.07-index*.07
        a=(side*.05,-.035,z); knee=(side*(.17+index*.025),-.06,z+.065)
        ankle=(side*(.28+index*.035),-.17,z-.08); tip=(side*(.36+index*.027),-.22,z-.16)
        rod('Leg upper',a,knee,.005,brown); rod('Leg lower',knee,ankle,.004,brown); rod('Leg foot',ankle,tip,.0025,brown)
        sphere('Leg pale joint',knee,(.013,.014,.014),stripe)
    rod('Antenna',(side*.025,.027,.18),(side*.10,.071,.32),.0025,brown)
    rod('Palp',(side*.017,-.01,.18),(side*.041,-.024,.29),.003,brown)
    name='Wing_L' if side<0 else 'Wing_R'
    blade=sphere(name+'_membrane',(side*.20,.045,-.065),(.43,.007,.125),wing)
    # Veins stay separate from the membrane, but share the animated wing group.
    for k in range(4):
        rod(name+'_vein',(side*.05,.051,.01-k*.016),(side*.39,.051,-.12+k*.026),.0017,vein)
    rod(name+'_leading_edge',(side*.045,.049,.005),(side*.398,.049,-.055),.0024,vein)
rod('Proboscis',(0,-.01,.18),(0,-.038,.43),.004,brown)
end('Mosquito',start)

# Export each asset in its own origin space before arranging the editable gallery.
report={}
for name,objects in assets.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
    report[name]={'triangles':sum(len(p.vertices)-2 for obj in objects for p in obj.data.polygons),'parts':len(objects)}
palette=[]
for mat in [skin,nail,black,blue,steel,green,ember,white,red,gold,brown,stripe,wing,vein]:
    shader=mat.node_tree.nodes.get('Principled BSDF')
    palette.append({'name':mat.name,'color':list(mat.diffuse_color),'texture':'',
        'metallic':shader.inputs['Metallic'].default_value,'roughness':shader.inputs['Roughness'].default_value})
(OUT/'ReferenceMaterials.json').write_text(json.dumps({'materials':palette},indent=2),encoding='utf-8')
# Gallery positions only affect the .blend and preview, never exported gameplay origins.
for name,x,scale in [('Mosquito',-2.9,2.4),('Hand',-1.1,1.4),('Zapper',1.0,1.1),('Incense',3.0,1.3),('LuckyCat',-.6,1.3),('Tower',.8,1.3)]:
    for obj in assets[name]:
        obj.location*=scale; obj.scale*=scale
        obj.location+=point((x,0,1.9 if name in ['LuckyCat','Tower'] else 0))
scene=bpy.context.scene; scene.world.color=(.18,.18,.18)
bpy.ops.object.camera_add(location=point((3,7,-8)))
cam=bpy.context.object; cam.rotation_euler=(point((0,0,.3))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'; cam.data.ortho_scale=9; scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=point((0,6,-2)))
light=bpy.context.object; light.data.energy=1000; light.data.size=7
light.rotation_euler=(point((0,0,0))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.render.resolution_x=1500; scene.render.resolution_y=900; scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT/'Docs/Preview/reference-assets-blender.png')
scene.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SourceArt/Blender/ReferenceAssets.blend'))
bpy.ops.render.render(write_still=True)
(ROOT/'SourceArt/Blender/ReferenceAssets-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('REFERENCE_ASSETS_READY '+json.dumps(report))
