"""Blender geometry and authored tileable materials for the study room."""
import math, shutil, hashlib
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
TEXTURES = ROOT / 'Assets/Game/Resources/Models/RoomTextures'
assets = []
category = 'Architecture'

def point(p):
    # Unity's FBX importer changes handedness on X as well as converting Z-up.
    return Vector((-p[0], -p[2], p[1]))

def texture_image(name, pixels):
    image = bpy.data.images.new(name, width=pixels.shape[1], height=pixels.shape[0], alpha=True)
    image.pixels.foreach_set(pixels.astype(np.float32).ravel())
    image.filepath_raw = str(TEXTURES / (name+'.png'))
    image.file_format = 'PNG'; image.save(); image.pack()
    return image

def textures():
    """Use the shipped CC0 PBR maps; never replace them with procedural colour noise.

    Tools/import_study_materials.py imports the downloaded source maps once.
    The meshes use metre-based UVs, shared by Blender and Unity.
    """
    TEXTURES.mkdir(parents=True, exist_ok=True)
    result={}
    for name in ['Wood','Plaster','Fabric']:
        image=bpy.data.images.load(str(TEXTURES/(name+'_Albedo.png')),check_existing=True)
        image.pack(); result[name]=image
    return result

def material(name, color, texture=None, metal=0, rough=.58):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    nodes=m.node_tree.nodes; links=m.node_tree.links
    shader=nodes.get('Principled BSDF'); shader.inputs['Base Color'].default_value=(*color,1)
    shader.inputs['Roughness'].default_value=rough; shader.inputs['Metallic'].default_value=metal
    if texture:
        tex=nodes.new('ShaderNodeTexImage'); tex.image=texture
        multiply=nodes.new('ShaderNodeMixRGB'); multiply.blend_type='MULTIPLY'; multiply.inputs[0].default_value=1
        multiply.inputs[2].default_value=(*color,1)
        links.new(tex.outputs['Color'],multiply.inputs[1]); links.new(multiply.outputs[0],shader.inputs['Base Color'])
        family=texture.name.split('_')[0]
        normal=nodes.new('ShaderNodeTexImage')
        normal.image=bpy.data.images.load(str(TEXTURES/(family+'_Normal.png')),check_existing=True)
        normal.image.colorspace_settings.name='Non-Color'
        if not normal.image.packed_file: normal.image.pack()
        decode=nodes.new('ShaderNodeNormalMap'); decode.inputs['Strength'].default_value=.45 if family=='Plaster' else .75
        links.new(normal.outputs['Color'],decode.inputs['Color']); links.new(decode.outputs['Normal'],shader.inputs['Normal'])
        rough_tex=nodes.new('ShaderNodeTexImage')
        rough_tex.image=bpy.data.images.load(str(TEXTURES/(family+'_Roughness.png')),check_existing=True)
        rough_tex.image.colorspace_settings.name='Non-Color'
        if not rough_tex.image.packed_file: rough_tex.image.pack()
        links.new(rough_tex.outputs['Color'],shader.inputs['Roughness'])
    return m

def finish(obj,name,mat,bevel=0):
    obj.name=name; obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod=obj.modifiers.new('Rounded joinery','BEVEL'); mod.width=bevel; mod.segments=3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    collection=bpy.data.collections.get(category)
    if collection is None:
        collection=bpy.data.collections.new(category); bpy.context.scene.collection.children.link(collection)
    for previous in list(obj.users_collection): previous.objects.unlink(obj)
    collection.objects.link(obj)
    assets.append(obj)
    return obj

