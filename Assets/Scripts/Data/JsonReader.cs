using UnityEngine;
using System;

[Serializable]
public class JsonReader<T> where T : class
{
	/// <summary>
	/// JSONテキストから指定された型のオブジェクトを読み込み
	/// </summary>
	public static T LoadFromJson(string jsonText)
	{
		try
		{
			return JsonUtility.FromJson<T>(jsonText);
		}
		catch (Exception e)
		{
			AppDebug.LogError("{0} JSON parse error: {1}", typeof(T).Name, e.Message);
			return null;
		}
	}
}
