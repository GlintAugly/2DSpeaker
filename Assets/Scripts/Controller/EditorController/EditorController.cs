using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorController : MonoBehaviour
{
    enum EditorMode
    {
        Main,
        CharacterSetting,
        ProjectSetting
    }

    readonly Dictionary<EditorMode, GameObject> m_modeCanvases = new();
    static readonly Dictionary<EditorMode, Type> m_modeControllers = new()
    {
        {EditorMode.Main, typeof(EditorMainPageController) },
        {EditorMode.CharacterSetting, typeof(EditCharacterController) },
        {EditorMode.ProjectSetting, typeof(EditProjectController) },
    };

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        foreach (var kvp in m_modeControllers)
        {
            var mode = kvp.Key;
            var type = kvp.Value;
            var canvasObj = GetComponentInChildren(type, true);
            if (canvasObj == null)
            {
                AppDebug.LogError("ERR: EditorControllerの子オブジェクトに{0}がありません", type.Name);
                continue;
            }
            m_modeCanvases[mode] = canvasObj.gameObject;
            if (canvasObj is IEditController objInterface)
            {
                objInterface.EditorController = this;
            }
        }
        SwitchMode(EditorMode.Main);
    }

    void SwitchMode(EditorMode mode)
    {
        foreach (var kvp in m_modeCanvases)
        {
            kvp.Value.SetActive(kvp.Key == mode);
        }
    }

    public void GotoEditProject(string projectName)
    {
        EditProjectController editProjectController = m_modeCanvases[EditorMode.ProjectSetting].GetComponentInChildren<EditProjectController>(true);
        if (editProjectController == null)
        {
            AppDebug.LogError("ERR: EditProjectControllerが子オブジェクトにありません");
            return;
        }
        editProjectController.ProjectName = projectName;
        SwitchMode(EditorMode.ProjectSetting);
    }

    public void GotoEditCharacter()
    {
        SwitchMode(EditorMode.CharacterSetting);
    }

    public void GotoMain()
    {
        SwitchMode(EditorMode.Main);
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene("Title");
    }
}
