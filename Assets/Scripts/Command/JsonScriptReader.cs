using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JSONフォーマットのコマンドスクリプト読み込み
/// </summary>
public class JsonScriptReader
{
	private readonly JsonCommandScript m_commandScript;
	private int m_currentIndex = 0;
	
	public int CurrentIndex => m_currentIndex;
	
	public JsonScriptReader(string subfolder, string scriptName)
	{
		// CommandSpecManagerシングルトンのInstanceアクセスで自動初期化
		// (静的メソッド呼び出しの代わりにInstanceで初期化を保証)
		var _ = CommandSpecManager.Instance;
		
		string jsonText = ModifiableAssetsUtils.LoadTextFile(subfolder, scriptName);
		if (string.IsNullOrEmpty(jsonText))
		{
			AppDebug.LogError("ERR: JSON script cannot read {0}", scriptName);
			m_commandScript = null;
			return;
		}
		
		try
		{
			m_commandScript = JsonUtility.FromJson<JsonCommandScript>(jsonText);
			if (m_commandScript == null || m_commandScript.commands == null)
			{
				AppDebug.LogError("ERR: JSON script parsing failed {0}", scriptName);
				m_commandScript = null;
			}
			else
			{
				// 各コマンドのパラメーターをプリパース
				foreach (var cmd in m_commandScript.commands)
				{
					cmd.ParseParameters();
				}
			}
		}
		catch (System.Exception e)
		{
			AppDebug.LogError("ERR: JSON parsing exception {0}: {1}", scriptName, e.Message);
			m_commandScript = null;
		}
		
		m_currentIndex = 0;
	}
	
	/// <summary>
	/// 次のコマンドを取得
	/// </summary>
	public bool ReadNextCommand(out string commandName, out Dictionary<string, object> parameters)
	{
		commandName = null;
		parameters = null;
		
		if (!IsValid || IsAllRead)
		{
			return false;
		}
		
		var command = m_commandScript.commands[m_currentIndex];
		m_currentIndex++;
		
		commandName = command.name;
		parameters = command.GetParamsAsDict();
		
		return true;
	}
	
	/// <summary>
	/// 全てのコマンドが読み込まれたか
	/// </summary>
	public bool IsAllRead => m_commandScript == null || m_currentIndex >= m_commandScript.commands.Length;
	
	/// <summary>
	/// 有効なスクリプトが読み込まれているか
	/// </summary>
	public bool IsValid => m_commandScript != null;
	
	/// <summary>
	/// 現在のコマンドデータを取得（内部使用）
	/// </summary>
	public JsonCommandData GetCurrentCommand()
	{
		if (!IsValid || IsAllRead || m_currentIndex >= m_commandScript.commands.Length)
		{
			return null;
		}
		return m_commandScript.commands[m_currentIndex];
	}
	
	/// <summary>
	/// 特定インデックスのコマンドを取得
	/// </summary>
	public bool TryGetCommand(int index, out string commandName, out Dictionary<string, object> parameters)
	{
		commandName = null;
		parameters = null;
		
		if (!IsValid || index < 0 || index >= m_commandScript.commands.Length)
		{
			return false;
		}
		
		var command = m_commandScript.commands[index];
		commandName = command.name;
		parameters = command.GetParamsAsDict();
		
		return true;
	}
	
	/// <summary>
	/// 読み込み位置をリセット
	/// </summary>
	public void Reset()
	{
		m_currentIndex = 0;
	}
	
	/// <summary>
	/// コマンド数を取得
	/// </summary>
	public int GetCommandCount()
	{
		return IsValid ? m_commandScript.commands.Length : 0;
	}
}
