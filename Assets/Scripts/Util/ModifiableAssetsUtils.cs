using UnityEngine;
using System;
using UnityEngine.Networking;
using System.IO;
using System.Linq;

public class ModifiableAssetsUtils
{
    static readonly Vector2 CENTER_PIVOT = new(0.5f, 0.5f);
    /// <summary>
    /// ModifiableAssets内のサブフォルダパスを取得
    /// </summary>
    public static string GetModifiableAssetsSubfolderPath(string subfolder)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return Path.Combine(Application.streamingAssetsPath, subfolder);
#else
#error "未対応のプラットフォームです"
#endif
    }

    public static bool IsFileExists(string subfolder, string fileName)
    {
        var filePath = Path.Combine(GetModifiableAssetsSubfolderPath(subfolder), fileName);
        return File.Exists(filePath);
    }
    public static string[] GetFolders(string targetSubFolder)
    {
        var targetPath = GetModifiableAssetsSubfolderPath(targetSubFolder);

        if (!Directory.Exists(targetPath))
        {
            AppDebug.LogError("対象フォルダが見つかりません: {0}", targetPath);
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(targetPath).Select(Path.GetFileName).ToArray();
    }

    public static void CreateFolderIfNotExists(string targetSubFolder, string folderName)
    {
        var targetPath = GetModifiableAssetsSubfolderPath(targetSubFolder);
        var newFolderPath = Path.Combine(targetPath, folderName);

        if (!Directory.Exists(newFolderPath))
        {
            Directory.CreateDirectory(newFolderPath);
        }
    }

	/// <summary>
	/// ModifiableAssetsからAudioClipを読み込む
	/// </summary>
	/// <param name="subfolder">ModifiableAssets内のサブフォルダ（例："BGM", "Voice"）</param>
	/// <param name="fileName">ファイル名</param>
	/// <param name="onLoaded">読み込み完了時のコールバック</param>
	public static System.Collections.IEnumerator LoadAudioClipCoroutine(string subfolder, string fileName, Action<AudioClip> onLoaded)
	{
		var filePath = Path.Combine(GetModifiableAssetsSubfolderPath(subfolder), fileName);
		
#if !UNITY_ANDROID || UNITY_EDITOR
        if (!File.Exists(filePath))
        {
            AppDebug.LogError("ファイルが見つかりません: {0}", filePath);
            yield break;
        }
#endif
        var url = $"{Application.streamingAssetsPath}/{subfolder}/{fileName}".Replace("\\", "/");
#if !UNITY_ANDROID || UNITY_EDITOR
        url = "file://" + url;
#endif
        using var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var clip = DownloadHandlerAudioClip.GetContent(www);
            onLoaded?.Invoke(clip);
        }
        else
        {
            AppDebug.LogError("AudioClip読み込み失敗: {0} - {1}", filePath, www.error);
        }
    }


	/// <summary>
	/// ModifiableAssetsからAudioClipを読み込む（同期的）
	/// </summary>
	/// <param name="subfolder">ModifiableAssets内のサブフォルダ（例："BGM", "Voice"）</param>
	/// <param name="fileName">ファイル名</param>
	/// <returns>読み込んだAudioClip</returns>
	public static AudioClip LoadAudioClip(string subfolder, string fileName)
	{
#if UNITY_ANDROID
        AppDebug.LogError("Androidでは同期読み込みは使用できません: {0}/{1}", subfolder, fileName);
        return null;
#else
		var filePath = Path.Combine(GetModifiableAssetsSubfolderPath(subfolder), fileName);
		
        if (!File.Exists(filePath))
        {
            AppDebug.LogError("ファイルが見つかりません: {0}", filePath);
            return null;
        }

        try
        {
            // ファイルをバイト列として読み込む
            byte[] audioData = File.ReadAllBytes(filePath);

            // WAVのみ対応
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".wav")
            {
                AppDebug.LogError("非圧縮形式(WAV)のみ同期読み込み対応です: {0}", filePath);
                return null;
            }

            if (!TryParseWavHeader(audioData, out int channels, out int sampleRate, out int bitsPerSample, out int dataOffset, out int dataSize))
            {
                AppDebug.LogError("WAVヘッダー解析に失敗しました: {0}", filePath);
                return null;
            }

            int bytesPerSample = bitsPerSample / 8;
            int totalSamples = dataSize / bytesPerSample / channels;
            if (totalSamples <= 0)
            {
                AppDebug.LogError("WAVデータサイズが不正です: {0}", filePath);
                return null;
            }

            // AudioClipを作成
            AudioClip clip = AudioClip.Create(
                Path.GetFileNameWithoutExtension(fileName),
                totalSamples,
                channels,
                sampleRate,
                false
            );

            float[] pcmData = ConvertByteToFloat(audioData, dataOffset, dataSize, bitsPerSample, channels);
            clip.SetData(pcmData, 0);
            return clip;
        }
        catch (Exception ex)
        {
            AppDebug.LogError("AudioClip読み込み失敗: {0} - {1}", filePath, ex.Message);
            return null;
        }
