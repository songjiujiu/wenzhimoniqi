"""Reference-inspired cozy study: editable Blender, FBX and authored textures.
blender --background --threads 6 --python Tools/create_room.py [-- --skip-render]
No game build. Coordinates below are in Unity world metres.
"""
from pathlib import Path
import sys, math, json
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import room_modeling as m
from room_modeling import box,cylinder,rod,sphere,ring,leaf,label,point,curved_backrest

ROOT=Path(__file__).resolve().parent.parent
SOURCE=ROOT/'SourceArt/Blender/Room.blend'
EXPORT=ROOT/'Assets/Game/Resources/Models/Room.fbx'
PREVIEW=ROOT/'Docs/Preview/room-blender.png'
for path in (SOURCE,EXPORT,PREVIEW): path.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'; bpy.context.scene.unit_settings.scale_length=1
tex=m.textures()
plaster=m.material('Room_Plaster',(.84,.81,.75),tex['Plaster'])
oak=m.material('Room_Wood_Oak',(.82,.71,.57),tex['Wood'])
walnut=m.material('Room_Wood_Walnut',(.48,.37,.27),tex['Wood'])
floor_mats=[m.material('Room_Wood_Floor'+str(i),(.70+i*.04,.60+i*.038,.47+i*.035),tex['Wood']) for i in range(4)]
linen=m.material('Room_Fabric_Linen',(1,1,.98),tex['Fabric'])
rug=m.material('Room_Fabric_Rug',(.64,.69,.73),tex['Fabric'])
black=m.material('Room_BlackMetal',(.045,.043,.04),metal=.55,rough=.30)
brass=m.material('Room_Brass',(.46,.31,.13),metal=.7,rough=.27)
cream=m.material('Room_Ceramic',(.80,.78,.70),rough=.30)
paper=m.material('Room_Paper',(.78,.74,.62),rough=.85)
ink=m.material('Room_Ink',(.075,.09,.105))
rust=m.material('Room_Ochre',(.31,.14,.06))
book_green=m.material('Room_BookGreen',(.11,.16,.15))
book_blue=m.material('Room_BookBlue',(.09,.135,.19))
leaf_green=m.material('Room_Leaf',(.075,.18,.043),rough=.45)
leaf_light=m.material('Room_LeafLight',(.16,.29,.065),rough=.48)
soil=m.material('Room_Soil',(.035,.025,.014))
sky=m.material('Room_Sky',(1,1,1),rough=.65)
outdoor=bpy.data.images.load(str(m.TEXTURES/'Window_Night.png')); outdoor.pack()
outdoor_node=sky.node_tree.nodes.new('ShaderNodeTexImage'); outdoor_node.image=outdoor
sky_shader=sky.node_tree.nodes.get('Principled BSDF')
sky.node_tree.links.new(outdoor_node.outputs['Color'],sky_shader.inputs['Base Color'])
sky.node_tree.links.new(outdoor_node.outputs['Color'],sky_shader.inputs['Emission Color'])
sky_shader.inputs['Emission Strength'].default_value=.18
hill=m.material('Room_DistantGreen',(.20,.32,.19))

m.category='01 Architecture'
box('Floor foundation',(0,-.3625,0),(15,.275,13),walnut)
# Four solid sections leave a real window opening for daylight and shadows.
for name,p,size in [
    ('Back wall below window',(0,.91,4),(15,2.82,.25)),
    ('Back wall above window',(0,5.97,4),(15,1.06,.25)),
    ('Back wall left of window',(-5.8375,3.88,4),(3.325,3.12,.25)),
    ('Back wall right of window',(3.2875,3.88,4),(8.425,3.12,.25))]:
    box(name,p,size,plaster)
box('Left wall',(-6,3,0),(.25,7,9),plaster)
for row in range(23):
    z=-6.5+(row+.5)*13/23
    cuts=[-7.5]+[v for v in [-7+(row%3)*.9+i*2.8 for i in range(6)] if -7.5<v<7.5]+[7.5]
    for col,(a,b) in enumerate(zip(cuts,cuts[1:])):
        box('Individual oak floorboard',((a+b)/2,-.215,z),(b-a-.012,.03,13/23-.012),floor_mats[(row+col)%4],.003)
