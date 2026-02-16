using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorMainPageController : MonoBehaviour, IEditController
{
    public EditorController EditorController {private get; set;}
    [SerializeField]
    Button m_editCharacterButton;
    [SerializeField]
    InputField m_projectNameInputField;
    [SerializeField]
    Button m_newProjectButton;
    [SerializeField]
    Dropdown m_existingProjectsDropdown;
    [SerializeField]
    Button m_editExistingProjectButton;
    [SerializeField]
    Button m_backToTitleButton;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_editCharacterButton.onClick.AddListener(OnClickCharacterSettingMode);
        m_newProjectButton.onClick.AddListener(OnClickNewProject);
        m_backToTitleButton.onClick.AddListener(OnClickBackToTitle);
        string[] projects = ProjectManager.GetAllProjects();
        if (projects.Length > 0)
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
    }

    void OnClickCharacterSettingMode()
    {
        EditorController.GotoEditCharacter();
    }
    void OnClickNewProject()
    {
        if (string.IsNullOrEmpty(m_projectNameInputField.text))
        {
            return;
        }
        EditorController.GotoEditProject(m_projectNameInputField.text);
    }

    void OnClickEditExistingProject()
    {
        int selectedIndex = m_existingProjectsDropdown.value;
        string projectName = m_existingProjectsDropdown.options[selectedIndex].text;
        EditorController.GotoEditProject(projectName);
    }

    void OnClickBackToTitle()
    {
        EditorController.BackToTitle();
    }
}