def box(name,p,size,mat,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=point(p))
    obj=bpy.context.object; obj.scale=(size[0],size[2],size[1])
    obj=finish(obj,name,mat,bevel)
    # Planar UV projection with a real-world texel density. Grain follows each
    # timber's longest axis, rather than the default cube atlas stretching it.
    uv=obj.data.uv_layers.active or obj.data.uv_layers.new()
    dimensions=(size[0],size[2],size[1])
    is_wood=mat.name.startswith('Room_Wood')
    seed=int(hashlib.sha256((name+str(p)).encode()).hexdigest()[:8],16)
    offset=((seed%997)/997.,((seed//997)%991)/991.) if is_wood else (0,0)
    for face in obj.data.polygons:
        normal_axis=max(range(3),key=lambda a:abs(face.normal[a]))
        axes=[a for a in range(3) if a!=normal_axis]
        if is_wood:
            length_axis=max(axes,key=lambda a:dimensions[a]); cross_axis=next(a for a in axes if a!=length_axis)
            # Wood051 grain runs along image X; 2 m tile at game scale.
            axes=[length_axis,cross_axis]
        tile=2.2 if is_wood else .65 if mat.name.startswith('Room_Fabric') else 3.0
        for index in face.loop_indices:
            vertex=obj.data.vertices[obj.data.loops[index].vertex_index].co
            uv.data[index].uv=(vertex[axes[0]]/tile+offset[0],vertex[axes[1]]/tile+offset[1])
    if bevel:
        for face in obj.data.polygons: face.use_smooth=True
        normal=obj.modifiers.new('Weighted joinery normals','WEIGHTED_NORMAL'); normal.keep_sharp=True; normal.weight=50
        bpy.context.view_layer.objects.active=obj; bpy.ops.object.modifier_apply(modifier=normal.name)
    return obj

def cylinder(name,p,radius,height,mat,rtop=None):
    bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=radius,radius2=radius if rtop is None else rtop,depth=height,location=point(p))
    obj=finish(bpy.context.object,name,mat)
    for face in obj.data.polygons: face.use_smooth=len(face.vertices)==4
    return obj

def rod(name,a,b,radius,mat):
    delta=point(b)-point(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=radius,depth=delta.length,location=(point(a)+point(b))/2)
    obj=bpy.context.object; obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(obj,name,mat)

def sphere(name,p,size,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=point(p))
    obj=bpy.context.object; obj.scale=(size[0]/2,size[2]/2,size[1]/2)
    obj=finish(obj,name,mat)
    for face in obj.data.polygons: face.use_smooth=True
    return obj

def ring(name,p,radius,tube,mat):
    bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=8,location=point(p),major_radius=radius,minor_radius=tube)
    return finish(bpy.context.object,name,mat)

def leaf(name,base,tip,width,mat):
    a,b=point(base),point(tip); along=b-a
    side=along.cross(Vector((0,0,1))).normalized()*width
    if side.length<.001: side=Vector((width,0,0))
    vertices=[]; faces=[]
    for i in range(13):
        t=i/12; center=a+along*t+Vector((0,0,math.sin(t*math.pi)*width*.32))
        breadth=math.sin(math.pi*t)**.8
        for s in [-1,0,1]:
            vertices.append(center+side*(s*breadth)+Vector((0,0,-abs(s)*breadth*width*.22)))
    for i in range(12):
        for j in range(2):
            k=i*3+j; faces.append((k,k+1,k+4,k+3))
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update(); obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj; obj.select_set(True)
    obj=finish(obj,name,mat)
    for face in obj.data.polygons: face.use_smooth=True
    solid=obj.modifiers.new('Leaf thickness','SOLIDIFY'); solid.thickness=.002
    bpy.ops.object.modifier_apply(modifier=solid.name)
    return obj

def curved_backrest(name,p,mat):
    """Steam-bent chair back with rounded edges, not a flat cube."""
    vertices=[]; faces=[]
    for j in range(5):
        for i in range(25):
            t=(i/24-.5)*1.1
            vertices.append(point((p[0]+t,p[1]+(j/4-.5)*.42,p[2]+.22*(t/.55)**2)))
    for j in range(4):
        for i in range(24):
            k=j*25+i; faces.append((k,k+1,k+26,k+25))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(vertices,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj; obj.select_set(True)
    obj=finish(obj,name,mat)
    uv=mesh.uv_layers.new()
    for loop in mesh.loops:
        co=mesh.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=(co.x/2.2,co.z/2.2)
    solid=obj.modifiers.new('Bent oak thickness','SOLIDIFY'); solid.thickness=.065
    bpy.ops.object.modifier_apply(modifier=solid.name)
    bevel=obj.modifiers.new('Soft edges','BEVEL'); bevel.width=.018; bevel.segments=3
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    for face in obj.data.polygons: face.use_smooth=True
    return obj

def label(name,text,p,size,mat):
    bpy.ops.object.text_add(location=point(p))
    obj=bpy.context.object; obj.name=name; obj.data.body=text; obj.data.size=size
    obj.data.align_x='CENTER'; obj.data.extrude=.0005
    # Facing the front of the room (Unity -Z).
    obj.rotation_euler=(math.pi/2,0,math.pi)
    bpy.ops.object.convert(target='MESH')
    return finish(bpy.context.object,name,mat)
