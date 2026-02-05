using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CommandSpec管理クラス
/// コマンド名からCommandSpecを検索するシングルトン
/// </summary>
public class CommandSpecManager : MonoBehaviourSingleton<CommandSpecManager>
{
	private Dictionary<string, CommandSpec> m_specCache;
	private bool m_initialized = false;
	
	/// <summary>
	/// Awakeで自動初期化
	/// </summary>
	protected override void Awake()
	{
		base.Awake();
		
		if (!m_initialized)
		{
			InitializeSpecs();
		}
	}
	
	/// <summary>
	/// CommandSpec辞書を初期化
	/// </summary>
	private void InitializeSpecs()
	{
		if (m_initialized)
		{
			return;
		}
		
		m_specCache = new Dictionary<string, CommandSpec>();
		var commandSpecs = Resources.LoadAll<CommandSpec>("CommandSpecs/");
		
		foreach (var spec in commandSpecs)
		{
			if (!string.IsNullOrEmpty(spec.CommandName))
			{
				m_specCache[spec.CommandName] = spec;
				spec.BuildParamMap();
			}
		}
		
		m_initialized = true;
	}
	
	/// <summary>
	/// コマンド名からCommandSpecを取得
	/// </summary>
	public static CommandSpec GetSpec(string commandName)
	{
		// シングルトンインスタンスが必要に応じて自動作成される
		var manager = Instance;
		
		if (manager.m_specCache == null || !manager.m_initialized)
		{
			manager.InitializeSpecs();
		}
		
		if (manager.m_specCache.TryGetValue(commandName, out var spec))
		{
			return spec;
		}
		
		AppDebug.LogWarning("CommandSpec not found: {0}", commandName);
		return null;
	}
	
	/// <summary>
	/// キャッシュをクリア（テスト用）
	/// </summary>
	public static void ClearCache()
	{
		var manager = Instance;
		manager.m_specCache?.Clear();
		manager.m_initialized = false;
	}
}
