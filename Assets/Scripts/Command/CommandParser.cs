using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// コマンドパラメーターの共通パーサー
/// 型情報に基づいて文字列をパースする
/// </summary>
public static class CommandParser
{
	
	/// <summary>
	/// 文字列を指定された型にパースする
	/// </summary>
	public static bool TryParseValue(string value, Type targetType, out object result)
	{
		result = null;
		
		try
		{
			// string型
			if (targetType == typeof(string))
			{
				result = value;
				return true;
			}
			
			// int型
			if (targetType == typeof(int))
			{
				if (int.TryParse(value, out int intVal))
				{
					result = intVal;
					return true;
				}
				return false;
			}
			
			// float型
			if (targetType == typeof(float))
			{
				if (float.TryParse(value, out float floatVal))
				{
					result = floatVal;
					return true;
				}
				return false;
			}
			
			// bool型
			if (targetType == typeof(bool))
			{
				if (bool.TryParse(value, out bool boolVal))
				{
					result = boolVal;
					return true;
				}
				// "true"/"false"以外も対応
				if (value == "1") { result = true; return true; }
				if (value == "0") { result = false; return true; }
				return false;
			}
			
			// Enum型（EPositionなど）
			if (targetType.IsEnum)
			{
				try
				{
					result = Enum.Parse(targetType, value, true);
					return true;
				}
				catch
				{
					return false;
				}
			}
			
			// Vector2型
			if (targetType == typeof(Vector2))
			{
				return TryParseVector2(value, out Vector2 vec2Result) && 
					   (result = vec2Result) != null;
			}
			
			AppDebug.LogWarning("未対応の型: {0}", targetType.Name);
			return false;
		}
		catch (Exception e)
		{
			AppDebug.LogWarning("パースエラー: {0}", e.Message);
			return false;
		}
	}
	
	/// <summary>
	/// 型情報の配列から人間が読める文字列を生成（エディター用）
	/// </summary>
	public static string GetTypeSignature(Type[] types)
	{
		if (types == null || types.Length == 0) return "なし";
		
		string[] typeNames = new string[types.Length];
		for (int i = 0; i < types.Length; i++)
		{
			typeNames[i] = GetFriendlyTypeName(types[i]);
		}
		return string.Join(", ", typeNames);
	}
	
	/// <summary>
	/// 型名を分かりやすい表記に変換
	/// </summary>
	public static string GetFriendlyTypeName(Type type)
	{
		if (type == typeof(string)) return "string";
		if (type == typeof(int)) return "int";
		if (type == typeof(float)) return "float";
		if (type == typeof(bool)) return "bool";
		if (type.IsEnum) return type.Name;
		return type.Name;
	}
	
	/// <summary>
	/// JSON形式（辞書）のパラメーターをパースする
	/// CommandSpecのParamTypeMapを使用してキー名ベースで型変換
	/// </summary>
	/// <param name="jsonParams">JSON（またはその他ソース）から取得した辞書形式のパラメーター</param>
	/// <param name="spec">コマンド仕様</param>
	/// <param name="results">パース結果（out）- キー名をキーとした辞書</param>
	/// <returns>パース成功したか</returns>
	public static bool TryParseParamsFromDict(Dictionary<string, object> jsonParams, CommandSpec spec, out Dictionary<string, object> results)
	{
		results = new Dictionary<string, object>();
		
		if (jsonParams == null)
		{
			jsonParams = new Dictionary<string, object>();
		}
		
		if (!spec.ValidateParamCount(jsonParams.Count))
		{
			AppDebug.LogWarning("パラメーター数が不正: {0} (期待: {1}~{2})", 
				jsonParams.Count, spec.MinParamCount, spec.MaxParamCount);
			return false;
		}
		
		// CommandSpecで定義されているパラメーターを順に処理
		foreach (var paramName in spec.ParamNames)
		{
			if (!jsonParams.TryGetValue(paramName, out object rawValue))
			{
				AppDebug.LogWarning("必須パラメーター '{0}' が見つかりません。コマンド: {1}", 
					paramName, spec.CommandName);
				return false;
			}
			
			// rawValueが既に正しい型の場合と文字列の場合を処理
			Type targetType = spec.GetParamType(paramName);
			if (targetType == null)
			{
				AppDebug.LogWarning("型情報が見つかりません。コマンド: {0}, パラメーター: {1}", 
					spec.CommandName, paramName);
				return false;
			}
			
			// オブジェクトを文字列に変換して、TryParseValueで処理
			string stringValue = rawValue?.ToString() ?? "";
			
			if (!TryParseValue(stringValue, targetType, out object parsedValue))
			{
				AppDebug.LogWarning("パース失敗: パラメーター '{0}' の値 '{1}' を {2} に変換できません。コマンド: {3}", 
					paramName, stringValue, targetType.Name, spec.CommandName);
				return false;
			}
			
			results[paramName] = parsedValue;
		}
		
		return true;
	}
	
	/// <summary>
	/// Vector2を文字列からパース
	/// フォーマット: "x,y" または "x y" または {"x":1.0,"y":2.0}（JSON文字列）
	/// </summary>
	public static bool TryParseVector2(string value, out Vector2 result)
	{
		result = Vector2.zero;
		
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		
		// JSON形式の場合：{"x":1.0,"y":2.0}
		if (value.Contains("{") && value.Contains("}"))
		{
			try
			{
				// 簡易的なJSON解析（完全ではないが、シンプルなVector2用）
				// 本格的にはJsonUtilityを使用
				var jsonWrapper = JsonUtility.FromJson<Vector2Wrapper>("{\"v\":" + value + "}");
				if (jsonWrapper != null)
				{
					result = jsonWrapper.v;
					return true;
				}
			}
			catch
			{
				// JSON解析失敗時は下記の区切り文字解析に進む
			}
		}
		
		// カンマ区切り：x,y またはスペース区切り:x y
		char[] separators = { ',', ' ' };
		string[] parts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 2 && 
			float.TryParse(parts[0], out float x) && 
			float.TryParse(parts[1], out float y))
		{
			result = new Vector2(x, y);
			return true;
		}
		
		return false;
	}
	
	/// <summary>
	/// JsonUtility用のVector2ラッパークラス
	/// </summary>
	[System.Serializable]
	private class Vector2Wrapper
	{
		public Vector2 v;
	}
}
