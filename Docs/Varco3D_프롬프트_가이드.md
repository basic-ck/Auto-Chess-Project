# 바르코 3D 에셋 제작 가이드 (AutoChess)

생성한 파일을 아래 규칙대로 저장하고 Unity 메뉴 **[AutoChess] → [비주얼 에셋 전체 자동 매칭]** 을 누르면 자동 연결된다.
파일이 없는 칸은 기본 도형/기본 UI로 표시되므로, 일부만 만들어도 게임은 항상 동작한다.

## 1. 폴더와 파일 이름

`Assets/Resources/Visuals/` (메뉴를 한 번 누르면 폴더가 자동 생성됨)

| 폴더 | 파일 이름 | 설명 |
|---|---|---|
| `Map/` | `Map_Tile_01` ~ `Map_Tile_08` (.fbx) | 맵 타일 (아래 역할표) |
| `Units/` | `Unit_01` ~ `Unit_40` (.fbx), 선택: `Unit_41` | 유닛 40종 (아래 번호표), 41 = 소환 유령 |
| `Environment/` | 이름 자유 (`Env_BG.fbx`, `Env_Crowd.fbx` …) | 폴더 안 3D 파일 전부를 무대 중심에 배치 |
| `UI/` | `UI_Icon_Gold.png`, `UI_Badge_<이름>.png` | 골드 아이콘, 시너지 배지 (PNG는 자동으로 Sprite 변환) |

### 맵 타일 역할 (Map_Tile_NN)

| 번호 | 역할 | 사용 위치 |
|---|---|---|
| 01 / 02 | 아군 진영 밝은 칸 / 어두운 칸 | 아래 4줄 체스판 무늬 |
| 03 / 04 | 적 진영 밝은 칸 / 어두운 칸 | 위 4줄 체스판 무늬 |
| 05 / 06 | 대기석 칸 A / B | 필드 아래 1×8 (홀짝 번갈아) |
| 07 | 중앙 경계선 조각 | 아군/적 진영 사이, 8열에 하나씩 |
| 08 | 무대 바닥 받침 | 전장+대기석 전체 아래 (한 개) |

### 시너지 배지 이름 (UI_Badge_*)

종족: `Human Beast Undead Spirit Golem Machine Goblin Orc Elf Dragon Dwarf Naga Demon God`
직업: `Knight Hunter Assassin Mage Warrior Priest Shaman Summoner Warlock Mecha`
예) `UI_Badge_Human.png`, `UI_Badge_Knight.png`

### 모델 제작 규칙 (자동 보정됨)

- 피벗·크기는 자유: 유닛은 발 밑 중앙 정렬 + 높이 자동 맞춤, 타일은 가로세로 자동 맞춤 + 윗면 높이 자동 정렬.
- 유닛이 바라보는 방향은 **+Z(앞)** 로 만들면 전투 방향과 맞는다 (현재는 회전 없이 그대로 표시).
- 재질은 1개(색 텍스처 한 장), 유닛 3천~8천 폴리곤, 타일 500 폴리곤 이하 권장. 내보내기 FBX, Y-up.
- 적 유닛은 같은 모델에 어두운 색 배율이 자동으로 곱해진다(별도 파일 불필요).

## 2. 통일 스타일 블록 (모든 프롬프트에 붙인다)

```
STYLE (prepend to every prompt):
stylized low-poly fantasy auto-battler game asset, faceted flat-shaded polygons,
hand-painted gradient-free flat color texture, clean readable silhouette,
chunky proportions, soft pastel-saturated palette, single object, centered,
plain white background, no ground, no shadow, game-ready 3D model, neutral pose

NEGATIVE (if supported):
realistic, photoreal, high-poly, noisy texture, thin parts, text, logo, watermark,
multiple objects, background scenery, dramatic lighting, blurry
```

색감 통일 팁: 모든 에셋에 `soft pastel-saturated palette, warm key light baked, no black outlines` 를 공통으로 유지한다.

