/*using System.Collections.Generic;
using UnityEngine;

public class EditScriptController : MonoBehaviour
{
    [SerializeField]
    ReorderableVirtualizedList m_list;

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
}
*/