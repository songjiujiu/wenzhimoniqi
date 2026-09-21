"""Run in Blender to import ambientCG source ZIPs and pack Unity smoothness.
Download URLs and CC0 license are documented in SourceArt/TextureSources.md.
ZIPs are cache-only; the resulting runtime PNGs are shipped with the project.
"""
from pathlib import Path
import zipfile, shutil, uuid
import bpy
import numpy as np
ROOT=Path(__file__).resolve().parent.parent
DEST=ROOT/'Assets/Game/Resources/Models/RoomTextures'
for family,asset in [('Wood','Wood051'),('Plaster','Plaster001'),('Fabric','Fabric030')]:
    with zipfile.ZipFile(ROOT/'.tools-cache'/(asset+'.zip')) as archive:
        for suffix,output in [('Color','Albedo'),('NormalGL','Normal'),('Roughness','Roughness')]:
            match=next(n for n in archive.namelist() if n.endswith('_'+suffix+'.png'))
            with archive.open(match) as source,open(DEST/(family+'_'+output+'.png'),'wb') as target:
                shutil.copyfileobj(source,target)
    rough=bpy.data.images.load(str(DEST/(family+'_Roughness.png')))
    rough.colorspace_settings.name='Non-Color'
    pixels=np.empty(len(rough.pixels),dtype=np.float32); rough.pixels.foreach_get(pixels)
    pixels=pixels.reshape((-1,4)); smooth=np.zeros_like(pixels)
    smooth[:,3]=1-pixels[:,0]
    image=bpy.data.images.new(family+'_MetallicSmoothness',width=rough.size[0],height=rough.size[1],alpha=True)
    image.colorspace_settings.name='Non-Color'; image.pixels.foreach_set(smooth.ravel())
    image.filepath_raw=str(DEST/(family+'_MetallicSmoothness.png')); image.file_format='PNG'; image.save()
template=(DEST/'Wood_Albedo.png.meta').read_text()
for path in DEST.glob('*.png'):
    meta=Path(str(path)+'.meta')
    # Preserve existing GUIDs. Normal and packed-data textures must be linear.
    old=meta.read_text() if meta.exists() else template.replace(template.split('guid: ')[1].splitlines()[0],uuid.uuid4().hex)
    old=old.replace('maxTextureSize: 1024','maxTextureSize: 2048').replace('aniso: 4','aniso: 8')
    if '_Normal' in path.stem:
        old=old.replace('textureType: 0','textureType: 1').replace('sRGBTexture: 1','sRGBTexture: 0')
    elif '_Roughness' in path.stem or '_MetallicSmoothness' in path.stem:
        old=old.replace('sRGBTexture: 1','sRGBTexture: 0')
    meta.write_text(old)
print('STUDY_PBR_MAPS_READY')
