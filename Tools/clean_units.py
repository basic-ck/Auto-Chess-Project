"""바르코 3D 유닛 FBX 일괄 정리 (Blender 백그라운드 실행용). 사용법은 Tools/README_모델정리.md 참고.

각 FBX 마다: 겹친 점 합치기 -> 떨어진 작은 조각 제거 -> 표면 매끈하게 -> 폴리곤 감축 -> 부드러운 음영 -> 색 텍스처 1024px JPG 로 저장 -> 텍스처 없는 가벼운 FBX 로 내보내기.
Unity 쪽에서는 메뉴 'AutoChess > 유닛 색상 텍스처 추출·적용' 이 JPG 를 보고 재질을 연결한다.
"""
import argparse
import os
import sys
import bmesh
import bpy


def parse():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--src", required=True, help="바르코 원본 FBX 폴더")
    p.add_argument("--out", required=True, help="정리된 FBX 를 둘 폴더 (Assets/Resources/Visuals/Units)")
    p.add_argument("--tex", required=True, help="색 텍스처 JPG 를 둘 폴더 (Assets/Resources/Visuals/UnitTextures)")
    p.add_argument("--faces", type=int, default=6000, help="목표 면 수 (기본 6000)")
    p.add_argument("--smooth", type=float, default=0.5, help="표면 매끈하게 정도 0~1 (기본 0.5, 0이면 안 함)")
    p.add_argument("--texsize", type=int, default=1024)
    p.add_argument("--minpart", type=float, default=0.02, help="전체 면의 이 비율보다 작은 떨어진 조각은 삭제 (기본 0.02)")
    p.add_argument("--only", default="", help="쉼표로 구분한 파일 이름(확장자 제외)만 처리. 예: Unit_01,Unit_05")
    return p.parse_args(argv)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def apply_mod(obj, mod):
    with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj]):
        bpy.ops.object.modifier_apply(modifier=mod.name)


def remove_small_islands(obj, min_ratio):
    bm = bmesh.new(); bm.from_mesh(obj.data); bm.faces.ensure_lookup_table()
    seen, islands = set(), []
    for f in bm.faces:
        if f.index in seen: continue
        stack, isl = [f], []
        seen.add(f.index)
        while stack:
            c = stack.pop(); isl.append(c)
            for e in c.edges:
                for n in e.link_faces:
                    if n.index not in seen:
                        seen.add(n.index); stack.append(n)
        islands.append(isl)
    total = len(bm.faces)
    big = max(len(i) for i in islands) if islands else 0
    dead = [f for i in islands if len(i) < total * min_ratio and len(i) < big for f in i]
    removed = len([i for i in islands if len(i) < total * min_ratio and len(i) < big])
    if dead: bmesh.ops.delete(bm, geom=dead, context="FACES")
    bm.to_mesh(obj.data); bm.free()
    return removed


def process(path, a):
    name = os.path.splitext(os.path.basename(path))[0]
    reset()
    bpy.ops.import_scene.fbx(filepath=path)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        print(f"[{name}] 메시 없음, 건너뜀"); return False
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1: bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    before = len(obj.data.polygons)

    # 1) 겹친 점 합치기
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0001)
    bm.to_mesh(obj.data); bm.free()

    # 2) 표면 매끈하게 (울퉁불퉁한 잡음 제거)
    if a.smooth > 0:
        m = obj.modifiers.new("sm", "SMOOTH"); m.factor = a.smooth; m.iterations = 10
        apply_mod(obj, m)

    # 3) 폴리곤 감축
    ratio = min(1.0, a.faces / max(1, len(obj.data.polygons)))
    m = obj.modifiers.new("dec", "DECIMATE"); m.decimate_type = "COLLAPSE"; m.ratio = ratio; m.use_collapse_triangulate = True
    apply_mod(obj, m)

    # 4) 떨어진 작은 조각 제거 (감축 후에 해야 빠르다)
    removed = remove_small_islands(obj, a.minpart)

    # 5) 부드러운 음영 (각진 면 줄이기)
    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_auto_smooth(angle=0.9)  # 약 50도 이상 꺾인 모서리는 살림
    except Exception:
        pass
    obj.name = "texture_mapping"  # Unity 에서 이전과 같은 오브젝트 이름 유지(필수 아님)

    # 6) 색 텍스처 -> 1024px JPG
    img = bpy.data.images.get("base_color")
    tex_note = "텍스처 없음"
    if img is not None:
        img.scale(a.texsize, a.texsize)
        os.makedirs(a.tex, exist_ok=True)
        jpg = os.path.join(a.tex, f"{name}_color.jpg")
        img.filepath_raw = jpg; img.file_format = "JPEG"
        img.save(quality=90)
        tex_note = f"{a.texsize}px JPG"

    # 재질은 이름만 material_0 으로 남기고 텍스처 노드는 비운다(Unity 가 JPG 로 재질을 새로 만듦)
    for slot in obj.material_slots:
        if slot.material:
            slot.material.name = "material_0"
    for mat in list(bpy.data.materials):
        if mat.use_nodes:
            for n in [n for n in mat.node_tree.nodes if n.type in {"TEX_IMAGE", "NORMAL_MAP"}]:
                mat.node_tree.nodes.remove(n)

    # 7) 내보내기 (텍스처 미포함)
    os.makedirs(a.out, exist_ok=True)
    outp = os.path.join(a.out, name + ".fbx")
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=outp, use_selection=True, object_types={"MESH"}, path_mode="STRIP",
                             embed_textures=False, add_leaf_bones=False, bake_anim=False, mesh_smooth_type="FACE")
    print(f"[{name}] 면 {before:,} -> {len(obj.data.polygons):,} / 떨어진 조각 {removed}개 삭제 / {tex_note} / {os.path.getsize(outp)//1024}KB")
    return True


def main():
    a = parse()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    files = sorted(f for f in os.listdir(a.src) if f.lower().endswith(".fbx") and (not only or os.path.splitext(f)[0] in only))
    ok = 0
    for f in files:
        try:
            ok += bool(process(os.path.join(a.src, f), a))
        except Exception as e:
            print(f"[{f}] 실패: {e}")
    print(f"완료: {ok}/{len(files)}")


main()
