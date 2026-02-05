using System;

[Serializable]
public class EmotionDataItem
{
    public string body;
    public string face;
    public string eye;
    public string mouth;
    public string eyebrow;
    public string backeffect;
    public string other;

    /// <summary>
    /// 指定されたパーツ名に対応する値を取得
    /// </summary>
    public string GetPartValue(CharParts.EPartName partsName)
    {
        return partsName switch
        {
            CharParts.EPartName.Body => body,
            CharParts.EPartName.Face => face,
            CharParts.EPartName.Eye => eye,
            CharParts.EPartName.Mouth => mouth,
            CharParts.EPartName.Eyebrow => eyebrow,
            CharParts.EPartName.Backeffect => backeffect,
            CharParts.EPartName.Other => other,
            _ => null
        };
    }
}