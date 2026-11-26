"""
Builds the Stalker: the gaunt, hunched surface form that bursts out of Europa's ice. An original design:
pale blue-grey skin mottled with dark veins, a bald domed skull, a lipless mouth full of teeth, long arms that
reach past the knees, and long clawed fingers and toes.

Runs headless in Blender 4.x, no add-ons needed:

    blender --background --factory-startup --python Tools/CreaturePipeline/make_stalker.py -- <output_dir>

How it is made: a stick skeleton (one vertex per joint) is fleshed out with Blender's Skin modifier and smoothed
with subdivision, which gives an organic body in one piece. A matching armature with named bones (the game
animates them procedurally) is parented with automatic weights. The skin colour and a bump-derived normal map
are baked from procedural noise and Voronoi veins. Output: <output_dir>/stalker/stalker.fbx plus PNG textures.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

# ---------------------------------------------------------------- skeleton (Z up, the creature faces -Y)

# name: (head, tail, parent). Rest pose is hunched and upright; the game bends it into a crouch or a run.
BONES = {
    "hips": ((0, 0.02, 1.00), (0, 0.00, 1.16), None),
    "spine": ((0, 0.00, 1.16), (0, -0.06, 1.44), "hips"),
    "chest": ((0, -0.06, 1.44), (0, -0.17, 1.70), "spine"),
    "neck": ((0, -0.17, 1.70), (0, -0.32, 1.84), "chest"),
    "head": ((0, -0.32, 1.84), (0, -0.50, 1.98), "neck"),
    "jaw": ((0, -0.40, 1.84), (0, -0.58, 1.79), "head"),
}
for side, sx in (("L", 1), ("R", -1)):
    BONES.update({
        f"shoulder.{side}": ((0.05 * sx, -0.15, 1.66), (0.21 * sx, -0.13, 1.67), "chest"),
        f"upperarm.{side}": ((0.21 * sx, -0.13, 1.67), (0.31 * sx, -0.16, 1.27), f"shoulder.{side}"),
        f"forearm.{side}": ((0.31 * sx, -0.16, 1.27), (0.35 * sx, -0.22, 0.84), f"upperarm.{side}"),
        f"hand.{side}": ((0.35 * sx, -0.22, 0.84), (0.37 * sx, -0.25, 0.70), f"forearm.{side}"),
        f"thigh.{side}": ((0.11 * sx, 0.03, 1.00), (0.16 * sx, -0.06, 0.56), "hips"),
        f"shin.{side}": ((0.16 * sx, -0.06, 0.56), (0.17 * sx, 0.06, 0.13), f"thigh.{side}"),
        f"foot.{side}": ((0.17 * sx, 0.06, 0.13), (0.18 * sx, -0.12, 0.04), f"shin.{side}"),
    })
    # Four long clawed fingers fanning out of each hand, and three long toes.
    for i, spread in enumerate((-0.05, -0.017, 0.017, 0.05)):
        base = Vector((0.37 * sx, -0.25, 0.70))
        tip = Vector(((0.37 + spread * 1.6) * sx, -0.30 - abs(spread), 0.36 + abs(spread) * 0.8))
        BONES[f"finger{i}.{side}"] = (tuple(base), tuple(tip), f"hand.{side}")
    for i, spread in enumerate((-0.04, 0.0, 0.04)):
        base = Vector((0.18 * sx, -0.12, 0.04))
        tip = Vector(((0.18 + spread) * sx, -0.30, 0.015))
        BONES[f"toe{i}.{side}"] = (tuple(base), tuple(tip), f"foot.{side}")

# Skin radius at each joint (metres). Gaunt: thin limbs, a narrow ribcage, a big skull.
RADIUS = {
    "hips": 0.15, "spine": 0.13, "chest": 0.19, "neck": 0.075, "head": 0.085, "jaw": 0.055,
    "shoulder": 0.095, "upperarm": 0.07, "forearm": 0.058, "hand": 0.042, "finger": 0.016,
    "thigh": 0.115, "shin": 0.072, "foot": 0.05, "toe": 0.016,
}
TIP_RADIUS = {"finger": 0.004, "toe": 0.004, "head": 0.06, "jaw": 0.035}


# Extra masses (position, radius, nearest joint): a bald domed skull pushed forward on the neck, a heavy brow and a
# long jutting jaw; then the muscle a starving but powerful thing still has: shoulders, back, chest, arms, legs.
SHAPE = [((0, -0.38, 1.99), 0.095, (0, -0.32, 1.84)),       # cranium
         ((0, -0.32, 2.03), 0.08, (0, -0.38, 1.99)),        # back of the dome
         ((0, -0.46, 1.94), 0.065, (0, -0.38, 1.99)),       # brow
         ((0, -0.51, 1.86), 0.06, (0, -0.47, 1.95)),        # jaw, jutting
         ((0, -0.13, 1.57), 0.16, (0, -0.06, 1.44)),        # ribcage
         ((0, 0.03, 1.55), 0.13, (0, -0.06, 1.44)),         # hunched upper back
         ((0, -0.02, 1.30), 0.10, (0, 0.00, 1.16))]         # belly, sunken
for _sx in (1, -1):
    SHAPE += [((0.18 * _sx, -0.10, 1.66), 0.085, (0.21 * _sx, -0.13, 1.67)),     # deltoid
              ((0.09 * _sx, -0.21, 1.56), 0.075, (0, -0.13, 1.57)),              # pectoral
              ((0.12 * _sx, 0.04, 1.46), 0.085, (0, 0.03, 1.55)),                # lat
              ((0.27 * _sx, -0.16, 1.47), 0.062, (0.21 * _sx, -0.13, 1.67)),     # biceps
              ((0.33 * _sx, -0.18, 1.10), 0.055, (0.31 * _sx, -0.16, 1.27)),     # forearm
              ((0.15 * _sx, -0.03, 0.80), 0.095, (0.11 * _sx, 0.03, 1.00)),      # thigh
              ((0.17 * _sx, 0.06, 0.40), 0.065, (0.16 * _sx, -0.06, 0.56))]      # calf


def radius_for(bone):
    return RADIUS[bone.split(".")[0].rstrip("0123456789")]


# Metaball element radius for a wanted surface radius (a lone ball's surface sits inside its radius).
META_SCALE = 1.0 / 0.72
ALBEDO_PATH = [""]  # set in main(); the preview renders with the baked texture


def build_body():
    """
    Flesh out the skeleton with metaballs: balls stepped along every bone (radius tapering head to tail) plus
    muscle and skull masses. Metaballs melt into each other, so joints and junctions come out organic. The result
    is converted to a mesh, voxel-remeshed for clean topology, and smoothed.
    """
    mb = bpy.data.metaballs.new("StalkerMeta")
    mb.resolution = 0.012
    mb.render_resolution = 0.012
    mb.threshold = 0.6
    meta = bpy.data.objects.new("Stalker", mb)
    bpy.context.collection.objects.link(meta)

    def ball(pos, r, stiffness=2.0):
        e = mb.elements.new()
        e.type = "BALL"
        e.co = Vector(pos)
        e.radius = r * META_SCALE
        e.stiffness = stiffness

    for name, (head, tail, _) in BONES.items():
        kind = name.split(".")[0].rstrip("0123456789")
        r0 = max(radius_for(name), 0.022)
        r1 = max(TIP_RADIUS.get(kind, radius_for(name) * 0.85), 0.012)
        a, b = Vector(head), Vector(tail)
        steps = max(2, int((b - a).length / (min(r0, r1) * 0.9)) + 1)
        for i in range(steps):
            t = i / (steps - 1)
            # Lower stiffness on the steps keeps a chain of balls from swelling where they overlap.
            ball(a.lerp(b, t), r0 + (r1 - r0) * t, stiffness=1.0)

    for pos, r, _ in SHAPE:
        ball(pos, r, stiffness=1.3)

    bpy.context.view_layer.objects.active = meta
    meta.select_set(True)
    bpy.ops.object.convert(target="MESH")
    body = bpy.context.active_object
    body.name = "Stalker"

    remesh = body.modifiers.new("Remesh", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = 0.009
    smooth = body.modifiers.new("Smooth", "SMOOTH")
    smooth.factor = 0.6
    smooth.iterations = 4
    bpy.ops.object.modifier_apply(modifier="Remesh")
    bpy.ops.object.modifier_apply(modifier="Smooth")
    bpy.ops.object.shade_smooth()
    return body


def build_armature():
    arm_data = bpy.data.armatures.new("StalkerRig")
    rig = bpy.data.objects.new("StalkerRig", arm_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    bones = {}
    for name, (head, tail, parent) in BONES.items():
        b = arm_data.edit_bones.new(name)
        b.head, b.tail = Vector(head), Vector(tail)
        bones[name] = b
    for name, (_, _, parent) in BONES.items():
        if parent:
            bones[name].parent = bones[parent]
            bones[name].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def add_part(name, prim, location, scale, material, rotation=(0, 0, 0)):
    if prim == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, location=location)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=1, depth=2, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    obj.data.materials.append(material)
    return obj


def build_face(skin_obj, mats):
    """
    Rigid parts, each with the bone it rides: a mouth cavity with two jagged rows of teeth set into the jaw,
    small black eyes deep under the brow, and long hooked claws at every finger and toe tip.
    """
    parts = [(add_part("Mouth", "sphere", (0, -0.548, 1.85), (0.044, 0.026, 0.03), mats["cavity"]), "head")]
    for row, z, tilt, bone in (("Upper", 1.868, math.pi, "head"), ("Lower", 1.832, 0.0, "jaw")):
        for i in range(9):
            a = (i / 8 - 0.5) * 1.7
            x, y = math.sin(a) * 0.042, -0.552 - math.cos(a) * 0.018
            length = 0.011 + (0.009 if i % 2 == 0 else 0.003)
            parts.append((add_part(f"Tooth{row}{i}", "cone", (x, y, z + (length if tilt == 0.0 else -length) * 0.3),
                                   (0.004, 0.004, length), mats["teeth"], rotation=(tilt, 0, 0)), bone))
    for sx in (1, -1):
        parts.append((add_part(f"Eye{'L' if sx > 0 else 'R'}", "sphere", (0.042 * sx, -0.532, 1.915),
                               (0.013, 0.009, 0.008), mats["eye"]), "head"))

    for name, (head, tail, _) in BONES.items():
        if not (name.startswith("finger") or name.startswith("toe")):
            continue
        a, b = Vector(head), Vector(tail)
        d = (b - a).normalized()
        length = 0.09 if name.startswith("finger") else 0.05
        centre = b + d * (length * 0.5)
        rot = d.to_track_quat("Z", "Y").to_euler()
        parts.append((add_part(f"Claw_{name}", "cone", tuple(centre), (0.011, 0.011, length * 0.5), mats["claw"],
                               rotation=tuple(rot)), name))
    return parts


def render_preview(path):
    """A quick three-quarter view against black, lit cold from above, to check the look."""
    scene = bpy.context.scene
    bpy.ops.object.empty_add(location=(0, -0.25, 0.85))
    target = bpy.context.active_object
    bpy.ops.object.camera_add(location=(1.5, -2.9, 1.25))
    cam = bpy.context.active_object
    track = cam.constraints.new("TRACK_TO")
    track.target = target
    scene.camera = cam
    bpy.ops.object.light_add(type="AREA", location=(1.2, -2.2, 2.6))
    key = bpy.context.active_object
    key.data.energy = 160
    key.data.color = (0.75, 0.85, 1.0)
    key.constraints.new("TRACK_TO").target = target
    bpy.ops.object.light_add(type="POINT", location=(-1.2, 1.2, 1.9))
    bpy.context.active_object.data.energy = 80
    bpy.context.active_object.data.color = (0.5, 0.7, 1.0)
    # Preview with the baked textures, as the game will show it.
    skin = bpy.data.materials["StalkerSkin"]
    nt = skin.node_tree
    for node in list(nt.nodes):
        if node.type not in ("BSDF_PRINCIPLED", "OUTPUT_MATERIAL"):
            nt.nodes.remove(node)
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(bpy.path.abspath(ALBEDO_PATH[0]))
    nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    scene.view_settings.view_transform = "AgX"
    world = bpy.data.worlds.new("Black")
    world.color = (0.005, 0.006, 0.01)
    scene.world = world
    scene.render.resolution_x, scene.render.resolution_y = 640, 800
    scene.cycles.samples = 24
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------- materials and baking

def node_material(name, color, roughness, emission=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1)
        bsdf.inputs["Emission Strength"].default_value = 1.0
    return mat


def skin_material():
    """Pale blue-grey skin, purple bruising, a network of dark veins; bump from the same patterns."""
    mat = bpy.data.materials.new("StalkerSkin")
    mat.use_nodes = True
    nt = mat.node_tree
    n, l = nt.nodes, nt.links
    bsdf = n["Principled BSDF"]
    coord = n.new("ShaderNodeTexCoord")

    mottle = n.new("ShaderNodeTexNoise")
    mottle.inputs["Scale"].default_value = 9.0
    mottle.inputs["Detail"].default_value = 8.0
    l.new(coord.outputs["Object"], mottle.inputs["Vector"])

    veins = n.new("ShaderNodeTexVoronoi")
    veins.feature = "DISTANCE_TO_EDGE"
    veins.inputs["Scale"].default_value = 22.0
    warp = n.new("ShaderNodeTexNoise")
    warp.inputs["Scale"].default_value = 3.0
    mix_vec = n.new("ShaderNodeMix")
    mix_vec.data_type = "VECTOR"
    mix_vec.inputs["Factor"].default_value = 0.55
    l.new(coord.outputs["Object"], mix_vec.inputs["A"])
    l.new(warp.outputs["Color"], mix_vec.inputs["B"])
    l.new(mix_vec.outputs["Result"], veins.inputs["Vector"])
    vein_mask = n.new("ShaderNodeMapRange")
    vein_mask.inputs["From Min"].default_value = 0.0
    vein_mask.inputs["From Max"].default_value = 0.025
    vein_mask.inputs["To Min"].default_value = 1.0
    vein_mask.inputs["To Max"].default_value = 0.0
    l.new(veins.outputs["Distance"], vein_mask.inputs["Value"])

    ramp = n.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.13, 0.15, 0.32, 1)   # bruised blue
    ramp.color_ramp.elements[1].color = (0.46, 0.56, 0.78, 1)   # pale frost-blue
    l.new(mottle.outputs["Fac"], ramp.inputs["Fac"])

    vein_col = n.new("ShaderNodeMix")
    vein_col.data_type = "RGBA"
    vein_col.inputs["B"].default_value = (0.20, 0.12, 0.26, 1)  # dark purple veins
    l.new(vein_mask.outputs["Result"], vein_col.inputs["Factor"])
    l.new(ramp.outputs["Color"], vein_col.inputs["A"])
    l.new(vein_col.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.45  # wet, waxy

    bump = n.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.6
    bump_mix = n.new("ShaderNodeMath")
    bump_mix.operation = "SUBTRACT"
    l.new(mottle.outputs["Fac"], bump_mix.inputs[0])
    l.new(vein_mask.outputs["Result"], bump_mix.inputs[1])
    l.new(bump_mix.outputs["Value"], bump.inputs["Height"])
    l.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def bake(obj, mat, kind, path, size=1024):
    img = bpy.data.images.new(os.path.basename(path), size, size, alpha=False, float_buffer=False)
    if kind == "NORMAL":
        img.colorspace_settings.name = "Non-Color"
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = img
    mat.node_tree.nodes.active = node
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if kind == "DIFFUSE":
        bpy.context.scene.render.bake.use_pass_direct = False
        bpy.context.scene.render.bake.use_pass_indirect = False
        bpy.context.scene.render.bake.use_pass_color = True
    bpy.ops.object.bake(type=kind, margin=8)
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    mat.node_tree.nodes.remove(node)


# ---------------------------------------------------------------- main

def smooth_weights(body):
    """
    Skin weights by distance: each vertex follows its two nearest bones, blended by inverse distance, so joints
    bend smoothly. (Bone-heat weighting cannot solve a mesh this organic and thin in places.)
    """
    segments = [(name, Vector(h), Vector(t)) for name, (h, t, _) in BONES.items()]
    groups = {name: body.vertex_groups.new(name=name) for name, _, _ in segments}
    for v in body.data.vertices:
        p = body.matrix_world @ v.co
        dists = []
        for name, a, b in segments:
            ab = b - a
            t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
            dists.append(((a + ab * t - p).length, name))
        dists.sort()
        (d0, n0), (d1, n1) = dists[0], dists[1]
        w0, w1 = 1.0 / max(d0, 1e-4) ** 4, 1.0 / max(d1, 1e-4) ** 4
        groups[n0].add([v.index], w0 / (w0 + w1), "REPLACE")
        groups[n1].add([v.index], w1 / (w0 + w1), "REPLACE")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out_dir = os.path.abspath(argv[0] if argv else ".")
    folder = os.path.join(out_dir, "stalker")
    tex_dir = os.path.join(folder, "Textures")
    os.makedirs(tex_dir, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 4

    body = build_body()

    # Ten of these at once on integrated graphics: keep the body near 7k vertices.
    dec = body.modifiers.new("Decimate", "DECIMATE")
    dec.ratio = min(1.0, 7000 / max(1, len(body.data.vertices)))
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_apply(modifier="Decimate")
    bpy.ops.object.shade_smooth()

    skin = skin_material()
    body.data.materials.append(skin)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    ALBEDO_PATH[0] = os.path.join(tex_dir, "stalker_skin_albedo.png")
    bake(body, skin, "DIFFUSE", ALBEDO_PATH[0])
    bake(body, skin, "NORMAL", os.path.join(tex_dir, "stalker_skin_normal.png"))

    mats = {
        "cavity": node_material("StalkerMouth", (0.025, 0.002, 0.006), 0.35),
        "teeth": node_material("StalkerTeeth", (0.82, 0.8, 0.7), 0.35),
        "eye": node_material("StalkerEye", (0.01, 0.01, 0.012), 0.05),
        "claw": node_material("StalkerClaw", (0.16, 0.17, 0.2), 0.3),
    }
    parts = build_face(body, mats)

    rig = build_armature()
    body.parent = rig
    smooth_weights(body)
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    # Rigid parts (mouth, teeth, eyes, claws) each follow one bone.
    for part, bone in parts:
        group = part.vertex_groups.new(name=bone)
        group.add(range(len(part.data.vertices)), 1.0, "REPLACE")
        pm = part.modifiers.new("Armature", "ARMATURE")
        pm.object = rig
        part.parent = rig

    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    body.select_set(True)
    for part, _ in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = rig
    path = os.path.join(folder, "stalker.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
        embed_textures=False,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        mesh_smooth_type="FACE",
    )
    print(f"[make_stalker] wrote {path}, {len(body.data.vertices)} body vertices, {len(parts)} rigid parts")

    if len(argv) > 1:
        pose_crouch(rig)
        render_preview(os.path.abspath(argv[1]))


def pose_crouch(rig):
    """Bend the rest pose into the hunched crouch the game uses, for the preview only (not exported)."""
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="POSE")
    pb = rig.pose.bones
    def bend(name, x=0.0, y=0.0, z=0.0):
        b = pb[name]
        b.rotation_mode = "XYZ"
        b.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))
    bend("hips", x=25)
    bend("spine", x=20)
    bend("chest", x=15)
    bend("neck", x=-35)
    bend("head", x=-20)
    bend("jaw", x=18)
    for s, m in (("L", 1), ("R", -1)):
        bend(f"thigh.{s}", x=-70)
        bend(f"shin.{s}", x=110)
        bend(f"foot.{s}", x=-40)
        bend(f"upperarm.{s}", x=-30, z=10 * m)
        bend(f"forearm.{s}", x=-25)
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.location.z = -0.35


main()