#endif
    }

    /// <summary>
    /// バイト配列をFloat配列に変換（WAV PCM用）
    /// </summary>
    private static float[] ConvertByteToFloat(byte[] byteArray, int dataOffset, int dataSize, int bitsPerSample, int channels)
    {
        if (bitsPerSample != 16)
        {
            throw new NotSupportedException($"WAVの16bit PCMのみ対応です (bitsPerSample={bitsPerSample})");
        }

        int bytesPerSample = bitsPerSample / 8;
        int sampleCount = dataSize / bytesPerSample;
        float[] floatArray = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            int idx = dataOffset + i * bytesPerSample;
            short sample = (short)(byteArray[idx] | (byteArray[idx + 1] << 8));
            floatArray[i] = sample / 32768f;
        }

        return floatArray;
    }

    /// <summary>
    /// WAVヘッダーを解析してフォーマット情報とデータ位置を取得
    /// </summary>
    private static bool TryParseWavHeader(byte[] data, out int channels, out int sampleRate, out int bitsPerSample, out int dataOffset, out int dataSize)
    {
        channels = 0;
        sampleRate = 0;
        bitsPerSample = 0;
        dataOffset = 0;
        dataSize = 0;

        if (data == null || data.Length < 44)
        {
            return false;
        }

        // "RIFF" and "WAVE"
        if (data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F')
        {
            return false;
        }
        if (data[8] != 'W' || data[9] != 'A' || data[10] != 'V' || data[11] != 'E')
        {
            return false;
        }

        int offset = 12;
        bool fmtFound = false;
        bool dataFound = false;

        while (offset + 8 <= data.Length)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(data, offset, 4);
            int chunkSize = BitConverter.ToInt32(data, offset + 4);
            int chunkDataOffset = offset + 8;

            if (chunkId == "fmt ")
            {
                if (chunkSize < 16 || chunkDataOffset + chunkSize > data.Length)
                {
                    return false;
                }

                short audioFormat = BitConverter.ToInt16(data, chunkDataOffset + 0);
                channels = BitConverter.ToInt16(data, chunkDataOffset + 2);
                sampleRate = BitConverter.ToInt32(data, chunkDataOffset + 4);
                bitsPerSample = BitConverter.ToInt16(data, chunkDataOffset + 14);

                // PCMのみ対応
                if (audioFormat != 1)
                {
                    return false;
                }

                fmtFound = true;
            }
            else if (chunkId == "data")
            {
                dataOffset = chunkDataOffset;
                dataSize = chunkSize;
                if (dataOffset + dataSize > data.Length)
                {
                    return false;
                }
                dataFound = true;
            }

            offset = chunkDataOffset + chunkSize;
        }

        return fmtFound && dataFound;
    }

    /// <summary>
    /// ModifiableAssetsからSpriteを読み込む
    /// </summary>
    /// <param name="subfolder">ModifiableAssets内のサブフォルダ（例："BGM", "Voice"）</param>
    /// <param name="fileName">ファイル名</param>
    /// <param name="onLoaded">読み込み完了時のコールバック</param>
    public static System.Collections.IEnumerator LoadSpriteCoroutine(string subfolder, string fileName, Action<Sprite> onLoaded)
    {
        var filePath = Path.Combine(GetModifiableAssetsSubfolderPath(subfolder), fileName);

#if !UNITY_ANDROID || UNITY_EDITOR
        if (!File.Exists(filePath))
        {
            AppDebug.LogError("ファイルが見つかりません: {0}", filePath);
            yield break;
        }
#endif
        var url = $"{Application.streamingAssetsPath}/{subfolder}/{fileName}".Replace("\\", "/");
#if !UNITY_ANDROID || UNITY_EDITOR
        url = "file://" + url;
#endif

        using var www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var tex = DownloadHandlerTexture.GetContent(www);
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), CENTER_PIVOT);
            onLoaded?.Invoke(sprite);
        }
        else
        {
            AppDebug.LogError("Sprite読み込み失敗: {0} - {1}", filePath, www.error);
        }
    }

    /// <summary>
    /// ModifiableAssetsからSpriteを同期的に読み込む（Android以外）
    /// </summary>
    /// <param name="subfolder">ModifiableAssets内のサブフォルダ（例："BGM", "Voice"）</param>
    /// <param name="fileName">ファイル名</param>
    public static Sprite LoadSprite(string subfolder, string fileName)
    {
#if UNITY_ANDROID
        AppDebug.LogError("Androidでは同期読み込みは使用できません: {0}/{1}", subfolder, fileName);
        return null;
#else
        var filePath = Path.Combine(GetModifiableAssetsSubfolderPath(subfolder), fileName);

        if (!File.Exists(filePath))
        {
            AppDebug.LogError("ファイルが見つかりません: {0}", filePath);
            return null;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        // LoadImage() により実際のサイズに自動リサイズされるため、初期サイズは任意
        Texture2D tex = new(2, 2);
        if (tex.LoadImage(fileData))
        {
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), CENTER_PIVOT);
            return sprite;
        }

        AppDebug.LogError("Sprite読み込み失敗: {0}", filePath);
        return null;
