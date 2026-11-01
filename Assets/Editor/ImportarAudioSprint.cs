using System;
using UnityEditor;
using UnityEngine;

/// <summary>Importación móvil y catálogo reproducible para T016.</summary>
public sealed class ImportarAudioSprint : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Audio/", StringComparison.Ordinal)) return;
        var audio = (AudioImporter)assetImporter;
        bool largo = assetPath.Contains("Ambiente_") || assetPath.Contains("Musica_");
        var ajustes = audio.defaultSampleSettings;
        ajustes.loadType = largo ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        ajustes.compressionFormat = largo ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        ajustes.quality = .75f;
        ajustes.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        ajustes.sampleRateOverride = 48000;
        ajustes.preloadAudioData = !largo;
        audio.defaultSampleSettings = ajustes;
        audio.loadInBackground = largo;
    }
}

