using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Tolik.RemakeSoF.Runtime.SoundManagement
{
    /// <summary>
    /// Lazy Loader fuer On-Demand AudioClip Loading von .wav-Dateien.
    /// Analog zu LazyTextureLoader — laedt AudioClips erst bei Bedarf aus dem Cache.
    /// </summary>
    public class LazySoundLoader : ISoundLoader
    {
        private readonly SoundRegistry m_Registry;

        /// <summary>
        /// Erstellt einen neuen LazySoundLoader.
        /// </summary>
        public LazySoundLoader(SoundRegistry registry)
        {
            m_Registry = registry;
        }

        /// <summary>
        /// Prueft ob der Key im Cache vorhanden ist (vorregistriert aber ggf. noch nicht geladen).
        /// </summary>
        public bool CanLoad(string key)
        {
            return m_Registry.SoundCache.ContainsKey(key);
        }

        /// <summary>
        /// Laedt den AudioClip fuer den Key. Wenn noch nicht geladen, wird von Disk gelesen.
        /// </summary>
        public SoundData Load(string key)
        {
            if (!m_Registry.SoundCache.TryGetValue(key, out SoundData soundData))
            {
                return null;
            }

            if (!File.Exists(soundData.FilePath))
            {
                return null;
            }

            if (!soundData.HasClip())
            {
                AudioClip clip = LoadAudioFromFile(soundData.FilePath, key);
                if (clip != null)
                {
                    m_Registry.UpdateSoundData(soundData.Id, clip);
                }
            }

            return soundData;
        }

        /// <summary>
        /// Laedt eine Audio-Datei (.wav oder .mp3) und erzeugt einen Unity AudioClip.
        /// WAV: Custom PCM Decoder (8/16/24/32 bit).
        /// MP3: Unity UnityWebRequestMultimedia.
        /// </summary>
        private AudioClip LoadAudioFromFile(string filePath, string clipName)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension == ".mp3")
                {
                    return LoadMp3FromFile(filePath);
                }

                byte[] fileData = File.ReadAllBytes(filePath);
                return DecodeWav(fileData, clipName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LazySoundLoader] Error loading audio from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Laedt eine MP3-Datei ueber UnityWebRequestMultimedia (synchroner Aufruf via file://).
        /// </summary>
        private static AudioClip LoadMp3FromFile(string filePath)
        {
            string fileUri = "file:///" + filePath.Replace('\\', '/');
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            // Synchron warten (laeuft auf Main Thread, file:// blockiert nicht lange)
            while (!operation.isDone) { }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LazySoundLoader] MP3 load failed: {filePath} — {request.error}");
                return null;
            }

            return DownloadHandlerAudioClip.GetContent(request);
        }

        /// <summary>
        /// Dekodiert eine WAV-Byte-Daten in einen AudioClip.
        /// Standard PCM WAV: RIFF header, fmt chunk, data chunk.
        /// </summary>
        private static AudioClip DecodeWav(byte[] wavData, string clipName)
        {
            if (wavData.Length < 44)
            {
                Debug.LogWarning($"[LazySoundLoader] WAV too short: {clipName}");
                return null;
            }

            // RIFF header validation
            if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F')
            {
                Debug.LogWarning($"[LazySoundLoader] Not a RIFF file: {clipName}");
                return null;
            }

            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int bitsPerSample = BitConverter.ToInt16(wavData, 34);

            // Find data chunk (skip past format chunk and any extra chunks)
            int dataOffset = 12;
            int dataSize = 0;
            while (dataOffset < wavData.Length - 8)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavData, dataOffset, 4);
                int chunkSize = BitConverter.ToInt32(wavData, dataOffset + 4);

                if (chunkId == "data")
                {
                    dataOffset += 8;
                    dataSize = chunkSize;
                    break;
                }

                dataOffset += 8 + chunkSize;
            }

            if (dataSize == 0)
            {
                Debug.LogWarning($"[LazySoundLoader] No data chunk found: {clipName}");
                return null;
            }

            int bytesPerSample = bitsPerSample / 8;
            int sampleCount = dataSize / (bytesPerSample * channels);

            float[] samples = new float[sampleCount * channels];

            for (int i = 0; i < samples.Length && dataOffset + bytesPerSample <= wavData.Length; i++)
            {
                switch (bitsPerSample)
                {
                    case 8:
                        samples[i] = (wavData[dataOffset] - 128) / 128f;
                        break;
                    case 16:
                        samples[i] = BitConverter.ToInt16(wavData, dataOffset) / 32768f;
                        break;
                    case 24:
                        int val24 = wavData[dataOffset] | (wavData[dataOffset + 1] << 8) | (wavData[dataOffset + 2] << 16);
                        if ((val24 & 0x800000) != 0) val24 |= unchecked((int)0xFF000000);
                        samples[i] = val24 / 8388608f;
                        break;
                    case 32:
                        samples[i] = BitConverter.ToInt32(wavData, dataOffset) / 2147483648f;
                        break;
                    default:
                        Debug.LogWarning($"[LazySoundLoader] Unsupported bits per sample: {bitsPerSample} in {clipName}");
                        return null;
                }

                dataOffset += bytesPerSample;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
