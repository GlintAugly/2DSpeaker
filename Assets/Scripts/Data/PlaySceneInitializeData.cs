using UnityEngine;
using System;

[Serializable]
public class PlaySceneInitializeData : JsonReader<PlaySceneInitializeData>
{
	public string[] characters;
    public Vector2 textPosition;
    public Vector2 textSize;
	public float sideRight;
	public float sideLeft;
	public float sideSlideX;
	public float sideSlideY;
	public float groundPos;
	public float bigPosY;
	public float bigPosRight;
	public float bigPosLeft;
	public float bigSize;
	public string BGName;
	public Vector2 BGSize;
	public Vector2 BGPos;
	public TextBoxDataItem BBData;
	public TextBoxDataItem SubtitleData;
	public string BGMName;
	public int BGMLoopSampleLength;
	public int BGMLoopEndSamplePos;
	public float BGMVolume;
	public string BGMCreditText;
	public float talkInterval;
}
