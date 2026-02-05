using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JSONのコマンドデータ構造
/// JsonUtilityでシリアライズ可能にするためのラッパークラス
/// </summary>
[Serializable]
public class JsonCommandData
{
	[SerializeField]
	public string name;
	
	[SerializeField]
	public JsonParameterValue[] paramArray;
	
	/// <summary>
	/// 変換済みパラメーター（内部キャッシュ）
	/// </summary>
	private Dictionary<string, object> m_parsedParams;
	private bool m_isParsed;
	
	/// <summary>
	/// パラメーターを解析・変換（初回のみ実行）
	/// CommandSpecの型情報を使用して自動変換
	/// </summary>
	public void ParseParameters()
	{
		if (m_isParsed)
		{
			return;
		}
		
		m_parsedParams = new Dictionary<string, object>();
		
		if (string.IsNullOrEmpty(name) || paramArray == null)
		{
			m_isParsed = true;
			return;
		}
		
		// CommandSpecを取得
		var spec = CommandSpecManager.GetSpec(name);
		if (spec == null)
		{
			AppDebug.LogWarning("CommandSpec not found for command: {0}", name);
			m_isParsed = true;
			return;
		}
		
		// 各パラメーターを変換
		foreach (var param in paramArray)
		{
			if (string.IsNullOrEmpty(param.key))
			{
				continue;
			}
			
			// この親キーの型を取得
			Type targetType = spec.GetParamType(param.key);
			if (targetType == null)
			{
				AppDebug.LogWarning("Parameter type not found: command={0}, paramKey={1}", name, param.key);
				m_parsedParams[param.key] = param.value;
				continue;
			}
			
			// 文字列値を型に変換
			if (CommandParser.TryParseValue(param.value, targetType, out object parsedValue))
			{
				m_parsedParams[param.key] = parsedValue;
			}
			else
			{
				AppDebug.LogWarning("Failed to parse parameter: command={0}, paramKey={1}, value={2}, type={3}", 
					name, param.key, param.value, targetType.Name);
				// 変換失敗時は文字列のまま保存
				m_parsedParams[param.key] = param.value;
			}
		}
		
		m_isParsed = true;
	}
	
	/// <summary>
	/// パラメーター配列を変換済みの辞書で取得
	/// </summary>
	public Dictionary<string, object> GetParamsAsDict()
	{
		if (!m_isParsed)
		{
			ParseParameters();
		}
		return m_parsedParams;
	}
	
	/// <summary>
	/// 変換をリセット（テスト用）
	/// </summary>
	public void ResetParsing()
	{
		m_parsedParams = null;
		m_isParsed = false;
	}
}

/// <summary>
/// JSONのパラメーター値（キー・値ペア）
/// </summary>
[Serializable]
public class JsonParameterValue
{
	[SerializeField]
	public string key;
	
	[SerializeField]
	public string value;
}

/// <summary>
/// JSONファイルのルート構造
/// </summary>
[Serializable]
public class JsonCommandScript
{
	[SerializeField]
	public JsonCommandData[] commands;
}
