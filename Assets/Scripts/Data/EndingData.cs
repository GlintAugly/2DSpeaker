using UnityEngine;
using System;

[Serializable]
public class EndingData : JsonReader<EndingData>
{
	public string bgmName;
	public string bgmCredit;
	public string[] credits;
}