for y,h in [(-.04,.30),(6.39,.18)]:
    box('Back timber molding',(0,y,3.82),(15,h,.11),walnut,.012)
    box('Left timber molding',(-5.82,y,0),(.11,h,9),walnut,.012)
box('Woven study rug',(-1.6,-.177,.30),(6.6,.038,4.4),rug,.018)
for x in [-4.77,1.57]: box('Rug stitched border',(x,-.155,.3),(.035,.003,4.13),linen)
for z in [-1.77,2.37]: box('Rug stitched border',(-1.6,-.155,z),(6.36,.003,.035),linen)

m.category='02 Window'
wx=-2.55; wy=3.88; ww=3.25; wh=3.12
# The sky mesh never casts shadows; the window is not a solid board anymore.
backdrop=box('Photographic distant outdoors',(wx,wy,4.10),(ww+.1,wh+.1,.035),sky)
backdrop.visible_shadow=False
for loop in backdrop.data.loops:
    co=backdrop.data.vertices[loop.vertex_index].co
    backdrop.data.uv_layers.active.data[loop.index].uv=(-co.x/(ww+.1)+.5,co.z/(wh+.1)+.5)
for x in [wx-ww/2,wx+ww/2]: box('Window outer stile',(x,wy,3.64),(.13,wh+.12,.20),black,.012)
for y in [wy-wh/2,wy+wh/2]: box('Window outer rail',(wx,y,3.64),(ww+.12,.13,.20),black,.012)
box('Casement meeting stile',(wx,wy,3.58),(.105,wh,.14),black,.009)
for y in [wy-wh/6,wy+wh/6]: box('Window glazing bar',(wx,y,3.60),(ww,.045,.095),black,.007)
for x in [wx-.11,wx+.11]: rod('Window latch',(x,3.63,3.49),(x,3.90,3.49),.021,brass)
box('Deep wooden sill',(wx,wy-wh/2-.10,3.47),(ww+.35,.14,.65),oak,.025)

m.category='03 Writing desk'
desk_start=len(m.assets)
dx=-1.9; dz=2.18; top=1.43
box('Solid oak desk top',(dx,top,dz),(4.8,.16,2.15),oak,.035)
for x in [dx-2.18,dx+2.18]:
    for z in [dz-.88,dz+.88]: box('Desk leg',(x,.57,z),(.14,1.50,.14),walnut,.013)
box('Desk right panel',(dx+2.19,.74,dz),(.12,1.24,1.84),oak,.012)
box('Desk back apron',(dx,1.19,dz+.9),(4.40,.33,.10),walnut,.012)
box('Drawer cabinet side',(dx-1.18,.91,dz),(.11,.91,1.83),walnut,.01)
for y in [.62,.91,1.20]:
    box('Pedestal drawer front',(dx-1.70,y,dz-.96),(.92,.265,.13),oak,.012)
    rod('Drawer brass handle',(dx-1.85,y,dz-1.055),(dx-1.55,y,dz-1.055),.014,black)
box('Wide stationery drawer',(dx+.48,1.20,dz-.96),(3.03,.265,.13),oak,.012)
rod('Wide drawer pull',(dx+.28,1.20,dz-1.055),(dx+.68,1.20,dz-1.055),.014,black)

m.category='04 Chair'
desk_end=len(m.assets); chair_start=len(m.assets)
cx=-1.60; cz=.20
box('Chair seat frame',(cx,.57,cz),(1.04,.15,.93),walnut,.035)
box('Padded linen seat',(cx,.70,cz),(.97,.17,.87),linen,.07)
for x in [cx-.43,cx+.43]:
    for z in [cz-.37,cz+.37]: rod('Chair splayed leg',(x,.58,z),(x+(x-cx)*.12,-.17,z+(z-cz)*.15),.049,oak)
    rod('Chair back upright',(x,.57,cz-.40),(x,1.83,cz-.53),.048,oak)
    rod('Chair side stretcher',(x,.15,cz-.37),(x,.15,cz+.37),.025,walnut)
curved_backrest('Steam bent oak backrest',(cx,1.60,cz-.52),oak)
for x in [cx-.39,cx+.39]: sphere('Backrest brass screw',(x,1.61,cz-.52+.22*(.39/.55)**2-.04),(.033,.033,.01),brass)
chair_end=len(m.assets)

