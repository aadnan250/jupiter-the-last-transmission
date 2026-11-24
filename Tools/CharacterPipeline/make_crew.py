"""
Builds the Meridian crew as rigged, textured characters and exports each one to FBX for Unity.

Runs headless in Blender with the MPFB (MakeHuman Plugin For Blender) extension and the
CC0 "makehuman_system_assets" pack installed:

    blender --background --python Tools/CharacterPipeline/make_crew.py -- <output_dir> [CHARACTER ...]

Each character is described as data (body shape, skin, hair, clothes) and built through MPFB's own
HumanService, rigged with MakeHuman's "default" skeleton (it has jaw, eyelid and eye bones for
lip sync and blinking), reduced to the low-poly game proxy, and exported with its textures.

Blender and MPFB are GPL build tools and are not shipped with the game; the exported models are
CC0 MakeHuman assets (see docs/CHARACTERS.md and docs/ASSET_LICENSES.md).
"""
import json
import os
import sys

import bpy

from bl_ext.user_default.mpfb.services.humanservice import HumanService
from bl_ext.user_default.mpfb.services.objectservice import ObjectService


def age_value(years):
    """MakeHuman's age slider: 0.5 = 25 years, 1.0 = 90 years (linear between)."""
    if years <= 25:
        return 0.1875 + (years - 1) / 24 * (0.5 - 0.1875)
    return 0.5 + (years - 25) / 65 * 0.5


# Character sheets, from docs/preproduction/01-story-bible.md
CREW = {
    "SAM": dict(  # 34, British-Nigerian geophysicist
        gender=1.0, age=age_value(34), muscle=0.55, weight=0.5, height=0.55, proportions=0.6,
        race=dict(african=0.9, caucasian=0.1, asian=0.0),
        skin="young_african_male/young_african_male.mhmat",
        proxy="male_generic/male_generic.proxy",
        hair="short02/short02.mhclo", eyebrows="eyebrow001/eyebrow001.mhclo",
        clothes=["male_worksuit01/male_worksuit01.mhclo", "shoes02/shoes02.mhclo"]),
    "ASHFORD": dict(  # 52, American commander
        gender=1.0, age=age_value(52), muscle=0.55, weight=0.6, height=0.62, proportions=0.6,
        race=dict(african=0.0, caucasian=1.0, asian=0.0),
        skin="middleage_caucasian_male/middleage_caucasian_male.mhmat",
        proxy="male_generic/male_generic.proxy",
        hair="short04/short04.mhclo", eyebrows="eyebrow003/eyebrow003.mhclo",
        clothes=["male_casualsuit01/male_casualsuit01.mhclo", "shoes02/shoes02.mhclo"]),
    "LUCA": dict(  # 29, Brazilian pilot, off duty in the galley
        gender=1.0, age=age_value(29), muscle=0.6, weight=0.45, height=0.52, proportions=0.6,
        race=dict(african=0.3, caucasian=0.55, asian=0.15),
        skin="young_caucasian_male/young_caucasian_male.mhmat",
        proxy="male_generic/male_generic.proxy",
        hair="short01/short01.mhclo", eyebrows="eyebrow002/eyebrow002.mhclo",
        clothes=["male_casualsuit04/male_casualsuit04.mhclo", "shoes01/shoes01.mhclo"]),
    "MARA": dict(  # 41, Swedish flight engineer
        gender=0.0, age=age_value(41), muscle=0.62, weight=0.45, height=0.62, proportions=0.6,
        race=dict(african=0.0, caucasian=1.0, asian=0.0),
        skin="middleage_caucasian_female/middleage_caucasian_female.mhmat",
        proxy="female_generic/female_generic.proxy",
        hair="ponytail01/ponytail01.mhclo", eyebrows="eyebrow010/eyebrow010.mhclo",
        clothes=["male_worksuit01/male_worksuit01.mhclo", "shoes06/shoes06.mhclo"]),
    "HANA": dict(  # 38, Japanese astrobiologist and flight surgeon
        gender=0.0, age=age_value(38), muscle=0.45, weight=0.45, height=0.5, proportions=0.6,
        race=dict(african=0.0, caucasian=0.0, asian=1.0),
        skin="young_asian_female/young_asian_female.mhmat",
        proxy="female_generic/female_generic.proxy",
        hair="bob02/bob02.mhclo", eyebrows="eyebrow011/eyebrow011.mhclo",
        clothes=["female_casualsuit01/female_casualsuit01.mhclo", "shoes05/shoes05.mhclo"]),
}


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def template():
    """A complete human_info dict produced by MPFB itself, so every key it expects is present."""
    basemesh = HumanService.create_human()
    info = json.loads(HumanService.serialize_to_json_string(basemesh))
    clear_scene()
    return info


