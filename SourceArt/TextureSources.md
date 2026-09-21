# Study room texture sources

The room uses ambientCG PBR materials, released under CC0 1.0.

- Wood: https://ambientcg.com/a/Wood051 — 2K PNG
- Plaster: https://ambientcg.com/a/Plaster001 — 1K PNG
- Woven fabric: https://ambientcg.com/a/Fabric030 — 1K PNG
- License: https://docs.ambientcg.com/license/ and https://creativecommons.org/publicdomain/zero/1.0/

Download: `https://ambientcg.com/get?file=<asset>_<resolution>-PNG.zip`.
Put the archives at `.tools-cache/<asset>.zip`, then run Blender in background
with `Tools/import_study_materials.py`. This keeps the original albedo,
OpenGL normal and roughness maps, and packs `1 - roughness` into the alpha
of Unity's metallic/smoothness map (RGB is zero for these nonmetal surfaces).
Run `Tools/create_room.py` afterwards to rebuild the editable Blender scene and FBX.

Runtime files are in `Assets/Game/Resources/Models/RoomTextures` and are
included in the project, so playing the game requires no downloads.

## Window view

The game now uses `Assets/Game/Resources/Models/RoomTextures/Window_Night.png`.
This is an edit of `Window_Outdoor.png` made with the built-in imagegen tool.
Final edit prompt:
> Edit this existing outdoor background texture into deep night, preserving exactly the same camera viewpoint, composition, trees and apartment building locations. Photorealistic quiet city at night, navy-black sky, barely visible dark cool-blue tree foliage silhouettes, a few believable small warm illuminated apartment windows and distant streetlights. No sunlight or sunset, no glowing trees, no giant moon, no additional window frame or indoor furniture, no text. This is the view through a warmly lit study room window in a game: the outdoors should clearly be dark nighttime with only sparse distant lights. Keep square framing.

`Assets/Game/Resources/Models/RoomTextures/Window_Outdoor.png` was generated
with the built-in imagegen tool, used only as the distant view behind the
modelled window. The room, joinery and furnishings remain 3D meshes.

Final prompt:
> Create a square photographic outdoor background texture for the distant view through a study room window in a 3D game. Photorealistic DSLR photograph looking slightly down from a second-floor room over leafy mature green deciduous trees, with a few distant muted grey-beige apartment buildings and a softly blue sky. Bright calm late afternoon natural daylight, gentle depth of field and atmospheric haze, naturally subdued colors. Trees fill lower half, distant small buildings middle third, pale sky upper third. No interior objects, no window frame, no glass reflections, no foreground railing, no text or logos, no illustration, no CGI. Full-bleed square composition, detailed photographic foliage.