def standing_book(x,y,z,h,width,cover,index=0):
    box('Book page block',(x,y+h/2,z),(width-.025,h-.035,.36),paper,.004)
    for sx in [x-width/2,x+width/2]: box('Book board',(sx,y+h/2,z),(.018,h,.40),cover,.004)
    box('Book spine',(x,y+h/2,z-.205),(width,h,.035),cover,.006)
    for sy in [y+.09,y+h-.08]: box('Spine foil rule',(x,sy,z-.225),(width*.70,.006,.004),brass)
    for sy in range(9):
        box('Paper page edges',(x,y+.06+sy*(h-.12)/9,z-.184),(width-.03,.002,.004),linen)
    if index%3==0: box('Spine paper label',(x,y+h*.53,z-.227),(width*.68,h*.23,.004),paper)

def book_stack(x,y,z,count=3):
    for i in range(count):
        w=.65+(i%2)*.13
        box('Stacked book pages',(x,y+.045+i*.12,z),(w,.08,.47),paper,.007)
        for sy in [y+i*.12,y+.09+i*.12]: box('Stacked book cover',(x,sy,z),(w+.035,.018,.495),[book_blue,walnut,book_green][i%3],.006)

def pot_plant(p,size=1,trailing=False):
    x,y,z=p
    cylinder('Plant pot',(x,y+.22*size,z),.19*size,.44*size,cream,.25*size)
    ring('Pot rolled rim',(x,y+.44*size,z),.24*size,.018*size,cream)
    cylinder('Potting soil',(x,y+.432*size,z),.22*size,.01*size,soil)
    if trailing:
        for branch in range(4):
            prev=(x,y+.46*size,z)
            for j in range(12):
                tip=(x+math.sin(j*.9+branch)*.16*size+(branch-1.5)*.11*size,y+.36*size-j*.13*size,z-.15*size-j*.038*size)
                rod('Trailing vine',prev,tip,.009*size,leaf_green)
                leaf('Ivy leaf',tip,(tip[0]+(-1 if j%2 else 1)*.17*size,tip[1]-.13*size,tip[2]-.03*size),.085*size,leaf_light if j%3==0 else leaf_green)
                prev=tip
    else:
        for i in range(11):
            a=i*2.39996; base=(x,y+.44*size,z)
            mid=(x+math.cos(a)*.10*size,y+(.65+(i%3)*.15)*size,z+math.sin(a)*.10*size)
            tip=(x+math.cos(a)*(.32+(i%2)*.12)*size,y+(.95+(i%3)*.16)*size,z+math.sin(a)*(.32+(i%2)*.12)*size)
            rod('Plant stem',base,mid,.010*size,leaf_green)
            leaf('Broad plant leaf',mid,tip,.095*size,leaf_light if i%3==0 else leaf_green)

m.category='05 Bookcase'
bx=2.30; bz=3.29
for x in [bx-1.24,bx+1.24]: box('Bookcase side',(x,2.07,bz),(.16,4.55,1.0),walnut,.018)
box('Bookcase backing',(bx,2.07,3.765),(2.45,4.55,.08),oak,.008)
for y in [-.08,.80,1.67,2.54,3.41,4.38]:
    box('Bookcase shelf',(bx,y,bz),(2.70 if y in [-.08,4.38] else 2.32,.12,1.05),walnut,.018)
box('Bookcase plinth',(bx,-.13,bz),(2.7,.14,1.07),walnut,.02)
for shelf,y in enumerate([-.02,.86,1.73,2.60,3.47]):
    for i in range(6 if shelf!=2 else 3):
        x=bx-1.05+i*.195; h=.50+((i+shelf)%4)*.055
        standing_book(x,y,3.17,h,.145+(.025 if i%2 else 0),[book_blue,book_green,ink,rust][(i+shelf)%4],i)
    if shelf in [0,1,3]: book_stack(bx+.62,y+.025,3.20,3 if shelf!=1 else 4)
    elif shelf==4: standing_book(bx+.47,y,3.17,.69,.28,walnut)