## 3. 맵 타일 프롬프트

```
[STYLE] square game board tile, 1x1 modular, flat top surface, slightly beveled edges,
thin slab, stone-and-grass fantasy arena floor, light checker variant (Map_Tile_01)
```

변형: 01 `light cream stone, blue-tinted trim` / 02 `darker slate stone, blue-tinted trim` /
03 `light terracotta stone, red-tinted trim` / 04 `dark maroon stone, red-tinted trim` /
05 `teal wooden bench platform slab` / 06 `darker teal wooden bench platform slab` /
07 `thin low stone border strip with small gold inlay, 1 unit long` /
08 `large round arena stage base platform, layered stone steps, flat top, 10 units wide`

## 4. 유닛 프롬프트

템플릿:

```
[STYLE] {캐릭터 설명}, standing idle pose facing forward, feet on flat base,
height about 1 unit, {대표 색} color theme, readable at small size
```

| 번호 | 유닛 | 영문 키워드 (템플릿의 {캐릭터 설명}) |
|---|---|---|
| 01 | 수련기사 | trainee knight, blue steel armor, small round shield, short sword |
| 02 | 숲의 견습사수 | forest apprentice archer, green hooded beast-kin, small bow |
| 03 | 골목 암살자 | alley assassin, slim undead rogue, black cloak, twin daggers |
| 04 | 불꽃 수습생 | flame novice mage, small fire spirit, orange robe, floating flame orb |
| 05 | 바위 파수꾼 | rock sentinel, bulky grey stone golem, mossy boulder fists |
| 06 | 태엽 쥐 | clockwork rat, tiny yellow wind-up mechanical rat, key on back |
| 07 | 마을 의사 | village doctor, white robe healer, medical satchel, cross staff |
| 08 | 야생 늑대 | wild wolf, brown fur, low aggressive stance |
| 09 | 고블린 도둑 | goblin thief, lime-green skin, coin sack, dagger |
| 10 | 사막 주술사 | desert shaman, purple robe orc, totem staff with feathers |
| 11 | 광전사 | berserker, red-skinned orc warrior, dual axes, torn armor |
| 12 | 독침 전갈 | venom scorpion, teal armor shell, glowing stinger tail |
| 13 | 그림자 닌자 | shadow ninja, dark purple demon, kunai, scarf |
| 14 | 얼음 학자 | ice scholar, sky-blue ice spirit, crystal book, frost cloak |
| 15 | 성기사 단장 | paladin commander, golden plate armor, war hammer, long cape |
| 16 | 영혼 소환사 | soul summoner, dark green undead necromancer, orbiting ghost wisps |
| 17 | 강철 수호자 | steel guardian, silver robot knight, tower shield |
| 18 | 바람 추적자 | wind tracker, bright green elf ranger, long bow, windy scarf |
| 19 | 해적 칼잡이 | pirate swordsman, blue captain coat, cutlass, tricorn hat |
| 20 | 흡혈 귀족 | vampire noble, crimson-purple lord, high collar cape, wine goblet |
| 21 | 드래곤 나이트 | dragon knight, orange scaled knight, dragon helm, spear |
| 22 | 저격 명수 | master sniper, brown dwarf marksman, very long rifle |
| 23 | 대성학자 | grand scholar, glowing blue sage, floating spell books |
| 24 | 달빛 무희 | moon dancer, pink elf dancer, crescent blade fans |
| 25 | 독폭탄 연금술사 | toxic alchemist, yellow-green goblin, flask bombs, goggles |
| 26 | 용암 거인 | lava giant, red-grey rock golem with glowing lava cracks |
| 27 | 사령관 | commander, human officer in uniform, epaulettes, saber |
| 28 | 심해 암살자 | deep-sea assassin, deep blue naga, trident, fin crest |
| 29 | 별빛 사제 | starlight priest, glowing white elf priestess, star staff |
| 30 | 시간 술사 | time mage, glowing purple celestial, floating hourglass |
| 31 | 메두사 여왕 | medusa queen, emerald naga, snake hair, bow |
| 32 | 심연의 군주 | abyss lord, dark red demon warlord, huge black sword, horns |
| 33 | 폭탄 광인 | bomb maniac, red mechanical bomber, bomb backpack, fuse |
| 34 | 성스러운 대수호자 | holy guardian, translucent golden armor, glowing divine shield |
| 35 | 대사제 역병의 주인 | plague archpriest, black-green undead priest, smoking censer |
| 36 | 천둥 주술사 | thunder shaman, yellow orc, lightning staff |
| 37 | 해일의 신 | tidal god, giant deep blue naga, armor made of waves, trident |
| 38 | 대재앙의 정령 | calamity spirit, giant black sphere spirit, swirling void ring |
| 39 | 비행함대 폭격기 | air fleet bomber, silver airship, bomb bay, small propellers |
| 40 | 태초의 신 제우스 | Zeus, glowing white lightning god, thunderbolt in hand, toga |

