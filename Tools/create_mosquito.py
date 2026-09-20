"""Blender background asset build. Run with --background --python this_file.
Creates the editable source and the Unity FBX from the same scene.
"""
from pathlib import Path
import math
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'SourceArt' / 'Blender' / 'Mosquito.blend'
EXPORT = ROOT / 'Assets' / 'Game' / 'Resources' / 'Models' / 'Mosquito.fbx'
SOURCE.parent.mkdir(parents=True, exist_ok=True)
EXPORT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def material(name, color, metallic=0.0):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1)
    value.use_nodes = True
    shader = value.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = 0.48
    shader.inputs['Metallic'].default_value = metallic
    return value

dark = material('Mosquito_Charcoal', (0.045, 0.065, 0.075))
amber = material('Mosquito_Amber', (0.88, 0.53, 0.16))
wing = material('Mosquito_Wing', (0.73, 0.88, 0.87))
eye = material('Mosquito_Eye', (0.95, 0.32, 0.22))

def ellipsoid(name, location, scale, mat, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj

def rod(name, start, end, radius, mat):
    delta = Vector(end) - Vector(start)
    center = (Vector(end) + Vector(start)) / 2
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=radius, depth=delta.length, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    obj.data.materials.append(mat)
    return obj

ellipsoid('Thorax', (0, 0, 0), (0.08, 0.085, 0.13), dark)
ellipsoid('Abdomen', (0, -0.015, -0.18), (0.075, 0.075, 0.19), amber)
ellipsoid('Head', (0, 0.005, 0.15), (0.067, 0.064, 0.07), dark)
for side in (-1, 1):
    ellipsoid('Eye_' + str(side), (side * 0.055, 0.023, 0.185), (0.026, 0.03, 0.035), eye, 8, 4)
    obj = ellipsoid('Wing_L' if side < 0 else 'Wing_R', (side * 0.17, 0.06, -0.05), (0.20, 0.008, 0.078), wing)
    obj.rotation_euler.y = side * math.radians(24)
    for index in range(3):
        z = 0.075 - index * 0.085
        knee = (side * 0.19, -0.09, z - 0.015)
        toe = (side * (0.25 + 0.025 * index), -0.19, z - 0.10)
        rod(f'Leg_{side}_{index}_a', (side * 0.04, -0.03, z), knee, 0.007, dark)
        rod(f'Leg_{side}_{index}_b', knee, toe, 0.005, dark)
    rod('Antenna_' + str(side), (side * 0.025, 0.04, 0.19), (side * 0.08, 0.08, 0.33), 0.004, dark)
rod('Proboscis', (0, 0, 0.19), (0, -0.025, 0.43), 0.008, dark)

bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1.0
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
bpy.ops.export_scene.fbx(filepath=str(EXPORT), use_selection=True,
    object_types={'MESH'}, axis_forward='-Z', axis_up='Y', global_scale=1.0,
    apply_unit_scale=True, bake_anim=False, add_leaf_bones=False, use_mesh_modifiers=True)
triangles = sum(sum(len(face.vertices) - 2 for face in obj.data.polygons)
    for obj in bpy.context.scene.objects if obj.type == 'MESH')
print(f'ASSET_READY source={SOURCE} export={EXPORT} triangles={triangles}')
