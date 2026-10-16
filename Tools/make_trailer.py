"""프레임 JPG 시퀀스(AutoChess.exe -demo -capture <폴더>)를 H.264 MP4 로 합친다. Blender(영상 편집기·FFmpeg 내장)를 백그라운드로 사용.

사용법(프로젝트 폴더에서):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --python Tools/make_trailer.py -- --frames TrailerFrames --out Trailer/trailer.mp4 --fps 30
  배경음악 추가: --audio Trailer/bgm.mp3   (길이가 영상보다 길면 영상 끝에서 자르고 마지막 2초 페이드 아웃)
"""
import argparse
import glob
import os
import sys
import bpy


def parse():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--frames", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--fps", type=int, default=30)
    p.add_argument("--audio", default="")
    p.add_argument("--crf", default="MEDIUM", help="화질: LOSSLESS/PERC_LOSSLESS/HIGH/MEDIUM/LOW")
    a = p.parse_args(argv)
    a.frames, a.out = os.path.abspath(a.frames), os.path.abspath(a.out)
    if a.audio: a.audio = os.path.abspath(a.audio)
    return a


def main():
    a = parse()
    files = sorted(glob.glob(os.path.join(a.frames, "f*.jpg")))
    if not files: raise SystemExit(f"프레임이 없습니다: {a.frames}")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.sequence_editor_create()
    seq = scn.sequence_editor
    first = bpy.data.images.load(files[0]); w, h = first.size
    scn.render.resolution_x, scn.render.resolution_y, scn.render.resolution_percentage = w, h, 100
    scn.render.fps = a.fps
    strip = seq.strips.new_image("frames", files[0], channel=1, frame_start=1)
    for f in files[1:]:
        strip.elements.append(os.path.basename(f))
    n = len(files)
    scn.frame_start, scn.frame_end = 1, n
    if a.audio:
        s = seq.strips.new_sound("bgm", a.audio, channel=2, frame_start=1)
        s.frame_final_end = min(s.frame_final_end, n)
        s.volume = .8
    scn.render.image_settings.media_type = "VIDEO"   # Blender 5.x: 먼저 영상 모드로 바꿔야 FFMPEG 를 고를 수 있다
    scn.render.image_settings.file_format = "FFMPEG"
    scn.render.ffmpeg.format = "MPEG4"
    scn.render.ffmpeg.codec = "H264"
    scn.render.ffmpeg.constant_rate_factor = a.crf
    scn.render.ffmpeg.ffmpeg_preset = "GOOD"
    scn.render.ffmpeg.audio_codec = "AAC" if a.audio else "NONE"
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    scn.render.filepath = a.out
    bpy.ops.render.render(animation=True)
    print(f"완료: {n}프레임 ({n / a.fps:.1f}초) -> {a.out}")


main()