cylinder('Globe base',(bx+.57,1.77,3.17),.20,.075,walnut)
rod('Globe pedestal',(bx+.57,1.80,3.17),(bx+.57,1.96,3.17),.033,brass)
sphere('Globe ocean',(bx+.57,2.13,3.17),(.47,.47,.47),book_blue)
globe_ring=ring('Globe meridian',(bx+.57,2.13,3.17),.27,.013,brass); globe_ring.rotation_euler.x=math.pi/2
for i in range(10):
    a=i*2.39996; yy=math.sin(i*1.7)*.15; rr=math.sqrt(.238**2-yy**2)
    sphere('Globe land motif',(bx+.57+math.cos(a)*rr,2.13+yy,3.17+math.sin(a)*rr),(.10,.075,.06),brass)
pot_plant((bx+.64,4.44,3.20),.72,True)

m.category='06 Desktop ornaments'
pot_plant((.90,-.18,2.46),1.2)
desktop_start=len(m.assets)
pot_plant((dx+1.65,1.52,dz+.48),.52)
book_stack(dx-.74,1.52,dz+.49,3)
for x in [dx+.03,dx+.58]: box('Open notebook pages',(x,1.548,dz-.30),(.53,.055,.67),paper,.012)
rod('Notebook spine',(dx+.30,1.55,dz-.65),(dx+.30,1.55,dz+.04),.018,walnut)
for row in range(7):
    for x in [dx+.03,dx+.58]: box('Notebook ruling',(x,1.580,dz-.52+row*.055),(.39,.002,.004),linen)
