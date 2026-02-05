using System;
using System.Collections.Generic;

/// <summary>
/// 感情データのルート
/// </summary>
[Serializable]
public class EmotionData : JsonReader<EmotionData>
{
    // 感情名をキー、パーツ設定を値とする辞書
    // Unity JsonUtilityは辞書を直接サポートしないため、リスト形式で保持
    public List<EmotionDataEntry> emotions = new();

    /// <summary>
    /// 辞書形式で感情データを取得
    /// </summary>
    public Dictionary<string, EmotionDataItem> GetEmotionMap()
    {
        var map = new Dictionary<string, EmotionDataItem>();
        foreach (var entry in emotions)
        {
            if (entry.name != null && entry.parts != null)
            {
                map[entry.name] = entry.parts;
            }
        }
        return map;
    }
}
