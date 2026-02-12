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

    [SerializeField]
    readonly Button m_editCharacterButton;
    [SerializeField]
    readonly GameObject m_editProjectMenu;
    [SerializeField]
    readonly InputField m_projectNameInputField;
    [SerializeField]
    readonly Button m_newProjectButton;
    [SerializeField]
    readonly Dropdown m_existingProjectsDropdown;
    [SerializeField]
    readonly Button m_editExistingProjectButton;
    [SerializeField]
    readonly Button m_backToTitleButton;

    readonly Dictionary<EditorMode, GameObject> m_modeCanvases = new();
    static readonly Dictionary<string, EditorMode> m_canvasNames = new Dictionary<string, EditorMode>()
    {
        {"MainUI", EditorMode.Main },
        {"CharacterSettingUI", EditorMode.CharacterSetting },
        {"EditProject", EditorMode.ProjectSetting },
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_editCharacterButton.onClick.AddListener(OnClickCharacterSettingMode);
        m_newProjectButton.onClick.AddListener(OnClickNewProject);
        m_backToTitleButton.onClick.AddListener(OnClickBackToTitle);
        string[] projects = ProjectManager.GetAllProjects();
        if(projects.Length > 0)
        {
            m_existingProjectsDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>(projects.Length);
            foreach (var project in projects)
            {
                options.Add(new Dropdown.OptionData(project));
            }
            m_existingProjectsDropdown.AddOptions(options);
            m_editExistingProjectButton.onClick.AddListener(OnClickEditExistingProject);
            m_existingProjectsDropdown.gameObject.SetActive(true);
            m_editExistingProjectButton.gameObject.SetActive(true);
        }
        else
        {
            m_existingProjectsDropdown.gameObject.SetActive(false);
            m_editExistingProjectButton.gameObject.SetActive(false);
        }

        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (!obj.scene.IsValid())
            {
                continue;
            }
            if (!obj.CompareTag("EditorRootUI"))
            {
                continue;
            }
            if (m_canvasNames.TryGetValue(obj.name, out var mode))
            {
                m_modeCanvases[mode] = obj;
            }
            else
            {
                AppDebug.LogError("ERR: Unknown UI Canvas found: {0}", obj.name);
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

    public void OnClickMainMode()
    {
        SwitchMode(EditorMode.Main);
    }

    void OnClickCharacterSettingMode()
    {
        SwitchMode(EditorMode.CharacterSetting);
    }
    void OnClickNewProject()
    {
        if(string.IsNullOrEmpty(m_projectNameInputField.text))
        {
            return;
        }
        m_modeCanvases[EditorMode.ProjectSetting].GetComponent<EditProjectController>().m_projectName = m_projectNameInputField.text;
        SwitchMode(EditorMode.ProjectSetting);
    }

    void OnClickEditExistingProject()
    {
        int selectedIndex = m_existingProjectsDropdown.value;
        m_modeCanvases[EditorMode.ProjectSetting].GetComponent<EditProjectController>().m_projectName
            = m_existingProjectsDropdown.options[selectedIndex].text;
        SwitchMode(EditorMode.ProjectSetting);
    }

    void OnClickBackToTitle()
    {
        SceneManager.LoadScene("Title");
    }
}