cylinder('Pen holder',(dx+.95,1.67,dz+.38),.135,.30,black)
cylinder('Pen cup interior',(dx+.95,1.823,dz+.38),.117,.006,soil)
for i in range(5):
    x=dx+.89+(i%3)*.048; z=dz+.33+(i//3)*.08
    rod('Pen',(x,1.68,z),(x+(i-2)*.035,2.04+(i%2)*.08,z),.017,book_blue if i%2 else brass)
cylinder('Tea mug',(dx+1.58,1.67,dz-.44),.14,.30,cream)
cylinder('Tea surface',(dx+1.58,1.822,dz-.44),.118,.007,soil)
ring('Mug rolled lip',(dx+1.58,1.824,dz-.44),.13,.013,cream)
handle=ring('Mug handle',(dx+1.77,1.68,dz-.44),.098,.024,cream); handle.rotation_euler.x=math.pi/2
lx=dx-1.80; lz=dz+.53
cylinder('Lamp weighted base',(lx,1.565,lz),.25,.09,black)
a=(lx,1.60,lz); b=(lx-.05,2.17,lz); c=(lx+.44,2.52,lz)
for offset in [-.045,.045]:
    rod('Lamp lower arm',(a[0],a[1],lz+offset),(b[0],b[1],lz+offset),.018,black)
    rod('Lamp upper arm',(b[0],b[1],lz+offset),(c[0],c[1],lz+offset),.018,black)
for p in [a,b,c]: sphere('Lamp hinge',p,(.095,.095,.095),brass)
cylinder('Black conical lampshade',(c[0],2.39,lz),.24,.29,black,.09)
cylinder('Lamp cream reflector',(c[0],2.24,lz),.217,.015,cream)
desktop_end=len(m.assets)
# The separate Blender Incense asset is placed here by Unity and drives the smoke effect.

m.category='07 Wall details'
px=.10; py=3.50
box('Print dark frame',(px,py,3.76),(.89,1.43,.12),black,.01)
box('Print paper',(px,py,3.69),(.79,1.33,.025),paper)
label('Print title','SLOW\nLIVING',(px,py+.30,3.671),.105,ink)
for i in range(4): box('Print landscape',(px,py-.43+i*.055,3.670),(.67-i*.10,.055,.008),[book_green,linen,book_blue,walnut][i])

# Match furniture proportions to a real writing desk instead of a low wide bench.
# Keep the floor/walls and bookcase fixed so egg placement bounds remain valid.
def remap(objects,center,scale,offset=(0,0,0)):
    origin=point(center)
    scaling=Matrix.Diagonal(Vector((scale[0],scale[2],scale[1],1)))
    matrix=Matrix.Translation(point(offset)) @ Matrix.Translation(origin) @ scaling @ Matrix.Translation(-origin)
    for obj in objects:
        obj.data.transform(obj.matrix_world.inverted() @ matrix @ obj.matrix_world)
        obj.data.update()
remap(m.assets[desk_start:desk_end],(dx,-.2,dz),(.8,1.2,.9))
remap(m.assets[chair_start:chair_end],(cx,-.18,cz),(1.25,1.3,1.2))
remap(m.assets[desktop_start:desktop_end],(dx,0,dz),(.8,1,.9),(0,.326,0))

# The scan is dark walnut. Lift the finish in linear space without painting
# artificial highlights into the albedo; both engines get the same multiplier.
for mat in bpy.data.materials:
    if mat.name.startswith('Room_Wood'):
        for node in mat.node_tree.nodes:
            if node.type=='MIX_RGB':
                color=node.inputs[2].default_value
                node.inputs[2].default_value=(color[0]*1.85,color[1]*1.85,color[2]*1.85,1)

scene=bpy.context.scene; scene.world.color=(.025,.035,.065)
bpy.ops.object.camera_add(location=point((9.6,7.8,-12)))
camera=bpy.context.object; camera.name='Study presentation camera'
camera.rotation_euler=(point((-.5,1.8,1.2))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=18.0; scene.camera=camera
for name,p,power,size,color in [('Warm ceiling lamp',(-.5,5.5,.3),500,.4,(1,.77,.48)),('Desk lamp',(-2.988,2.516,2.657),45,.09,(1,.70,.33))]:
    bpy.ops.object.light_add(type='POINT',location=point(p)); light=bpy.context.object
    light.name=name; light.data.energy=power; light.data.shadow_soft_size=size; light.data.color=color
    light.rotation_euler=(point((-1,1,1))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.render.resolution_x=1600; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW); scene.view_settings.view_transform='AgX'
bpy.ops.object.select_all(action='DESELECT')
for obj in m.assets: obj.select_set(True)
bpy.context.view_layer.objects.active=m.assets[0]
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
if '--skip-render' not in sys.argv: bpy.ops.render.render(write_still=True)
groups={}
for obj in m.assets: groups.setdefault(obj.data.materials[0].name,[]).append(obj)
export_objects=[]
for name,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1: bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name; scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR'); export_objects.append(obj)
bpy.ops.object.select_all(action='DESELECT')
for obj in export_objects: obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(EXPORT),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
    global_scale=1,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True,path_mode='STRIP')
triangles=sum(len(face.vertices)-2 for obj in export_objects for face in obj.data.polygons)
report={'triangles':triangles,'material_groups':len(export_objects),'editable_objects':len(m.assets),
    'source':str(SOURCE.relative_to(ROOT)),'export':str(EXPORT.relative_to(ROOT)),
    'style':'Reference-inspired warm plaster, walnut and oak study',
    'floor_y':-.2,'back_wall_z':3.875,'left_wall_x':-5.875}
(SOURCE.parent/'Room-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
palette=[]
for obj in export_objects:
    mat=obj.data.materials[0]; shader=mat.node_tree.nodes.get('Principled BSDF')
    family='Wood' if mat.name.startswith('Room_Wood') else 'Fabric' if mat.name.startswith('Room_Fabric') else 'Plaster' if mat.name=='Room_Plaster' else ''
    color=list(mat.diffuse_color)
    if family=='Wood': color[:3]=[channel*1.85 for channel in color[:3]]
    palette.append({'name':mat.name,'color':color, 'texture':family+'_Albedo' if family else 'Window_Night' if mat.name=='Room_Sky' else '',
        'emission':.18 if mat.name=='Room_Sky' else 0,
        'normal':family+'_Normal' if family else '', 'mask':family+'_MetallicSmoothness' if family else '',
        'normalStrength':.45 if family=='Plaster' else .75,
        'metallic':shader.inputs['Metallic'].default_value,'roughness':shader.inputs['Roughness'].default_value})
(EXPORT.parent/'RoomMaterials.json').write_text(json.dumps({'materials':palette},indent=2),encoding='utf-8')
print('ROOM_READY '+json.dumps(report))
