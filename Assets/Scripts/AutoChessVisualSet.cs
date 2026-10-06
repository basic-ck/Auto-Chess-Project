using System;
using System.Collections.Generic;
using UnityEngine;

// 바르코 3D 등으로 만든 비주얼 에셋의 연결표. 에디터 메뉴 [AutoChess > 비주얼 에셋 전체 자동 매칭]이
// Assets/Resources/Visuals/ 를 탐색해 채우며(Assets/Resources/Visuals/VisualSet.asset), 비어 있는 칸은 게임에서 기본 도형/기본 UI로 대체된다.
public class AutoChessVisualSet : ScriptableObject
{
    public const string ResourcePath = "Visuals/VisualSet";

    public GameObject[] units = new GameObject[41];    // Unit_01~Unit_40 (인덱스 0~39 = 유닛 ID), Unit_41 = 소환 유령(선택)
    public GameObject[] mapTiles = new GameObject[8];  // Map_Tile_01~08 (역할은 AutoChessVisualManager.TileKind 참고)
    public GameObject[] environment = new GameObject[0]; // Environment/ 의 배경·관중석 등
    public List<SpriteEntry> sprites = new List<SpriteEntry>(); // UI/ 의 아이콘·배지 (파일 이름이 키)

    [Serializable] public struct SpriteEntry { public string key; public Sprite sprite; }

    public Sprite GetSprite(string key)
    {
        foreach (var e in sprites) if (e.key == key && e.sprite != null) return e.sprite;
        return null;
    }
}
