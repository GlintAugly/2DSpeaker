using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// パラメーター情報（キー名と型名のペア）
/// インスペクターで編集可能
/// </summary>
[System.Serializable]
public class ParamInfo
{
	[SerializeField]
	[Tooltip("パラメーター名")]
	public string paramName;
	
	[SerializeField]
	[Tooltip("型名")]
	public string typeName;
	[SerializeField]
	[Tooltip("説明")]
	public string hint;
}

/// <summary>
/// コマンドの仕様定義
/// ScriptableObjectとして、エディターから設定可能
/// </summary>
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CreateCommandSpecAsset")]
public class CommandSpec : ScriptableObject
{
	[SerializeField]
	[Tooltip("コマンド名")]
	private string m_commandName;
	[SerializeField]
	[Tooltip("コマンドの説明")]
	private string m_commandHint;
	
	[SerializeField]
	[Tooltip("パラメーター情報")]
	private ParamInfo[] m_paramInfos;
	
	[SerializeField]
	[Tooltip("最小パラメーター数")]
	private int m_minParamCount;
	
	[SerializeField]
	[Tooltip("最大パラメーター数")]
	private int m_maxParamCount;
	
	public string CommandName => m_commandName;
	/// <summary>パラメーター情報の配列</summary>
	public ParamInfo[] ParamInfos => m_paramInfos;
	/// <summary>パラメーター名の順序付きリスト</summary>
	public List<string> ParamNames { get; private set; }
	/// <summary>パラメーター名 → 型 のマッピング</summary>
	public Dictionary<string, Type> ParamTypeMap { get; private set; }
	public int MinParamCount => m_minParamCount;
	public int MaxParamCount => m_maxParamCount;
	
	/// <summary>
	/// パラメーター数が仕様に合っているか検証
	/// </summary>
	public bool ValidateParamCount(int count)
	{
		return count >= MinParamCount && count <= MaxParamCount;
	}
	
	/// <summary>
	/// パラメーター名が有効か検証
	/// </summary>
	public bool IsValidParamName(string paramName)
	{
		return ParamNames != null && ParamNames.Contains(paramName);
	}
	
	/// <summary>
	/// 指定パラメーターの型を取得
	/// </summary>
	public Type GetParamType(string paramName)
	{
		if (ParamTypeMap != null && ParamTypeMap.TryGetValue(paramName, out Type type))
		{
			return type;
		}
		return null;
	}
	
	/// <summary>
	/// パラメーター情報をキー名と型のマッピングに変換（エディター用）
	/// </summary>
	private void OnEnable()
	{
		BuildParamMap();
	}
	
	/// <summary>
	/// パラメーターマップを構築
	/// </summary>
	public void BuildParamMap()
	{
		ParamNames = new List<string>();
		ParamTypeMap = new Dictionary<string, Type>();
		
		if (m_paramInfos == null || m_paramInfos.Length == 0)
		{
			return;
		}
		
		foreach (var paramInfo in m_paramInfos)
		{
			if (string.IsNullOrEmpty(paramInfo.paramName))
			{
				continue;
			}
			
			ParamNames.Add(paramInfo.paramName);
			Type mappedType = StringToType(paramInfo.typeName, paramInfo.paramName);
			
			if (mappedType != null)
			{
				ParamTypeMap[paramInfo.paramName] = mappedType;
			}
			else
			{
				AppDebug.LogError("型 '{0}' が見つかりません。CommandSpec: {1}, パラメーター: {2}", 
					paramInfo.typeName, m_commandName, paramInfo.paramName);
			}
		}
	}
	
	/// <summary>
	/// 型名の文字列をType に変換
	/// </summary>
	private Type StringToType(string typeName, string paramName) => typeName switch
	{
		"float" => typeof(float),
		"int" => typeof(int),
		"string" => typeof(string),
		"Vector2" => typeof(Vector2),
		"EHorizontalSlot" => typeof(PlayController.EHorizontalSlot),
		"EVerticalSlot" => typeof(PlayController.EVerticalSlot),
		"bool" => typeof(bool),
		"character" => typeof(string),
		"emotion" => typeof(string),
		"animation" => typeof(string),
		_ => GetCustomType(typeName)
	};

	private Type GetCustomType(string typeName)
	{
		return System.Type.GetType(typeName);
	}
}