def build(name, sheet, base_info):
    info = json.loads(json.dumps(base_info))
    info["name"] = name.lower()
    info["phenotype"].update(dict(
        gender=sheet["gender"], age=sheet["age"], muscle=sheet["muscle"], weight=sheet["weight"],
        height=sheet["height"], proportions=sheet["proportions"], race=sheet["race"]))
    info.update(dict(
        rig="default",
        proxy=sheet["proxy"],
        skin_mhmat=sheet["skin"],
        skin_material_type="MAKESKIN",
        eyes="low-poly/low-poly.mhclo",
        eyes_material_type="MAKESKIN",
        eyebrows=sheet["eyebrows"],
        eyelashes="eyelashes01/eyelashes01.mhclo",
        teeth="teeth_base/teeth_base.mhclo",
        tongue="tongue01/tongue01.mhclo",
        hair=sheet["hair"],
        clothes=sheet["clothes"],
        clothes_material_type="MAKESKIN",
    ))
    settings = HumanService.get_default_deserialization_settings()
    settings["subdiv_levels"] = 0
    return HumanService.deserialize_from_dict(info, settings)


def export(basemesh, out_dir, name):
    rig = ObjectService.find_object_of_type_amongst_nearest_relatives(basemesh, "Skeleton")
    proxy = ObjectService.find_object_of_type_amongst_nearest_relatives(basemesh, "Proxymeshes")
    if rig is None:
        raise RuntimeError(f"{name}: no rig was created")

    # Keep the low-poly proxy body for the game; drop the high-poly base mesh and its helper geometry.
    if proxy is not None:
        bpy.data.objects.remove(basemesh, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for obj in rig.children_recursive:
        if obj.type == "MESH":
            obj.select_set(True)
    bpy.context.view_layer.objects.active = rig

    folder = os.path.join(out_dir, name.lower())
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, name.lower() + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=False,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        mesh_smooth_type="FACE",
    )
    # Blender copies textures into "<name>.fbm"; Unity looks for them in a "Textures" folder next to the model.
    fbm = os.path.join(folder, name.lower() + ".fbm")
    textures = os.path.join(folder, "Textures")
    if os.path.isdir(fbm):
        os.makedirs(textures, exist_ok=True)
        for f in os.listdir(fbm):
            os.replace(os.path.join(fbm, f), os.path.join(textures, f))
        os.rmdir(fbm)

    # MPFB's node-based materials do not survive FBX, so record each material's colour texture for Unity.
    manifest = {}
    for obj in rig.children_recursive:
        if obj.type != "MESH":
            continue
        for slot in obj.material_slots:
            mat = slot.material
            if mat is None or not mat.use_nodes:
                continue
            images = [n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image is not None]
            names = [os.path.basename(bpy.path.abspath(img.filepath)) or img.name for img in images]
            colour = [n for n in names if "diffuse" in n.lower()] or \
                     [n for n in names if not any(k in n.lower() for k in ("normal", "_ao", "bump", "spec", "rough"))]
            manifest[mat.name] = {"texture": colour[0] if colour else None, "object": obj.name, "all_images": names}
    with open(os.path.join(folder, "materials.json"), "w") as f:
        json.dump(manifest, f, indent=2, sort_keys=True)

    meshes = [o.name for o in rig.children_recursive if o.type == "MESH"]
    print(f"[make_crew] {name}: exported {path} with {len(rig.data.bones)} bones and meshes {meshes}")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if not argv:
        raise SystemExit("usage: blender --background --python make_crew.py -- <output_dir> [NAME ...]")
    out_dir = os.path.abspath(argv[0])
    names = [n.upper() for n in argv[1:]] or list(CREW)

    clear_scene()
    base_info = template()
    for name in names:
        clear_scene()
        basemesh = build(name, CREW[name], base_info)
        export(basemesh, out_dir, name)
    print("[make_crew] done")


main()
