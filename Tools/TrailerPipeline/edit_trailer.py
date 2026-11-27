"""
Cuts the trailer for Jupiter: The Last Transmission in Blender's video sequencer and renders it to MP4.

Input: the frames recorded by Assets/_Project/Tests/PlayMode/TrailerCapture.cs (Builds/Trailer/<shot>/f_0000.jpg),
the game's own synthesised music and sound effects, and its voice lines. Output: Builds/Trailer/trailer.mp4.

    blender -b --factory-startup --python Tools/TrailerPipeline/edit_trailer.py -- <project folder>
"""
import os
import sys

import bpy

PROJECT = os.path.abspath(sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else ".")
FRAMES = os.path.join(PROJECT, "Builds", "Trailer")
OUT = os.path.join(FRAMES, "trailer.mp4")
AUDIO = os.path.join(PROJECT, "Assets", "_Project", "Audio", "Generated")
VOICE = os.path.join(PROJECT, "Assets", "_Project", "Resources", "Voice")
FPS = 30


def voice(conv, line):
    return os.path.join(VOICE, conv, f"{conv}_L{line:03d}.wav")


def music(name):
    return os.path.join(AUDIO, "Music", f"mus_{name}.wav")


def sfx(name):
    return os.path.join(AUDIO, "Combat", f"{name}.wav")


scene = bpy.context.scene
scene.render.resolution_x, scene.render.resolution_y = 1280, 720
scene.render.resolution_percentage = 100
scene.render.fps = FPS
se = scene.sequence_editor_create()
strips = se.strips if hasattr(se, "strips") else se.sequences


def fade(strip, attr, points):
    for frame, value in points:
        setattr(strip, attr, value)
        strip.keyframe_insert(data_path=attr, frame=frame)


def center(t):
    """Centres a text strip on its location (the property names changed across Blender versions)."""
    for name in ("anchor_x", "align_x"):
        if hasattr(t, name):
            setattr(t, name, "CENTER")
            break
    for name in ("anchor_y", "align_y"):
        if hasattr(t, name):
            setattr(t, name, "CENTER")
            break
    if hasattr(t, "alignment_x"):
        t.alignment_x = "CENTER"


def card(text, start, length, size=54, sub=None, channel=6):
    t = strips.new_effect(name=text[:20], type="TEXT", channel=channel, frame_start=start, frame_end=start + length)
    t.text = text
    t.font_size = size
    t.color = (0.92, 0.95, 0.97, 1.0)
    t.location = (0.5, 0.52 if sub else 0.5)
    center(t)
    t.use_shadow = True
    fade(t, "blend_alpha", [(start, 0.0), (start + 12, 1.0), (start + length - 12, 1.0), (start + length, 0.0)])
    if sub:
        s = strips.new_effect(name=sub[:20], type="TEXT", channel=channel + 1, frame_start=start, frame_end=start + length)
        s.text = sub
        s.font_size = int(size * 0.45)
        s.color = (0.6, 0.85, 0.95, 1.0)
        s.location = (0.5, 0.42)
        center(s)
        fade(s, "blend_alpha", [(start, 0.0), (start + 20, 1.0), (start + length - 12, 1.0), (start + length, 0.0)])


def shot(name, start, channel=2):
    """Places a recorded shot; returns its length in frames (0 if it was not recorded)."""
    folder = os.path.join(FRAMES, name)
    if not os.path.isdir(folder):
        print(f"[trailer] missing shot {name}")
        return 0
    files = sorted(f for f in os.listdir(folder) if f.endswith(".jpg"))
    if not files:
        return 0
    s = strips.new_image(name=name, filepath=os.path.join(folder, files[0]), channel=channel, frame_start=start)
    for f in files[1:]:
        s.elements.append(f)
    n = len(files)
    fade(s, "blend_alpha", [(start, 0.0), (start + 6, 1.0), (start + n - 6, 1.0), (start + n, 0.0)])
    return n


_next_channel = [20]


def sound(path, start, volume=1.0, channel=None, fade_in=0, fade_out=0, length=None):
    """One-off sounds each get their own channel (overlapping strips on one channel would be shuffled)."""
    if channel is None or channel < 13 or channel > 14:
        channel = _next_channel[0]
        _next_channel[0] += 1
    if not os.path.exists(path):
        print(f"[trailer] missing sound {path}")
        return None
    s = strips.new_sound(name=os.path.basename(path)[:20], filepath=path, channel=channel, frame_start=start)
    if length is not None:
        s.frame_final_end = start + length
    end = s.frame_final_end
    pts = []
    if fade_in:
        pts += [(start, 0.0), (start + fade_in, volume)]
    else:
        pts += [(start, volume)]
    if fade_out:
        pts += [(end - fade_out, volume), (end, 0.0)]
    fade(s, "volume", pts)
    return s


