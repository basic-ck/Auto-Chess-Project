# 바르코 3D 유닛 정리 방법 (Blender 일괄 처리)

바르코 원본은 면 50만 개 · 텍스처 4096px · 파일당 약 55MB 라서 표면이 울퉁불퉁하고 무겁다.
이 스크립트가 **정리된 가벼운 FBX(약 0.3MB, 6천 면) + 1024px 색 JPG** 로 바꿔 준다. (게임 화면에서는 원본과 거의 구분이 안 됨)

## 순서
1. 바르코에서 받은 원본 FBX 를 `RawModels/Unit_NN.fbx` 에 둔다 (Assets 밖, git 제외 — 원본 보관용).
2. 프로젝트 폴더에서 실행 (PowerShell 또는 Bash, 한 줄):

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --python Tools/clean_units.py -- --src RawModels --out Assets/Resources/Visuals/Units --tex Assets/Resources/Visuals/UnitTextures
```

   유닛 하나당 약 8초. 일부만: `--only Unit_01,Unit_05`
3. Unity 로 돌아가 메뉴 `AutoChess > 유닛 색상 텍스처 추출·적용` 실행 → 재질 연결 완료.

## 옵션 (결과가 마음에 안 들 때)
| 옵션 | 기본 | 설명 |
|---|---|---|
| `--faces` | 6000 | 목표 면 수. 형태가 뭉개지면 올리고(8000~12000), 더 가볍게 하려면 내린다 |
| `--smooth` | 0.5 | 표면 매끈하게(0~1). 울퉁불퉁하면 올리고(0.8), 얇은 부분이 사라지면 내린다(0.2) |
| `--minpart` | 0.02 | 전체 면의 2% 미만인 떨어진 조각 삭제 (공중에 뜬 파편 제거) |
| `--texsize` | 1024 | 색 텍스처 크기 |

## 참고
- 정리된 FBX 는 가벼워서 git 에 포함된다. 원본(`RawModels/`)은 포함되지 않으니 따로 백업할 것.
- 같은 이름으로 다시 돌리면 덮어쓴다(원본은 안전).
- 정리해도 형태 자체가 이상하면(팔이 붙음, 소품이 뭉개짐) 바르코에서 다시 만드는 게 빠르다 → `Docs/Varco3D_초보자_가이드.md` 참고.