크기 힌트(게임 안에서 자동 맞춤되지만 비율 참고): 1~2골드 소형, 3골드 중형, 4~5골드 대형. 소환 유령(`Unit_41`)은 `tiny cute ghost, pale green`.

## 5. 배경/관중석 프롬프트 (Environment/)

```
[STYLE] fantasy colosseum arena backdrop, circular stone stands with crowd of tiny low-poly spectators,
banners in blue and red, open center left empty for a game board, wide ring shape (Env_Crowd)
```
```
[STYLE] floating island arena background, soft sky, clouds, distant mountains, flat color,
open center, wide ring shape (Env_BG)
```
배경은 만든 크기 그대로 전장 중심(보드 중앙)에 놓인다. 보드(약 8×10)보다 안쪽이 비어 있게 만들 것.

## 6. UI 아이콘/배지 프롬프트 (UI/)

```
[STYLE] game UI icon, gold coin with simple star emboss, front view, thick outline,
flat color, transparent background, 256x256 (UI_Icon_Gold)
```
```
[STYLE] game UI badge icon, round emblem, thick rim, {상징} symbol in the center,
{배지 색} color, flat color, transparent background, 256x256 (UI_Badge_{이름})
```
예: `Human` = `simple crown, blue` / `Beast` = `claw marks, green` / `Undead` = `skull, dark green` /
`Spirit` = `flame wisp, orange` / `Golem` = `boulder, grey` / `Machine` = `gear, yellow` /
`Goblin` = `pointed ear, lime` / `Orc` = `tusk, purple` / `Elf` = `leaf, mint green` /
`Dragon` = `dragon head, orange-red` / `Dwarf` = `anvil, brown` / `Naga` = `wave and fish scale, teal` /
`Demon` = `horns, dark red` / `God` = `lightning, white-gold` /
`Knight` = `shield, silver` / `Hunter` = `crossed arrows, green` / `Assassin` = `dagger, black` /
`Mage` = `star wand, blue` / `Warrior` = `crossed swords, red` / `Priest` = `cross and light, white` /
`Shaman` = `totem, purple` / `Summoner` = `spirit orb, light green` / `Warlock` = `curse rune, dark purple` /
`Mecha` = `bolt and gear, steel`

## 7. 작업 순서

1. 타일 → 유닛 → 배경 → UI 순서로 만들고, 만든 만큼만 폴더에 넣는다.
2. Unity에서 **[AutoChess] → [비주얼 에셋 전체 자동 매칭]** 실행 (콘솔에 연결된 개수와 빠진 번호가 표시됨).
3. Play로 확인: 유닛이 땅에 붙어 있는지, 크기가 일정한지, 적 유닛이 어둡게 보이는지 본다.
4. 이름 규칙에 안 맞는 파일은 "이름 규칙에 안 맞아 건너뜀"으로 표시되니 이름을 고쳐 다시 실행한다.