def bed(name, start, length, volume, channel):
    """A music bed: the loop repeated to fill the length, faded in and out."""
    path = music(name)
    t = start
    while t < start + length:
        s = sound(path, t, volume, channel)
        if s is None:
            return
        if s.frame_final_end > start + length:
            s.frame_final_end = start + length
        t = s.frame_final_end
    # Fade the whole bed by keying the first and last pieces.
    first = [x for x in strips if x.type == "SOUND" and x.channel == channel and x.frame_final_start == start][0]
    fade(first, "volume", [(start, 0.0), (start + 20, volume)])
    last = max((x for x in strips if x.type == "SOUND" and x.channel == channel), key=lambda x: x.frame_final_end)
    fade(last, "volume", [(start + length - 25, volume), (start + length, 0.0)])


f = 0
# Cold open: the countdown in the dark.
card("2183", f, 90, size=72)
sound(voice("ch01_launch_radio", 7), f + 12, 1.0, channel=11)
theme_start = f
f += 90

n = shot("01_lounge", f); sound(voice("ch01_family", 6), f + 40, 1.0, channel=11); f += n
n = shot("02_rocket", f); sound(voice("ch01_launch_radio", 9), f + 10, 1.0, channel=11)
sound(sfx("sfx_quake"), f + 20, 0.5, channel=12, fade_in=15); f += n
bed("theme", theme_start, f - theme_start + 30, 0.55, channel=13)

card("THE FIRST CREWED MISSION TO EUROPA", f, 75, size=40); f += 75
ship_start = f - 15
n = shot("03_jupiter", f); f += n
n = shot("04_galley", f); f += n
card("THEY WENT TO STUDY THE ICE", f, 75, size=40); f += 75
bed("ship", ship_start, f - ship_start, 0.5, channel=14)

dread_start = f - 10
n = shot("05_plain", f); f += n
n = shot("06_lander", f); f += n
card("SOMETHING BENEATH IT", f, 90, size=40, sub="WAS ALREADY LISTENING")
sound(sfx("sfx_feature7_pulse"), f, 0.9, channel=12, length=90, fade_out=20); f += 90
bed("dread", dread_start, f - dread_start, 0.45, channel=13)

# The ambush: the quake starts 15 frames into the shot.
amb = f
n = shot("07_ambush", f)
sound(sfx("sfx_quake"), amb + 15, 1.0, channel=12)
for k, dt in enumerate([40, 70, 95, 120, 150, 175]):
    sound(sfx("sfx_ice_burst"), amb + dt, 0.8, channel=15 + (k % 2))
sound(voice("ch05_quake_radio", 8), amb + 95, 1.0, channel=11)
sound(sfx("sfx_stalker_shriek"), amb + 160, 0.9, channel=17)
f += n
n = shot("08_charge", f)
sound(sfx("sfx_stalker_shriek"), f + 10, 1.0, channel=17)
f += n
bed("combat", amb + 15, f - amb - 15, 0.6, channel=14)
f += 30  # a second of black and silence

# Beyond the ridge.
sil = f
n = shot("09_silhouette", f)
sound(sfx("sfx_feature7_pulse"), f, 1.0, channel=12, fade_in=20)
sound(voice("eu_silhouette_radio", 4), f + 30, 1.0, channel=11)
sound(voice("eu_silhouette_radio", 6), f + 120, 1.0, channel=11)
f += n
n = shot("10_canister", f)
sound(voice("ch09_transmission", 6), f + 30, 1.0, channel=11)
f += n
bed("dread", sil, f - sil, 0.35, channel=13)

end = f
card("JUPITER", f, 150, size=96, sub="THE LAST TRANSMISSION"); f += 150
card("OUT NOW ON WINDOWS", f, 90, size=36); f += 90
bed("ending", end - 20, f - end + 20, 0.6, channel=14)

scene.frame_start, scene.frame_end = 0, f
r = scene.render
r.image_settings.file_format = "FFMPEG"
r.ffmpeg.format = "MPEG4"
r.ffmpeg.codec = "H264"
r.ffmpeg.constant_rate_factor = "HIGH"
r.ffmpeg.ffmpeg_preset = "GOOD"
r.ffmpeg.audio_codec = "AAC"
r.ffmpeg.audio_channels = "STEREO"
r.ffmpeg.audio_bitrate = 192
r.filepath = OUT
print(f"[trailer] {f} frames ({f / FPS:.1f} s) -> {OUT}")
bpy.ops.render.render(animation=True)
print("[trailer] done")