#endif
    }

    /// <summary>
    /// ModifiableAssets内のフォントを読み込み
    /// </summary>
    /// <param name="fontPath">ModifiableAssets内のフォントファイルパス</param>
    /// <param name="fontSize">フォントサイズ</param>
    public static Font LoadFont(string fontPath, int fontSize)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AppDebug.LogError("Androidでは同期的なフォント読み込みは使用できません: {0}", fontPath);
        return null;
#else
        var filePath = Path.Combine(Application.streamingAssetsPath, fontPath);
        if (!File.Exists(filePath))
        {
            AppDebug.LogError("フォントファイルが見つかりません: {0}", filePath);
            return null;
        }

        var fontName = Path.GetFileNameWithoutExtension(filePath);
        Font font = Font.CreateDynamicFontFromOSFont(fontName, fontSize);
        if (font == null)
        {
            AppDebug.LogError("フォント読み込み失敗: {0}", filePath);
            return null;
        }
        return font;
#endif
    }

    public static string LoadTextFile(string subfolder, string fileName)
    {
        var filePath = Path.Combine(GetModifiableAssetsSubfolderPath(subfolder), fileName);

        if (!File.Exists(filePath))
        {
            AppDebug.LogError("ファイルが見つかりません: {0}", filePath);
            return null;
        }

        try
        {
            var content = File.ReadAllText(filePath);
            return content;
        }
        catch (Exception e)
        {
            AppDebug.LogError("テキストファイル読み込み失敗: {0} - {1}", filePath, e.Message);
            return null;
        }
    }
}
