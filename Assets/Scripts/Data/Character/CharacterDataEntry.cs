[System.Serializable]
public class CharacterDataEntry
{
    public string name;
    public string emotionListPath;
    public BlinkInitializeDataItem[] blink;
    public LipSyncInitializeDataItem[] lipSync;
    public string defaultEmotion;
    public CharacterTextDataItem textData;
}
