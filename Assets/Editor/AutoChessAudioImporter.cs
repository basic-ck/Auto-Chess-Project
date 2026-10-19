using UnityEditor;
using UnityEngine;

// Resources/Audio/ 아래 소리를 가져올 때 용량을 줄이는 설정.
// 효과음: 짧으니 Vorbis 압축 + 메모리에 압축 상태로 보관, 모노(용량 절반)
public class AutoChessAudioImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var im = (AudioImporter)assetImporter;
        var s = im.defaultSampleSettings;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = .6f;
        s.loadType = AudioClipLoadType.CompressedInMemory;
        im.defaultSampleSettings = s;
        im.forceToMono = true;
    }
}
