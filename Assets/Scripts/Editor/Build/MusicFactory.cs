// What Lies In The Depths — the music, as the project sees it.
//
// Two jobs, both small. It hands the composite the tracks to play, and it makes sure the
// files are imported in a way a long piece of music can afford: five ten-minute MP3s
// decompressed into memory is a hundred megabytes of silence waiting to be heard, so they
// are streamed from disk instead and re-encoded on the way in.
//
// The import settings are applied by a postprocessor rather than by the build, so a track
// dropped into the folder tomorrow gets them without anyone remembering to run anything.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class MusicFactory
    {
        public const string Folder = "Assets/Music";

        /// <summary>Every track in the music folder, in file-name order so a build is
        /// repeatable. The order is not the playing order — the Jukebox shuffles — but a
        /// stable list keeps the prefab from churning between builds.</summary>
        public static List<AudioClip> Tracks()
        {
            var clips = new List<AudioClip>();
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogWarning($"[What Lies In The Depths] No {Folder} folder: the screen will be built "
                                 + "with no music, and the now-playing line will stay hidden.");
                return clips;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null) clips.Add(clip);
            }
            clips.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
            return clips;
        }

        /// <summary>Applies the import settings to what is already in the folder. Only needed
        /// once, for files imported before the postprocessor existed.</summary>
        [MenuItem("Tools/What Lies In The Depths/Reimport Music", priority = 22)]
        public static void Reimport()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) { Debug.LogWarning($"[What Lies In The Depths] No {Folder} folder."); return; }

            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { Folder });
            foreach (var guid in guids)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);

            Debug.Log($"[What Lies In The Depths] Reimported {guids.Length} track(s) as streamed Vorbis.");
        }
    }

    /// <summary>Streamed, so a track costs a buffer rather than its whole decompressed self;
    /// Vorbis, because an MP3 is re-encoded on import anyway and Vorbis is what Unity is
    /// happiest with; loaded in the background, so nothing stalls on a ten-minute file.
    ///
    /// Nothing is said here about 3D. The importer's old flag is gone in Unity 6, and it was
    /// only ever a default: the Jukebox sets spatialBlend on its own sources, which is the
    /// only setting that decides whether music is in the room or in the ears.</summary>
    public sealed class MusicImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(MusicFactory.Folder + "/")) return;
            if (!(assetImporter is AudioImporter importer)) return;

            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.preloadAudioData = false;
            importer.defaultSampleSettings = s;

            importer.loadInBackground = true;
            importer.forceToMono = false;
        }
    }
}
