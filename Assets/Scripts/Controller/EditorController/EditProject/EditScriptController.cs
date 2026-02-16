using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditScriptController : MonoBehaviour, IEditProjectController
{
	public EditProjectController EditProjectController { private get; set; }
	[SerializeField]
	Button m_backButton;
	
    [SerializeField]
    ReorderableVirtualizedList m_list;

	void Start()
	{
		m_backButton.onClick.AddListener(OnClickBack);
	}

	void OnEnable()
	{
		Initialize();
	}

    public void Initialize()
    {
        string scriptData = ModifiableAssetsUtils.LoadTextFile(ProjectManager.GetScriptFolder(), Definition.EXEC_SCRIPT_FILE);
        JsonCommandScript commandScript = null;
		try
		{
			commandScript = JsonUtility.FromJson<JsonCommandScript>(scriptData);
			if (commandScript == null || commandScript.commands == null)
			{
				AppDebug.LogError("ERR: JSON script parsing failed {0}/{1}", ProjectManager.GetScriptFolder(), Definition.EXEC_SCRIPT_FILE);
				commandScript = null;
			}
		}
		catch (System.Exception e)
		{
			AppDebug.LogError("ERR: JSON parsing exception {0}/{1}: {2}", ProjectManager.GetScriptFolder(), Definition.EXEC_SCRIPT_FILE, e.Message);
			commandScript = null;
		}

		if (commandScript == null)
		{
			return;
		}

        List<JsonCommandListItemData> listDatas = new(commandScript.commands.Length);
        for (int i = 0; i < commandScript.commands.Length; i++)
        {
            listDatas.Add(new JsonCommandListItemData
			{
				Command = commandScript.commands[i],
				Id = i.ToString(),
			});
        }

		m_list.SetItems(listDatas.ToArray());
    }
	
	void OnClickBack()
	{
		EditProjectController.GotoEditProjectMain();
	}
}
